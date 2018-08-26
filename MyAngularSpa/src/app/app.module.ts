import { HttpClientModule } from '@angular/common/http';
import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { OAuthModule, AuthConfig, OAuthStorage } from 'angular-oauth2-oidc';

import { AppComponent } from './app.component';

const myConfig: AuthConfig = {
  issuer: 'https://localhost:44385',
  clientId: 'angular-spa-001',
  redirectUri: window.location.origin + '/',
  silentRefreshRedirectUri: window.location.origin + '/silent-refresh.html',
  scope: 'openid profile email',
};

@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    OAuthModule.forRoot(),
  ],
  providers: [
    { provide: AuthConfig, useValue: myConfig },
    { provide: OAuthStorage, useValue: localStorage },
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
