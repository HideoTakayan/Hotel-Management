using System;
using System.ComponentModel.DataAnnotations;

namespace Hotel_Management.Models
{
    public class Room
    {
        [Key]
        public int RoomId { get; set; }

        [Required]
        [MaxLength(20)]
        public string RoomNumber { get; set; }

        [Required]
        public int NumberPeople { get; set; }

        [Required]
        public int NumberBed { get; set; }

        [Required]
        public int Quality { get; set; }

        [Required]
        [MaxLength(50)]
        public string BedType { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoomType { get; set; }

        [Required]
        public float RoomArea { get; set; }

        [Required]
        public int Status { get; set; } = 0; // 0: Trống, 1: Đã đặt, 2: Đang sử dụng, 3: Chờ dọn dẹp, 4: Đang dọn dẹp

        public string StatusName
        {
            get
            {
                return Status switch
                {
                    0 => "Trống (Sẵn sàng)",
                    1 => "Đã đặt",
                    2 => "Đang sử dụng",
                    3 => "Chờ dọn dẹp",
                    4 => "Đang dọn dẹp",
                    _ => "Không xác định"
                };
            }
        }

        public string QualityName
        {
            get
            {
                return Quality switch
                {
                    1 => "1 sao",
                    2 => "2 sao",
                    3 => "3 sao",
                    4 => "4 sao",
                    5 => "5 sao",
                    _ => "Không xác định"
                };
            }
        }

        public override string ToString() => RoomNumber ?? string.Empty;
    }
}
