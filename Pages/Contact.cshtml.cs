using BestShop.MyHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Numerics;

namespace BestShop.Pages
{
    public class ContactModel : PageModel
    {
        public void OnGet()
        {
        }

        [BindProperty, Required(ErrorMessage = "The First Name is required")]
        [Display(Name = "First Name*")]
		public string FirstName { get; set; } = "";

		[BindProperty,Required(ErrorMessage = "The Last Name is required")]
		[Display(Name = "Last Name*")]
		public string LastName { get; set; } = "";

		[BindProperty, Required(ErrorMessage = "Email is required"), EmailAddress]
		[Display(Name = "Email*")]
		public string Email { get; set; } = "";

		[BindProperty]
		public string? Phone { get; set; } = "";

		[BindProperty, Required(ErrorMessage = "Subject is required")]
		[Display(Name = "Subject*")]
		public string Subject { get; set; } = "";

        [BindProperty, Required(ErrorMessage = "Message is required")]
        [MinLength(5, ErrorMessage = "Message should be at least 5 characters")]
        [MaxLength(1024, ErrorMessage = "Message should not be less than 1024 characters")]
		[Display(Name = "Message*")]
		public string Message { get; set; } = "";

        public List<SelectListItem> SubjectList { get; } = new List<SelectListItem>
        {
          new SelectListItem {Value = "Order Status", Text = "Order Status"},
          new SelectListItem {Value = "Refund Request", Text = "Refund Request"},
          new SelectListItem {Value = "Job Application", Text = "Job Application"},
          new SelectListItem {Value = "Other", Text = "Other"},
        };

		public string SuccessMessage { get; set; } = "";
		public string ErrorMessage { get; set; } = "";

		public void OnPost() 
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please fill all required fields";
                return;
            }

            if (Phone == null) Phone = "";

            //Add this message to DB

            try
            {
                string conectionString = "Data Source=localhost;Initial Catalog=bestshop;Integrated Security=True";

                using (SqlConnection connection = new SqlConnection(conectionString))
                {
                    connection.Open();
                    string sql = "INSERT INTO messages " +
                        "(firstname, lastname, email, phone, subject, message) VALUES " +
                        "(@firstname, @lastname, @email, @phone, @subject, @message);";

                    using (SqlCommand command = new SqlCommand(sql, connection)) 
                    {
                        command.Parameters.AddWithValue("@firstname", FirstName);
						command.Parameters.AddWithValue("@lastname", LastName);
						command.Parameters.AddWithValue("@email", Email);
						command.Parameters.AddWithValue("@phone", Phone);
						command.Parameters.AddWithValue("@subject", Subject);
						command.Parameters.AddWithValue("@message", Message);

                        command.ExecuteNonQuery();
					}
                    
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return;
            }

            //Send confirmation Email to the client

            string userName = $"{FirstName} {LastName}";
            string emailSubject = "About your message";
            string emailMessage = "Dear " + userName + ",\nWe recived your message. Thank you for contacting us.\n" +
                "Our team will contact you very soon.\nBest Regards\n\nYour message:\n" + Message;

            EmailSender.SendEmail(Email, userName, emailSubject, emailMessage).Wait();

            ///////////////////////
            ///
            SuccessMessage = "Your message has been recived corectly";

            FirstName = "";
            LastName = "";
            Email = "";
            Phone = "";
            Subject = "";
            Message = "";

            ModelState.Clear();
		}

    }
}
