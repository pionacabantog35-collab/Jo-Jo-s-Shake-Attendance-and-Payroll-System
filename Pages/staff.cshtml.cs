using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shakepayrollsystem.Data;
using System.Data;
using System;
using System.Collections.Generic;

namespace Shakepayrollsystem.Pages
{
    public class staffModel : PageModel
    {
        private DatabaseHelper db = new DatabaseHelper();

        public string Username { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public DataTable StaffList { get; set; } = new DataTable();
        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            // Check if user is logged in
            Username = HttpContext.Session.GetString("Username") ?? string.Empty;
            UserType = HttpContext.Session.GetString("UserType") ?? string.Empty;

            // If no user in session or not admin, redirect to login
            if (string.IsNullOrEmpty(Username) || UserType != "ADMIN")
            {
                return RedirectToPage("/Login");
            }

            // Load staff list
            LoadStaffList();

            return Page();
        }

        private void LoadStaffList()
        {
            try
            {
                // Added firstName to the SELECT
                StaffList = db.GetData(@"
                    SELECT firstName, lastName, username, contact, email, rateperhour, datecreated, 
                           address, createdby 
                    FROM tblaccounts 
                    WHERE usertype = 'STAFF' 
                    ORDER BY datecreated DESC");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading staff: " + ex.Message;
            }
        }

        public IActionResult OnPostAddStaff(
            string FirstName, string LastName, string Username, string Password, 
            string Contact, string Email, string Address, decimal RatePerHour)
        {
            try
            {
                // Check if username already exists
                var checkParams = new Dictionary<string, object>
                {
                    { "@username", Username }
                };
                
                var existing = db.GetData("SELECT username FROM tblaccounts WHERE username = @username", checkParams);
                
                if (existing.Rows.Count > 0)
                {
                    ErrorMessage = "Username already exists!";
                    LoadStaffList();
                    return Page();
                }

                // Insert new staff (added firstName)
                var parameters = new Dictionary<string, object>
                {
                    { "@firstName", FirstName },
                    { "@lastName", LastName },
                    { "@username", Username },
                    { "@password", Password }, // In production, hash the password!
                    { "@usertype", "STAFF" },
                    { "@createdby", HttpContext.Session.GetString("Username") ?? "SYSTEM" },
                    { "@datecreated", DateTime.Now.ToString("dd/MM/yyyy") },
                    { "@address", Address ?? "" },
                    { "@contact", Contact },
                    { "@email", Email },
                    { "@rateperhour", RatePerHour }
                };

                db.ExecuteQuery(@"
                    INSERT INTO tblaccounts 
                    (firstName, lastName, username, password, usertype, createdby, datecreated, address, contact, email, rateperhour) 
                    VALUES 
                    (@firstName, @lastName, @username, @password, @usertype, @createdby, @datecreated, @address, @contact, @email, @rateperhour)",
                    parameters);

                SuccessMessage = "Staff added successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error adding staff: " + ex.Message;
            }

            LoadStaffList();
            return Page();
        }

        public IActionResult OnPostEditStaff(
            string OriginalUsername, string FirstName, string LastName, string Username,
            string Contact, string Email, string Address, decimal RatePerHour)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@originalUsername", OriginalUsername },
                    { "@firstName", FirstName },
                    { "@lastName", LastName },
                    { "@username", Username },
                    { "@address", Address ?? "" },
                    { "@contact", Contact },
                    { "@email", Email },
                    { "@rateperhour", RatePerHour }
                };

                db.ExecuteQuery(@"
                    UPDATE tblaccounts 
                    SET firstName = @firstName,
                        lastName = @lastName,
                        username = @username,
                        address = @address,
                        contact = @contact,
                        email = @email,
                        rateperhour = @rateperhour
                    WHERE username = @originalUsername",
                    parameters);

                SuccessMessage = "Staff updated successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error updating staff: " + ex.Message;
            }

            LoadStaffList();
            return Page();
        }

        public IActionResult OnPostDeleteStaff(string Username)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@username", Username }
                };

                db.ExecuteQuery("DELETE FROM tblaccounts WHERE username = @username", parameters);
                SuccessMessage = "Staff deleted successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error deleting staff: " + ex.Message;
            }

            LoadStaffList();
            return Page();
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Login");
        }
        public IActionResult OnGetCheckUsername(string username)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@username", username }
                };
                var result = db.GetData("SELECT username FROM tblaccounts WHERE username = @username", parameters);
                bool exists = result.Rows.Count > 0;
                return new JsonResult(new { exists });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = ex.Message });
            }
        }
    }
}