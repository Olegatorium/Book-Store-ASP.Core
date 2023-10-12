using BestShop.MyHelpers;
using BestShop.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

namespace BestShop.Pages.Admin.Books
{
	[RequireAuth(RequiredRole = "admin")]
	public class IndexModel : PageModel
	{
		public List<BookInfo> listBooks = new List<BookInfo>();

		public string search { get; set; } = "";

		public int page = 1;
		public int totalPages = 0; 
		private readonly int pageSize = 5;

		public string column = "id";
		public string order = "desc";

		public void OnGet()
		{
			search = Request.Query["search"];
			if (search == null)
			{
				search = "";
			}

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

			string[] validColumns = { "id", "title", "authors", "num_pages", "price", "category", "created_at" };

			column = Request.Query["column"];

			if (column == null || !validColumns.Contains(column))
			{
				column = "id";
			}

			order = Request.Query["order"];

            if (order == null || !order.Equals("asc"))
            {
                order = "desc";
            }

            try
			{
				string connectionString = "Data Source=localhost;Initial Catalog=bestshop;Integrated Security=True";

				using (SqlConnection connection = new SqlConnection(connectionString)) 
				{
					connection.Open();

					string sqlCount = "SELECT COUNT(*) FROM books";

					if (search.Length > 0)
					{
						sqlCount += " WHERE title LIKE @search OR authors LIKE @search";

                    }

                    using (SqlCommand command = new SqlCommand(sqlCount, connection)) 
					{
                        command.Parameters.AddWithValue("@search", "%" + search + "%");
                        decimal count = (int)command.ExecuteScalar();
                        totalPages = (int)Math.Ceiling(count / pageSize);
                    }

                    string sql = "SELECT * FROM books";

					if (!string.IsNullOrWhiteSpace(search))
					{
						sql += " WHERE title LIKE @search OR authors LIKE @search";
					}

					sql += $" ORDER BY {column} {order}";

                    sql += " OFFSET @skip ROWS FETCH NEXT @pageSize ROWS ONLY";

                    using (SqlCommand command = new SqlCommand(sql, connection))
					{
                        command.Parameters.AddWithValue("@skip", (page - 1) * pageSize);
                        command.Parameters.AddWithValue("@pageSize", pageSize);

                        command.Parameters.AddWithValue("@search", "%" + search + "%");

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

