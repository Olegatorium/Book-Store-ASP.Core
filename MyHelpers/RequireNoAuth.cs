using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BestShop.MyHelpers
{
	public class RequireNoAuth : Attribute, IPageFilter
	{
		public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
		{
			
		}

		public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
		{
			string? role = context.HttpContext.Session.GetString("role");

			if (role != null)
			{
				context.Result = new RedirectResult("/");
			}
		}

		public void OnPageHandlerSelected(PageHandlerSelectedContext context)
		{
			
		}
	}
}
