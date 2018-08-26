using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyIdServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyIdServer.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login(string returnUrl)
        {
            return View();
        }

        // TODO: AntiForgeryToken
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model, string action)
        {
            if (action != "login")
            {
                // TODO: Cancelling login
                return View();
            }

            if (!ModelState.IsValid)
            {
                // TODO: Implement good validation
                return View();
            }

            // TODO: Implement real stuff here
            if (model.Username != "mary" || model.Password != "Secret123!")
            {
                // TODO: Implement great login failures
                ModelState.AddModelError("credentials", "use the hardcoded credentials!");
                return View();
            }

            var props = new AuthenticationProperties { IsPersistent = model.RememberMe };
            await HttpContext.SignInAsync("fake-guid-123", "mary", props);

            // TODO: Check if the ReturnUrl is still okay using IIdentityServerInteractionService

            return Redirect(model.ReturnUrl);
        }
    }
}
