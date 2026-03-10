using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using AzureGroupAuth.Services;
using AzureGroupAuth.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddAzureWebAppDiagnostics();

// --- AUTHENTICATION SETUP ---
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        options.SaveTokens = true;
        options.SignedOutRedirectUri = "/";

        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProviderForSignOut = context =>
            {
                var sessionTracker = context.HttpContext.RequestServices.GetRequiredService<SessionTrackingService>();
                var oid = context.HttpContext.User.FindFirst("oid")?.Value
                    ?? context.HttpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
                if (!string.IsNullOrEmpty(oid))
                {
                    sessionTracker.RemoveSession(oid);
                }

                var postLogoutUri = $"{context.Request.Scheme}://{context.Request.Host}/";
                context.ProtocolMessage.PostLogoutRedirectUri = postLogoutUri;

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                // Remove ALL group claims from token (cookie too large with many groups)
                // Then fetch only the groups we care about using Microsoft Graph
                var identity = (System.Security.Claims.ClaimsIdentity)context.Principal!.Identity!;
                var groupClaims = identity.FindAll("groups").ToList();

                foreach (var claim in groupClaims)
                {
                    identity.RemoveClaim(claim);
                }

                var userObjectId = context.Principal?.FindFirst("oid")?.Value
                    ?? context.Principal?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

                if (!string.IsNullOrEmpty(userObjectId))
                {
                    try
                    {
                        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                        var adminGroupId = configuration["AzureAd:Groups:Admins"];
                        var usersEditGroupId = configuration["AzureAd:Groups:UsersEdit"];
                        var usersViewGroupId = configuration["AzureAd:Groups:UsersView"];

                        logger.LogWarning("=== AUTH DEBUG: User OID: {Oid}", userObjectId);
                        logger.LogWarning("=== AUTH DEBUG: Config - Admins GroupId: '{AdminId}'", adminGroupId ?? "NULL");
                        logger.LogWarning("=== AUTH DEBUG: Config - UsersEdit GroupId: '{EditId}'", usersEditGroupId ?? "NULL");
                        logger.LogWarning("=== AUTH DEBUG: Config - UsersView GroupId: '{ViewId}'", usersViewGroupId ?? "NULL");

                        var clientId = configuration["AzureAd:ClientId"];
                        var clientSecret = configuration["AzureAd:ClientSecret"];
                        var tenantId = configuration["AzureAd:TenantId"];

                        // Dump all AzureAd keys to find the env var mismatch
                        var azureAdSection = configuration.GetSection("AzureAd");
                        foreach (var child in azureAdSection.GetChildren())
                        {
                            logger.LogWarning("=== AUTH DEBUG: Config key 'AzureAd:{Key}' = '{Value}'", child.Key, child.Value ?? "NULL");
                        }
                        // Also check env var directly
                        var envSecret = Environment.GetEnvironmentVariable("AzureAD__ClientSecret");
                        logger.LogWarning("=== AUTH DEBUG: ENV AzureAD__ClientSecret = '{EnvSecret}'", envSecret ?? "NOT SET");

                        logger.LogWarning("=== AUTH DEBUG: TenantId: '{TenantId}', ClientId: '{ClientId}', SecretValue: '{SecretValue}'",
                            tenantId ?? "NULL", clientId ?? "NULL", clientSecret ?? "NULL");

                        var clientSecretCredential = new Azure.Identity.ClientSecretCredential(
                            tenantId, clientId, clientSecret);
                        var graphClient = new Microsoft.Graph.GraphServiceClient(clientSecretCredential);

                        var allMemberOf = new List<Microsoft.Graph.Models.DirectoryObject>();
                        var memberOf = await graphClient.Users[userObjectId].MemberOf.GetAsync();

                        if (memberOf?.Value != null)
                        {
                            allMemberOf.AddRange(memberOf.Value);

                            while (memberOf.OdataNextLink != null)
                            {
                                memberOf = await graphClient.Users[userObjectId].MemberOf
                                    .WithUrl(memberOf.OdataNextLink)
                                    .GetAsync();
                                if (memberOf?.Value != null)
                                {
                                    allMemberOf.AddRange(memberOf.Value);
                                }
                            }
                        }

                        logger.LogWarning("=== AUTH DEBUG: Graph returned {Count} group memberships", allMemberOf.Count);
                        foreach (var g in allMemberOf)
                        {
                            logger.LogWarning("=== AUTH DEBUG: MemberOf group ID: {GroupId}", g.Id);
                        }

                        if (allMemberOf.Count > 0)
                        {
                            var allIds = allMemberOf.Select(g => g.Id).ToList();

                            if (allIds.Any(id => string.Equals(id, adminGroupId, StringComparison.OrdinalIgnoreCase)))
                            {
                                identity.AddClaim(new System.Security.Claims.Claim("groups", adminGroupId!));
                                logger.LogWarning("=== AUTH DEBUG: MATCHED Admins group");
                            }
                            if (allIds.Any(id => string.Equals(id, usersEditGroupId, StringComparison.OrdinalIgnoreCase)))
                            {
                                identity.AddClaim(new System.Security.Claims.Claim("groups", usersEditGroupId!));
                                logger.LogWarning("=== AUTH DEBUG: MATCHED UsersEdit group");
                            }
                            if (allIds.Any(id => string.Equals(id, usersViewGroupId, StringComparison.OrdinalIgnoreCase)))
                            {
                                identity.AddClaim(new System.Security.Claims.Claim("groups", usersViewGroupId!));
                                logger.LogWarning("=== AUTH DEBUG: MATCHED UsersView group");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "=== AUTH DEBUG: EXCEPTION during Graph API call");
                    }
                }
                else
                {
                    logger.LogWarning("=== AUTH DEBUG: userObjectId was NULL or empty - cannot look up groups");
                }

                // Register active session
                var sessionTracker = context.HttpContext.RequestServices.GetRequiredService<SessionTrackingService>();
                var displayName = context.Principal?.Identity?.Name ?? "Unknown";
                var email = context.Principal?.FindFirst("preferred_username")?.Value ?? "";
                var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var ua = context.HttpContext.Request.Headers.UserAgent.ToString();
                if (!string.IsNullOrEmpty(userObjectId))
                {
                    sessionTracker.RegisterSession(userObjectId, displayName, email, ip, ua);
                }
            }
        };
    });

