using BestShop.Pages.Admin.Books;
using BestShop.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;

namespace BestShop.Pages
{
    [BindProperties]
    public class CartModel : PageModel
    {
        [Required(ErrorMessage = "The Address is required")]
        public string Address { get; set; } = "";

        [Required]
        public string PaymentMethod { get; set; } = "";

        public List<OrderItem> listOrderItems = new List<OrderItem>();
        public decimal subtotal = 0;
        public decimal shippingFee = 5;
        public decimal total = 0;

        private Dictionary<String, int> getBookDictionary()
        {
            var bookDictionary = new Dictionary<string, int>();

            // Read shopping cart items from cookie
            string cookieValue = Request.Cookies["shopping_cart"] ?? "";

            if (cookieValue.Length > 0)
            {
                string[] bookIdArray = cookieValue.Split('-');

                for (int i = 0; i < bookIdArray.Length; i++)
                {
                    string bookId = bookIdArray[i];
                    if (bookDictionary.ContainsKey(bookId))
                    {
                        bookDictionary[bookId] += 1;
                    }
                    else
                    {
                        bookDictionary.Add(bookId, 1);
                    }
                }
            }

            return bookDictionary;
        }

        public void OnGet()
        {
            // Read shopping cart items from cookie
            var bookDictionary = getBookDictionary();


            // action can be null, "add", "sub" or "delete"
            string? action = Request.Query["action"];
            string? id = Request.Query["id"];

            if (action != null && id != null && bookDictionary.ContainsKey(id))
            {
                if (action.Equals("add"))
                {
                    bookDictionary[id] += 1;
                }
                else if (action.Equals("sub"))
                {
                    if (bookDictionary[id] > 1) bookDictionary[id] -= 1;
                }
                else if (action.Equals("delete"))
                {
                    bookDictionary.Remove(id);
                }


                // build the new cookie value
                string newCookieValue = "";
                foreach (var keyValuePair in bookDictionary)
                {
                    for (int i = 0; i < keyValuePair.Value; i++)
                    {
                        newCookieValue += "-" + keyValuePair.Key;
                    }
                }

                if (newCookieValue.Length > 0)
                    newCookieValue = newCookieValue.Substring(1);

                var cookieOptions = new CookieOptions();
                cookieOptions.Expires = DateTime.Now.AddDays(365);
                cookieOptions.Path = "/";

                Response.Cookies.Append("shopping_cart", newCookieValue, cookieOptions);

                // redirect to the same page:
                //   - to remove the query string from the url
                //   - to set the shopping cart size using the updated cookie
                Response.Redirect(Request.Path.ToString());
                return;
            }

            try
            {
                string connectionString = "Data Source=localhost;Initial Catalog=bestshop;Integrated Security=True";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string sql = "SELECT * FROM books WHERE id=@id";
                    foreach (var keyValuePair in bookDictionary)
                    {
                        string bookID = keyValuePair.Key;
                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@id", bookID);

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    OrderItem item = new OrderItem();

                                    item.bookInfo.Id = reader.GetInt32(0);
                                    item.bookInfo.Title = reader.GetString(1);
                                    item.bookInfo.Authors = reader.GetString(2);
                                    item.bookInfo.Isbn = reader.GetString(3);
                                    item.bookInfo.NumPages = reader.GetInt32(4);
                                    item.bookInfo.Price = reader.GetDecimal(5);
                                    item.bookInfo.Category = reader.GetString(6);
                                    item.bookInfo.Description = reader.GetString(7);
                                    item.bookInfo.ImageFileName = reader.GetString(8);
                                    item.bookInfo.CreatedAt = reader.GetDateTime(9).ToString("MM/dd/yyyy");

                                    item.numCopies = keyValuePair.Value;
                                    item.totalPrice = item.numCopies * item.bookInfo.Price;

                                    listOrderItems.Add(item);


                                    subtotal += item.totalPrice;
                                    total = subtotal + shippingFee;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Address = HttpContext.Session.GetString("address") ?? "";

            TempData["Total"] = total.ToString();
            TempData["ProductIdentifiers"] = "";
            TempData["DeliveryAddress"] = "";
        }


        public string errorMessage = "";
        public string successMessage = "";

        public void OnPost()
        {
            int client_id = HttpContext.Session.GetInt32("id") ?? 0;
            if (client_id < 1)
            {
                Response.Redirect("/Auth/Login");
                return;
            }

            if (!ModelState.IsValid)
            {
                errorMessage = "Data validation failed";
                return;
            }

            // Read shopping cart items from cookie
            var bookDictionary = getBookDictionary();

            if (bookDictionary.Count < 1)
            {
                errorMessage = "Your cart is empty";
                return;
            }


            string productIdentifiers = Request.Cookies["shopping_cart"] ?? "";
            TempData["ProductIdentifiers"] = productIdentifiers;
            TempData["DeliveryAddress"] = Address;

            if (PaymentMethod == "credit_cart" || PaymentMethod == "paypal")
            {
                Response.Redirect("/Checkout");
                return;
            }


            // save the order in the database
            try
            {
                string connectionString = "Data Source=localhost;Initial Catalog=bestshop;Integrated Security=True";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // create a new order in the orders table
                    int newOrderId = 0;
                    string sqlOrder = "INSERT INTO orders (client_id, order_date, shipping_fee, " +
                        "delivery_address, payment_method, payment_status, order_status) " +
                        "OUTPUT INSERTED.id " +
                        "VALUES (@client_id, CURRENT_TIMESTAMP, @shipping_fee, " +
                        "@delivery_address, @payment_method, 'pending', 'created')";

                    using (SqlCommand command = new SqlCommand(sqlOrder, connection))
                    {
                        command.Parameters.AddWithValue("@client_id", client_id);
                        command.Parameters.AddWithValue("@shipping_fee", shippingFee);
                        command.Parameters.AddWithValue("@delivery_address", Address);
                        command.Parameters.AddWithValue("@payment_method", PaymentMethod);

                        newOrderId = (int)command.ExecuteScalar();
                    }


                    // add the ordered books to the order_items table
                    string sqlItem = "INSERT INTO order_items (order_id, book_id, quantity, unit_price) " +
                        "VALUES (@order_id, @book_id, @quantity, @unit_price)";

                    foreach (var keyValuePair in bookDictionary)
                    {
                        string bookID = keyValuePair.Key;
                        int quantity = keyValuePair.Value;
                        decimal unitPrice = getBookPrice(bookID);

                        using (SqlCommand command = new SqlCommand(sqlItem, connection))
                        {
                            command.Parameters.AddWithValue("@order_id", newOrderId);
                            command.Parameters.AddWithValue("@book_id", bookID);
                            command.Parameters.AddWithValue("@quantity", quantity);
                            command.Parameters.AddWithValue("@unit_price", unitPrice);

                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return;
            }

            //Delete the Cookie "shopping_cart" from Browser.
            Response.Cookies.Delete("shopping_cart");

            successMessage = "Order created successfully";
        }


        private decimal getBookPrice(string bookID)
        {
            decimal price = 0;

            try
            {
                string connectionString = "Data Source=localhost;Initial Catalog=bestshop;Integrated Security=True";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sql = "SELECT price FROM books WHERE id=@id";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@id", bookID);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                price = reader.GetDecimal(0);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

            }

            return price;
        }
    }

    public class OrderItem
    {
        public BookInfo bookInfo = new BookInfo();
        public int numCopies = 0;
        public decimal totalPrice = 0;
    }
}
