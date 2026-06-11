using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Celleseum.Web.Pages.Account;

public class LoginModel : PageModel
{
    [FromQuery(Name = "error")]
    public string? Error { get; set; }

    [FromQuery(Name = "message")]
    public string? Message { get; set; }

    public void OnGet()
    {
    }
}
