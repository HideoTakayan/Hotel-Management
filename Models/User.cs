using System;
using System.ComponentModel.DataAnnotations;

namespace Hotel_Management.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        [MaxLength(50)]
        public string FullName { get; set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; set; }

        [MaxLength(12)]
        public string Phone { get; set; }
        public int Role { get; set; }

        public string RoleName
        {
            get
            {
                return Role switch
                {
                    0 => "Quản lý",
                    1 => "Nhân viên",
                    _ => "Không xác định"
                };
            }
        }
    }
}
