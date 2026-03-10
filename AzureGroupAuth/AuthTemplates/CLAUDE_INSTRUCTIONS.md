# AuthTemplates - Azure AD Group Authentication Setup

## FOR CLAUDE CODE: READ THIS FIRST

You are setting up Azure AD (Entra ID) group-based authentication on this web application using the template files in this `AuthTemplates/` folder. Follow the steps below precisely. Do NOT skip any step.

---

## PHASE 1: GATHER REQUIRED VALUES FROM THE USER

Before writing any code, you MUST ask the user for the following values. Present this as a checklist in the terminal and wait for their responses. Do not guess or assume any values.

### Required Values - Ask the user for ALL of these:

```
I need a few values to configure Azure AD authentication for your app.
Please provide the following:

1. APP NAME
   What is the name of your application? (shown on login page and browser tab)
   Example: "Employee Portal", "Inventory Manager", "HR Dashboard"

2. APP NAMESPACE
   What is the root namespace of your project? (check your .csproj or existing controllers)
   Example: "EmployeePortal", "InventoryApp", "HRDashboard"

3. AZURE AD - TENANT ID
   Found in: Azure Portal > Azure Active Directory > Overview > Tenant ID
   Format: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

4. AZURE AD - CLIENT ID (Application ID)
   Found in: Azure Portal > App Registrations > Your App > Application (client) ID
   Format: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

5. AZURE AD - CLIENT SECRET VALUE
   Found in: Azure Portal > App Registrations > Your App > Certificates & secrets
   IMPORTANT: You need the Secret VALUE (not the Secret ID)
   Note: If they haven't created one yet, tell them:
     "Go to App Registrations > Your App > Certificates & secrets > New client secret"

6. AZURE AD GROUP IDs - How many groups do you need? (minimum 1)
   For each group, I need:
     - A friendly name (e.g., "Admins", "UsersEdit", "UsersView")
     - The Azure AD Group Object ID
   Found in: Azure Portal > Azure Active Directory > Groups > click group > Object Id
   Format: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

7. MAIN PAGE ROUTE
   After a user logs in, which page should they land on?
   Example: "/Dashboard/Index", "/Products", "/Home/Dashboard"
   This is the controller/action of your app's main authenticated page.

8. DATABASE CONNECTION (optional)
   Are you using a SQL database? If yes, what is the connection string?
   This will be stored as "DefaultConnection" in appsettings.json.
   If they don't have one yet, skip this and note it needs to be added later.
```

**IMPORTANT:** Wait for the user to provide ALL values before proceeding. If they don't have a value yet (like group IDs), note it as a placeholder `"<REPLACE-WITH-GROUP-ID>"` and remind them to update it.

---

## PHASE 2: INSTALL NUGET PACKAGES

Run these commands in the project root:

```bash
dotnet add package Microsoft.Identity.Web --version 4.*
dotnet add package Microsoft.Identity.Web.UI --version 4.*
dotnet add package Microsoft.Graph --version 5.*
dotnet add package Azure.Identity
```

Verify they installed by checking the .csproj file for the PackageReference entries.

---

## PHASE 3: CONFIGURE appsettings.json

