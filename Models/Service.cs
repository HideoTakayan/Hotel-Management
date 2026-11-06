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

        public bool RequiresStaffDelivery
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ServiceName)) return false;
                string lower = ServiceName.ToLower();

                if (lower.Contains("public onsen") ||
                    lower.Contains("tắm khoáng") ||
                    lower.Contains("ngâm bùn") ||
                    lower.Contains("xông hơi") ||
                    lower.Contains("ngâm chân") ||
                    lower.Contains("ashiyu") ||
                    lower.Contains("tắm rừng") ||
                    lower.Contains("shinrin-yoku") ||
                    lower.Contains("vườn thiền") ||
                    lower.Contains("thiền trà") ||
                    lower.Contains("yuzuyu") ||
                    lower.Contains("vé tham quan") ||
                    lower.Contains("bóng bàn") ||
                    lower.Contains("takkyu") ||
                    lower.Contains("bida") ||
                    lower.Contains("billiard") ||
                    lower.Contains("bắn súng") ||
                    lower.Contains("shateki") ||
                    lower.Contains("cờ shogi") ||
                    lower.Contains("cờ vây") ||
                    lower.Contains("arcade") ||
                    lower.Contains("gắp thú") ||
                    lower.Contains("karaoke") ||
                    lower.Contains("wanage") ||
                    lower.Contains("ném vòng") ||
                    lower.Contains("ufo catcher") ||
                    lower.Contains("giải trí"))
                {
                    return false;
                }

                return true;
            }
        }

        public string ServiceTypeDisplay => RequiresStaffDelivery ? "Phục vụ tại phòng" : "Trải nghiệm & Giải trí";

        public override string ToString() => ServiceName ?? string.Empty;
    }
}
