using System;
using System.ComponentModel.DataAnnotations;

namespace Hotel_Management.Models
{
    public class Task
    {
        [Key]
        public int TaskId { get; set; }

        [Required]
        public int RoomId { get; set; }

        public string TaskName { get; set; } = "Dọn phòng";

        public int? AssignedTo { get; set; }

        [Required]
        public int ShiftId { get; set; }

        [Required]
        public int TaskStatus { get; set; } = 0; 
        // 0: Chờ thực hiện
        // 1: Đang dọn dẹp
        // 2: Chờ xác nhận
        // 3: Hoàn thành
        // 4: Yêu cầu dọn lại

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        // Mở rộng hiển thị
        public string StatusName
        {
            get
            {
                return TaskStatus switch
                {
                    0 => "Chờ nhận việc",
                    1 => "Đang thực hiện",
                    2 => "Chờ xác nhận",
                    3 => "Hoàn thành",
                    4 => "Yêu cầu làm lại",
                    _ => "Không xác định"
                };
            }
        }
    }
}
