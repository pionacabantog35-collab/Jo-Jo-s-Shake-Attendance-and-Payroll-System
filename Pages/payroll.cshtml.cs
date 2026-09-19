using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shakepayrollsystem.Data;
using System.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shakepayrollsystem.Pages
{
    public class payrollModel : PageModel
    {
        private DatabaseHelper db = new DatabaseHelper();

        public string Username { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;

        // Staff list for cards
        public DataTable StaffList { get; set; } = new DataTable();

        // Selected staff properties
        public string SelectedUsername { get; set; } = string.Empty;
        public string SelectedStaffName { get; set; } = string.Empty;

        // Date filter
        public DateTime StartDate { get; set; } = DateTime.Now.AddDays(-30);
        public DateTime EndDate { get; set; } = DateTime.Now;

        // Salary information
        public decimal HoursWorked { get; set; } = 0;
        public decimal GrossPay { get; set; } = 0;
        public decimal SSS { get; set; } = 0;
        public decimal PagIbig { get; set; } = 0;
        public decimal PhilHealth { get; set; } = 0;
        public decimal TotalDeduction { get; set; } = 0;
        public decimal NetPay { get; set; } = 0;

        // Payroll history
        public DataTable PayrollHistory { get; set; } = new DataTable();

        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public IActionResult OnGet(string username, DateTime? startDate, DateTime? endDate)
        {
            // Check if user is logged in
            Username = HttpContext.Session.GetString("Username") ?? string.Empty;
            UserType = HttpContext.Session.GetString("UserType") ?? string.Empty;

            if (string.IsNullOrEmpty(Username))
            {
                return RedirectToPage("/Login");
            }

            // Load staff list for cards
            LoadStaffList();

            // If a username is selected, load their payroll data
            if (!string.IsNullOrEmpty(username))
            {
                SelectedUsername = username;
                LoadSelectedStaffInfo();
                
                // Apply date filters if provided
                if (startDate.HasValue)
                    StartDate = startDate.Value;
                if (endDate.HasValue)
                    EndDate = endDate.Value;

                LoadPayrollData();
                LoadPayrollHistory();
            }

            return Page();
        }

        private void LoadStaffList()
        {
            try
            {
                StaffList = db.GetData(@"
                    SELECT username, lastName, email 
                    FROM tblaccounts 
                    WHERE usertype = 'STAFF' 
                    ORDER BY username");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading staff: " + ex.Message;
            }
        }

        private void LoadSelectedStaffInfo()
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@username", SelectedUsername }
                };

                var result = db.GetData(@"
                    SELECT lastName 
                    FROM tblaccounts 
                    WHERE username = @username", parameters);

                if (result.Rows.Count > 0)
                {
                    SelectedStaffName = result.Rows[0]["lastName"]?.ToString() ?? "";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading staff info: " + ex.Message;
            }
        }

        private void LoadPayrollData()
        {
            try
            {
                // Get attendance data for the date range
                var parameters = new Dictionary<string, object>
                {
                    { "@username", SelectedUsername },
                    { "@startDate", StartDate.ToString("yyyy-MM-dd") },
                    { "@endDate", EndDate.ToString("yyyy-MM-dd") }
                };

                var attendance = db.GetData(@"
                    SELECT SUM(hoursWorked) as TotalHours
                    FROM tblattendance 
                    WHERE username = @username 
                    AND date BETWEEN @startDate AND @endDate", parameters);

                if (attendance.Rows.Count > 0 && attendance.Rows[0]["TotalHours"] != DBNull.Value)
                {
                    HoursWorked = Convert.ToDecimal(attendance.Rows[0]["TotalHours"]);
                    
                    // Calculate gross pay (assuming rate from staff table)
                    var rateData = db.GetData("SELECT rateperhour FROM tblaccounts WHERE username = @username", 
                        new Dictionary<string, object> { { "@username", SelectedUsername } });
                    
                    decimal ratePerHour = 50; // Default rate
                    if (rateData.Rows.Count > 0 && rateData.Rows[0]["rateperhour"] != DBNull.Value)
                    {
                        ratePerHour = Convert.ToDecimal(rateData.Rows[0]["rateperhour"]);
                    }

                    GrossPay = HoursWorked * ratePerHour;

                    // Calculate deductions (example rates)
                    SSS = GrossPay * 0.01m; // 4.5%
                    PagIbig = GrossPay * 0.005m; // 0.5%
                    PhilHealth = GrossPay * 0.00625m; // 0.625%
                    
                    TotalDeduction = SSS + PagIbig + PhilHealth;
                    NetPay = GrossPay - TotalDeduction;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error calculating payroll: " + ex.Message;
            }
        }

        private void LoadPayrollHistory()
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@username", SelectedUsername }
                };

                PayrollHistory = db.GetData(@"
                    SELECT cutOffStart, cutOffEnd, hoursWorked, grossPay, 
                           totalDeduction, netPay, datePaid
                    FROM tblwagelog 
                    WHERE username = @username 
                    ORDER BY datePaid DESC, cutOffStart DESC", parameters);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading payroll history: " + ex.Message;
            }
        }

        public IActionResult OnPostProcessPay(string Username, string StartDate, string EndDate)
        {
            try
            {
                // Reload data to get current calculations
                SelectedUsername = Username;
                this.StartDate = DateTime.Parse(StartDate);
                this.EndDate = DateTime.Parse(EndDate);
                
                LoadSelectedStaffInfo();
                LoadPayrollData();

                // Validate that there are hours worked
                if (HoursWorked <= 0)
                {
                    ErrorMessage = "Cannot process payroll: No hours worked for the selected period.";
                    return RedirectToPage(new { username = Username, startDate = StartDate, endDate = EndDate });
                }

                // Save to wagelog
                var parameters = new Dictionary<string, object>
                {
                    { "@username", Username },
                    { "@cutOffStart", StartDate },
                    { "@cutOffEnd", EndDate },
                    { "@hoursWorked", HoursWorked },
                    { "@grossPay", GrossPay },
                    { "@totalDeduction", TotalDeduction },
                    { "@netPay", NetPay },
                    { "@datePaid", DateTime.Now }
                };

                db.ExecuteQuery(@"
                    INSERT INTO tblwagelog 
                    (username, cutOffStart, cutOffEnd, hoursWorked, grossPay, totalDeduction, netPay, datePaid) 
                    VALUES 
                    (@username, @cutOffStart, @cutOffEnd, @hoursWorked, @grossPay, @totalDeduction, @netPay, @datePaid)",
                    parameters);

                SuccessMessage = "Payroll processed successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error processing payroll: " + ex.Message;
            }

            return RedirectToPage(new { username = Username, startDate = StartDate, endDate = EndDate });
        }

        public IActionResult OnGetPrint(string type, string username, string startDate, string endDate)
        {
            // This will generate PDF payslip
            // You'll need to implement PDF generation logic here
            // For now, redirect back
            return RedirectToPage(new { username, startDate, endDate });
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Login");
        }
    }
}