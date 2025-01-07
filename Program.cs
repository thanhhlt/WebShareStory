using System.Globalization;
using App.Models;
using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions();
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddIdentity<AppUser, IdentityRole>()
                .AddErrorDescriber<AppIdentityErrorDescriber>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

builder.Logging.AddConsole();

// Connect database
builder.Services.AddDbContext<AppDbContext>(options => {
    string connectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS") ?? "";
    options.UseSqlServer(connectionString);
});

// Add SignalR
builder.Services.AddSignalR();

//Add Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var mailsettings = builder.Configuration.GetSection("MailSettings").Get<MailSettings>();
string gmailPassword = Environment.GetEnvironmentVariable("GMAIL_PASSWORD") ?? "";
if (mailsettings != null)
{
    builder.Services.Configure<MailSettings>(options => {
        options.Mail = mailsettings.Mail;
        options.DisplayName = mailsettings.DisplayName;
        options.Password = gmailPassword;
        options.Host = mailsettings.Host;
        options.Port = mailsettings.Port;
    });
}
builder.Services.AddSingleton<IEmailSender, SendMailService>();

builder.Services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IDeleteUserService, DeleteUserService>();
builder.Services.AddScoped<IThumbnailService, ThumbnailService>();

//IdentityOptions
builder.Services.Configure<IdentityOptions> (options => {
    // Password
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;

    //Lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes (15);
    options.Lockout.AllowedForNewUsers = true;

    //User.
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    //Login.
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
});

builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
});

builder.Services.AddAuthentication()
    .AddGoogle(options => {
        options.ClientId = Environment.GetEnvironmentVariable("CLIENT_ID") ?? "";
        options.ClientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET") ?? "";
        options.CallbackPath = new PathString("/signin-google");
        // https://localhost:5000/signin-google
        options.AccessDeniedPath = new PathString("/externalloginfail");
    })
    .AddFacebook(options => {
        options.AppId = Environment.GetEnvironmentVariable("APP_ID") ?? "";
        options.AppSecret = Environment.GetEnvironmentVariable("APP_SECRET") ?? "";
        options.CallbackPath = new PathString("/sign-facebook");
        // https://localhost:5000/signin-facebook
        options.AccessDeniedPath = new PathString("/externalloginfail");
    });

// Config Format Time
var cultureInfo = new CultureInfo("vi-VN");
cultureInfo.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
cultureInfo.DateTimeFormat.LongDatePattern = "dd/MM/yyyy";

CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(1);
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

app.UseStaticFiles(new StaticFileOptions() {
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "Images")
    ),
    RequestPath = "/imgs"
});

app.UseSession();
app.UseRouting();
app.UseAuthorization();
app.MapHub<PresenceHub>("presenceHub");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
