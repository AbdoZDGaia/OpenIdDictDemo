using AuthorizationServer.Data;
using AuthorizationServer.Helpers;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Okta.AspNetCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

var config = builder.Configuration;
var issuer = $"https://{config["OktaSettings:OktaDomain"]}/oauth2/default";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<IdentityDbContext>(options =>
{
    options.UseSqlServer(connectionString);

    // Register the entity sets needed by OpenIddict.
    options.UseOpenIddict();
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddHostedService<TestData>();
builder.Services.AddHttpClient();
// Replace with your authorization server URL:

IdentityModelEventSource.ShowPII = true;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OktaDefaults.MvcAuthenticationScheme;
})
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/account/login";
    })
    .AddOktaMvc(new OktaMvcOptions
    {
        // Replace these values with your Okta configuration
        OktaDomain = $"https://{config["OktaSettings:OktaDomain"]}",
        ClientId = $"{config["OktaSettings:ClientId"]}",
        ClientSecret = $"{config["OktaSettings:ClientSecret"]}",
        GetClaimsFromUserInfoEndpoint = true,
        Scope = new List<string> { "openid", "profile", "email", "offline_access" }
    });

builder.Services.AddOpenIddict()
        // Register the OpenIddict core components.
        .AddCore(options =>
        {
            // Configure OpenIddict to use the EF Core stores/models.
            options.UseEntityFrameworkCore()
                .UseDbContext<IdentityDbContext>();
        })
        // Register the OpenIddict server components.
        .AddServer(options =>
        {
            options
                .AllowClientCredentialsFlow()
                .AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();

            options
                .SetTokenEndpointUris("/connect/token")
                .SetAuthorizationEndpointUris("/connect/authorize")
                .SetUserinfoEndpointUris("/connect/userinfo");

            // Encryption and signing of tokens
            options
                .AddEphemeralEncryptionKey()
                .AddEphemeralSigningKey()
                .DisableAccessTokenEncryption();

            // Register scopes (permissions)
            options.RegisterScopes("api");

            // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
            options
                .UseAspNetCore()
                .EnableTokenEndpointPassthrough()
                .EnableAuthorizationEndpointPassthrough()
                .EnableUserinfoEndpointPassthrough();
        });

var app = builder.Build();

//app.MapGet("/", () => "Hello World!");

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();

app.UseRouting();

app.Use(async (context, next) =>
{
    DateTime expires;
    var idToken = await context.GetTokenAsync("id_token");
    var expiresToken = await context.GetTokenAsync("expires_at");
    var accessToken = await context.GetTokenAsync("access_token");
    var refreshToken = await context.GetTokenAsync("refresh_token");

    if (refreshToken != null && (DateTime.TryParse(expiresToken, out expires)))
    {
        if (expires < DateTime.Now) //Token is expired, let's refresh
        {
            var client = new HttpClient();
            var tokenResult = client.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = "https://localhost:7154/oauth2/v1/token",
                ClientId = $"{config["OktaSettings:ClientId"]}",
                ClientSecret = $"{config["OktaSettings:ClientSecret"]}",
                RefreshToken = refreshToken
            }).Result;


            if (!tokenResult.IsError)
            {
                var oldIdToken = idToken;
                var newAccessToken = tokenResult.AccessToken;
                var newRefreshToken = tokenResult.RefreshToken;
                idToken = tokenResult.IdentityToken;

                var tokens = new List<AuthenticationToken>
                {
                    new AuthenticationToken {Name = OpenIdConnectParameterNames.IdToken, Value = tokenResult.IdentityToken},
                    new AuthenticationToken
                    {
                        Name = OpenIdConnectParameterNames.AccessToken,
                        Value = newAccessToken
                    },
                    new AuthenticationToken
                    {
                        Name = OpenIdConnectParameterNames.RefreshToken,
                        Value = newRefreshToken
                    }
                };

                var expiresAt = DateTime.Now + TimeSpan.FromSeconds(tokenResult.ExpiresIn);
                tokens.Add(new AuthenticationToken
                {
                    Name = "expires_at",
                    Value = expiresAt.ToString("o", CultureInfo.InvariantCulture)
                });

                var result = await context.AuthenticateAsync();
                result.Properties.StoreTokens(tokens);

                await context.SignInAsync(result.Principal, result.Properties);
            }
        }
    }

    await next.Invoke();
});

app.UseAuthentication();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute();
});

app.Run();
