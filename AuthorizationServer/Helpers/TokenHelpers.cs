using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace AuthorizationServer.Helpers
{
    public static class TokenHelpers
    {
        public static async Task<JwtSecurityToken> ValidateToken(
        string token,
        string issuer,
        Microsoft.IdentityModel.Protocols.IConfigurationManager<OpenIdConnectConfiguration> configurationManager,
        CancellationToken ct = default(CancellationToken))
        {

            try
            {
                if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));
                if (string.IsNullOrEmpty(issuer)) throw new ArgumentNullException(nameof(issuer));

                var discoveryDocument = await configurationManager.GetConfigurationAsync(ct);
                var signingKeys = discoveryDocument.SigningKeys;

                var validationParameters = new TokenValidationParameters
                {
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = signingKeys,
                    ValidateLifetime = true,
                    LifetimeValidator = new LifetimeValidator((notBefore, expires, secToken, tokenParams) =>
                    {
                        if (!expires.HasValue)
                            return false;

                        //Add 3 hours to bump the expiration to where our session refreshes
                        return (expires.Value > DateTime.Now.ToUniversalTime());
                    }),
                    ValidateAudience = false,
                    // Allow for some drift in server time
                    // (a lower value is better; we recommend two minutes or less)
                    ClockSkew = TimeSpan.FromMinutes(2),
                };
                var principal = new JwtSecurityTokenHandler()
                    .ValidateToken(token, validationParameters, out var rawValidatedToken);

                return (JwtSecurityToken)rawValidatedToken;
            }
            catch (Exception ex)
            {
                //Error handling/loggin
                return null;
            }
        }
    }
}
