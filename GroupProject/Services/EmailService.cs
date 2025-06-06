using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net.Mail;

namespace GroupProject.Services
{
    public static class EmailService
    {
        private const string SendGridApiKey = "Please add your SendGrid API key here";

        public static async Task<bool> SendMfaCodeEmailAsync(string recipientEmail, string code)
        {
            var client = new SendGridClient(SendGridApiKey);
            var from = new EmailAddress("charlesgeorgeclarke@gmail.com", "Group 3");
            var subject = "Your MFA Verification Code";
            var to = new EmailAddress(recipientEmail);
            var plainTextContent = $"Your verification code is: {code}";
            var htmlContent = $"<strong>Your verification code is: {code}</strong>";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            var response = await client.SendEmailAsync(msg);
            return response.IsSuccessStatusCode;
        }
    }
}
