// ============================================================================
// LOGIN PAGE SETUP INSTRUCTIONS
// ============================================================================
// This folder contains everything needed to add an Azure AD login/landing page
// to an existing ASP.NET Core MVC web app.
//
// ============================================================================
// WHAT'S IN THIS FOLDER
// ============================================================================
//
//   HomeController_Login.cs           - Controller: login landing + logout logic
//   Views/Home/Index.cshtml           - The login/landing page (what users see)
//   Views/Shared/_LayoutLanding.cshtml - Minimal layout for the login page (no navbar)
//   Views/Shared/_LoginPartial.cshtml  - Sign in/out dropdown for the main navbar
//
// ============================================================================
// STEP-BY-STEP SETUP
// ============================================================================
//
// STEP 1: INSTALL REQUIRED NUGET PACKAGES
// ----------------------------------------
//   dotnet add package Microsoft.Identity.Web
//   dotnet add package Microsoft.Identity.Web.UI
//   dotnet add package Microsoft.Graph          (only if using group-based auth)
//   dotnet add package Azure.Identity            (only if using group-based auth)
//
//
// STEP 2: ADD AZURE AD CONFIG TO appsettings.json
// ------------------------------------------------
//   "AzureAd": {
//     "Instance": "https://login.microsoftonline.com/",
//     "TenantId": "<YOUR-TENANT-ID>",
//     "ClientId": "<YOUR-CLIENT-ID>",
//     "ClientSecret": "<YOUR-CLIENT-SECRET>",
//     "CallbackPath": "/signin-oidc",
//     "SignedOutCallbackPath": "/signout-callback-oidc"
//   }
//
//   To find these values:
//     1. Go to Azure Portal > Azure Active Directory > App Registrations
//     2. Select your app (or register a new one)
//     3. TenantId = Directory (tenant) ID
//     4. ClientId = Application (client) ID
//     5. ClientSecret = Certificates & secrets > New client secret
//
//
// STEP 3: COPY FILES INTO YOUR PROJECT
// -------------------------------------
//   1. Copy HomeController_Login.cs -> Controllers/HomeController.cs
//   2. Copy Views/Home/Index.cshtml -> Views/Home/Index.cshtml
//   3. Copy Views/Shared/_LayoutLanding.cshtml -> Views/Shared/_LayoutLanding.cshtml
//   4. Copy Views/Shared/_LoginPartial.cshtml -> Views/Shared/_LoginPartial.cshtml
//
//
// STEP 4: ADD AUTHENTICATION TO Program.cs
// -----------------------------------------
//   Add these using statements at the top:
//
//     using Microsoft.AspNetCore.Authentication.OpenIdConnect;
//     using Microsoft.Identity.Web;
//     using Microsoft.Identity.Web.UI;
//
//   Add this BEFORE builder.Services.AddControllersWithViews():
//
//     builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
//         .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
//
//   Update your MVC registration:
//
//     builder.Services.AddControllersWithViews()
//         .AddMicrosoftIdentityUI();   // <-- add this
//     builder.Services.AddRazorPages(); // <-- add this (required for Identity UI)
//
//   Add these to the middleware pipeline (ORDER MATTERS):
//
//     app.UseAuthentication();   // <-- add BEFORE UseAuthorization
//     app.UseAuthorization();
//
//   Add this AFTER app.MapControllerRoute():
//
//     app.MapRazorPages();  // <-- required for Microsoft Identity sign-in/sign-out pages
//
//
// STEP 5: SET THE LOGIN PAGE AS THE DEFAULT LAUNCH PAGE
// ------------------------------------------------------
//   Your default route in Program.cs should already point to Home/Index:
//
//     app.MapControllerRoute(
//         name: "default",
//         pattern: "{controller=Home}/{action=Index}/{id?}");
//                           ^^^^^         ^^^^^
//                           This means the app launches to HomeController.Index
//                           which is the login page.
//
//   If your app currently launches to a DIFFERENT controller (e.g., Dashboard),
//   you have two options:
//
//   OPTION A: Change the default route (simplest):
//     Change: pattern: "{controller=Dashboard}/{action=Index}/{id?}"
//     To:     pattern: "{controller=Home}/{action=Index}/{id?}"
//
//   OPTION B: Keep your default route but add a redirect:
//     In your current default controller, add this at the top of the Index action:
//       if (User.Identity?.IsAuthenticated != true)
//           return RedirectToAction("Index", "Home");
//
//
// STEP 6: *** UPDATE THE REDIRECT AFTER LOGIN ***
// -------------------------------------------------
//   *** THIS IS THE MOST IMPORTANT STEP ***
//
//   In Views/Home/Index.cshtml, find this line:
//
//     <a href="/MicrosoftIdentity/Account/SignIn?redirectUri=/SOW/Index" ...>
//                                                ^^^^^^^^^^^^^^^^^^
//                                                CHANGE THIS VALUE!
//
//   Change "/SOW/Index" to YOUR app's main page. Examples:
//
//     /Dashboard/Index     - if your main page is a DashboardController
//     /Products            - if your main page is ProductsController.Index
//     /Admin/Home          - if your main page is AdminController.Home
//     /                    - to go to the default route after login
//
//   Also in HomeController_Login.cs, find this line in the Index() method:
//
//     return RedirectToAction("Index", "SOW");
//                              ^^^^^   ^^^
//                              CHANGE THESE to match your main page
//
//   Change to match your app. Examples:
//
//     return RedirectToAction("Index", "Dashboard");
//     return RedirectToAction("Index", "Products");
//     return RedirectToAction("Home", "Admin");
//
//
// STEP 7: ADD _LoginPartial TO YOUR MAIN LAYOUT
// -----------------------------------------------
//   In your _Layout.cshtml (main layout), add this inside the navbar
//   to show the sign-in/sign-out dropdown:
//
//     <partial name="_LoginPartial" />
//
//   Place it at the end of the navbar, typically after the nav links:
//
//     <div class="navbar-collapse collapse">
//         <ul class="navbar-nav">
//             ... your nav links ...
//         </ul>
//         <partial name="_LoginPartial" />   <!-- add here -->
//     </div>
//
//
// STEP 8: PROTECT YOUR CONTROLLERS
// ----------------------------------
//   Add [Authorize] to any controller that requires login:
//
//     [Authorize]
//     public class DashboardController : Controller { ... }
//
//   Keep [AllowAnonymous] on HomeController so the login page is accessible:
//
//     [AllowAnonymous]
//     public class HomeController : Controller { ... }
//
//
// ============================================================================
// OPTIONAL: CUSTOMIZE THE LOGIN PAGE APPEARANCE
// ============================================================================
//
//   The login page (Views/Home/Index.cshtml) uses inline CSS for easy
//   portability. To customize for your brand:
//
//   - Change .caso-logo img src to your company logo
//   - Change .caso-title text to your app name
//   - Change .caso-subtitle text to your app description
//   - Change .btn-caso colors to match your brand
//   - Change the sign-in button text
//
//
// ============================================================================
// OPTIONAL: ADD GROUP-BASED AUTHORIZATION
// ============================================================================
//
//   If you need role/group-based access (Admins, UsersEdit, UsersView, etc.),
//   see the full Program_AuthSetup_Template.cs in the parent AuthTemplates
//   folder. That file contains:
//     - OnTokenValidated event to fetch groups from Microsoft Graph
//     - Authorization policy definitions (AdminOnly, CanEdit, CanView)
//     - Cookie configuration for large token handling
//
//
// ============================================================================
// TROUBLESHOOTING
// ============================================================================
//
//   ERROR: "AADSTS50011: The reply URL specified in the request does not match"
//     -> In Azure Portal > App Registration > Authentication, add your
//        redirect URI: https://localhost:PORT/signin-oidc
//        (and https://yourapp.azurewebsites.net/signin-oidc for production)
//
//   ERROR: "AADSTS90015: Post logout redirect URI is not valid"
//     -> In Azure Portal > App Registration > Authentication, add your
//        post-logout URI: https://localhost:PORT/signout-callback-oidc
//
//   ERROR: Cookie too large / authentication loop
//     -> If users belong to 80+ Azure AD groups, the token cookie exceeds
//        browser limits. See the OnTokenValidated code in
//        Program_AuthSetup_Template.cs which strips and re-fetches groups.
//
//   ERROR: "No sign-in audience is configured for the application"
//     -> In Azure Portal > App Registration > Authentication, under
//        "Supported account types", select your preferred option.
//
// ============================================================================