Add the `AzureAd` section to `appsettings.json` using the values gathered in Phase 1. Use environment variable references for sensitive values so secrets are NOT hardcoded:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<USER-PROVIDED-TENANT-ID>",
    "ClientId": "<USER-PROVIDED-CLIENT-ID>",
    "ClientSecret": "<USER-PROVIDED-CLIENT-SECRET>",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "Groups": {
      "<GroupName1>": "<GROUP-ID-1>",
      "<GroupName2>": "<GROUP-ID-2>"
    }
  }
}
```

Also remind the user that in production (Azure Web App), these should be set as **Environment Variables / App Settings** on the Azure Web App:

| Azure App Setting Name | Value |
|------------------------|-------|
| `AzureAD__TenantId` | Their Tenant ID |
| `AzureAD__ClientId` | Their Client ID |
| `AzureAD__ClientSecret` | Their Client Secret VALUE |
| `AzureAD__Groups__<GroupName>` | Each group's Object ID |
| `ConnectionStrings__DefaultConnection` | SQL connection string (if applicable) |

Note: Azure App Settings use `__` (double underscore) as the hierarchy separator instead of `:`.

---

## PHASE 4: IMPLEMENT THE CODE

Use the template files in this folder as the source. Copy and adapt each file into the project, making the following replacements in EVERY file:

### String Replacements:
| Find | Replace With |
|------|-------------|
| `SOWTracker` (namespace) | User's app namespace from Phase 1 |
| `SOW Tracker` (display text) | User's app name from Phase 1 |
| `"Index", "SOW"` (redirect) | User's main page route from Phase 1 |
| `redirectUri=/SOW/Index` | User's main page route from Phase 1 |
| CASO logo URL | Ask user if they have a logo URL, otherwise remove the img tag |
| `CASO` references in text | User's company/app name |

### Files to copy and adapt (in this order):

#### 4a. Models
- `AuthTemplates/Models/UserSession_Template.cs` → `Models/UserSession.cs`
- `AuthTemplates/Models/ErrorViewModel_Template.cs` → `Models/ErrorViewModel.cs` (skip if already exists)

#### 4b. Services
- `AuthTemplates/Services/GraphService_Template.cs` → `Services/GraphService.cs`
- `AuthTemplates/Services/SessionTrackingService_Template.cs` → `Services/SessionTrackingService.cs`

#### 4c. Middleware
- `AuthTemplates/Middleware/SessionActivityMiddleware_Template.cs` → `Middleware/SessionActivityMiddleware.cs`

#### 4d. Controllers
- `AuthTemplates/LoginPage/HomeController_Login.cs` → `Controllers/HomeController.cs`
  - Update the namespace
  - Update the redirect in `Index()` to user's main page
  - Keep `[AllowAnonymous]`
- If user wants a User Management page:
  - `AuthTemplates/Controllers/UserManagementController_Template.cs` → `Controllers/UserManagementController.cs`
- If user wants an Active Sessions page:
  - `AuthTemplates/Controllers/AdminController_Template.cs` → `Controllers/AdminController.cs`

#### 4e. Views
- `AuthTemplates/LoginPage/Views/Home/Index.cshtml` → `Views/Home/Index.cshtml`
  - Update app name, subtitle, logo
  - **Update the redirectUri in the sign-in link**
- `AuthTemplates/LoginPage/Views/Shared/_LayoutLanding.cshtml` → `Views/Shared/_LayoutLanding.cshtml`
  - Update app name in `<title>`
- `AuthTemplates/LoginPage/Views/Shared/_LoginPartial.cshtml` → `Views/Shared/_LoginPartial.cshtml`
- `AuthTemplates/Views/_ViewImports_Template.cshtml` → `Views/_ViewImports.cshtml` (skip if already exists, but ensure the @using and @addTagHelper lines are present)
- `AuthTemplates/Views/_ViewStart_Template.cshtml` → `Views/_ViewStart.cshtml` (skip if already exists)
- If user wants User Management page:
  - `AuthTemplates/Views/UserManagement/Index_Template.cshtml` → `Views/UserManagement/Index.cshtml`
- If user wants Active Sessions page:
  - `AuthTemplates/Views/Admin/ActiveSessions_Template.cshtml` → `Views/Admin/ActiveSessions.cshtml`

#### 4f. Update the existing _Layout.cshtml
Do NOT replace the user's existing `_Layout.cshtml`. Instead, ADD these to it:
- Add `<partial name="_LoginPartial" />` inside the navbar
- Add navigation links for User Management and Active Sessions if those pages were added
- Reference `AuthTemplates/Views/Shared/_Layout_Template.cshtml` as an example of how the navbar auth checks work (the `@inject IAuthorizationService` and policy checks)

---

## PHASE 5: CONFIGURE Program.cs

This is the most critical step. Modify the existing `Program.cs` — do NOT replace it. Add the following blocks using `AuthTemplates/Program_AuthSetup_Template.cs` as reference:

### 5a. Add using statements at the top:
```csharp
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
```

### 5b. Add authentication setup BEFORE `builder.Services.AddControllersWithViews()`:
Copy the `AddAuthentication` + `AddMicrosoftIdentityWebApp` block from `Program_AuthSetup_Template.cs`, including the full `OnTokenValidated` event handler. Adapt the group names/IDs to match what the user provided.

### 5c. Add cookie configuration after authentication setup:
Copy the `Configure<CookieAuthenticationOptions>` block.

### 5d. Update MVC registration:
```csharp
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();   // <-- ADD THIS
builder.Services.AddRazorPages(); // <-- ADD THIS if not already present
```

### 5e. Add authorization policies AFTER authentication setup:
Copy the `AddAuthorization` block from `Program_AuthSetup_Template.cs`. Adapt the policy names and group mappings to match the user's groups from Phase 1. Build policies logically:
- If user has 1 group: just use `[Authorize]` everywhere, no custom policies needed
- If user has 2+ groups: create tiered policies (e.g., AdminOnly, CanEdit, CanView)

### 5f. Register services:
```csharp
builder.Services.AddSingleton<SessionTrackingService>();
builder.Services.AddScoped<GraphService>();
```

### 5g. Add middleware (ORDER MATTERS):
Ensure this order exists in the middleware pipeline:
```csharp
app.UseAuthentication();          // must be BEFORE UseAuthorization
app.UseAuthorization();
app.UseSessionActivityTracking(); // add AFTER UseAuthorization
```

### 5h. Add Razor Pages mapping:
```csharp
app.MapRazorPages(); // required for Microsoft Identity UI sign-in/sign-out
```

### 5i. Verify the default route points to Home/Index:
```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

