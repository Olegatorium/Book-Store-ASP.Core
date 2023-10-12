using BestShop.MyHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;

namespace BestShop.Pages.Admin.Books
{
    [RequireAuth(RequiredRole ="admin")]
    public class CreateModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "The Title is required")]
        [MaxLength(100, ErrorMessage = "The Title cannot exceed 100 characters")]
        public string Title { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage = "The Authors is required")]
        [MaxLength(100, ErrorMessage = "The Authors cannot exceed 100 characters")]
        public string Authors { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage = "The ISBN is required")]
        [MaxLength(20, ErrorMessage = "The ISBN cannot exceed 20 characters")]
        public string Isbn { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage = "The Number of Pages is required")]
        [Range(25,10000, ErrorMessage ="The Number of Pages must be in the range from 25 to 10000")]
        public int NumPages { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "The Price is required")]
        public decimal Price { get; set; } 

        [BindProperty]
        [Required]
        public string Category { get; set; } = "";

        [BindProperty]
        [MaxLength(1000, ErrorMessage = "The Description cannot exceed 1000 characters")]
        public string? Description { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage = "The Image is required")]
        public IFormFile ImageFile { get; set; }

        public string errorMessage = "";
        public string successMessage = "";

        private IWebHostEnvironment _webHostEnvironment;

        public CreateModel(IWebHostEnvironment env) 
        {
            _webHostEnvironment = env;
        }

        public void OnGet()
        {
        }

        public void OnPost()
        {
            if (!ModelState.IsValid)
            {
                errorMessage = "Data validation failed";
                return;
            }

            //succesfull data validation

            if (Description == null) Description = "";

            //save the image file on the server

            string newFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            newFileName += Path.GetExtension(ImageFile.FileName);

            string imageFolder = _webHostEnvironment.WebRootPath + "/images/books/";

            string imageFullPath = Path.Combine(imageFolder, newFileName);

            using (var stream = System.IO.File.Create(imageFullPath)) 
            {
                ImageFile.CopyTo(stream);
            }

            //save the new book in the DataBase

            try
            {
                string connectionString = "Data Source=localhost;Initial Catalog=bestshop;Integrated Security=True";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sql = "INSERT INTO books " +
                    "(title, authors, isbn, num_pages, price, category, description, image_filename) VALUES " +
                    "(@title, @authors, @isbn, @num_pages, @price, @category, @description, @image_filename);";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@title", Title);
                        command.Parameters.AddWithValue("@authors", Authors);
                        command.Parameters.AddWithValue("@isbn", Isbn);
                        command.Parameters.AddWithValue("@num_pages", NumPages);
                        command.Parameters.AddWithValue("@price", Price);
                        command.Parameters.AddWithValue("@category", Category);
                        command.Parameters.AddWithValue("@description", Description);
                        command.Parameters.AddWithValue("@image_filename", newFileName);

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return;
            }

            successMessage = "Data saved correctly";
            Response.Redirect("/Admin/Books/Index");
        }
    }
}
