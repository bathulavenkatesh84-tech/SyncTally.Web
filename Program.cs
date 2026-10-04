//using Microsoft.AspNetCore.Authentication.Cookies;
//using SyncTally.Web.Middleware;
//using SyncTally.Web.Models;
//using SyncTally.Web.Repositories.Implementations;
//using SyncTally.Web.Repositories.Interfaces;
//using SyncTally.Web.Services;
//using SyncTally.Web.Services.Implementations;
//using SyncTally.Web.Services.Interfaces;

//var builder = WebApplication.CreateBuilder(args);

//// ---- Configuration ----
//builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("DatabaseSettings"));
//builder.Services.Configure<ExternalAppSettings>(builder.Configuration.GetSection("ExternalApp"));

//// ---- MVC / framework ----
//builder.Services.AddControllersWithViews();
//builder.Services.AddMemoryCache();
//builder.Services.AddHttpContextAccessor();
//builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken"); // used by the AJAX WhatsApp dialog

//builder.Services
//    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//    .AddCookie(o =>
//    {
//        o.Cookie.Name = "SyncTally.Auth";
//        o.Cookie.HttpOnly = true;
//        o.LoginPath = "/Account/Login";          // redirects on to https://localhost:7299/tallywebapp/Login
//        o.AccessDeniedPath = "/Account/AccessDenied";
//        o.ExpireTimeSpan = TimeSpan.FromHours(8);
//        o.SlidingExpiration = true;
//    });
//builder.Services.AddAuthorization();

//// ---- Data access (Flexidb = plain ADO.NET over Npgsql) ----
//builder.Services.AddSingleton<IFlexidb, Flexidb>();

//// ---- Repositories ----
//builder.Services.AddScoped<IFeatureRepository, FeatureRepository>();
//builder.Services.AddScoped<IBlogRepository, BlogRepository>();
//builder.Services.AddScoped<IPartnerRepository, PartnerRepository>();
//builder.Services.AddScoped<IPlanRepository, PlanRepository>();
//builder.Services.AddScoped<ISiteContentRepository, SiteContentRepository>();
//builder.Services.AddScoped<IContactRepository, ContactRepository>();
//builder.Services.AddScoped<IErrorLogRepository, ErrorLogRepository>();

//// ---- Services ----
//builder.Services.AddScoped<IFeatureService, FeatureService>();
//builder.Services.AddScoped<IBlogService, BlogService>();
//builder.Services.AddScoped<IPartnerService, PartnerService>();
//builder.Services.AddScoped<IPricingService, PricingService>();
//builder.Services.AddScoped<ISiteContentService, SiteContentService>();
//builder.Services.AddScoped<IContactService, ContactService>();
//builder.Services.AddScoped<IAuthService, AuthService>();
//builder.Services.AddScoped<IErrorLogService, ErrorLogService>();

//var app = builder.Build();

//// ---- Pipeline ----
//app.UseMiddleware<ExceptionHandlingMiddleware>();          // centralized exception handling
//app.UseStatusCodePagesWithReExecute("/Home/StatusCodePage/{0}");

//if (!app.Environment.IsDevelopment())
//{
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseRouting();
//app.UseAuthentication();
//app.UseAuthorization();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.Run();

using Microsoft.AspNetCore.Authentication.Cookies;
using SyncTally.Web.Middleware;
using SyncTally.Web.Models;
using SyncTally.Web.Repositories.Implementations;
using SyncTally.Web.Repositories.Interfaces;
using SyncTally.Web.Services;
using SyncTally.Web.Services.Implementations;
using SyncTally.Web.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration ----
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("DatabaseSettings"));
builder.Services.Configure<ExternalAppSettings>(builder.Configuration.GetSection("ExternalApp"));
builder.Services.Configure<TrialSettings>(builder.Configuration.GetSection("Trial"));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));

// ---- MVC / framework ----
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken"); // used by the AJAX WhatsApp dialog

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "SyncTally.Auth";
        o.Cookie.HttpOnly = true;
        o.LoginPath = "/Account/Login";          // redirects on to https://localhost:7299/tallywebapp/Login
        o.AccessDeniedPath = "/Account/AccessDenied";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

// ---- Data access (Flexidb = plain ADO.NET over Npgsql) ----
builder.Services.AddSingleton<IFlexidb, Flexidb>();

// ---- Repositories ----
builder.Services.AddScoped<IFeatureRepository, FeatureRepository>();
builder.Services.AddScoped<IBlogRepository, BlogRepository>();
builder.Services.AddScoped<IPartnerRepository, PartnerRepository>();
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<ISiteContentRepository, SiteContentRepository>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IErrorLogRepository, ErrorLogRepository>();
builder.Services.AddScoped<ITrialRepository, TrialRepository>();

// ---- Services ----
builder.Services.AddScoped<IFeatureService, FeatureService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<ISiteContentService, SiteContentService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IErrorLogService, ErrorLogService>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<ITrialService, TrialService>();

var app = builder.Build();

// ---- Pipeline ----
app.UseMiddleware<ExceptionHandlingMiddleware>();          // centralized exception handling
app.UseStatusCodePagesWithReExecute("/Home/StatusCodePage/{0}");

if (!app.Environment.IsDevelopment())
{
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

