using SendGrid;
using SendGrid.Helpers.Mail;

namespace BestShop.MyHelpers
{
	public class EmailSender
	{
		public static async Task SendEmail(string toEmail, string userName, string subject, string message) 
		{
			string apiKey = "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";

			var client = new SendGridClient(apiKey);

			var from = new EmailAddress("olegatorium@gmail.com", "BestShop.com");
			var to = new EmailAddress(toEmail, userName);
			var plainTextContent = message;
			var htmlContent = ""; 

			var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
			var response = await client.SendEmailAsync(msg);
			

		}
	}
}
