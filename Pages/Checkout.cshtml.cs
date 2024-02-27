using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using BestShop.Pages.Shared;

namespace BestShop.Pages
{
    [IgnoreAntiforgeryToken]
    public class CheckoutModel : PageModel
    {
        public string PaypalClientId { get; set; } = "";
        private string PaypalSecret { get; set; } = "";
        public string PaypalUrl { get; set; } = "";

        public decimal shippingFee = 5;
        public string DeliveryAddress { get; set; } = "";
        public string Total { get; set; } = "";
        public string ProductIdentifiers { get; set; } = "";
        public string PaymentMethod { get; set; } = "";

        BookInfo bookInfo;

        public CheckoutModel(IConfiguration configuration)
        {
            PaypalClientId = configuration["PaypalSettings:ClientId"]!;
            PaypalSecret = configuration["PaypalSettings:Secret"]!;
            PaypalUrl = configuration["PaypalSettings:Url"]!;

            bookInfo = new BookInfo();
        }

        public void UpdatePaymentStatus(string paymentStatus, int client_id)
        {
            try
            {
                string connectionString = "Data Source=localhost;Initial Catalog=bestshop;Integrated Security=True";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    int newOrderId = 0;
                    string sqlOrder = "UPDATE orders SET payment_status=@payment_status WHERE client_id=@client_id";

                    using (SqlCommand command = new SqlCommand(sqlOrder, connection))
                    {
                        command.Parameters.AddWithValue("@client_id", client_id);
                        command.Parameters.AddWithValue("@payment_status", paymentStatus);

                        newOrderId = (int)command.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void OnGet()
        {
            DeliveryAddress = TempData["DeliveryAddress"]?.ToString() ?? "";
            Total = TempData["Total"]?.ToString() ?? "";
            ProductIdentifiers = TempData["ProductIdentifiers"]?.ToString() ?? "";
            PaymentMethod = TempData["PaymentMethod"]?.ToString() ?? "";

            TempData.Keep();

            if (DeliveryAddress == "" || Total == "" || ProductIdentifiers == "")
            {
                Response.Redirect("/");
                return;
            }
        }

        public JsonResult OnPostCreateOrder()
        {
            int client_id = HttpContext.Session.GetInt32("id") ?? 0;

            string cookieValue = Request.Cookies["shopping_cart"] ?? "";

            var bookDictionary = bookInfo.getBookDictionary(cookieValue);

            DeliveryAddress = TempData["DeliveryAddress"]?.ToString() ?? "";
            Total = TempData["Total"]?.ToString() ?? "";
            ProductIdentifiers = TempData["ProductIdentifiers"]?.ToString() ?? "";
            PaymentMethod = TempData["PaymentMethod"]?.ToString() ?? "";

            TempData.Keep();

            if (DeliveryAddress == "" || Total == "" || ProductIdentifiers == "")
            {
                return new JsonResult("");
            }


            // create the request body
            JsonObject createOrderRequest = new JsonObject();
            createOrderRequest.Add("intent", "CAPTURE");

            JsonObject amount = new JsonObject();
            amount.Add("currency_code", "USD");
            amount.Add("value", Total);

            JsonObject purchaseUnit1 = new JsonObject();
            purchaseUnit1.Add("amount", amount);

            JsonArray purchaseUnits = new JsonArray();
            purchaseUnits.Add(purchaseUnit1);

            createOrderRequest.Add("purchase_units", purchaseUnits);


            // get access token
            string accessToken = GetPaypalAccessToken();


            // send request
            string url = PaypalUrl + "/v2/checkout/orders";

            string orderId = "";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                requestMessage.Content = new StringContent(createOrderRequest.ToString(), null, "application/json");

                var responseTask = client.SendAsync(requestMessage);
                responseTask.Wait();

                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadAsStringAsync();
                    readTask.Wait();

                    var strResponse = readTask.Result;
                    var jsonResponse = JsonNode.Parse(strResponse);
                    if (jsonResponse != null)
                    {
                        orderId = jsonResponse["id"]?.ToString() ?? "";

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
                                    command.Parameters.AddWithValue("@delivery_address", DeliveryAddress);
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
                                    decimal unitPrice = bookInfo.getBookPrice(bookID);

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
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }


            var response = new
            {
                Id = orderId
            };
            return new JsonResult(response);
        }


        public JsonResult OnPostCompleteOrder([FromBody] JsonObject data)
        {

            if (data == null || data["orderID"] == null) return new JsonResult("");

            int client_id = HttpContext.Session.GetInt32("id") ?? 0;

            var orderID = data["orderID"]!.ToString();


            // get access token
            string accessToken = GetPaypalAccessToken();


            string url = PaypalUrl + "/v2/checkout/orders/" + orderID + "/capture";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                requestMessage.Content = new StringContent("", null, "application/json");

                var responseTask = client.SendAsync(requestMessage);
                responseTask.Wait();

                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadAsStringAsync();
                    readTask.Wait();

                    var strResponse = readTask.Result; Console.WriteLine("Paypal complete order success - response: " + strResponse);

                    var jsonResponse = JsonNode.Parse(strResponse);
                    if (jsonResponse != null)
                    {
                        string paypalOrderStatus = jsonResponse["status"]?.ToString() ?? "";
                        if (paypalOrderStatus == "COMPLETED")
                        {
                            // clear TempData
                            TempData.Clear();

                            // update payment status in the database => "accepted"

                            UpdatePaymentStatus("accepted", client_id);

                            // clear cookie
                            Response.Cookies.Delete("shopping_cart");

                            return new JsonResult("success");
                        }
                    }
                }
            }

            return new JsonResult("");
        }

        public JsonResult OnPostCancelOrder([FromBody] JsonObject data)
        {
            if (data == null || data["orderID"] == null) return new JsonResult("");

            int client_id = HttpContext.Session.GetInt32("id") ?? 0;

            var orderID = data["orderID"]!.ToString();

            // update payment status in the database => "canceled"

            UpdatePaymentStatus("canceled", client_id);

            return new JsonResult("");
        }

        private string GetPaypalAccessToken()
        {
            string accessToken = "";

            string url = PaypalUrl + "/v1/oauth2/token";

            using (var client = new HttpClient())
            {
                string credentials64 =
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(PaypalClientId + ":" + PaypalSecret));

                client.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials64);

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                requestMessage.Content = new StringContent("grant_type=client_credentials", null
                    , "application/x-www-form-urlencoded");

                var responseTask = client.SendAsync(requestMessage);
                responseTask.Wait();

                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadAsStringAsync();
                    readTask.Wait();

                    var strResponse = readTask.Result;

                    var jsonResponse = JsonNode.Parse(strResponse);
                    if (jsonResponse != null)
                    {
                        accessToken = jsonResponse["access_token"]?.ToString() ?? "";
                    }
                }
            }
            Console.WriteLine("JWT: " + accessToken);
            return accessToken;
        }
    }
}
