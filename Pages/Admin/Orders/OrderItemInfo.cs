using BestShop.Pages.Shared;

namespace BestShop.Pages.Admin.Orders
{
    public class OrderItemInfo
    {
        public int id;
        public int orderId;
        public int bookId;
        public int quantity;
        public decimal unitPrice;

        public BookInfo bookInfo = new BookInfo();

    }
}
