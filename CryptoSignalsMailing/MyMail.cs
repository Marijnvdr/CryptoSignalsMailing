using System;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace CryptoSignalsMailing
{
    public class MyMail
    {
        // sendGrid account: c59otrza
        public static async Task SendEmail(string emailAddressTo, string emailAddressFrom, string subject, string text)
        {
            var client = new SendGridClient("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");
            var from = new EmailAddress(emailAddressFrom, "My Crypto Signals");
            var to = new EmailAddress(emailAddressTo);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, text, text);
            var response = await client.SendEmailAsync(msg);
        }
    }
}
