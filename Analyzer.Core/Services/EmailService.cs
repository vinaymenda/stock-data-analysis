using System.Net;
using System.Net.Mail;

namespace Analyzer.Core.Services
{
    public class EmailService
    {
        public static async System.Threading.Tasks.Task SendAsync(MailMessage message)
        {            
            var fromAddress = new MailAddress(ConfigurationSettings.Instance.Get("email:username"));            
            var fromPassword = ConfigurationSettings.Instance.Get("email:password");
            message.From = fromAddress;

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };
            await smtp.SendMailAsync(message);
        }
    }
}
