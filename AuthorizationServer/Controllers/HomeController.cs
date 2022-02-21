using AuthorizationServer.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;

namespace AuthorizationServer.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpContext _httpContext;
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration, IHttpContextAccessor httpContext)
        {
            _httpContext = httpContext.HttpContext;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var idToken = await _httpContext.GetTokenAsync("id_token");
            var verified = VerifiedToken(idToken);
            return View(verified);
        }

        public JwtSecurityToken VerifiedToken(string token)
        {
            var issuer = $"https://{_configuration["OktaSettings:OktaDomain"]}/oauth2/default";

            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
        issuer + "/.well-known/openid-configuration",
        new OpenIdConnectConfigurationRetriever(),
        new HttpDocumentRetriever());

            try
            {
                JwtSecurityToken validatedToken = TokenHelpers.ValidateToken(token, issuer, configurationManager).Result;

                return validatedToken;
            }
            catch (Exception ex)
            {
                //Do some logging
                return new JwtSecurityToken();
            }
        }
    }
}
