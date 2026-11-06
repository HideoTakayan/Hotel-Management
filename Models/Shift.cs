using System;
using System.ComponentModel.DataAnnotations;

namespace Hotel_Management.Models
{
    public class Shift
    {
        [Key]
        public int ShiftId { get; set; }

        [Required]
        public DateTime WorkDate { get; set; }

        [Required]
        public int ShiftType { get; set; }

        [Required]
        public int Employee1_Id { get; set; }

        [Required]
        public int Employee2_Id { get; set; }

        public string ShiftName => ShiftType == 0 ? "Ca Sáng" : "Ca Tối";
        public string TimeRange => ShiftType == 0 ? "07:00 - 15:00" : "15:00 - 23:00";
    }
}
