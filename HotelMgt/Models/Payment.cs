using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelMgt.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public int CheckInId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime PaymentDate { get; set; }
        public int ProcessedByEmployeeId { get; set; }
        public string TransactionReference { get; set; }

        // Navigation properties
        public string GuestName { get; set; }
        public string RoomNumber { get; set; }
        public string EmployeeName { get; set; }
    }
}
