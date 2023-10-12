using System.Data.SqlClient;

namespace BestShop.Pages.Admin.Orders
{
    public class OrderInfo
    {
        public int id;
        public int clientId;
        public string orderDate;
        public decimal shippingFee;
        public string deliveryAddress;
        public string paymentMethod;
        public string paymentStatus;
        public string orderStatus;

        public List<OrderItemInfo> items = new List<OrderItemInfo>();

        public static List<OrderItemInfo> getOrderItems(int orderId)
        {
            List<OrderItemInfo> items = new List<OrderItemInfo>();

            try
            {
                string connectionString = "Data Source=localhost;Initial Catalog=bestshop;Integrated Security=True";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sql = "SELECT order_items.*, books.* FROM order_items, books " +
                        "WHERE order_items.order_id=@order_id AND order_items.book_id = books.id";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@order_id", orderId);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                OrderItemInfo item = new OrderItemInfo();

                                item.id = reader.GetInt32(0);
                                item.orderId = reader.GetInt32(1);
                                item.bookId = reader.GetInt32(2);
                                item.quantity = reader.GetInt32(3);
                                item.unitPrice = reader.GetDecimal(4);

                                item.bookInfo.Id = reader.GetInt32(5);
                                item.bookInfo.Title = reader.GetString(6);
                                item.bookInfo.Authors = reader.GetString(7);
                                item.bookInfo.Isbn = reader.GetString(8);
                                item.bookInfo.NumPages = reader.GetInt32(9);
                                item.bookInfo.Price = reader.GetDecimal(10);
                                item.bookInfo.Category = reader.GetString(11);
                                item.bookInfo.Description = reader.GetString(12);
                                item.bookInfo.ImageFileName = reader.GetString(13);
                                item.bookInfo.CreatedAt = reader.GetDateTime(14).ToString("MM/dd/yyyy");

                                items.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return items;
        }
    }
}
        
    

    

