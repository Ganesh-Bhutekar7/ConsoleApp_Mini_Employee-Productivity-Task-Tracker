using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace EmployeeProductivityTracker
{
    // ====== Domain Models ======
    enum UserRole { Employee, Manager }
    enum TaskStatus { Pending, InProgress, Completed }

    class Employee
    {
        public int EmployeeId { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; } // Employee or Manager
        public string Password { get; set; } // Plain text for demo only
    }

    class TaskItem
    {
        public int TaskId { get; set; }
        public int EmployeeId { get; set; }
        public string TaskName { get; set; }
        public double HoursSpent { get; set; }
        public DateTime Date { get; set; }
        public TaskStatus Status { get; set; }
        public DateTime DueDate { get; set; }
        public List<TaskComment> Comments { get; set; } = new();
        public List<StatusChange> StatusHistory { get; set; } = new();
    }

    class TaskComment
    {
        public int CommentId { get; set; }
        public int TaskId { get; set; }
        public string CommentText { get; set; }
        public string AddedBy { get; set; }
        public DateTime AddedDate { get; set; }
    }

    class StatusChange
    {
        public TaskStatus OldStatus { get; set; }
        public TaskStatus NewStatus { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; }
    }

    // ====== In-memory Data Store ======
    static class DataStore
    {
        public static List<Employee> Employees { get; } = new();
        public static List<TaskItem> Tasks { get; } = new();
        public static int NextTaskId { get; set; } = 1;
        public static int NextCommentId { get; set; } = 1;

        public static void Seed()
        {
            Employees.AddRange(new[]
            {
                new Employee{ EmployeeId = 1, Name = "Shiv", Department = "IT", Email = "Shiv@gbsoft.com", Role = UserRole.Employee, Password = "123" },
                new Employee{ EmployeeId = 2, Name = "Bhagwat", Department = "HR", Email = "bhagwat@gbsoft.com", Role = UserRole.Employee, Password = "123" },
                new Employee{ EmployeeId = 3, Name = "Ganesh Bhutekar", Department = "IT", Email = "ganesh@gbsoft.com", Role = UserRole.Manager, Password = "admin" }
            });

            AddTask(new TaskItem { EmployeeId = 1, TaskName = "Fix Bug #101", HoursSpent = 5, Date = DateTime.Today, Status = TaskStatus.Completed, DueDate = DateTime.Today }, "System");
            AddTask(new TaskItem { EmployeeId = 1, TaskName = "Develop Feature X", HoursSpent = 6, Date = DateTime.Today.AddDays(-2), Status = TaskStatus.InProgress, DueDate = DateTime.Today.AddDays(1) }, "System");
            AddTask(new TaskItem { EmployeeId = 2, TaskName = "Recruitment Drive", HoursSpent = 4, Date = DateTime.Today, Status = TaskStatus.Pending, DueDate = DateTime.Today }, "System");
        }

        public static TaskItem AddTask(TaskItem t, string createdBy)
        {
            t.TaskId = NextTaskId++;
            t.StatusHistory.Add(new StatusChange { OldStatus = t.Status, NewStatus = t.Status, ChangedAt = DateTime.Now, ChangedBy = createdBy });
            Tasks.Add(t);
            return t;
        }
    }

    // ====== Auth Service ======
    static class AuthService
    {
        public static Employee Login()
        {
            Console.WriteLine("=== Login ===");
            Console.Write("Email: ");
            var email = Console.ReadLine();
            Console.Write("Password: ");
            var pwd = ReadPassword();

            var user = DataStore.Employees.FirstOrDefault(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && e.Password == pwd);
            if (user == null)
            {
                Console.WriteLine("\n⚠ Invalid credentials.\n");
                return null;
            }
            Console.WriteLine($"\n✔ Welcome, {user.Name}! Role: {user.Role}\n");
            return user;
        }

        private static string ReadPassword()
        {
            var pwd = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;
                if (key == ConsoleKey.Backspace && pwd.Length > 0)
                {
                    pwd = pwd[0..^1];
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    pwd += keyInfo.KeyChar;
                    Console.Write("*");
                }
            } while (key != ConsoleKey.Enter);
            return pwd;
        }
    }

    // ====== Date Helpers ======
    static class DateHelpers
    {
        public static int GetIsoWeekOfYear(DateTime date)
        {
            var cal = CultureInfo.InvariantCulture.Calendar;
            return cal.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }

    // ====== Program ======
    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            DataStore.Seed();

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Employee Productivity & Task Tracker ===\n");
                var user = AuthService.Login();
                if (user == null) continue;

                if (user.Role == UserRole.Manager) ManagerMenu(user);
                else EmployeeMenu(user);
            }
        }

        #region Employee Menu
        static void EmployeeMenu(Employee user)
        {
            while (true)
            {
                Console.WriteLine("-- Employee Menu --");
                Console.WriteLine("1. Create Task");
                Console.WriteLine("2. Update Task");
                Console.WriteLine("3. Delete Task");
                Console.WriteLine("4. View My Tasks");
                Console.WriteLine("5. Add Comment / Update Status");
                Console.WriteLine("6. My Weekly / Monthly Timesheet");
                Console.WriteLine("7. Logout");
                Console.Write("Select: ");
                var ch = Console.ReadLine();
                Console.WriteLine();
                switch (ch)
                {
                    case "1": CreateTask(user); break;
                    case "2": UpdateTask(user); break;
                    case "3": DeleteTask(user); break;
                    case "4": ViewTasks(DataStore.Tasks.Where(t => t.EmployeeId == user.EmployeeId)); break;
                    case "5": CommentOrStatus(user); break;
                    case "6": ShowTimesheet(user.EmployeeId); break;
                    case "7": return;
                    default: Console.WriteLine("⚠ Invalid option."); break;
                }
                Pause();
            }
        }
        #endregion

        #region Manager Menu
        static void ManagerMenu(Employee user)
        {
            while (true)
            {
                Console.WriteLine("-- Manager Menu --");
                Console.WriteLine("1. Assign Task to Employee");
                Console.WriteLine("2. View/Filter Tasks");
                Console.WriteLine("3. Group Tasks by Employee");
                Console.WriteLine("4. Weekly Hours (All Employees)");
                Console.WriteLine("5. Top 3 Performers");
                Console.WriteLine("6. Overdue Tasks");
                Console.WriteLine("7. Analytics Dashboard");
                Console.WriteLine("8. Export CSV Reports");
                Console.WriteLine("9. Logout");
                Console.Write("Select: ");
                var ch = Console.ReadLine();
                Console.WriteLine();
                switch (ch)
                {
                    case "1": AssignTask(user); break;
                    case "2": FilterSearchAll(); break;
                    case "3": GroupTasksByEmployee(); break;
                    case "4": WeeklySummaryAll(); break;
                    case "5": ShowTopPerformers(); break;
                    case "6": OverdueTasks(); break;
                    case "7": AnalyticsDashboard(); break;
                    case "8": ExportReports(); break;
                    case "9": return;
                    default: Console.WriteLine("⚠ Invalid option."); break;
                }
                Pause();
            }
        }
        #endregion

        #region Task Actions
        static void CreateTask(Employee user)
        {
            Console.WriteLine("=== Create Task ===");

            var t = new TaskItem { EmployeeId = user.EmployeeId, Date = DateTime.Today };

            Console.Write("Task Name: ");
            t.TaskName = ReadNonEmptyString("Task Name");

            t.HoursSpent = ReadDouble("Hours Spent (numeric): ");

            t.Status = (TaskStatus)ReadIntInRange("Status (0-Pending,1-InProgress,2-Completed): ", 0, 2);

            t.DueDate = ReadDate("Due Date (yyyy-mm-dd): ");

            DataStore.AddTask(t, user.Name);
            Console.WriteLine("✔ Task created successfully.");
        }

        static void UpdateTask(Employee user)
        {
            var myTasks = DataStore.Tasks.Where(t => t.EmployeeId == user.EmployeeId).ToList();
            ViewTasks(myTasks);
            int taskId = ReadInt("Enter TaskId to update: ");

            var task = myTasks.FirstOrDefault(t => t.TaskId == taskId);
            if (task == null) { Console.WriteLine("⚠ Task not found."); return; }

            string name = ReadOptionalString($"New Name ({task.TaskName}): ");
            if (!string.IsNullOrEmpty(name)) task.TaskName = name;

            double hours = ReadOptionalDouble($"New Hours ({task.HoursSpent}): ");
            if (hours >= 0) task.HoursSpent = hours;

            int st = ReadOptionalInt($"New Status ({(int)task.Status}): ", 0, 2);
            if (st >= 0) ChangeStatus(task, (TaskStatus)st, user.Name);

            DateTime dd = ReadOptionalDate($"New Due ({task.DueDate:yyyy-MM-dd}): ");
            if (dd != default) task.DueDate = dd;

            Console.WriteLine("✔ Task updated successfully.");
        }

        static void DeleteTask(Employee user)
        {
            var myTasks = DataStore.Tasks.Where(t => t.EmployeeId == user.EmployeeId).ToList();
            ViewTasks(myTasks);
            int taskId = ReadInt("Enter TaskId to delete: ");

            var task = myTasks.FirstOrDefault(t => t.TaskId == taskId);
            if (task == null) { Console.WriteLine("⚠ Task not found."); return; }

            DataStore.Tasks.Remove(task);
            Console.WriteLine("✔ Task deleted successfully.");
        }

        static void CommentOrStatus(Employee user)
        {
            var myTasks = DataStore.Tasks.Where(t => t.EmployeeId == user.EmployeeId).ToList();
            ViewTasks(myTasks);
            int taskId = ReadInt("TaskId: ");

            var task = myTasks.FirstOrDefault(t => t.TaskId == taskId);
            if (task == null) { Console.WriteLine("⚠ Task not found."); return; }

            Console.WriteLine("1) Add Comment  2) Change Status");
            string op = Console.ReadLine();

            if (op == "1")
            {
                string comment = ReadNonEmptyString("Comment: ");
                task.Comments.Add(new TaskComment { CommentId = DataStore.NextCommentId++, TaskId = task.TaskId, CommentText = comment, AddedBy = user.Name, AddedDate = DateTime.Now });
                Console.WriteLine("✔ Comment added successfully.");
            }
            else if (op == "2")
            {
                int newStatus = ReadIntInRange("New Status (0-Pending,1-InProgress,2-Completed): ", 0, 2);
                ChangeStatus(task, (TaskStatus)newStatus, user.Name);
            }
            else
            {
                Console.WriteLine("⚠ Invalid option.");
            }
        }

        static void AssignTask(Employee manager)
        {
            Console.WriteLine("Employees:");
            foreach (var e in DataStore.Employees) Console.WriteLine($"{e.EmployeeId} - {e.Name} ({e.Department})");

            int empId = ReadInt("Enter EmployeeId to assign: ");
            var t = new TaskItem { EmployeeId = empId, Date = DateTime.Today };

            t.TaskName = ReadNonEmptyString("Task Name: ");
            t.HoursSpent = ReadDouble("Estimated Hours: ");
            t.Status = TaskStatus.Pending;
            t.DueDate = ReadDate("Due Date (yyyy-mm-dd): ");

            DataStore.AddTask(t, manager.Name);
            Console.WriteLine("✔ Task assigned successfully.");
        }

        static void ChangeStatus(TaskItem t, TaskStatus newStatus, string by)
        {
            if (t.Status == newStatus) return;
            t.StatusHistory.Add(new StatusChange { OldStatus = t.Status, NewStatus = newStatus, ChangedAt = DateTime.Now, ChangedBy = by });
            t.Status = newStatus;
            Console.WriteLine("✔ Status updated.");
        }
        #endregion

        #region Views / Reports
        static void ViewTasks(IEnumerable<TaskItem> tasks = null)
        {
            tasks ??= DataStore.Tasks;
            Console.WriteLine("TaskId | EmpId | Name | Hours | Status | Date | Due | Comments");
            foreach (var t in tasks.OrderBy(x => x.TaskId))
            {
                Console.WriteLine($"{t.TaskId,6} | {t.EmployeeId,5} | {t.TaskName,-20} | {t.HoursSpent,5} | {t.Status,10} | {t.Date:yyyy-MM-dd} | {t.DueDate:yyyy-MM-dd} | {t.Comments.Count}");
            }
        }

        static void OverdueTasks()
        {
            var overdue = DataStore.Tasks.Where(t => t.DueDate < DateTime.Today && t.Status != TaskStatus.Completed);
            Console.WriteLine("Overdue Tasks:");
            foreach (var t in overdue)
            {
                var emp = DataStore.Employees.First(e => e.EmployeeId == t.EmployeeId);
                Console.WriteLine($"- {t.TaskName} (Emp: {emp.Name}) Due: {t.DueDate:yyyy-MM-dd}");
            }
        }
        #endregion

        #region Missing Methods (Stubs)
        static void ShowTimesheet(int employeeId)
        {
            Console.WriteLine("⚠ ShowTimesheet not implemented yet.");
        }

        static void FilterSearchAll()
        {
            Console.WriteLine("⚠ FilterSearchAll not implemented yet.");
        }

        static void GroupTasksByEmployee()
        {
            Console.WriteLine("⚠ GroupTasksByEmployee not implemented yet.");
        }

        static void WeeklySummaryAll()
        {
            Console.WriteLine("⚠ WeeklySummaryAll not implemented yet.");
        }

        static void ShowTopPerformers()
        {
            Console.WriteLine("⚠ ShowTopPerformers not implemented yet.");
        }

        static void AnalyticsDashboard()
        {
            Console.WriteLine("⚠ AnalyticsDashboard not implemented yet.");
        }

        static void ExportReports()
        {
            Console.WriteLine("⚠ ExportReports not implemented yet.");
        }
        #endregion

        #region Utilities
        static void Pause()
        {
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            Console.Clear();
        }

        static string ReadNonEmptyString(string prompt)
        {
            string input;
            do
            {
                Console.Write(prompt);
                input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) Console.WriteLine("⚠ Cannot be empty.");
            } while (string.IsNullOrWhiteSpace(input));
            return input;
        }

        static string ReadOptionalString(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine();
        }

        static int ReadInt(string prompt)
        {
            int value;
            while (true)
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out value)) return value;
                Console.WriteLine("⚠ Invalid number.");
            }
        }

        static int ReadIntInRange(string prompt, int min, int max)
        {
            int value;
            while (true)
            {
                value = ReadInt(prompt);
                if (value >= min && value <= max) return value;
                Console.WriteLine($"⚠ Value must be between {min} and {max}.");
            }
        }

        static int ReadOptionalInt(string prompt, int min, int max)
        {
            Console.Write(prompt);
            var input = Console.ReadLine();
            return int.TryParse(input, out var value) && value >= min && value <= max ? value : -1;
        }

        static double ReadDouble(string prompt)
        {
            double value;
            while (true)
            {
                Console.Write(prompt);
                if (double.TryParse(Console.ReadLine(), out value)) return value;
                Console.WriteLine("⚠ Invalid number.");
            }
        }

        static double ReadOptionalDouble(string prompt)
        {
            Console.Write(prompt);
            var input = Console.ReadLine();
            return double.TryParse(input, out var value) ? value : -1;
        }

        static DateTime ReadDate(string prompt)
        {
            DateTime date;
            while (true)
            {
                Console.Write(prompt);
                if (DateTime.TryParse(Console.ReadLine(), out date)) return date;
                Console.WriteLine("⚠ Invalid date format.");
            }
        }

        static DateTime ReadOptionalDate(string prompt)
        {
            Console.Write(prompt);
            var input = Console.ReadLine();
            return DateTime.TryParse(input, out var date) ? date : default;
        }
        #endregion
    }
}
