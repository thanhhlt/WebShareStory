using App.Models;
using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

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

//IdentityOptions
builder.Services.Configure<IdentityOptions> (options => {
    // Password
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;

    //Lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes (15);

    //User.
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    //Login.
    options.SignIn.RequireConfirmedEmail = true;
    options.SignIn.RequireConfirmedAccount = true;
});

builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
});

builder.Services.AddAuthentication().AddGoogle(options => {
    options.ClientId = Environment.GetEnvironmentVariable("CLIENT_ID") ?? "";
    options.ClientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET") ?? "";
    options.CallbackPath = new PathString("/signin-google");
    // https://localhost:5001/signin-google
    options.AccessDeniedPath = new PathString("/");
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

// app.UseStaticFiles(new StaticFileOptions() {
//     FileProvider = new PhysicalFileProvider(
//         Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates")
//     ),
//     RequestPath = "/emailcontent"
// });

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
