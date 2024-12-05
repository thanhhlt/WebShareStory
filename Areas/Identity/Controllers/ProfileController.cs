#nullable disable

using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using App.Areas.Identity.Models.ProfileViewModels;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System.Text.Encodings.Web;

namespace App.Areas.Identity.Controllers
{
    [Authorize]
    [Area("Identity")]
    [Route("/profile/[action]")]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IEmailSender emailSender,
        IEmailTemplateService emailTemplateService,
        ILogger<ProfileController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _emailTemplateService = emailTemplateService;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public IActionResult GetStatusMessage()
        {
            return PartialView("_StatusMessage");
        }

        // GET: /profile
        [HttpGet("/profile")]
        public async Task<IActionResult> Index()
        {
           var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return NotFound("Không tìm thấy tài khoản.");
            }

           IndexProfileViewModel model = new IndexProfileViewModel()
           {
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                BirthDate = user.BirthDate,
                Gender = user.Gender,
                Address = user.Address,
                Introduction = user.Introduction
           };

           return View(model);
        }

        // POST: /profile/ChangeProfileInfo
        [HttpPost]
        public async Task<IActionResult> ChangeProfileInfo(IndexProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            if (user == null)
            {
                StatusMessage = "Error Không tìm thấy tài khaoản.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                user.PhoneNumber = model.PhoneNumber;
                user.BirthDate = model.BirthDate;
                user.Gender = model.Gender;
                user.Address = model.Address;
                user.Introduction = model.Introduction;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    StatusMessage = "Cập nhật thông tin cá nhân thành công.";
                    return RedirectToAction("Index");
                }
            }
            var errorMessages = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            StatusMessage = $"Error Cập nhật thông tin cá nhân thất bại.<br/> {string.Join("<br/>", errorMessages)}";
            _logger.LogWarning(StatusMessage);
            return RedirectToAction("Index");
        }
        
        //GET: /profile/ChangeEmail
        [HttpGet]
        public async Task<IActionResult> ChangeEmail(string NewEmail)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            ChangeEmailViewModel changeEmailViewModel = new ChangeEmailViewModel()
            {
                OldEmail = await _userManager.GetEmailAsync(user),
            };

            return View(changeEmailViewModel);
        }

        //POST: /profile/ChangeEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmailAsync(ChangeEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                StatusMessage = $"Error Thay đổi Email thất bại.<br/> {string.Join("<br/>", errorMessages)}";
                return Json(new {success = false});
            }

            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                StatusMessage = "Error Không tìm thấy tài khoản";
                return Json(new {success = false});
            }

            var logins = await _userManager.GetLoginsAsync(user);
            foreach(var login in logins)
            {
                if (login.ProviderDisplayName == "Google")
                {
                    StatusMessage = "Error Gỡ bỏ liên kết tài khoản với Google trước khi thay đổi Email";
                    return Json(new { success = false, redirect = Url.Action("Index") });
                }
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordValid)
            {
                StatusMessage = "Error Sai mật khẩu xác nhận.";
                return Json(new {success = false});
            }
            
            var oldEmail = await _userManager.GetEmailAsync(user);
            if (model.NewEmail == oldEmail)
            {
                StatusMessage = "Error Email mới trùng với Email hiện tại.";
                return Json(new {success = false});
            }

            if (await _userManager.FindByEmailAsync(model.NewEmail) != null)
            {
                StatusMessage = "Error Đã tồn tại tài khoản có Email này.";
                return Json(new {success = false});
            }

            user.Email = model.NewEmail;
            user.EmailConfirmed = false;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                StatusMessage = "Error Thay đổi Email thất bại với lỗi:";
                foreach (var error in result.Errors)
                {  
                    StatusMessage += $"<br/>{error.Description}";
                }
                return Json(new { success = false, redirect = Url.Action("Index") });   
            }
        
            // Gửi qua email mới email xác nhận
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.ActionLink(
                action: nameof(ConfirmEmail),
                values: 
                    new { area = "Identity",
                        controller = "Account",
                        userId = user.Id, 
                        code = code},
                protocol: Request.Scheme);

            if (callbackUrl != null)
            {
                var emailBodyTemplate = await _emailTemplateService.GetTemplateAsync("ConfirmEmail.html");
                var emailBody = emailBodyTemplate.Replace("{{ConfirmationLink}}", HtmlEncoder.Default.Encode(callbackUrl))
                                                .Replace("{{UserName}}", user.UserName);

                await _emailSender.SendEmailAsync(model.NewEmail, "Xác nhận địa chỉ email - GocKeChuyen", emailBody);
            }

            StatusMessage = "Thay đổi Email thành công. Kiểm tra Email mới để xác thực.";
            await _signInManager.RefreshSignInAsync(user);

            return Json(new { success = true, redirect = Url.Action("Index") });
        }

        //GET: /profile/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        //POST: /profile/ChangePassword
        [HttpPost]
        public async Task<IActionResult> ChangePasswordAsync(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                StatusMessage = $"Error Thay đổi mật khẩu thất bại.<br/> {string.Join("<br/>", errorMessages)}";
                return Json(new {success = false});
            }

            if (model.OldPassword == model.NewPassword)
            {
                StatusMessage = "Error Mật khẩu mới phải khác mật khẩu hiện tại.";
                return Json(new {success = false});
            }

            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                StatusMessage = "Error Không tìm thấy tài khoản.";
                return Json(new {success = false});
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                StatusMessage = "Error Thay đổi mật khẩu thất bại với lỗi:";
                foreach (var error in result.Errors)
                {  
                    StatusMessage += $"<br/>{error.Description}";
                }
                return Json(new {success = false});
            }
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Thay đổi mật khẩu thành công.";

            return Json(new {success = true, redirect = Url.Action("Index")});
        }
    }
}