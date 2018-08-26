using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Mvc;
using MyIdServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyIdServer.Controllers
{
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

        // TODO: Antiforgery
        [HttpPost]
        public async Task<IActionResult> Index(ConsentModel model)
        {
            // TODO: Create real implementation
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
}
