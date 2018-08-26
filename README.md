# Implicit Flow Coding Dojo

This workshop leads you to create an OAuth2 and OpenID Connect Identity Server, and an Angular SPA that uses Implicit flow to let users log in using that ID Server.

## Introduction

This Dojo shows you how the OAuth2 "Implicit Flow" works.
It uses .NET Core, IdentityServer4, Angular 6, and Angular-OAuth2-OIDC; but only for demonstration purposes.
We do assume *some* web development experience with .NET and JavaScript frameworks.

Prerequisites:

- .NET Core 2 (tested with 2.1.400)
- Node and NPM (tested with v10.8.0 and 6.3.0 respectively)
- Angular CLI (tested with 6.1.3)

Optionally grab the `.gitignore` from this repository and use it during the dojo steps, below.

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

⚠️ Mind the slash at the end for the *first* two properties!

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

⚠️ You should be redirected to `/account/login?...` which gives a 404.
Don't worry, that's okay!
We haven't created any login screens, after all.
This is for later.

### Create a Test User

This Dojo purposely skips persisting `Client`s, `ApiResource`s, and so forth in a database.
Similarly, we will also skip persistence for users.
However, we do need users before we can move to Angular!

As a final step, add `AddTestUsers(...)` to the `AddIdentityServer()` builder chain in `ConfigureServices(...)`.
Provide it with a single `new TestUser { ... }` setting a dummy `SubjectId`, `Username`, and `Password`.

## Part 2: Angular

Now we will create the Single Page Application that is the actual Client.
If you ever get lost you can peek at [this *extensive* example repo](https://github.com/jeroenheijmans/sample-angular-oauth2-oidc-with-auth-guards) for guidance.

⚠️ Warning: we *will* violate Angular best practices.
Lots of them.
(Remember: we are focusing on learning about the Implicit Flow.)

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

## Part 3: Create Login Screen

TODO

## Part 4: Bonus section

TODO

## Conclusion

TODO