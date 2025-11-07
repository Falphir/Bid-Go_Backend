using Bid_Go_Backend.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Email
{
    /// <summary>
    /// SMTP email sender implementation using configuration-provided credentials.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;

        public EmailService(IConfiguration configuration)
        {
            _smtpHost = configuration["SmtpSettings:Host"];
            _smtpPort = int.Parse(configuration["SmtpSettings:Port"]);
            _smtpUser = configuration["SmtpSettings:User"];
            _smtpPass = configuration["SmtpSettings:Pass"];
        }

        /// <summary>
        /// Send an email message using SMTP over SSL.
        /// </summary>
        /// <param name="to">Recipient address.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="body">Email body (HTML allowed).</param>
        public virtual async Task SendEmailAsync(string to, string subject, string body)
        {
            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUser, _smtpPass),
                EnableSsl = true
            };

            var mail = new MailMessage(_smtpUser, to, subject, body)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(mail);
        }
    }
}
