using BestShop.Pages.Admin.Books;
using BestShop.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

namespace BestShop.Pages
{
    [BindProperties(SupportsGet = true)]
    public class BooksModel : PageModel
    {
        public string? Search { get; set; }
        public string PriceRange { get; set; } = "any";
        public string PageRange { get; set; } = "any";
        public string Category { get; set; } = "any";

        public List<BookInfo> listBooks = new List<BookInfo>();


        public int page = 1; // the current html page
        public int totalPages = 0;
        private readonly int pageSize = 5; // books per page

        public void OnGet()
        {
            page = 1;
            string requestPage = Request.Query["page"];
            if (requestPage != null)
            {
                try
                {
                    page = int.Parse(requestPage);
                }
                catch (Exception)
                {
                    page = 1;
                }
            }

            try
            {
                string connectionString = "Data Source=localhost;Initial Catalog=bestshop;Integrated Security=True";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string sqlCount = "SELECT COUNT(*) FROM books";
                    sqlCount += " WHERE (title LIKE @search OR authors LIKE @search)";

                    if (PriceRange.Equals("0_50"))
                    {
                        sqlCount += " AND price <= 50";
                    }
                    else if (PriceRange.Equals("50_100"))
                    {
                        sqlCount += " AND price >= 50 AND price <= 100";
                    }
                    else if (PriceRange.Equals("above100"))
                    {
                        sqlCount += " AND price >= 100";
                    }


                    if (PageRange.Equals("0_100"))
                    {
                        sqlCount += " AND num_pages <= 100";
                    }
                    else if (PageRange.Equals("100_299"))
                    {
                        sqlCount += " AND num_pages >= 100 AND num_pages <= 299";
                    }
                    else if (PageRange.Equals("above300"))
                    {
                        sqlCount += " AND num_pages >= 300";
                    }


                    if (!Category.Equals("any"))
                    {
                        sqlCount += " AND category=@category";
                    }

                    using (SqlCommand command = new SqlCommand(sqlCount, connection))
                    {
                        command.Parameters.AddWithValue("@search", "%" + Search + "%");
                        command.Parameters.AddWithValue("@category", Category);

                        decimal count = (int)command.ExecuteScalar();
                        totalPages = (int)Math.Ceiling(count / pageSize);
                    }



                    string sql = "SELECT * FROM books";
                    sql += " WHERE (title LIKE @search OR authors LIKE @search)";

                    if (PriceRange.Equals("0_50"))
                    {
                        sql += " AND price <= 50";
                    }
                    else if (PriceRange.Equals("50_100"))
                    {
                        sql += " AND price >= 50 AND price <= 100";
                    }
                    else if (PriceRange.Equals("above100"))
                    {
                        sql += " AND price >= 100";
                    }


                    if (PageRange.Equals("0_100"))
                    {
                        sql += " AND num_pages <= 100";
                    }
                    else if (PageRange.Equals("100_299"))
                    {
                        sql += " AND num_pages >= 100 AND num_pages <= 299";
                    }
                    else if (PageRange.Equals("above300"))
                    {
                        sql += " AND num_pages >= 300";
                    }


                    if (!Category.Equals("any"))
                    {
                        sql += " AND category=@category";
                    }

                    sql += " ORDER BY id DESC";
                    sql += " OFFSET @skip ROWS FETCH NEXT @pageSize ROWS ONLY";


                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@search", "%" + Search + "%");
                        command.Parameters.AddWithValue("@category", Category);
                        command.Parameters.AddWithValue("@skip", (page - 1) * pageSize);
                        command.Parameters.AddWithValue("@pageSize", pageSize);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                BookInfo bookInfo = new BookInfo();

                                bookInfo.Id = reader.GetInt32(0);
                                bookInfo.Title = reader.GetString(1);
                                bookInfo.Authors = reader.GetString(2);
                                bookInfo.Isbn = reader.GetString(3);
                                bookInfo.NumPages = reader.GetInt32(4);
                                bookInfo.Price = reader.GetDecimal(5);
                                bookInfo.Category = reader.GetString(6);
                                bookInfo.Description = reader.GetString(7);
                                bookInfo.ImageFileName = reader.GetString(8);
                                bookInfo.CreatedAt = reader.GetDateTime(9).ToString("MM/dd/yyyy");

                                listBooks.Add(bookInfo);
                            }
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
