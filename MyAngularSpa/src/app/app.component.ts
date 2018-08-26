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
