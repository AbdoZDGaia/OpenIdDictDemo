using Microsoft.AspNetCore.Mvc;
using Okta.AspNetCore;

namespace OktaWebApp.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult OktaSignIn()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Challenge(OktaDefaults.MvcAuthenticationScheme);
            }
            var claims = HttpContext.User.Claims;
            return RedirectToAction("Index", "Home");
        }
    }
}
