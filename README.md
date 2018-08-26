# Implicit Flow Coding Dojo

This workshop leads you to create an OAuth2 and OpenID Connect Identity Server, and an Angular SPA that uses Implicit flow to let users log in using that ID Server.

⚠️ Warning: you're looking at a *solution* branch with heavy *spoilers*.

## Introduction

This Dojo shows you how the OAuth2 "Implicit Flow" works.
It uses .NET Core, IdentityServer4, Angular 6, and Angular-OAuth2-OIDC; but only for demonstration purposes.
We do assume *some* web development experience with .NET and JavaScript frameworks.

Prerequisites:

- .NET Core 2 (tested with 2.1.400)
- Node and NPM (tested with v10.8.0 and 6.3.0 respectively)
- Angular CLI (tested with 6.1.3)

Optionally grab the `.gitignore` from this repository and use it during the dojo steps, below.

> ⚠️ If you get stuck, have a look at the `solution-v1.0.0` branch!

Let's get started!

## Part 1: Identity Server 4

Let's create our Authorization Server first.

### Initialize ID Server

First:

```powershell
# Create main repository folder
mkdir dojo-implicit-flow
cd dojo-implicit-flow

# Create ID Server folder and solution
mkdir MyIdServer
cd MyIdServer
dotnet net webapi
```

The effect of this setup is that we also get a `ValuesController` in a scaffolded Web API.
Although normally you might want to seperate things, for now let's stick with that.

