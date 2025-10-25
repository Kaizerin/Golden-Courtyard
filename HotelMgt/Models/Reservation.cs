using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelMgt.Models
{
    public class Reservation
    {
        public int ReservationId { get; set; }
        public int GuestId { get; set; }
        public int RoomId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; }
        public string Status { get; set; }
        public string SpecialRequests { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedByEmployeeId { get; set; }

        // Navigation properties (for display)
        public string GuestName { get; set; }
        public string RoomNumber { get; set; }
        public string EmployeeName { get; set; }
        public int NumberOfNights => (CheckOutDate - CheckInDate).Days;
    }
}