// --- COOKIE CONFIGURATION ---
builder.Services.Configure<CookieAuthenticationOptions>("Cookies", options =>
{
    options.Cookie.Path = "/";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// --- MVC + IDENTITY UI ---
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();
builder.Services.AddRazorPages();

// --- AUTHORIZATION POLICIES ---
builder.Services.AddAuthorization(options =>
{
    var adminGroupId = builder.Configuration["AzureAd:Groups:Admins"];
    var usersEditGroupId = builder.Configuration["AzureAd:Groups:UsersEdit"];
    var usersViewGroupId = builder.Configuration["AzureAd:Groups:UsersView"];

    // Admin policy - members of Admin group
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("groups", adminGroupId!));

    // Editor policy - members of Admin or UsersEdit groups
    options.AddPolicy("CanEdit", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("groups", adminGroupId!) ||
            context.User.HasClaim("groups", usersEditGroupId!)));

    // Viewer policy - members of Admin, UsersEdit, or UsersView groups
    options.AddPolicy("CanView", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("groups", adminGroupId!) ||
            context.User.HasClaim("groups", usersEditGroupId!) ||
            context.User.HasClaim("groups", usersViewGroupId!)));
});

// --- SERVICE REGISTRATION ---
builder.Services.AddSingleton<SessionTrackingService>();
builder.Services.AddScoped<GraphService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSessionActivityTracking();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

app.Run();
