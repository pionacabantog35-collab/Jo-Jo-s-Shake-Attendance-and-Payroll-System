using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shakepayrollsystem.Data;
using System.Data;
using System;

namespace Shakepayrollsystem.Pages
{
    public class dashboardModel : PageModel
    {
        private DatabaseHelper db = new DatabaseHelper();

        public string Username { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;

        // Dashboard Statistics
        public int TotalStaff { get; set; } = 0;
        public int PresentToday { get; set; } = 0;
        public int LateToday { get; set; } = 0;
        public int AbsentToday { get; set; } = 0;
        public decimal MonthlyPayroll { get; set; } = 0;

        // Recent Data Tables
        public DataTable RecentAttendance { get; set; } = new DataTable();
        public DataTable RecentWageLogs { get; set; } = new DataTable();
        public DataTable UpcomingPayroll { get; set; } = new DataTable();

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

            LoadDashboardData();
            return Page();
        }

        private void LoadDashboardData()
        {
            try
            {
                // --- Total Staff Count ---
                var staffCount = db.ExecuteScalar("SELECT COUNT(*) FROM tblaccounts WHERE usertype = 'STAFF'");
                TotalStaff = staffCount != null && staffCount != DBNull.Value ? Convert.ToInt32(staffCount) : 0;

                string today = DateTime.Now.ToString("yyyy-MM-dd");

                // --- Present Today (all who timed in, regardless of status) ---
                var presentCount = db.ExecuteScalar($@"
                    SELECT COUNT(*) FROM tblattendance 
                    WHERE date = '{today}'");
                PresentToday = presentCount != null && presentCount != DBNull.Value ? Convert.ToInt32(presentCount) : 0;

                // --- Late Today ---
                var lateCount = db.ExecuteScalar($@"
                    SELECT COUNT(*) FROM tblattendance 
                    WHERE date = '{today}' AND status = 'Late'");
                LateToday = lateCount != null && lateCount != DBNull.Value ? Convert.ToInt32(lateCount) : 0;

                // --- Absent Today ---
                AbsentToday = TotalStaff - PresentToday;
                if (AbsentToday < 0) AbsentToday = 0;

                // --- Monthly Payroll (SUM may return NULL) ---
                string currentMonth = DateTime.Now.ToString("yyyy-MM");
                var monthlyPayroll = db.ExecuteScalar($@"
                    SELECT SUM(netPay) FROM tblwagelog 
                    WHERE DATE_FORMAT(datePaid, '%Y-%m') = '{currentMonth}'");
                MonthlyPayroll = (monthlyPayroll != null && monthlyPayroll != DBNull.Value) 
                                ? Convert.ToDecimal(monthlyPayroll) : 0;

                // --- Load Recent Attendance (unchanged) ---
                RecentAttendance = db.GetData(@"
                    SELECT username, date, timeIN, timeOUT, status, hoursWorked 
                    FROM tblattendance 
                    ORDER BY date DESC, timeIN DESC 
                    LIMIT 5");

                // Replace DBNull values with safe defaults
                foreach (DataRow row in RecentAttendance.Rows)
                {
                    if (row["username"] == DBNull.Value) row["username"] = "";
                    if (row["date"] == DBNull.Value) row["date"] = DateTime.MinValue;
                    if (row["timeIN"] == DBNull.Value) row["timeIN"] = "";
                    if (row["timeOUT"] == DBNull.Value) row["timeOUT"] = "";
                    if (row["status"] == DBNull.Value) row["status"] = "";
                    if (row["hoursWorked"] == DBNull.Value) row["hoursWorked"] = 0;
                }

                // --- Load Recent Wage Logs (unchanged) ---
                RecentWageLogs = db.GetData(@"
                    SELECT username, cutOffStart, cutOffEnd, netPay, datePaid 
                    FROM tblwagelog 
                    ORDER BY datePaid DESC 
                    LIMIT 5");

                foreach (DataRow row in RecentWageLogs.Rows)
                {
                    if (row["username"] == DBNull.Value) row["username"] = "";
                    if (row["cutOffStart"] == DBNull.Value) row["cutOffStart"] = DateTime.MinValue;
                    if (row["cutOffEnd"] == DBNull.Value) row["cutOffEnd"] = DateTime.MinValue;
                    if (row["netPay"] == DBNull.Value) row["netPay"] = 0m;
                    if (row["datePaid"] == DBNull.Value) row["datePaid"] = DateTime.MinValue;
                }

                // --- Load Upcoming Payroll (unchanged) ---
                UpcomingPayroll = db.GetData(@"
                    SELECT DISTINCT username, 
                        DATE_ADD(CURDATE(), INTERVAL 7 DAY) as nextPayDate,
                        'Pending' as status
                    FROM tblaccounts 
                    WHERE usertype = 'STAFF'
                    LIMIT 5");

                foreach (DataRow row in UpcomingPayroll.Rows)
                {
                    if (row["username"] == DBNull.Value) row["username"] = "";
                    if (row["nextPayDate"] == DBNull.Value) row["nextPayDate"] = DateTime.MinValue;
                    if (row["status"] == DBNull.Value) row["status"] = "";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading dashboard data: " + ex.Message;
            }
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Login");
        }
    }
}