using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hotel_Management.Models
{
    public class BillService
    {
        [Key]
        public int BillServiceId { get; set; }

        [Required]
        public int BillId { get; set; }
        
        [ForeignKey("BillId")]
        public virtual Bill Bill { get; set; }

        [Required]
        public int ServiceId { get; set; }
        
        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; }

        public string ServiceName { get; set; }

        public decimal Price { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;

        [Required]
        public decimal Total { get; set; }
    }
}
