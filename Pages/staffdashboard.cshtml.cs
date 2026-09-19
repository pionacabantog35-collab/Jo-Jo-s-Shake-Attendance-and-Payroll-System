using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shakepayrollsystem.Data;
using System.Data;
using System;
using System.Collections.Generic;

namespace Shakepayrollsystem.Pages
{
    public class staffdashboardModel : PageModel
    {
        private DatabaseHelper db = new DatabaseHelper();

        public string Username { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        
        // Staff details
        public string StaffEmail { get; set; } = string.Empty;
        public string StaffContact { get; set; } = string.Empty;
        public decimal RatePerHour { get; set; } = 0;

        // Attendance status
        public bool IsTimedIn { get; set; } = false;
        public string TimeIn { get; set; } = "--:-- --";
        public string? TimeOut { get; set; } = null;
        public decimal TodayHoursWorked { get; set; } = 0;

        // Recent attendance history
        public DataTable RecentAttendance { get; set; } = new DataTable();

        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            // Check if user is logged in
            Username = HttpContext.Session.GetString("Username") ?? string.Empty;
            UserType = HttpContext.Session.GetString("UserType") ?? string.Empty;

            // If no user in session, redirect to login page
            if (string.IsNullOrEmpty(Username))
            {
                return RedirectToPage("/Login");
            }

            // Load staff details
            LoadStaffDetails();
            
            // Check today's attendance status
            CheckTodayAttendance();
            
            // Load recent attendance history
            LoadRecentAttendance();

            return Page();
        }

        private void LoadStaffDetails()
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@username", Username }
                };

                var result = db.GetData(@"
                    SELECT email, contact, rateperhour 
                    FROM tblaccounts 
                    WHERE username = @username", parameters);

                if (result.Rows.Count > 0)
                {
                    StaffEmail = result.Rows[0]["email"]?.ToString() ?? "N/A";
                    StaffContact = result.Rows[0]["contact"]?.ToString() ?? "N/A";
                    RatePerHour = result.Rows[0]["rateperhour"] != DBNull.Value 
                        ? Convert.ToDecimal(result.Rows[0]["rateperhour"]) 
                        : 0;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading staff details: " + ex.Message;
            }
        }

