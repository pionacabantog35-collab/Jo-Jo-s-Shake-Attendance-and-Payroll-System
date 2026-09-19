using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shakepayrollsystem.Data;
using System.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shakepayrollsystem.Pages
{
    public class attendanceModel : PageModel
    {
        private DatabaseHelper db = new DatabaseHelper();

        public string Username { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public DataTable AttendanceList { get; set; } = new DataTable();
        public List<DataRow> PagedAttendance { get; set; } = new List<DataRow>();
        
        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        // Filter properties
        [BindProperty(SupportsGet = true)]
        public string SelectedMonth { get; set; } = DateTime.Now.ToString("yyyy-MM"); // default to current month

        [BindProperty(SupportsGet = true)]
        public string SelectedStaff { get; set; } = string.Empty; // empty means "All Staff"

        // For dropdown
        public List<string> StaffUsernames { get; set; } = new List<string>();

        // Pagination properties
        [BindProperty(SupportsGet = true, Name = "page")]
        public int CurrentPage { get; set; } = 1;
        
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        public IActionResult OnGet()
        {
            // Check if user is logged in
            Username = HttpContext.Session.GetString("Username") ?? string.Empty;
            UserType = HttpContext.Session.GetString("UserType") ?? string.Empty;

            if (string.IsNullOrEmpty(Username))
            {
                return RedirectToPage("/Login");
            }
            // Manually read the 'page' parameter
            if (Request.Query.ContainsKey("page"))
            {
                int.TryParse(Request.Query["page"], out int page);
                if (page > 0)
                {
                    CurrentPage = page;
                }
            }

            // Load staff list and attendance
            LoadStaffList();
            LoadAttendance();
            return Page();
        }

        private void LoadStaffList()
        {
            try
            {
                StaffUsernames = new List<string>();
                DataTable dt = db.GetData("SELECT username FROM tblaccounts WHERE usertype = 'STAFF' ORDER BY username");

                if (dt == null)
                {
                    ErrorMessage = "Failed to load staff data: DataTable is null.";
                    return;
                }

                // Ensure there is at least one column
                if (dt.Columns.Count == 0)
                {
                    ErrorMessage = "No columns returned from query.";
                    return;
                }

                foreach (DataRow row in dt.Rows)
                {
                    // Use index 0 because we only selected one column
                    if (!row.IsNull(0))
                    {
                        string? username = row[0].ToString();
                        if (!string.IsNullOrEmpty(username))
                        {
                            StaffUsernames.Add(username);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading staff list: " + ex.Message;
            }
        }

        private void LoadAttendance()
        {
            try
            {
                string query = @"
                    SELECT username, date, timeIN, timeOUT, status, hoursWorked 
                    FROM tblattendance 
                    WHERE 1=1";

                var parameters = new Dictionary<string, object>();

                // Apply month filter if provided
                if (!string.IsNullOrEmpty(SelectedMonth) && SelectedMonth.Length >= 7)
                {
                    string year = SelectedMonth.Substring(0, 4);
                    string month = SelectedMonth.Substring(5, 2);
                    string startDate = $"{year}-{month}-01";
                    DateTime start = DateTime.ParseExact(startDate, "yyyy-MM-dd", null);
                    DateTime end = start.AddMonths(1).AddDays(-1);
                    
                    query += " AND date BETWEEN @startDate AND @endDate";
                    parameters.Add("@startDate", start.ToString("yyyy-MM-dd"));
                    parameters.Add("@endDate", end.ToString("yyyy-MM-dd"));
                }

                // Apply staff filter if selected
                if (!string.IsNullOrEmpty(SelectedStaff))
                {
                    query += " AND username = @username";
                    parameters.Add("@username", SelectedStaff);
                }

                query += " ORDER BY date DESC, timeIN DESC";

                AttendanceList = db.GetData(query, parameters);

                if (AttendanceList.Rows.Count > 0)
                {
                    TotalRecords = AttendanceList.Rows.Count;
                    TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);

                    if (CurrentPage < 1) CurrentPage = 1;
                    if (CurrentPage > TotalPages) CurrentPage = TotalPages;

                    int startIndex = (CurrentPage - 1) * PageSize;
                    PagedAttendance = AttendanceList.AsEnumerable()
                        .Skip(startIndex)
                        .Take(PageSize)
                        .ToList();
                }
                else
                {
                    // ... empty handling ...
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading attendance: " + ex.Message;
            }
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Login");
        }
    }
}