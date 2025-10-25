using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelMgt.Models
{
    public class Room
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; }
        public string RoomType { get; set; }
        public decimal PricePerNight { get; set; }
        public int MaxOccupancy { get; set; }
        public string Status { get; set; }
        public int FloorNumber { get; set; }
        public string Amenities { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
    }
}