---

## PHASE 6: AZURE APP REGISTRATION CONFIGURATION

After implementation, remind the user to configure their Azure App Registration:

```
IMPORTANT: Configure these in Azure Portal > App Registrations > Your App > Authentication:

1. Add Redirect URI:
   - https://localhost:<PORT>/signin-oidc          (for local dev)
   - https://<your-app>.azurewebsites.net/signin-oidc  (for production)

2. Add Front-channel logout URL:
   - https://localhost:<PORT>/signout-callback-oidc
   - https://<your-app>.azurewebsites.net/signout-callback-oidc

3. Under "Implicit grant and hybrid flows":
   - Check "ID tokens"

4. Under API Permissions (if using Graph for group lookup):
   - Microsoft Graph > Directory.Read.All (Application permission)
   - Microsoft Graph > User.Read (Delegated permission)
   - Click "Grant admin consent"
```

---

## PHASE 7: VERIFY AND TEST

After all code is in place:

1. Build the project: `dotnet build`
2. Fix any compilation errors (usually namespace mismatches)
3. Tell the user to run the app and verify:
   - The login page appears at the root URL
   - Clicking "Sign In with Microsoft" redirects to Azure AD
   - After login, user is redirected to their main page
   - The navbar shows the user's name with a sign-out dropdown
   - Sign out returns to the login page

---

## PHASE 8: CLEANUP

After everything is working, ask the user if they want to:
1. Delete the `AuthTemplates/` folder (no longer needed)
2. Keep it as reference

---

## NOTES FOR CLAUDE

- NEVER hardcode secrets directly in source files. Use appsettings.json for local dev and Azure App Settings (environment variables) for production.
- If the project already has a HomeController, merge the login logic into it rather than replacing it.
- If the project already has authentication configured, adapt rather than duplicate.
- If the user's project uses a different CSS framework (not Bootstrap), adapt the login page HTML accordingly.
- The `_Template` suffix files are reference copies. The `LoginPage/` subfolder contains the clean versions ready to copy.
- Always check if files already exist before overwriting. Ask the user if conflicts arise.
