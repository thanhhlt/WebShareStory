using System.Globalization;
using App.Models;
using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore.Proxies;
using App.Security.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions();
// Add services to the container.
builder.Services.AddHttpContextAccessor();
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

// Add Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

// Add Session
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
builder.Services.AddScoped<IUserBlockService, UserBlockService>();
builder.Services.AddSingleton<IGoogleAnalyticsService>(provider =>
{
    var propertyId = Environment.GetEnvironmentVariable("PROPERTY_ID") ?? "";
    var credentialPath = Environment.GetEnvironmentVariable("CREDENTIAL_PATH") ?? "";
    return new GoogleAnalyticsService(propertyId, credentialPath);
});

//IdentityOptions
builder.Services.Configure<IdentityOptions> (options => {
    // Password
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;

    //Lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes (30);
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

// Policy
builder.Services.AddAuthorization(options => {
    options.AddPolicy("AllowTwoFactorAuth", policy => {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Feature", "TwoFactorAuth");
    });
    options.AddPolicy("AllowPinPost", policy => {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Permission", "PinPost");
    });
    options.AddPolicy("AllowCreatePost", policy => {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Permission", "CreatePost");
        policy.Requirements.Add(new PostCreateRequirement());
    });
    options.AddPolicy("AllowUpdatePost", policy => {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new PostUpdateRequirement());
    });
    options.AddPolicy("AllowComment", policy => {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new CmtCreateRequirement());
    });
    //Manage
    options.AddPolicy("CanManageContact", policy => {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Feature", "ContactManage");
    });
    options.AddPolicy("CanPostAnmnt", policy => {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Permission", "PostAnmnt");
    });
    //Post
    options.AddPolicy("CanManagePost", policy => {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Feature", "PostManage");
    });
    options.AddPolicy("CanManageCate", policy => {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Feature", "CateManage");
    });
    //User
    options.AddPolicy("CanManageUser", policy => {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Feature", "UserManage");
    });
    options.AddPolicy("CanLockUser", policy => {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Permission", "UserLock");
    });
    options.AddPolicy("CanResetPassword", policy => {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Permission", "ResetPassword");
    });
    options.AddPolicy("CanDeleteUser", policy => {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Permission", "DeleteUser");
    });
    //Other
    options.AddPolicy("CanManageDb", policy => {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Feature", "DatabaseManage");
    });
    options.AddPolicy("CanViewStatistics", policy => {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Permission", "ViewStatistics");
    });
    options.AddPolicy("CanManageRole", policy => {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Feature", "RoleManage");
    });
});

builder.Services.AddScoped<IAuthorizationHandler, AppAuthorizationHandler>();

// Config Format Time
var cultureInfo = new CultureInfo("vi-VN");
cultureInfo.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
cultureInfo.DateTimeFormat.LongDatePattern = "dd/MM/yyyy";

CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    // options.ValidationInterval = TimeSpan.FromMinutes(5);
    options.ValidationInterval = TimeSpan.FromMinutes(30);
});

builder.WebHost.UseUrls("http://0.0.0.0:8090");

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

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto
});
app.UseSession();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<PresenceHub>("presenceHub");

app.MapGet("/robots.txt", async context =>
{
    var robotsContent = "User-agent: *\nDisallow:";
    context.Response.ContentType = "text/plain";
    await context.Response.WriteAsync(robotsContent);
});
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
