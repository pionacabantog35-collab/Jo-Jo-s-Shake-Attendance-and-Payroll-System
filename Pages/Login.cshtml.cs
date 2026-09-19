using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shakepayrollsystem.Data;
using System.Data;
using System.Threading.Tasks;
using System;

namespace Shakepayrollsystem.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public void OnGet()
        {
            // Clear any existing session when loading login page
            HttpContext.Session.Clear();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Please enter both username and password.";
                return Page();
            }

            try
            {
                DatabaseHelper db = new DatabaseHelper();
                
                var parameters = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "@username", Username },
                    { "@password", Password }
                };

                DataTable dt = await Task.Run(() => 
                    db.GetData("SELECT * FROM tblaccounts WHERE username = @username AND password = @password", parameters)
                );

                if (dt.Rows.Count > 0)
                {
                    string usertype = dt.Rows[0]["usertype"]?.ToString() ?? "";

                    // Store user info in session (photo removed)
                    HttpContext.Session.SetString("Username", Username);
                    HttpContext.Session.SetString("UserType", usertype);

                    // Redirect based on user type
                    if (usertype == "ADMIN")
                    {
                        return RedirectToPage("/dashboard");
                    }
                    else if (usertype == "STAFF")
                    {
                        return RedirectToPage("/staffdashboard");
                    }
                    else
                    {
                        ErrorMessage = "Invalid user type.";
                        return Page();
                    }
                }
                else
                {
                    ErrorMessage = "Incorrect username or password.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Database connection error: " + ex.Message;
            }

            return Page();
        }
    }
}