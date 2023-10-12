using BestShop.MyHelpers;
using BestShop.Pages.Admin.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

namespace BestShop.Pages.Admin.Orders
{
    [RequireAuth(RequiredRole = "admin")]
    public class DetailsModel : PageModel
    {
        public OrderInfo orderInfo = new OrderInfo();
        public UserInfo userInfo = new UserInfo();

        public void OnGet(int id)
        {
            if (id < 1)
            {
                Response.Redirect("/Admin/Orders/Index");
                return;
            }

            string paymentStatus = Request.Query["payment_status"];
            string orderStatus = Request.Query["order_status"];

            try
            {
                string connectionString = "Data Source=localhost;Initial Catalog=bestshop;Integrated Security=True";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    if (paymentStatus != null)
                    {
                        string sqlUpdate = "UPDATE orders SET payment_status=@payment_status WHERE id=@id";
                        using (SqlCommand command = new SqlCommand(sqlUpdate, connection))
                        {
                            command.Parameters.AddWithValue("@payment_status", paymentStatus);
                            command.Parameters.AddWithValue("@id", id);

                            command.ExecuteNonQuery();
                        }
                    }


                    if (orderStatus != null)
                    {
                        string sqlUpdate = "UPDATE orders SET order_status=@order_status WHERE id=@id";
                        using (SqlCommand command = new SqlCommand(sqlUpdate, connection))
                        {
                            command.Parameters.AddWithValue("@order_status", orderStatus);
                            command.Parameters.AddWithValue("@id", id);

                            command.ExecuteNonQuery();
                        }
                    }


                    string sql = "SELECT * FROM orders WHERE id=@id";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                orderInfo.id = reader.GetInt32(0);
                                orderInfo.clientId = reader.GetInt32(1);
                                orderInfo.orderDate = reader.GetDateTime(2).ToString("MM/dd/yyyy");
                                orderInfo.shippingFee = reader.GetDecimal(3);
                                orderInfo.deliveryAddress = reader.GetString(4);
                                orderInfo.paymentMethod = reader.GetString(5);
                                orderInfo.paymentStatus = reader.GetString(6);
                                orderInfo.orderStatus = reader.GetString(7);

                                orderInfo.items = OrderInfo.getOrderItems(orderInfo.id);
                            }
                            else
                            {
                                Response.Redirect("/Admin/Orders/Index");
                                return;
                            }
                        }
                    }

                    sql = "SELECT * FROM users WHERE id=@id";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@id", orderInfo.clientId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                userInfo.id = reader.GetInt32(0);
                                userInfo.firstName = reader.GetString(1);
                                userInfo.lastName = reader.GetString(2);
                                userInfo.email = reader.GetString(3);
                                userInfo.phone = reader.GetString(4);
                                userInfo.address = reader.GetString(5);
                                userInfo.password = reader.GetString(6);
                                userInfo.role = reader.GetString(7);
                                userInfo.createdAt = reader.GetDateTime(8).ToString("MM/dd/yyyy");
                            }
                            else
                            {
                                Console.WriteLine("Client not found, id=" + orderInfo.clientId);
                                Response.Redirect("/Admin/Orders/Index");
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Response.Redirect("/Admin/Orders/Index");
            }
        }
    }
}
