using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shakepayrollsystem.Data;
using System.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shakepayrollsystem.Pages
{
    public class wagelogModel : PageModel
    {
        private DatabaseHelper db = new DatabaseHelper();

        public string Username { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;

        // Filter properties
        [BindProperty(SupportsGet = true)]
        public string SelectedMonth { get; set; } = DateTime.Now.ToString("yyyy-MM");

        [BindProperty(SupportsGet = true)]
        public string FilterStaff { get; set; } = string.Empty;

        // Pagination properties
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        // Data tables
        public DataTable StaffList { get; set; } = new DataTable();
        public DataTable WageLogs { get; set; } = new DataTable();
        public List<DataRow> PagedWageLogs { get; set; } = new List<DataRow>();

        // Summary totals
        public decimal TotalGrossPay { get; set; } = 0;
        public decimal TotalDeductions { get; set; } = 0;
        public decimal TotalNetPay { get; set; } = 0;

        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            // Check if user is logged in
            Username = HttpContext.Session.GetString("Username") ?? string.Empty;
            UserType = HttpContext.Session.GetString("UserType") ?? string.Empty;

            if (string.IsNullOrEmpty(Username))
            {
                return RedirectToPage("/Login");
            }

            // Manually read the 'page' parameter (fix for pagination)
            if (Request.Query.ContainsKey("page"))
            {
                int.TryParse(Request.Query["page"], out int page);
                if (page > 0)
                {
                    CurrentPage = page;
                }
            }

            LoadStaffList();
            LoadWageLogs();

            return Page();
        }

        private void LoadStaffList()
        {
            try
            {
                StaffList = db.GetData("SELECT username FROM tblaccounts WHERE usertype = 'STAFF' ORDER BY username");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading staff list: " + ex.Message;
            }
        }

        private void LoadWageLogs()
        {
            try
            {
                // Calculate date range from SelectedMonth
                DateTime start, end;
                if (!string.IsNullOrEmpty(SelectedMonth) && SelectedMonth.Length >= 7)
                {
                    string year = SelectedMonth.Substring(0, 4);
                    string month = SelectedMonth.Substring(5, 2);
                    string startDate = $"{year}-{month}-01";
                    start = DateTime.ParseExact(startDate, "yyyy-MM-dd", null);
                    end = start.AddMonths(1).AddDays(-1);
                }
                else
                {
                    // Default to current month
                    start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    end = start.AddMonths(1).AddDays(-1);
                }

                string query = @"
                    SELECT username, cutOffStart, cutOffEnd, hoursWorked, 
                           grossPay, totalDeduction, netPay, datePaid
                    FROM tblwagelog 
                    WHERE datePaid BETWEEN @startDate AND @endDate";

                var parameters = new Dictionary<string, object>
                {
                    { "@startDate", start.ToString("yyyy-MM-dd") },
                    { "@endDate", end.ToString("yyyy-MM-dd") }
                };

                if (!string.IsNullOrEmpty(FilterStaff))
                {
                    query += " AND username = @username";
                    parameters.Add("@username", FilterStaff);
                }

                query += " ORDER BY datePaid DESC, cutOffStart DESC";

                WageLogs = db.GetData(query, parameters);

                // Reset totals
                TotalGrossPay = 0;
                TotalDeductions = 0;
                TotalNetPay = 0;

                if (WageLogs.Rows.Count > 0)
                {
                    // Calculate totals
                    foreach (System.Data.DataRow row in WageLogs.Rows)
                    {
                        TotalGrossPay += Convert.ToDecimal(row["grossPay"]);
                        TotalDeductions += Convert.ToDecimal(row["totalDeduction"]);
                        TotalNetPay += Convert.ToDecimal(row["netPay"]);
                    }

                    TotalRecords = WageLogs.Rows.Count;
                    TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);

                    if (CurrentPage < 1) CurrentPage = 1;
                    if (CurrentPage > TotalPages) CurrentPage = TotalPages;

                    int startIndex = (CurrentPage - 1) * PageSize;
                    PagedWageLogs = WageLogs.AsEnumerable()
                        .Skip(startIndex)
                        .Take(PageSize)
                        .ToList();
                }
                else
                {
                    TotalRecords = 0;
                    TotalPages = 1;
                    PagedWageLogs = new List<DataRow>();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading wage logs: " + ex.Message;
            }
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Login");
        }
    }
}