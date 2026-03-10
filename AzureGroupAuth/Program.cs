using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using AzureGroupAuth.Services;
using AzureGroupAuth.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Azure App Service logging
builder.Logging.AddAzureWebAppDiagnostics();

// Authentication setup
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

                        var clientId = configuration["AzureAd:ClientId"];
                        var clientSecret = configuration["AzureAd:ClientSecret"];
                        var tenantId = configuration["AzureAd:TenantId"];
                        var clientSecretCredential = new Azure.Identity.ClientSecretCredential(
                            tenantId, clientId, clientSecret);
                        var graphClient = new Microsoft.Graph.GraphServiceClient(clientSecretCredential);

                        var allMemberOf = new List<Microsoft.Graph.Models.DirectoryObject>();
                        var memberOf = await graphClient.Users[userObjectId].MemberOf.GetAsync();

                        if (memberOf?.Value != null)
                        {
                            allMemberOf.AddRange(memberOf.Value);

                            while (memberOf?.OdataNextLink != null)
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

                        if (allMemberOf.Count > 0)
                        {
                            var allIds = allMemberOf.Select(g => g.Id).ToList();

                            if (allIds.Any(id => string.Equals(id, adminGroupId, StringComparison.OrdinalIgnoreCase)))
                            {
                                identity.AddClaim(new System.Security.Claims.Claim("groups", adminGroupId!));
                            }
                            if (allIds.Any(id => string.Equals(id, usersEditGroupId, StringComparison.OrdinalIgnoreCase)))
                            {
                                identity.AddClaim(new System.Security.Claims.Claim("groups", usersEditGroupId!));
                            }
                            if (allIds.Any(id => string.Equals(id, usersViewGroupId, StringComparison.OrdinalIgnoreCase)))
                            {
                                identity.AddClaim(new System.Security.Claims.Claim("groups", usersViewGroupId!));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error fetching user groups from Graph API");
                    }
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

// Cookie configuration
builder.Services.Configure<CookieAuthenticationOptions>("Cookies", options =>
{
    options.Cookie.Path = "/";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// MVC + Identity UI
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();
builder.Services.AddRazorPages();

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    var adminGroupId = builder.Configuration["AzureAd:Groups:Admins"];
    var usersEditGroupId = builder.Configuration["AzureAd:Groups:UsersEdit"];
    var usersViewGroupId = builder.Configuration["AzureAd:Groups:UsersView"];

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("groups", adminGroupId!));

    options.AddPolicy("CanEdit", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("groups", adminGroupId!) ||
            context.User.HasClaim("groups", usersEditGroupId!)));

    options.AddPolicy("CanView", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("groups", adminGroupId!) ||
            context.User.HasClaim("groups", usersEditGroupId!) ||
            context.User.HasClaim("groups", usersViewGroupId!)));
});

// Service registration
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
