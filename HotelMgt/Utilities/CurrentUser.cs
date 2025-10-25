using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelMgt.Utilities
{
    public static class CurrentUser
    {
        public static int EmployeeId { get; set; }
        public static string FirstName { get; set; }
        public static string LastName { get; set; }
        public static string Email { get; set; }
        public static string Role { get; set; }  // "Employee" or "Admin"
        public static string FullName => $"{FirstName} {LastName}";
        public static bool IsAdmin => Role == "Admin";
        public static bool IsLoggedIn => EmployeeId > 0;

        public static void Clear()
        {
            EmployeeId = 0;
            FirstName = null;
            LastName = null;
            Email = null;
            Role = null;
        }
    }
}
