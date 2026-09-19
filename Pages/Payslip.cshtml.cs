using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shakepayrollsystem.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Shakepayrollsystem.Pages
{
    public class PayslipModel : PageModel
    {
        private DatabaseHelper db = new DatabaseHelper();

        public string? EmployeeName { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Contact { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalHours { get; set; }
        public decimal RatePerHour { get; set; }
        public decimal GrossPay { get; set; }
        public decimal SSS { get; set; }
        public decimal PhilHealth { get; set; }
        public decimal PagIbig { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetPay { get; set; }
        public string? Type { get; set; } // "detailed" or "summary"
        public List<DailyAttendance> AttendanceRecords { get; set; } = new();

        public class DailyAttendance
        {
            public DateTime Date { get; set; }
            public string? TimeIn { get; set; }
            public string? TimeOut { get; set; }
            public decimal HoursWorked { get; set; }
            public string? Status { get; set; }
        }

        public IActionResult OnGet(string username, DateTime startDate, DateTime endDate, string type)
        {
            // Validate login (optional, you may require session)
            var loggedUser = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(loggedUser))
                return RedirectToPage("/Login");

            Username = username;
            StartDate = startDate;
            EndDate = endDate;
            Type = type;

            try
            {
                // Load employee details including firstName and lastName
                var empParams = new Dictionary<string, object> { { "@username", username } };
                var empData = db.GetData(@"
                    SELECT firstName, lastName, email, contact, address, rateperhour 
                    FROM tblaccounts 
                    WHERE username = @username", empParams);

                if (empData.Rows.Count > 0)
                {
                    var row = empData.Rows[0];
                    string firstName = row["firstName"]?.ToString() ?? "";
                    string lastName = row["lastName"]?.ToString() ?? "";
                    EmployeeName = $"{firstName} {lastName}".Trim(); // Full name
                    Email = row["email"]?.ToString() ?? "N/A";
                    Contact = row["contact"]?.ToString() ?? "N/A";
                    RatePerHour = row["rateperhour"] != DBNull.Value ? Convert.ToDecimal(row["rateperhour"]) : 0;
                }

                // Load attendance for the period
                var attParams = new Dictionary<string, object>
                {
                    { "@username", username },
                    { "@start", startDate.ToString("yyyy-MM-dd") },
                    { "@end", endDate.ToString("yyyy-MM-dd") }
                };
                var attData = db.GetData(@"
                    SELECT date, timeIN, timeOUT, hoursWorked, status
                    FROM tblattendance
                    WHERE username = @username AND date BETWEEN @start AND @end
                    ORDER BY date", attParams);

                TotalHours = 0;
                foreach (DataRow row in attData.Rows)
                {
                    var day = new DailyAttendance
                    {
                        Date = Convert.ToDateTime(row["date"]),
                        TimeIn = row["timeIN"]?.ToString() ?? "--:--",
                        TimeOut = row["timeOUT"]?.ToString() ?? "--:--",
                        HoursWorked = row["hoursWorked"] != DBNull.Value ? Convert.ToDecimal(row["hoursWorked"]) : 0,
                        Status = row["status"]?.ToString() ?? "Unknown"
                    };
                    AttendanceRecords.Add(day);
                    TotalHours += day.HoursWorked;
                }

                // Calculate earnings and deductions (same as payroll page)
                GrossPay = TotalHours * RatePerHour;
                SSS = GrossPay * 0.01m; // 4.5%
                PagIbig = GrossPay * 0.005m; // 0.5%
                PhilHealth = GrossPay * 0.00625m; // 0.625%
                TotalDeductions = SSS + PhilHealth + PagIbig;
                NetPay = GrossPay - TotalDeductions;
            }
            catch (Exception)
            {
                // You can log the error if needed
                // For now, just set empty values
            }

            return Page();
        }
    }
}