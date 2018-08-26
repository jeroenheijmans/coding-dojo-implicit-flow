using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static IdentityServer4.IdentityServerConstants;

namespace MyIdServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddIdentityServer()
                .AddInMemoryApiResources(new [] { new ApiResource("fake-api-1") })
                .AddInMemoryClients(new []
                {
                    new Client
                    {
                        ClientId = "foo-client-001",
                        AllowedGrantTypes = GrantTypes.ClientCredentials,
                        ClientSecrets = { new Secret("apisleutel".Sha256()) },
                        AllowedScopes = { "fake-api-1" },
                    },
                    new Client
                    {
                        ClientId = "angular-spa-001",
                        AllowedGrantTypes = GrantTypes.Implicit,
                        AllowAccessTokensViaBrowser = true,
                        AllowedScopes = {
                            StandardScopes.OpenId,
                            StandardScopes.Profile,
                            StandardScopes.Email,
                        },
                        AllowedCorsOrigins = { "http://localhost:4200", },
                        RedirectUris = { "http://localhost:4200/", "http://localhost:4200/silent-refresh.html" },
                        PostLogoutRedirectUris = { "http://localhost:4200/", },
                    }
                })
                .AddInMemoryIdentityResources(new List<IdentityResource>
                {
                    new IdentityResources.OpenId(),
                    new IdentityResources.Profile(),
                    new IdentityResources.Email(),
                })
                .AddTestUsers(new List<TestUser>
                {
                    new TestUser
                    {
                        SubjectId = "fake-guid-123",
                        Username = "mary",
                        Password = "Secret123!",                        
                    },
                })
                .AddDeveloperSigningCredential();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseIdentityServer();

            app.UseCors(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

            app.UseHttpsRedirection();
            app.UseMvcWithDefaultRoute();
        }
    }
}
