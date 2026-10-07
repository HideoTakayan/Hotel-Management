using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hotel_Management.Models
{
    public class Bill
    {
        [Key]
        public int BillId { get; set; }

        [Required]
        public int RoomId { get; set; }
        [ForeignKey("RoomId")]
        public virtual Room Room { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(50)]
        public string FullName { get; set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        [MaxLength(12)]
        public string Phone { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public int NumberPeople { get; set; }

        // Lưu trữ tổng tiền
        public decimal? Total { get; set; }
        [Required]
        public int Status { get; set; }

        public string StatusName
        {
            get
            {
                return Status switch
                {
                    0 => "Chưa thanh toán",
                    1 => "Đã thanh toán",
                    2 => "Hủy",
                    3 => "Check in",
                    4 => "Check out",
                    _ => "Không xác định"
                };
            }
        }
        public void CalculateTotal()
        {
            if (Room != null && EndDate > StartDate)
            {
                Total = (decimal)(EndDate.Date - StartDate.Date).TotalDays * Room.Price;
            }
        }
    }
}