        private void CheckTodayAttendance()
        {
            try
            {
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                
                var parameters = new Dictionary<string, object>
                {
                    { "@username", Username },
                    { "@date", today }
                };

                var result = db.GetData(@"
                    SELECT timeIN, timeOUT, hoursWorked 
                    FROM tblattendance 
                    WHERE username = @username AND date = @date", parameters);

                if (result.Rows.Count > 0)
                {
                    DataRow row = result.Rows[0];

                    // Time In
                    if (row["timeIN"] != DBNull.Value)
                    {
                        TimeIn = Convert.ToString(row["timeIN"]) ?? "--:-- --";
                        
                        // Time Out
                        if (row["timeOUT"] != DBNull.Value)
                        {
                            TimeOut = Convert.ToString(row["timeOUT"]);
                            IsTimedIn = false;
                        }
                        else
                        {
                            IsTimedIn = true;
                        }
                        
                        // Hours Worked – handle both TimeSpan and numeric types
                        object hoursObj = row["hoursWorked"];
                        if (hoursObj != DBNull.Value)
                        {
                            if (hoursObj is TimeSpan ts)
                            {
                                TodayHoursWorked = (decimal)ts.TotalHours;
                            }
                            else
                            {
                                TodayHoursWorked = Convert.ToDecimal(hoursObj);
                            }
                        }
                        else
                        {
                            TodayHoursWorked = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                ErrorMessage = "Error checking attendance: " + ex.Message;
            }
        }
        private void LoadRecentAttendance()
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@username", Username }
                };

                RecentAttendance = db.GetData(@"
                    SELECT date, timeIN, timeOUT, hoursWorked, status 
                    FROM tblattendance 
                    WHERE username = @username 
                    ORDER BY date DESC 
                    LIMIT 10", parameters);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading attendance history: " + ex.Message;
            }
        }

        public IActionResult OnPostTimeIn(string Username, string TimeIn, string Date)
{
    try
    {
        Console.WriteLine($"=== TIME IN ATTEMPT ===");
        Console.WriteLine($"Username: {Username}");
        Console.WriteLine($"TimeIn: {TimeIn}");
        Console.WriteLine($"Date: {Date}");
        
        if (string.IsNullOrEmpty(TimeIn))
        {
            ErrorMessage = "Time value is empty!";
            return RedirectToPage();
        }
        
        // Check if already timed in today
        var checkParams = new Dictionary<string, object>
        {
            { "@username", Username },
            { "@date", Date }
        };

        var existing = db.GetData(@"
            SELECT * FROM tblattendance 
            WHERE username = @username AND date = @date", checkParams);

        if (existing.Rows.Count > 0)
        {
            ErrorMessage = "You have already timed in today!";
            return RedirectToPage();
        }

        // Determine status based on shift (ON TIME window: 2:45 PM - 3:00 PM)
        string status = "Late";
        TimeSpan timeIn = TimeSpan.Parse(TimeIn);
        TimeSpan onTimeStart = new TimeSpan(14, 45, 0);   // 2:45 PM
        TimeSpan onTimeEnd = new TimeSpan(15, 0, 0);     // 3:00 PM
        
        // Check if time in is within the on-time window (2:45 PM - 3:00 PM)
        if (timeIn >= onTimeStart && timeIn <= onTimeEnd)
        {
            status = "On Time";
        }
        // Otherwise, it's Late (before 2:45 PM OR after 3:00 PM)

        // Insert attendance record
        var parameters = new Dictionary<string, object>
        {
            { "@username", Username },
            { "@date", Date },
            { "@timeIN", TimeIn },
            { "@timeOUT", DBNull.Value },
            { "@status", status },
            { "@hoursWorked", 0 }
        };

        db.ExecuteQuery(@"
            INSERT INTO tblattendance (username, date, timeIN, timeOUT, status, hoursWorked) 
            VALUES (@username, @date, @timeIN, @timeOUT, @status, @hoursWorked)",
            parameters);

        SuccessMessage = $"Timed in successfully at {TimeIn}! Status: {status}";
        Console.WriteLine($"SUCCESS: Timed in at {TimeIn}, Status: {status}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
        ErrorMessage = "Error timing in: " + ex.Message;
    }

    return RedirectToPage();
}

        public IActionResult OnPostTimeOut(string Username, string Date, string TimeOut)
        {
            try
            {
                Console.WriteLine($"=== TIME OUT ATTEMPT ===");
                Console.WriteLine($"Username: {Username}");
                Console.WriteLine($"TimeOut: {TimeOut}");
                Console.WriteLine($"Date: {Date}");

                if (string.IsNullOrEmpty(TimeOut))
                {
                    ErrorMessage = "Time value is empty!";
                    return RedirectToPage();
                }

                // Get today's record
                var getParams = new Dictionary<string, object>
                {
                    { "@username", Username },
                    { "@date", Date }
                };

                var record = db.GetData(@"
                    SELECT * FROM tblattendance 
                    WHERE username = @username AND date = @date", getParams);

                if (record.Rows.Count == 0)
                {
                    ErrorMessage = "No time in record found for today!";
                    return RedirectToPage();
                }

                if (record.Rows[0]["timeOUT"] != DBNull.Value)
                {
                    ErrorMessage = "You have already timed out today!";
                    return RedirectToPage();
                }

                // Safely parse time in from database
                object timeInObj = record.Rows[0]["timeIN"];
                if (timeInObj == DBNull.Value || !TimeSpan.TryParse(timeInObj.ToString(), out TimeSpan timeIn))
                {
                    ErrorMessage = "Invalid time in value in database.";
                    return RedirectToPage();
                }

                // Parse time out from user input
                if (!TimeSpan.TryParse(TimeOut, out TimeSpan timeOut))
                {
                    ErrorMessage = "Invalid time out value.";
                    return RedirectToPage();
                }

                // Shift ends at 11PM (23:00)
                TimeSpan shiftEnd = new TimeSpan(23, 0, 0);

                // Calculate hours (if timeOut exceeds shift end, cap at shift end)
                TimeSpan actualTimeOut = timeOut > shiftEnd ? shiftEnd : timeOut;
                double hoursWorked = (actualTimeOut - timeIn).TotalHours;

                // Round to 2 decimal places
                hoursWorked = Math.Round(hoursWorked, 2);

                // Ensure hours worked is not negative
                if (hoursWorked < 0) hoursWorked = 0;

                // Update record
                var updateParams = new Dictionary<string, object>
                {
                    { "@username", Username },
                    { "@date", Date },
                    { "@timeOUT", TimeOut },
                    { "@hoursWorked", hoursWorked }
                };

                db.ExecuteQuery(@"
                    UPDATE tblattendance 
                    SET timeOUT = @timeOUT, hoursWorked = @hoursWorked 
                    WHERE username = @username AND date = @date",
                    updateParams);

                SuccessMessage = $"Timed out successfully! Hours worked: {hoursWorked}";
                Console.WriteLine($"SUCCESS: Timed out at {TimeOut}, Hours: {hoursWorked}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                ErrorMessage = "Error timing out: " + ex.Message;
            }

            return RedirectToPage();
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Login");
        }
    }
}