From here, we follow a simplified version of [the IdentityServer4 quickstart docs](https://identityserver4.readthedocs.io/en/release/quickstarts/0_overview.html).

Start with:

```powershell
dotnet add package IdentityServer4
```

Add to `Startup.cs` in `Configure`:

```csharp
app.UseIdentityServer();

// For easier testing:
app.UseCors(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
```

And in `ConfigureServices`:

```csharp
services.AddIdentityServer()
    // More stuff here!
    .AddDeveloperSigningCredential();
```

Then comes "more stuff", being:

- `.AddInMemoryApiResources(my_hardcoded_resources_here)` that specifies a list with 1 fake API (the "Resource Server")
- `.AddInMemoryClients(my_hardcoded_clients_here)` that hard codes a list of clients (more on that below)

Go ahead, start with a `new ApiResource(...)` for that first one.
After that the interesting bits start.

First, let's start with one hard coded client that's really straightforward.
A Client that supports getting a token with just an API Key ("ClientSecret").
Add to your hard coded list of clients one with:

- `ClientId` set to e.g. `foo-client-001`
- `AllowedGrantTypes` set to `GrantTypes.ClientCredentials`
- `ClientSecrets` set to a list of just one secret, created by `new Secret("apisleutel".Sha256())`
- `AllowedScopes` to the identifier of your `ApiResource` created earlier

### Test Initial Setup

Let's test our initial set up.
Run the application as is (e.g. from Visual Studio).
Note that your IDE might run things over TLS (i.e. HTTPS), via a self-signed certificate.
For some Http Clients (e.g. Postman) you need to change your settings to skip certificate validation for now.

Let's try this (substitute your port if needed):

```bash
curl -X GET https://localhost:44385/.well-known/openid-configuration
```

You should get a JSON response with the public info for your ID Server.
Now try this:

```bash
curl -X POST \
  https://localhost:44385/connect/token \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  -d 'grant_type=client_credentials&client_id=foo-client-001&client_secret=apisleutel'
```

You should get back a JSON response with an `access_token` in it.

### Configure SPA Client

Let's now add a hard coded `new Client()` to represent our Angular SPA:

- `ClientId` set to e.g. `angular-spa-001`
- `AllowedGrantTypes` this time to `GrantTypes.Implicit`
- we only have *one* grant type so we can set `AllowAccessTokensViaBrowser` to `true`
- `AllowedScopes` to at least `StandardScopes.OpenId` (but add `Profile` and `Email` for good measure)

Our Angular SPA will be hoste at `localhost:4200` in a bit, so:

- set `RedirectUris` and `PostLogoutRedirectUris` to contain one URL for now: `http://localhost:4200/`
- set `AllowedCorsOrigins` to contain one *origin* for now: `http://localhost:4200`

> ⚠️ Mind the slash at the end for the *first* two properties!

This "Client" represents the Angular SPA we're going to create later on.

For such a Client to work with those Scopes, we also need to instruct IdentityServer4 to handle them.
Extend the `AddIdentityServer()` builder in `ConfigureServices(...)` with a call to `AddInMemoryIdentityResources`.
Provide it a list of `new IdentityResources.OpenId()` (add `.Profile()` and `.Email()` for good measure).

## Test the Client

We want to pretend we're *Angular* for a sec, and send all this to IdentityServer:

| Part            | Value                   | Explanation                                            |
| ----------------|-------------------------|--------------------------------------------------------|
| client_id       | `angular-spa-001`       | as we configured in IDS4                               |
| scope           | `openid profile email`  | the info we want about a user's identity               |
| response_type   | `token id_token`        | we want an (access) token as well as an identity token |
| redirect_uri    | `http://localhost:4200` | send the user back here after logging in               |
| nonce           | `dummy`                 | to help clients prevent replay attacks                 |

So open this Url in your browser:

```none
https://localhost:44385/connect/authorize?client_id=angular-spa-001&scope=openid%20profile%20email&response_type=token%20id_token&redirect_uri=http://localhost:4200&nonce=dummy
```

> ⚠️ You should be redirected to `/account/login?...` which gives a 404.
> Don't worry, that's okay!
> We haven't created any login screens, after all.
> This is for later.

### Create a Test User

This Dojo purposely skips persisting `Client`s, `ApiResource`s, and so forth in a database.
Similarly, we will also skip persistence for users.
However, we do need users before we can move to Angular!

As a final step, add `AddTestUsers(...)` to the `AddIdentityServer()` builder chain in `ConfigureServices(...)`.
Provide it with a single `new TestUser { ... }` setting a dummy `SubjectId`, `Username`, and `Password`.

## Part 2: Angular

Now we will create the Single Page Application that is the actual Client.
If you ever get lost you can peek at [this *extensive* example repo](https://github.com/jeroenheijmans/sample-angular-oauth2-oidc-with-auth-guards) for guidance.

> ⚠️ Warning: we *will* violate Angular best practices.
> Lots of them.
> (Remember: we are focusing on learning about the Implicit Flow.)

### Set up

Let's scaffold a *simple* Angular project (remember, this Dojo is *not* about Angular itself).
From your root, create a *sibling* for `MyIdServer`:

```powershell
# From the root folder (e.g. /dojo-implicit-flow)
ng new MyAngularSpa --inline-style --inline-template --skip-tests --skip-git
```

Just promise me you won't `--skip-tests` in your real application, okay?!

```powershell
cd MyAngularSpa
npm install angular-oauth2-oidc
ng serve --open
```

Wham!
You should see a scaffolded Angular application.
Leave it running, it has hot reloading while we change things up.

### Make users log in

Open the Angular App in your favorite editor (VSCode works well).
Let's change the `app.module.ts` first:

```typescript
import { HttpClientModule } from '@angular/common/http';
import { OAuthModule, AuthConfig, OAuthStorage } from 'angular-oauth2-oidc';
// Etc.

const myConfig: AuthConfig = {
  issuer: 'https://localhost:44385',
  clientId: 'angular-spa-001',
  redirectUri: window.location.origin + '/',
  scope: 'openid profile email',
};

@NgModule({
  // Etc.
  imports: [
    BrowserModule,
    HttpClientModule,
    OAuthModule.forRoot(),
  ],
  providers: [
    { provide: AuthConfig, useValue: myConfig },
    { provide: OAuthStorage, useValue: localStorage }, // sessionStorage is default
  ],
  // Etc.
})
export class AppModule { }
```

Change your `app.component.ts` to this:

```typescript
import { Component } from '@angular/core';
import { OAuthService, OAuthErrorEvent } from 'angular-oauth2-oidc';

@Component({
  selector: 'app-root',
  template: `
    <h1>Angular OAuth2 OIDC Test App</h1>
    <p>
      <button (click)="clear()">Clear LocalStorage</button>
      <button (click)="login()">Log in</button>
    </p>
    <p>Token:</p>
    <pre>{{oauthService.getAccessToken()}}</pre>
  `,
  styles: []
})
export class AppComponent {
  constructor(public oauthService: OAuthService) {
    this.oauthService.events.subscribe(event => event instanceof OAuthErrorEvent ? console.error(event) : console.warn(event));
    this.oauthService.loadDiscoveryDocument();
  }

  login() { this.oauthService.initImplicitFlow(); }
  clear() { localStorage.clear(); }
}

```

Now (re)load your application in the browser, and hit the "login" button.
If you get the same 404 error (because you got redirected to `/account/login?...`) you got before: congratulations!

Nearly done...

## Part 3: Create Login and Consent Screen

> ⚠️ Warning: in this step we create "toy" versions of a real Auth Server.
> We do this to see that (a) it is not *magic* but (b) it is still a *lot* of work.
> You can safely skip this step and substitute a test Auth0 account or reconfigure [this Quickstart Client from IdentityServer4](https://github.com/IdentityServer/IdentityServer4.Samples/blob/2.0.0/Quickstarts/7_JavaScriptClient/src/QuickstartIdentityServer/Config.cs#L87) (just clone the repo, edit linked Client to your needs, and start that specific project - and change your Angular app accordingly).

Still here? Good!
Let's do some dirty hacking to see what's under the hood!

Here's the plan for this part:

1. Add MVC basics to our Web API project
1. Add a model, view, and controller for `/account/login`
1. Add a model, view, and controller for `/consent`
1. Update our app to connect to this.

Let's get going!

### Add MVC Basics

Let's add some web application stuff to our .NET Core project. Start by changing this in `Startup.cs`:

```diff
new TestUser
{
-   SubjectId = Guid.NewGuid().ToString(),
+   SubjectId = "fake-guid-123",
```

```diff
- app.UseMvc();
+ app.UseMvcWithDefaultRoute();
```

We will also need a `/Views/_ViewImports.cshtml` file with just this line:

```razor
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

And a `/Views/Shared/_Layout.cshtml` file:

```html
<!DOCTYPE html>
<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>@ViewBag.Title</title>
</head>
<body>
    <div>
        @RenderBody()
    </div>
</body>
</html>
```

Now we're ready to create the first view.

### Add Login

This section will give you the code straight up.
Try to read and understand (and possibly tweak) the code as you paste it over to your project.

First up the `/Controllers/AccountController.cs` file:

```csharp
public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login(string returnUrl)
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginModel model, string action)
    {
        if (action != "login") { return View(); }
        if (!ModelState.IsValid) { return View(); }
        if (model.Username != "mary" || model.Password != "Secret123!")
        {
            ModelState.AddModelError("credentials", "use the hardcoded credentials!");
            return View();
        }
        await HttpContext.SignInAsync("fake-guid-123", "mary", new AuthenticationProperties { IsPersistent = model.RememberMe });
        return Redirect(model.ReturnUrl);
    }
}
```

Lots of open TODOs, security issues, and whatnot.
But it serves well to prove a point.
Next up the `/Models/LoginModel.cs` it uses:

```csharp
public class LoginModel
{
    [Required] public string Username { get; set; }
    [Required] public string Password { get; set; }
    public bool RememberMe { get; set; }
    public string ReturnUrl { get; set; }
}
```

Pretty straightforward, I'd say.
Finally, the associated `/Views/Account/Login.cshtml` file:

```html
@model MyIdServer.Models.LoginModel

<h1>Login</h1>
<div asp-validation-summary="All" style="color: red;"></div>
<form asp-route="Login">
    <p><input asp-for="ReturnUrl"> (normally hidden input)</p>
    <p><input asp-for="Username" autofocus placeholder="Username" required>*</p>
    <p><input asp-for="Password" type="password" placeholder="Password" required>*</p>
    <p><label><input asp-for="RememberMe" type="checkbox"> Remember login</label></p>
    <p>
        <button name="action" value="login">Log in</button>
        <button name="action" value="cancel">Cancel</button>
    </p>
</form>
```

And we've hacked ourselves a Login form together!

### Consent

Normally, an Authorization Server would first ask you to authenticate by logging in.
After that, this server should ask you to confirm that the Client (your Angular app) really should get access to your stuff.
This is known as "Consent": you consent that the Client gets access to the `scope`s it sent along when it directed you to the Authorization Server.

A proper Authorization Server would take great care showing such a screen.
But not us, not today!
We will just ask the user to blindly consent to everything.

Let's start with the `/Controllers/ConsentController.cs` class:

```csharp
public class ConsentController : Controller
{
    private readonly IIdentityServerInteractionService identiyServer4;

    public ConsentController(IIdentityServerInteractionService identiyServer4)
    {
        this.identiyServer4 = identiyServer4;
    }

    [HttpGet]
    public IActionResult Index(string returnUrl)
    {
        return View("Index", new ConsentModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Index(ConsentModel model)
    {
        if (!model.AgreesBlindlyToEverything)
        {
            throw new NotImplementedException("Only dummy implementation ready");
        }

        var authRequest = await identiyServer4.GetAuthorizationContextAsync(model.ReturnUrl);
        var consentResponse = new ConsentResponse
        {
            RememberConsent = true,
            ScopesConsented = new[] { "openid", "email", "profile" },
        };
        await identiyServer4.GrantConsentAsync(authRequest, consentResponse);
        return Redirect(model.ReturnUrl);
    }
}
```

This uses an extremely simple `/Models/ConsentModel.cs`:

```csharp
public class ConsentModel
{
    public string ReturnUrl { get; set; }
    public bool AgreesBlindlyToEverything { get; set; }
}
```

Which is presented in a terrible (but functional!) view:

```html
@model MyIdServer.Models.ConsentModel

<h2>Index</h2>
<form asp-route="Index">
    <p>Dummy view where you have to blindly consent to everything.</p>
    <p><input asp-for="ReturnUrl"> (normally hidden input)</p>
    <p><label><input type="checkbox" asp-for="AgreesBlindlyToEverything"> Consent to all the things</label></p>
    <p><button name="action" value="consent">Consent</button></p>
</form>
```

And now we're ready to roll!

### Updating Angular

Our existing Angular application's "login" button should already somewhat work.
Don't click it yet though!
Let's first do some minor changes.

First, add some debug info to the template in the `app.component.ts`:

```html
<p>Claims:</p>
<pre>{{oauthService.getIdentityClaims() | json}}</pre>
```

Next, change the `loadDiscoveryDocument()` call to be like this:

```typescript
this.oauthService
  .loadDiscoveryDocument()
  .then(() => {
    console.table((location.hash || '').split('&'));
    return this.oauthService.tryLogin();
  });
```

The OAuth2 spec tells the Authorization Server to finally send the user back to your "Redirect URI".
It also specifies that the URL, specifically in our case the hash fragment, should contain the results of the user's login process.

The `tryLogin()` call we've just added to the code will:

1. Grab the hash fragment
1. Store the parsed parts (including the Access Token) for later use
1. Clear the hash in your browser to prevent leaking details

That's all!
Now go and try to log in with your Angular App.
Everything should work fine.

Please take some time to use the developer tools' network tab and console to inspect (use "Preserve Log"!) what's going on.

### Silent Refreshes

After all this trouble we don't even have Refresh Tokens.
But we should be happy, because we don't trust our own JavaScript app with those!
Instead, we can use "Silent Refreshes".

On the Authorization Server side, this is part of the spec.
When calling `/authorize` endpoints we can specify `prompt=none` and the Auth Server should *try* to log in the user without prompting.
This will only work if the session with the Auth Server is still alive.

Each OAuth2 JavaScript library seems to handle the details slightly differently.
For our current library, it requires a few small changes.
First, add this to the `"assets"` in `angular.json`:

```json
"src/silent-refresh.html",
```

In fact, go ahead and create that file next to `index.html` right now:

```html
<!doctype html>
<html>
<body>
  <script>parent.postMessage(location.hash, location.origin);</script>
</body>
</html>
```

Then add a button to the `app.component.ts`:

```html
<button (click)="refresh()">Try Silent Refresh</button>
```

And this method:

```typescript
refresh() { this.oauthService.silentRefresh(); }
```

You need to restart `ng serve` because the assets changed.

If you hit the button now IdentityServer4 will encounter an error.
The silent-refresh URL needs to be registered as a valid redirect URI.
Security above all!

The fix is sipmle.
Just add `http://localhost:4200/silent-refresh.html` to the `RedirectUris` in our `Client` and reboot.
Now the silent refresh should *just work*!

### Epilogue

In this Part we've created our own Authorization Server screens for the Implicit flow.
The brutal truth is that (even though it's *possible*) it's a *lot* of work to create all the needed views and endpoints.
The absolute minimum would include:

- "Please log in" screen + option to use external provider
- "Log out?" and "Successfully logged out" screen
- "Consent" screen
- "My Details" or "Home" screen, including all granted scopes

Please don't let this scare you away from using Implicit Flow, because it's great and secure for SPA's.
What you should do though is think hard and decide whether to use an IdentityServer Quick Start, or some SAAS solution (Auth0, OKTA, Keycloack).

## Part 4: Bonus section

This section outlines several bonus objectives.
All of them are direct follow-ups from the above Dojo.
None of them are further specified: you can now continue on alone!

- Clone, run, and tweak [this example repo](https://github.com/jeroenheijmans/sample-angular-oauth2-oidc-with-auth-guards) we created earlier. Note the [`auth.service.ts` wrapper](https://github.com/jeroenheijmans/sample-angular-oauth2-oidc-with-auth-guards/blob/master/src/app/auth.service.ts) which annotates a proper full use case of the angular-oauth2-oidc library.
- Clone and run [the Implicit Flow Quickstart from IdentityServer4](https://github.com/IdentityServer/IdentityServer4.Samples/tree/release/Quickstarts/3_ImplicitFlowAuthentication)
- Set up an [Auth0](https://auth0.com/) (or OKTA) account and connect your Angular app to that (should be a matter of reconfiguring!)

Or finally, you can actually *put this all to use* and try to send `bearer` tokens along to an API:

- Configure [`OAuthModule.forRoot(...)`](https://manfredsteyer.github.io/angular-oauth2-oidc/docs/additional-documentation/working-with-httpinterceptors.html) so that it sends bearer tokens to your API
- Create an API that accepts the tokens and verifies the JWT token
- Create an Angular service that retrieves data from this API

After that, you can consider yourself a master of this sub!

## Conclusion

Congratulations! You've made it to the end of this Dojo.
If you have any feedback: let us know!