using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

public class MailSettings
{
    public string? Mail {get; set;}
    public string? DisplayName {get; set;}
    public string? Password {get; set;}
    public string? Host {get; set;}
    public int Port {get; set;}
}

public class MailContent
{
    public string? To {get; set;}
    public string? Sub {get; set;}
    public string? Body {get; set;}
}

public class SendMailService : IEmailSender
{
    private readonly MailSettings mailSetting;

    private readonly ILogger<SendMailService> logger;

    public SendMailService(ILogger<SendMailService> _logger, IOptions<MailSettings> _mailSettings)
    {
        logger = _logger;
        mailSetting = _mailSettings.Value;
    }
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (string.IsNullOrEmpty(mailSetting.Mail) || string.IsNullOrEmpty(mailSetting.Host))
        {
            logger.LogError("Địa chỉ email không hợp lệ.");
            return;
        }

        var message = new MailMessage();
        message.From = new MailAddress(mailSetting.Mail);
        message.To.Add(email);
        message.Subject = subject;

        message.Body = htmlMessage;
        message.IsBodyHtml = true;

        using SmtpClient smtp = new SmtpClient();

        try {
            smtp.Port = mailSetting.Port;
            smtp.EnableSsl = true;
            smtp.UseDefaultCredentials = false;
            smtp.Host = mailSetting.Host;
            smtp.Credentials = new NetworkCredential(mailSetting.Mail, mailSetting.Password);
            await smtp.SendMailAsync(message);
        } catch (Exception ex) {
            // Gửi mail thất bại, nội dung email sẽ lưu vào thư mục mailssave
            System.IO.Directory.CreateDirectory("mailssave");
            var emailsavefile = string.Format(@"mailssave/{0}.eml", Guid.NewGuid ());
            await File.WriteAllTextAsync(emailsavefile, $"To: {email}\nSubject: {subject}\nBody: {htmlMessage}");

            logger.LogInformation("Lỗi gửi mail, lưu tại - " + emailsavefile);
            logger.LogError(ex.Message);
        }

        logger.LogInformation("send mail to: " + email);
    }
}