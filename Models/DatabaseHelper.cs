using System;
using System.Configuration;
using MySql.Data.MySqlClient;

namespace Hotel_Management.Models
{
    public static class DatabaseHelper
    {
        private static string _connStr;
        public static string MySqlConnStr
        {
            get
            {
                if (_connStr == null)
                {
                    var raw = ConfigurationManager.ConnectionStrings["MySqlConn"]?.ConnectionString
                        ?? throw new InvalidOperationException("Không tìm thấy cấu hình chuỗi kết nối 'MySqlConn' trong App.config!");

                    var builder = new MySqlConnectionStringBuilder(raw);
                    builder.MinimumPoolSize = 0;
                    builder.MaximumPoolSize = 20;
                    builder.ConnectionLifeTime = 30;
                    builder.Keepalive = 10;
                    if (builder.ConnectionTimeout == 0 || builder.ConnectionTimeout > 5)
                        builder.ConnectionTimeout = 5;
                    if (builder.DefaultCommandTimeout == 0 || builder.DefaultCommandTimeout > 10)
                        builder.DefaultCommandTimeout = 10;

                    _connStr = builder.ConnectionString;
                }
                return _connStr;
            }
        }

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(MySqlConnStr);
        }

        public static void OpenConnection(MySqlConnection conn)
        {
            if (conn.State == System.Data.ConnectionState.Open) return;
            try
            {
                conn.Open();
            }
            catch
            {
                MySqlConnection.ClearPool(conn);
                conn.Open();
            }
        }

        public static async System.Threading.Tasks.Task OpenConnectionAsync(MySqlConnection conn)
        {
            if (conn.State == System.Data.ConnectionState.Open) return;
            try
            {
                await conn.OpenAsync();
            }
            catch
            {
                MySqlConnection.ClearPool(conn);
                await conn.OpenAsync();
            }
        }

        public static MySqlConnection GetOpenConnection()
        {
            var conn = GetConnection();
            OpenConnection(conn);
            return conn;
        }

        public static async System.Threading.Tasks.Task<MySqlConnection> GetOpenConnectionAsync()
        {
            var conn = GetConnection();
            await OpenConnectionAsync(conn);
            return conn;
        }

        public static int EnsureShiftForType(MySqlConnection conn, int shiftType, MySqlTransaction trans = null)
        {
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

            var staffList = new System.Collections.Generic.List<(int Id, string FullName)>();
            string queryStaff = "SELECT Id, FullName FROM Users WHERE Role = 1 ORDER BY Id ASC";
            using (var cmdStaff = new MySqlCommand(queryStaff, conn, trans))
            using (var reader = cmdStaff.ExecuteReader())
            {
                while (reader.Read())
                {
                    staffList.Add((reader.GetInt32("Id"), reader.GetString("FullName")));
                }
            }

            if (staffList.Count == 0)
            {
                string queryUsers = "SELECT Id, FullName FROM Users ORDER BY Role DESC, Id ASC";
                using (var cmdUser = new MySqlCommand(queryUsers, conn, trans))
                using (var reader = cmdUser.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        staffList.Add((reader.GetInt32("Id"), reader.GetString("FullName")));
                    }
                }
            }

            int? empAn = null;
            int? empBinh = null;
            int? empLong = null;
            int? empDuc = null;

