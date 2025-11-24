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
        public async Task<bool> SendInvitationEmailAsync(string email, string invitationCode, string businessName, string inviterName, string message)
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
                    Subject = $"{businessName} İşletmesi İçin Davetiyeniz Var - Personelim",
                    Body = GetInvitationEmailBody(businessName, inviterName, invitationCode, message),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Davetiye emaili gönderimi başarısız: {Email}", email);
                return false;
            }
        }

        private string GetInvitationEmailBody(string businessName, string inviterName, string code, string message)
        {
            // Mesaj varsa gösterelim, yoksa boş geçelim
            string messageHtml = string.IsNullOrEmpty(message) 
                ? "" 
                : $"<div style='background-color: #fff3cd; padding: 10px; margin: 10px 0; border-left: 4px solid #ffc107; font-style: italic;'>\"{message}\"</div>";

            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background-color: #f9f9f9; }}
                        .container {{ max-width: 600px; margin: 20px auto; padding: 30px; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
                        .header {{ text-align: center; padding-bottom: 20px; border-bottom: 1px solid #eee; }}
                        .content {{ padding: 20px 0; text-align: center; }}
                        .business-name {{ color: #2c3e50; font-weight: bold; }}
                        .code-box {{ background-color: #e8f0fe; padding: 20px; text-align: center; border-radius: 8px; margin: 25px 0; border: 1px dashed #1a73e8; }}
                        .code {{ font-size: 36px; font-weight: bold; letter-spacing: 8px; color: #1a73e8; font-family: monospace; }}
                        .info-text {{ color: #555; margin-bottom: 10px; }}
                        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #888; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>İşletme Daveti</h2>
                        </div>
                        <div class='content'>
                            <p class='info-text'>Merhaba,</p>
                            <p class='info-text'><strong>{inviterName}</strong> sizi <span class='business-name'>{businessName}</span> ekibine katılmaya davet etti.</p>
                            
                            {messageHtml}

                            <p>Daveti kabul etmek için aşağıdaki kodu uygulamaya giriniz:</p>
                            
                            <div class='code-box'>
                                <div class='code'>{code}</div>
                            </div>
                            
                            <p style='font-size: 14px; color: #666;'>Bu kod 7 gün boyunca geçerlidir.</p>
                        </div>
                        
                        <div class='footer'>
                            <p>Bu işlemi siz yapmadıysanız veya tanımadığınız bir yerden geldiyse, bu e-postayı görmezden gelebilirsiniz.</p>
                            <p>&copy; 2024 Personelim. Tüm hakları saklıdır.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";
        }

        public async Task<bool> SendAccountCreatedEmailAsync(string email, string firstName, string plainPassword)
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
                    Subject = "Personelim Hesabınız Oluşturuldu",
                    Body = GetAccountCreatedEmailBody(firstName, email, plainPassword),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);
                await client.SendMailAsync(mailMessage);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hesap oluşturma maili gönderimi başarısız: {Email}", email);
                return false;
            }
        }

        private string GetAccountCreatedEmailBody(string firstName, string email, string plainPassword)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; }}
                        .container {{ max-width: 600px; margin: 20px auto; padding: 30px; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 15px rgba(0,0,0,0.1); }}
                        .header {{ text-align: center; padding-bottom: 20px; border-bottom: 2px solid #eee; }}
                        .header h2 {{ color: #2c3e50; margin: 0; }}
                        .content {{ padding: 30px 20px; text-align: center; }}
                        .credentials-box {{ background-color: #eef2f5; border: 1px solid #dce1e6; border-radius: 8px; padding: 20px; margin: 25px 0; text-align: left; display: inline-block; min-width: 250px; }}
                        .credential-item {{ margin-bottom: 10px; }}
                        .credential-label {{ color: #7f8c8d; font-size: 0.9em; font-weight: 600; display: block; margin-bottom: 4px; }}
                        .credential-value {{ color: #2c3e50; font-size: 1.1em; font-weight: 500; font-family: monospace; background: #fff; padding: 5px 10px; border-radius: 4px; border: 1px solid #eee; }}
                        .password-value {{ color: #e74c3c; letter-spacing: 1px; }}
                        .footer {{ margin-top: 30px; font-size: 12px; color: #95a5a6; text-align: center; border-top: 1px solid #eee; padding-top: 20px; }}
                        .btn {{ display: inline-block; background-color: #3498db; color: white; padding: 12px 25px; text-decoration: none; border-radius: 25px; font-weight: bold; margin-top: 20px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Hoşgeldiniz!</h2>
                        </div>
                        <div class='content'>
                            <p>Sayın <strong>{firstName}</strong>,</p>
                            <p>Personelim uygulamasındaki hesabınız yönetici tarafından başarıyla oluşturulmuştur.</p>
                            
                            <p>Aşağıdaki bilgileri kullanarak sisteme giriş yapabilir ve şirketinizi kurabilirsiniz:</p>
                            
                            <div class='credentials-box'>
                                <div class='credential-item'>
                                    <span class='credential-label'>E-posta Adresi:</span>
                                    <div class='credential-value'>{email}</div>
                                </div>
                                <div class='credential-item' style='margin-bottom: 0;'>
                                    <span class='credential-label'>Geçici Şifre:</span>
                                    <div class='credential-value password-value'>{plainPassword}</div>
                                </div>
                            </div>
                            
                            <p style='color: #e74c3c; font-size: 0.9em;'>Güvenliğiniz için giriş yaptıktan sonra şifrenizi değiştirmenizi öneririz.</p>
                        </div>
                        
                        <div class='footer'>
                            <p>Bu otomatik bir bilgilendirme e-postasıdır.</p>
                            <p>&copy; 2024 Personelim</p>
                        </div>
                    </div>
                </body>
                </html>
            ";
        }
    }
}
    
