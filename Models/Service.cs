using System;
using System.ComponentModel.DataAnnotations;

namespace Hotel_Management.Models
{
    public class Service
    {
        [Key]
        public int ServiceId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ServiceName { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        [MaxLength(20)]
        public string Unit { get; set; }

        public override string ToString() => ServiceName ?? string.Empty;
    }
}
