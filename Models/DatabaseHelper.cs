using System;
using System.Configuration;
using MySql.Data.MySqlClient;

namespace Hotel_Management.Models
{
    public static class DatabaseHelper
    {
        public static string MySqlConnStr =>
            ConfigurationManager.ConnectionStrings["MySqlConn"]?.ConnectionString
            ?? throw new InvalidOperationException("Không tìm thấy cấu hình chuỗi kết nối 'MySqlConn' trong App.config!");

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(MySqlConnStr);
        }

        /// <summary>
        /// Lấy ShiftId của ca trực hiện tại theo thời gian thực (CURDATE() & ShiftType).
        /// Nếu hôm nay chưa có ca trực tương ứng, tự động khởi tạo ca trực mới cho ngày hôm nay.
        /// </summary>
        public static int GetOrCreateCurrentShiftId(MySqlConnection conn, MySqlTransaction trans = null)
        {
            // Ca Sáng: 07:00 - 15:00 (ShiftType = 0), Ca Tối: 15:00 - 23:00 (ShiftType = 1)
            int shiftType = (DateTime.Now.Hour >= 15) ? 1 : 0;

            string queryShift = @"SELECT ShiftId FROM Shifts 
                                  WHERE WorkDate = CURDATE() AND ShiftType = @ShiftType 
                                  LIMIT 1";
            using (var cmdShift = new MySqlCommand(queryShift, conn, trans))
            {
                cmdShift.Parameters.Add("@ShiftType", MySqlDbType.Int32).Value = shiftType;
                var res = cmdShift.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                {
                    return Convert.ToInt32(res);
                }
            }

            int emp1 = -1, emp2 = -1;
            // Ưu tiên phân công nhân viên phục vụ/lễ tân (Role = 1) cho ca trực
            string queryStaff = "SELECT Id FROM Users WHERE Role = 1 ORDER BY Id ASC LIMIT 2";
            using (var cmdStaff = new MySqlCommand(queryStaff, conn, trans))
            using (var reader = cmdStaff.ExecuteReader())
            {
                if (reader.Read()) emp1 = reader.GetInt32("Id");
                if (reader.Read()) emp2 = reader.GetInt32("Id");
                else if (emp1 != -1) emp2 = emp1;
            }

            // Fallback nếu chưa có nhân viên nào có Role = 1, lấy bất kỳ user nào trong bảng Users
            if (emp1 == -1)
            {
                string queryUsers = "SELECT Id FROM Users ORDER BY Role DESC, Id ASC LIMIT 2";
                using (var cmdUser = new MySqlCommand(queryUsers, conn, trans))
                using (var reader = cmdUser.ExecuteReader())
                {
                    if (reader.Read()) emp1 = reader.GetInt32("Id");
                    if (reader.Read()) emp2 = reader.GetInt32("Id");
                    else if (emp1 != -1) emp2 = emp1;
                }
            }

            // Trường hợp hy hữu CSDL trống hoàn toàn bảng Users
            if (emp1 == -1) { emp1 = 1; emp2 = 1; }

            string insertShift = @"INSERT INTO Shifts (WorkDate, ShiftType, Employee1_Id, Employee2_Id) 
                                   VALUES (CURDATE(), @ShiftType, @Emp1, @Emp2)";
            using (var cmdInsert = new MySqlCommand(insertShift, conn, trans))
            {
                cmdInsert.Parameters.Add("@ShiftType", MySqlDbType.Int32).Value = shiftType;
                cmdInsert.Parameters.Add("@Emp1", MySqlDbType.Int32).Value = emp1;
                cmdInsert.Parameters.Add("@Emp2", MySqlDbType.Int32).Value = emp2;
                cmdInsert.ExecuteNonQuery();
                return (int)cmdInsert.LastInsertedId;
            }
        }

        /// <summary>
        /// Đảm bảo ca trực hôm nay đã tồn tại trong CSDL để hiển thị đồng bộ thời gian thực
        /// </summary>
        public static void EnsureTodayShifts(MySqlConnection conn)
        {
            try
            {
                GetOrCreateCurrentShiftId(conn);
            }
            catch
            {
                // Bỏ qua lỗi nếu đã được xử lý ở tầng ngoài
            }
        }

