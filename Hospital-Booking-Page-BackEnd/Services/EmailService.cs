using System.Net;
using System.Web;
using Hospital_Booking_Page_BackEnd.helpers;
using Hospital_Booking_Page_BackEnd.Models;
using MailKit.Net.Smtp;
using MimeKit;

namespace Hospital_Booking_Page_BackEnd.Services
{
    public class EmailService : IEmailService
    {

        private readonly IConfiguration _config;

        public EmailService(IConfiguration configuration)
        {
            _config = configuration;
        }

        public void SendEmail(EmailModel emailModel)
        {
            var emailMessage = new MimeMessage();
            var from = _config["EmailSettings:From"]; 
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var password = _config["EmailSettings:Password"];

            emailMessage.From.Add(new MailboxAddress("Hospital Booking", from));
            emailMessage.To.Add(new MailboxAddress(emailModel.To, emailModel.To));
            emailMessage.Subject = emailModel.Subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = emailModel.Content };

            using (var client = new SmtpClient())
            {
                try
                {
                    client.Connect(smtpServer, 465, true); 
                    client.Authenticate(from, password);
                    client.Send(emailMessage);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Failed to send email.", ex);
                }
                finally
                {
                    client.Disconnect(true);
                    client.Dispose();
                }
            }
        }
        
    }
}