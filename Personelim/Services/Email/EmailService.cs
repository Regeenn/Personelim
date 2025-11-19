using System.Net;
using System.Net.Mail;

namespace Personelim.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendPasswordResetCodeAsync(string email, string code, string userName)
        {
            try
            {
                var smtpHost = _configuration["Email:SmtpHost"];
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"]);
                var smtpUser = _configuration["Email:SmtpUser"];
                var smtpPass = _configuration["Email:SmtpPass"];
                var fromEmail = _configuration["Email:FromEmail"];
                var fromName = _configuration["Email:FromName"];

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = "Şifre Sıfırlama Kodu - Personelim",
                    Body = GetEmailBody(userName, code),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email gönderimi başarısız: {Email}", email);
                return false;
            }
        }

        private string GetEmailBody(string userName, string code)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .code-box {{ background-color: #f4f4f4; padding: 20px; text-align: center; 
                                     border-radius: 5px; margin: 20px 0; }}
                        .code {{ font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #007bff; }}
                        .footer {{ margin-top: 30px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h2>Merhaba {userName},</h2>
                        <p>Şifre sıfırlama talebiniz alınmıştır. Aşağıdaki kodu kullanarak yeni şifrenizi belirleyebilirsiniz:</p>
                        
                        <div class='code-box'>
                            <div class='code'>{code}</div>
                        </div>
                        
                        <p><strong>Bu kod 15 dakika boyunca geçerlidir.</strong></p>
                        
                        <p>Eğer bu talebi siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz.</p>
                        
                        <div class='footer'>
                            <p>Bu otomatik bir e-postadır, lütfen yanıtlamayınız.</p>
                            <p>&copy; 2024 Personelim. Tüm hakları saklıdır.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";
        }
    }
}