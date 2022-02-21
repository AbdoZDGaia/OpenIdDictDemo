using Microsoft.AspNetCore.Authentication.Cookies;
using Okta.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;
// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OktaDefaults.MvcAuthenticationScheme;
})
    .AddCookie()
    .AddOktaMvc(new OktaMvcOptions
    {
        // Replace these values with your Okta configuration
        OktaDomain = $"https://{config["OktaSettings:OktaDomain"]}",
        ClientId = $"{config["OktaSettings:ClientId"]}",
        ClientSecret = $"{config["OktaSettings:ClientSecret"]}",
        AuthorizationServerId = ""
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
