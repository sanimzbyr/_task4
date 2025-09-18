using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

public class CheckUserStatusAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var path = context.HttpContext.Request.Path.Value?.ToLower() ?? string.Empty;

        // Skip login/register actions
        if (path.Contains("/account/login") || path.Contains("/account/register"))
            return;

        var userEmail = context.HttpContext.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        var db = context.HttpContext.RequestServices.GetService(typeof(AppDbContext)) as AppDbContext;
        if (db == null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        var user = db.Users.FirstOrDefault(u => u.Email == userEmail);

        if (user == null || user.Status == UserStatus.Blocked || user.Status == UserStatus.Deleted)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }
    }
}