        private static DateTime _lastSyncTime = DateTime.MinValue;
        private static int _isSyncing = 0;
        private static bool _servicesEnsured = false;

        /// <summary>
        /// Đồng bộ toàn bộ dữ liệu hệ thống theo ngày thực tế (bắt đầu từ hôm nay CURDATE()):
        /// - Các nhiệm vụ trước ngày hiện tại (WorkDate < CURDATE()) tự động hoàn tất (TaskStatus = 3).
        /// - Các hóa đơn có ngày trả phòng trước hôm nay (EndDate < CURDATE()) tự động hoàn tất/thanh toán (Status = 1).
        /// - Đồng bộ trạng thái phòng thực tế (Đang ở nếu có khách check-in hôm nay, Đã đặt nếu có khách đặt trước, Trống nếu hết hạn).
        /// Cơ chế Throttle 15 phút và Non-blocking Lock đảm bảo không bao giờ lock table hay làm đơ giao diện.
        /// </summary>
        public static void SyncAllDataToCurrentDate(MySqlConnection conn, bool force = false)
        {
            if (!force && (DateTime.Now - _lastSyncTime).TotalMinutes < 15)
            {
                return;
            }

            // Đảm bảo không bao giờ có 2 luồng cùng thực hiện đồng bộ CSDL cùng lúc (tránh deadlock)
            if (System.Threading.Interlocked.CompareExchange(ref _isSyncing, 1, 0) != 0)
            {
                return;
            }

            _lastSyncTime = DateTime.Now;

            try
            {
                // Giảm thời gian chờ lock xuống tối đa 3 giây để không bao giờ treo ứng dụng
                using (var cmdTimeout = new MySqlCommand("SET SESSION innodb_lock_wait_timeout = 3;", conn))
                {
                    cmdTimeout.CommandTimeout = 3;
                    try { cmdTimeout.ExecuteNonQuery(); } catch { }
                }

                // 1. Đồng bộ Tasks: Các nhiệm vụ thuộc ca làm trước ngày hôm nay chưa hoàn thành -> Đánh dấu hoàn tất
                string sqlSyncTasks = @"
                    UPDATE Tasks t
                    JOIN Shifts s ON t.ShiftId = s.ShiftId
                    SET t.TaskStatus = 3,
                        t.CompletedAt = COALESCE(t.CompletedAt, CONCAT(s.WorkDate, ' 14:00:00'))
                    WHERE s.WorkDate < CURDATE() AND t.TaskStatus != 3";
                using (var cmdTasks = new MySqlCommand(sqlSyncTasks, conn))
                {
                    cmdTasks.CommandTimeout = 5;
                    cmdTasks.ExecuteNonQuery();
                }

                // 2. Đồng bộ Bills: Các hóa đơn có ngày trả phòng trước ngày hôm nay (EndDate < CURDATE()) -> Đã thanh toán (Status = 1)
                string sqlSyncBills = @"
                    UPDATE Bills 
                    SET Status = 1 
                    WHERE EndDate < CURDATE() AND Status IN (0, 3, 4)";
                using (var cmdBills = new MySqlCommand(sqlSyncBills, conn))
                {
                    cmdBills.CommandTimeout = 5;
                    cmdBills.ExecuteNonQuery();
                }

                // 3. Đảm bảo ca trực hôm nay tồn tại trong hệ thống
                EnsureTodayShifts(conn);

                // 4. Đồng bộ Trạng thái Phòng theo hóa đơn đang hoạt động của ngày hôm nay:
                // - Đang ở (Status = 2): Phòng có hóa đơn Check-in (Status = 3) và đang trong thời gian lưu trú hôm nay
                string sqlSyncOccupied = @"
                    UPDATE Rooms r
                    JOIN Bills b ON r.RoomId = b.RoomId
                    SET r.Status = 2
                    WHERE b.Status = 3 AND b.StartDate <= CURDATE() AND b.EndDate >= CURDATE()";
                using (var cmdOccupied = new MySqlCommand(sqlSyncOccupied, conn))
                {
                    cmdOccupied.CommandTimeout = 5;
                    cmdOccupied.ExecuteNonQuery();
                }

                // - Đã đặt (Status = 1): Phòng có hóa đơn đặt trước (Status = 0) và chưa hết hạn lưu trú
                string sqlSyncBooked = @"
                    UPDATE Rooms r
                    JOIN Bills b ON r.RoomId = b.RoomId
                    SET r.Status = 1
                    WHERE b.Status = 0 AND b.EndDate >= CURDATE() AND r.Status != 2";
                using (var cmdBooked = new MySqlCommand(sqlSyncBooked, conn))
                {
                    cmdBooked.CommandTimeout = 5;
                    cmdBooked.ExecuteNonQuery();
                }

                // - Trống (Status = 0): Các phòng không có hóa đơn Check-in (3) hay Đặt trước (0) cho hôm nay
                string sqlSyncAvailable = @"
                    UPDATE Rooms 
                    SET Status = 0 
                    WHERE RoomId NOT IN (
                        SELECT RoomId FROM Bills 
                        WHERE (Status = 3 AND StartDate <= CURDATE() AND EndDate >= CURDATE())
                           OR (Status = 0 AND EndDate >= CURDATE())
                    ) AND Status NOT IN (3, 4)";
                using (var cmdAvailable = new MySqlCommand(sqlSyncAvailable, conn))
                {
                    cmdAvailable.CommandTimeout = 5;
                    cmdAvailable.ExecuteNonQuery();
                }

                // 5. Đảm bảo danh mục dịch vụ Onsen & Ryokan đầy đủ
                EnsureDefaultOnsenServices(conn);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi đồng bộ dữ liệu thời gian thực: " + ex.Message);
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref _isSyncing, 0);
            }
        }

        /// <summary>
        /// Tự động cập nhật danh mục dịch vụ Onsen & Ryokan chuẩn Nhật Bản thực tế nếu chưa có trong CSDL.
        /// Chỉ chạy 1 lần trong phiên làm việc.
        /// </summary>
        public static void EnsureDefaultOnsenServices(MySqlConnection conn)
        {
            if (_servicesEnsured) return;
            try
            {
                // Kiểm tra nhanh: Nếu bảng Services đã có từ 15 dịch vụ trở lên thì bỏ qua ngay, không duyệt vòng lặp
                using (var cmdCount = new MySqlCommand("SELECT COUNT(*) FROM Services", conn))
                {
                    cmdCount.CommandTimeout = 5;
                    var countObj = cmdCount.ExecuteScalar();
                    if (countObj != null && Convert.ToInt32(countObj) >= 15)
                    {
                        _servicesEnsured = true;
                        return;
                    }
                }

                var defaultServices = new (string Name, decimal Price, string Unit)[]
                {
                    // Nhóm 1: Trải nghiệm & Tiện ích Onsen (Khách tự trải nghiệm, không cần nhân viên giao việc)
                    ("Vé tắm khoáng nóng Public Onsen (Bể Rotenburo & Jacuzzi)", 350000m, "Lượt"),
                    ("Thuê bồn khoáng riêng tư gia đình (Private Kashikiri Hinoki)", 750000m, "Giờ"),
                    ("Xông hơi trị liệu đá muối Himalaya & Tuyết lạnh (Ganbanyoku)", 250000m, "Lượt"),
                    ("Ngâm bùn khoáng nóng dưỡng da & Xông thảo dược ngải cứu", 230000m, "Lượt"),
                    ("Ngâm bồn gỗ Bách Mộc thảo mộc bưởi vàng Yuzu (Yuzuyu Onsen)", 300000m, "Lượt"),
                    ("Ngâm chân khoáng nóng ngoài trời (Ashiyu) ngắm hoa anh đào", 120000m, "Lượt"),
                    ("Trải nghiệm tắm rừng trị liệu thiên nhiên (Shinrin-yoku)", 200000m, "Lượt"),
                    ("Vé tham quan vườn thiền Karesansui & Trải nghiệm thiền trà Zen", 180000m, "Lượt"),

                    // Nhóm 2: Trò chơi Giải trí Onsen & Ryokan (Văn hóa Ryokan Nhật Bản / Thám tử Conan)
                    ("Bóng bàn Onsen truyền thống (Onsen Takkyu - kèm vợt & bóng)", 80000m, "Giờ"),
                    ("Phòng Bida lỗ giải trí (Billiards Club - Bàn & Cơ cao cấp)", 120000m, "Giờ"),
                    ("Quầy bắn súng nút bấc trúng thưởng kiểu Nhật (Shateki Game)", 50000m, "Lượt"),
                    ("Thuê bàn cờ Tướng Nhật Bản Shogi & Cờ Vây (kèm trà xanh)", 60000m, "Buổi"),
                    ("Khu máy trò chơi điện tử Retro & Gắp thú Onsen (Xu chơi game)", 50000m, "10 Xu"),
                    ("Phòng hát Karaoke Ryokan cách âm cao cấp (kèm micro & dàn loa)", 250000m, "Giờ"),
                    ("Trò chơi ném vòng trúng thưởng dân gian Onsen (Wanage Game)", 40000m, "Lượt"),

                    // Nhóm 3: Ẩm thực & Phục vụ tại phòng (Cần nhân viên trực tiếp phục vụ / giao việc)
                    ("Thực đơn tối thượng hạng Kaiseki Ryori truyền thống (9 món)", 850000m, "Suất"),
                    ("Set Bò Wagyu A5 thượng hạng nướng đá nham thạch tại phòng", 950000m, "Suất"),
                    ("Buffet Ẩm thực Nhật Bản (Sushi, Sashimi cá hồi & Lẩu Shabu)", 550000m, "Suất"),
                    ("Set bữa sáng Ryokan truyền thống (Cá hồi nướng, Súp Miso)", 220000m, "Suất"),
                    ("Mì Ramen đêm truyền thống phục vụ tại phòng (Yonaki Soba)", 130000m, "Bát"),
                    ("Trứng trường thọ luộc suối khoáng nóng (Onsen Tamago 4 quả)", 65000m, "Phần"),
                    ("Trà đạo Uji Matcha thượng hạng & Bánh ngọt Wagashi theo mùa", 120000m, "Phần"),
                    ("Rượu Sake Junmai Daiginjo ướp lạnh (Chai 720ml cao cấp)", 680000m, "Chai"),
                    ("Rượu Mơ Nhật Bản Umeshu thủ công kèm đá viên tinh khiết", 320000m, "Bình"),
                    ("Bia thủ công Nhật Bản Asahi Super Dry / Sapporo ướp lạnh", 55000m, "Lon"),
                    ("Trị liệu bấm huyệt Shiatsu Nhật Bản chuyên sâu (60 phút)", 550000m, "Lượt"),
                    ("Thuê trang phục Kimono / Yukata cao cấp (kèm guốc Geta & quạt)", 180000m, "Bộ"),
                    ("Dịch vụ giặt hấp & Là hơi trang phục cao cấp lấy nhanh", 80000m, "Kg"),
                    ("Thêm bộ nệm Futon & Chiếu cói Tatami dự phòng cho khách", 250000m, "Bộ")
                };

                var existingNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string loadQuery = "SELECT ServiceName FROM Services";
                using (var cmdLoad = new MySqlCommand(loadQuery, conn))
                using (var reader = cmdLoad.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        existingNames.Add(reader.GetString("ServiceName"));
                    }
                }

                string insertQuery = "INSERT INTO Services (ServiceName, Price, Unit) VALUES (@Name, @Price, @Unit)";
                foreach (var svc in defaultServices)
                {
                    if (!existingNames.Contains(svc.Name))
                    {
                        using (var cmdInsert = new MySqlCommand(insertQuery, conn))
                        {
                            cmdInsert.Parameters.Add("@Name", MySqlDbType.VarChar, 100).Value = svc.Name;
                            cmdInsert.Parameters.Add("@Price", MySqlDbType.Decimal).Value = svc.Price;
                            cmdInsert.Parameters.Add("@Unit", MySqlDbType.VarChar, 20).Value = svc.Unit;
                            cmdInsert.ExecuteNonQuery();
                        }
                        existingNames.Add(svc.Name);
                    }
                }
                _servicesEnsured = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi khởi tạo dịch vụ mẫu: " + ex.Message);
            }
        }
    }
}
