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

        public string QualityName
        {
            get
            {
                return Quality switch
                {
                    1 => "1 ★",
                    2 => "2 ★",
                    3 => "3 ★",
                    4 => "4 ★",
                    5 => "5 ★",
                    _ => "Không xác định"
                };
            }
        }
    }
}