            foreach (var staff in staffList)
            {
                string name = staff.FullName ?? "";
                if (empAn == null && name.IndexOf("An", StringComparison.OrdinalIgnoreCase) >= 0)
                    empAn = staff.Id;
                else if (empBinh == null && (name.IndexOf("Bình", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Binh", StringComparison.OrdinalIgnoreCase) >= 0))
                    empBinh = staff.Id;
                else if (empLong == null && name.IndexOf("Long", StringComparison.OrdinalIgnoreCase) >= 0)
                    empLong = staff.Id;
                else if (empDuc == null && (name.IndexOf("Đức", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Duc", StringComparison.OrdinalIgnoreCase) >= 0))
                    empDuc = staff.Id;
            }

            int emp1 = -1, emp2 = -1;

            if (shiftType == 0)
            {
                emp1 = empAn ?? (staffList.Count > 0 ? staffList[0].Id : 2);
                emp2 = empBinh ?? (staffList.Count > 1 ? staffList[1].Id : (staffList.Count > 0 ? staffList[0].Id : 3));
            }
            else
            {
                emp1 = empLong ?? (staffList.Count > 2 ? staffList[2].Id : (staffList.Count > 0 ? staffList[0].Id : 4));
                emp2 = empDuc ?? (staffList.Count > 3 ? staffList[3].Id : (staffList.Count > 1 ? staffList[1].Id : (staffList.Count > 0 ? staffList[0].Id : 5)));
            }

            if (emp1 == -1) emp1 = 1;
            if (emp2 == -1) emp2 = emp1;

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

        public static int GetOrCreateCurrentShiftId(MySqlConnection conn, MySqlTransaction trans = null)
        {
            int currentShiftType = (DateTime.Now.Hour >= 15) ? 1 : 0;
            return EnsureShiftForType(conn, currentShiftType, trans);
        }

        public static void EnsureTodayShifts(MySqlConnection conn, MySqlTransaction trans = null)
        {
            try
            {
                EnsureShiftForType(conn, 0, trans);
                EnsureShiftForType(conn, 1, trans);
            }
            catch
            {
            }
        }

        private static DateTime _lastSyncTime = DateTime.MinValue;
        private static int _isSyncing = 0;
        private static bool _servicesEnsured = false;

        public static void SyncAllDataToCurrentDate(MySqlConnection conn, bool force = false)
        {
            if (!force && (DateTime.Now - _lastSyncTime).TotalMinutes < 15)
            {
                return;
            }

            if (System.Threading.Interlocked.CompareExchange(ref _isSyncing, 1, 0) != 0)
            {
                return;
            }

            _lastSyncTime = DateTime.Now;

            try
            {
                using (var cmdTimeout = new MySqlCommand("SET SESSION innodb_lock_wait_timeout = 3;", conn))
                {
                    cmdTimeout.CommandTimeout = 3;
                    try { cmdTimeout.ExecuteNonQuery(); } catch { }
                }

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

                string sqlSyncUnclaimed = @"
                    UPDATE Bills 
                    SET Status = 2 
                    WHERE EndDate < CURDATE() AND Status = 0";
                using (var cmdUnclaimed = new MySqlCommand(sqlSyncUnclaimed, conn))
                {
                    cmdUnclaimed.CommandTimeout = 5;
                    cmdUnclaimed.ExecuteNonQuery();
                }

                string sqlSyncCompleted = @"
                    UPDATE Bills 
                    SET Status = 1 
                    WHERE EndDate < CURDATE() AND Status IN (3, 4)";
                using (var cmdCompleted = new MySqlCommand(sqlSyncCompleted, conn))
                {
                    cmdCompleted.CommandTimeout = 5;
                    cmdCompleted.ExecuteNonQuery();
                }

                EnsureTodayShifts(conn);

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

        public static void EnsureDefaultOnsenServices(MySqlConnection conn)
        {
            if (_servicesEnsured) return;
            try
            {
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
                    ("Vé tắm khoáng nóng Public Onsen (Bể Rotenburo & Jacuzzi)", 350000m, "Lượt"),
                    ("Thuê bồn khoáng riêng tư gia đình (Private Kashikiri Hinoki)", 750000m, "Giờ"),
                    ("Xông hơi trị liệu đá muối Himalaya & Tuyết lạnh (Ganbanyoku)", 250000m, "Lượt"),
                    ("Ngâm bùn khoáng nóng dưỡng da & Xông thảo dược ngải cứu", 230000m, "Lượt"),
                    ("Ngâm bồn gỗ Bách Mộc thảo mộc bưởi vàng Yuzu (Yuzuyu Onsen)", 300000m, "Lượt"),
                    ("Ngâm chân khoáng nóng ngoài trời (Ashiyu) ngắm hoa anh đào", 120000m, "Lượt"),
                    ("Trải nghiệm tắm rừng trị liệu thiên nhiên (Shinrin-yoku)", 200000m, "Lượt"),
                    ("Vé tham quan vườn thiền Karesansui & Trải nghiệm thiền trà Zen", 180000m, "Lượt"),

                    ("Bóng bàn Onsen truyền thống (Onsen Takkyu - kèm vợt & bóng)", 80000m, "Giờ"),
                    ("Phòng Bida lỗ giải trí (Billiards Club - Bàn & Cơ cao cấp)", 120000m, "Giờ"),
                    ("Quầy bắn súng nút bấc trúng thưởng kiểu Nhật (Shateki Game)", 50000m, "Lượt"),
                    ("Thuê bàn cờ Tướng Nhật Bản Shogi & Cờ Vây (kèm trà xanh)", 60000m, "Buổi"),
                    ("Khu máy trò chơi điện tử Retro & Gắp thú Onsen (Xu chơi game)", 50000m, "10 Xu"),
                    ("Phòng hát Karaoke Ryokan cách âm cao cấp (kèm micro & dàn loa)", 250000m, "Giờ"),
                    ("Trò chơi ném vòng trúng thưởng dân gian Onsen (Wanage Game)", 40000m, "Lượt"),

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
