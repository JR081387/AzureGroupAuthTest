// ============================================================================
// AZURE AD GROUP-BASED AUTHENTICATION TEMPLATE
// ============================================================================
// This file contains the auth-related code snippets to add to your Program.cs.
// It is NOT a standalone file — copy the relevant sections into your Program.cs.
//
// Required NuGet packages:
//   - Microsoft.Identity.Web (v4.3.0+)
//   - Microsoft.Identity.Web.UI
//   - Microsoft.Graph (v5.80.0+)
//   - Azure.Identity
//
// Required appsettings.json structure:
//   "AzureAd": {
//     "Instance": "https://login.microsoftonline.com/",
//     "TenantId": "<your-tenant-id>",
//     "ClientId": "<your-client-id>",
//     "ClientSecret": "<your-client-secret>",
//     "CallbackPath": "/signin-oidc",
//     "SignedOutCallbackPath": "/signout-callback-oidc",
//     "Groups": {
//       "Admins": "<group-guid>",
//       "UsersEdit": "<group-guid>",
//       "UsersView": "<group-guid>"
//     }
//   }
// ============================================================================

// --- USING STATEMENTS (add to top of Program.cs) ---
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

// --- AUTHENTICATION SETUP (add after builder creation) ---
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
                // Remove active session on sign-out
                var sessionTracker = context.HttpContext.RequestServices.GetRequiredService<SessionTrackingService>();
                var oid = context.HttpContext.User.FindFirst("oid")?.Value
                    ?? context.HttpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
                if (!string.IsNullOrEmpty(oid))
                {
                    sessionTracker.RemoveSession(oid);
                }

                // Set a clean post-logout redirect URI to avoid AADSTS90015
                var postLogoutUri = $"{context.Request.Scheme}://{context.Request.Host}/";
                context.ProtocolMessage.PostLogoutRedirectUri = postLogoutUri;

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                // FIX: Remove ALL group claims from token (cookie too large with 80+ groups)
                // Then fetch only the groups we care about using Microsoft Graph
                var identity = (System.Security.Claims.ClaimsIdentity)context.Principal!.Identity!;
                var groupClaims = identity.FindAll("groups").ToList();

                foreach (var claim in groupClaims)
                {
                    identity.RemoveClaim(claim);
                }

                // Get user's object ID (oid claim)
                var userObjectId = context.Principal?.FindFirst("oid")?.Value
                    ?? context.Principal?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

                if (!string.IsNullOrEmpty(userObjectId))
                {
                    try
                    {
                        // Fetch user's group memberships from Microsoft Graph
                        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                        var adminGroupId = configuration["AzureAd:Groups:Admins"];
                        var usersEditGroupId = configuration["AzureAd:Groups:UsersEdit"];
                        var usersViewGroupId = configuration["AzureAd:Groups:UsersView"];

                        // Create Graph client
                        var clientId = configuration["AzureAd:ClientId"];
                        var clientSecret = configuration["AzureAd:ClientSecret"];
                        var tenantId = configuration["AzureAd:TenantId"];
                        var clientSecretCredential = new Azure.Identity.ClientSecretCredential(
                            tenantId, clientId, clientSecret);
                        var graphClient = new Microsoft.Graph.GraphServiceClient(clientSecretCredential);

                        // Get user's group memberships (with pagination)
                        var allMemberOf = new List<Microsoft.Graph.Models.DirectoryObject>();
                        var memberOf = await graphClient.Users[userObjectId].MemberOf.GetAsync();

                        if (memberOf?.Value != null)
                        {
                            allMemberOf.AddRange(memberOf.Value);

                            // Handle pagination - get ALL pages
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

                        if (allMemberOf.Count > 0)
                        {
                            var allIds = allMemberOf.Select(g => g.Id).ToList();

                            // Add claims ONLY for the groups we care about (case-insensitive)
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

// --- AUTHORIZATION POLICIES (add after authentication setup) ---
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

// --- MIDDLEWARE PIPELINE (add after app.UseAuthorization()) ---
// app.UseAuthentication();
// app.UseAuthorization();
// app.UseSessionActivityTracking();  // <-- Add this line

// --- RAZOR PAGES (required for Identity UI sign-in/sign-out) ---
// app.MapRazorPages();
