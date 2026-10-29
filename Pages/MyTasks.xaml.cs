using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Hotel_Management.Models;
using MySql.Data.MySqlClient;

namespace Hotel_Management.Pages
{
    public partial class MyTasks : Page
    {
        public MyTasks()
        {
            InitializeComponent();
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null && mainWindow.CurrentRole == 0)
            {
                lblTitle.Content = "Quản Lý Tất Cả Nhiệm Vụ";
                AdminAssignPanel.Visibility = Visibility.Visible;
                LoadAssignFormControls();
            }
            else
            {
                AdminAssignPanel.Visibility = Visibility.Collapsed;
            }
            LoadData();
        }

        private void LoadAssignFormControls()
        {
            try
            {
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    // Load rooms
                    string queryRooms = "SELECT RoomId, RoomNumber FROM Rooms ORDER BY RoomNumber";
                    var rooms = new System.Collections.Generic.List<Room>();
                    using (MySqlCommand cmd = new MySqlCommand(queryRooms, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rooms.Add(new Room { RoomId = reader.GetInt32("RoomId"), RoomNumber = "Phòng " + reader.GetString("RoomNumber") });
                        }
                    }
                    cbTaskRoom.ItemsSource = rooms;
                    if (rooms.Count > 0) cbTaskRoom.SelectedIndex = 0;

                    // Load staff (Role = 1)
                    string queryStaff = "SELECT Id, FullName FROM Users WHERE Role = 1 ORDER BY FullName";
                    var staffList = new System.Collections.Generic.List<User>();
                    staffList.Add(new User { Id = 0, FullName = "-- Ai rảnh nhận việc --" });
                    using (MySqlCommand cmd = new MySqlCommand(queryStaff, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            staffList.Add(new User { Id = reader.GetInt32("Id"), FullName = reader.GetString("FullName") });
                        }
                    }
                    cbTaskStaff.ItemsSource = staffList;
                    cbTaskStaff.SelectedIndex = 0;
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1042 || ex.Number == 0)
                    MessageBox.Show("Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show($"Lỗi Cơ Sở Dữ Liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi nạp danh sách giao việc: " + ex.Message);
            }
        }

        private static bool IsCleaningTaskName(string taskName)
        {
            if (string.IsNullOrWhiteSpace(taskName)) return true;
            if (taskName.StartsWith("Giao dịch vụ", StringComparison.OrdinalIgnoreCase)) return false;

            return taskName.IndexOf("dọn", StringComparison.OrdinalIgnoreCase) >= 0
                || taskName.IndexOf("vệ sinh", StringComparison.OrdinalIgnoreCase) >= 0
                || taskName.IndexOf("lau", StringComparison.OrdinalIgnoreCase) >= 0
                || taskName.IndexOf("thay chiếu", StringComparison.OrdinalIgnoreCase) >= 0
                || taskName.IndexOf("trải nệm", StringComparison.OrdinalIgnoreCase) >= 0
                || taskName.IndexOf("trải đệm", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void btnCreateTask_Click(object sender, RoutedEventArgs e)
        {
            if (cbTaskRoom.SelectedItem is not Room selectedRoom)
            {
                MessageBox.Show("Vui lòng chọn phòng cần giao việc!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedStaff = cbTaskStaff.SelectedItem as User;
            int roomId = selectedRoom.RoomId;
            int? assignedTo = (selectedStaff != null && selectedStaff.Id > 0) ? (int?)selectedStaff.Id : null;

            string taskName = txtTaskName.Text.Trim();
            if (string.IsNullOrWhiteSpace(taskName))
            {
                taskName = $"Dọn dẹp {selectedRoom.RoomNumber}";
            }

            try
            {
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            int shiftId = DatabaseHelper.GetOrCreateCurrentShiftId(conn, trans);

                            int initialStatus = assignedTo.HasValue ? 1 : 0;
                            string query = @"INSERT INTO Tasks (RoomId, AssignedTo, ShiftId, TaskName, TaskStatus) 
                                             VALUES (@RoomId, @AssignedTo, @ShiftId, @TaskName, @TaskStatus)";
                            using (MySqlCommand cmd = new MySqlCommand(query, conn, trans))
                            {
                                cmd.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = roomId;
                                cmd.Parameters.Add("@AssignedTo", MySqlDbType.Int32).Value = assignedTo.HasValue ? assignedTo.Value : (object)DBNull.Value;
                                cmd.Parameters.Add("@ShiftId", MySqlDbType.Int32).Value = shiftId;
                                cmd.Parameters.Add("@TaskName", MySqlDbType.VarChar, 255).Value = taskName;
                                cmd.Parameters.Add("@TaskStatus", MySqlDbType.Int32).Value = initialStatus;
                                cmd.ExecuteNonQuery();
                            }

                            bool isCleaning = IsCleaningTaskName(taskName);
                            if (isCleaning)
                            {
                                int newRoomStatus = assignedTo.HasValue ? 4 : 3;
                                string updateRoom = "UPDATE Rooms SET Status = @NewStatus WHERE RoomId = @RoomId AND Status = 0";
                                using (MySqlCommand cmdRoom = new MySqlCommand(updateRoom, conn, trans))
                                {
                                    cmdRoom.Parameters.Add("@NewStatus", MySqlDbType.Int32).Value = newRoomStatus;
                                    cmdRoom.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = roomId;
                                    cmdRoom.ExecuteNonQuery();
                                }
                            }

                            trans.Commit();
                            MessageBox.Show("Đã giao nhiệm vụ thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                            txtTaskName.Clear();
                            LoadData();
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1042 || ex.Number == 0)
                    MessageBox.Show("Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show($"Lỗi Cơ Sở Dữ Liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tạo nhiệm vụ: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData()
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
            {
                MessageBox.Show("Không xác định được người dùng đang đăng nhập!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                        SELECT t.TaskId, t.TaskName, s.WorkDate, s.ShiftType, r.RoomNumber, t.TaskStatus, t.AssignedTo, u.FullName as AssigneeName, t.CompletedAt
                        FROM Tasks t
                        JOIN Shifts s ON t.ShiftId = s.ShiftId
                        JOIN Rooms r ON t.RoomId = r.RoomId
                        LEFT JOIN Users u ON t.AssignedTo = u.Id";

                    if (mainWindow.CurrentRole == 1) // Nhân viên chỉ thấy việc của mình hoặc việc chưa ai nhận
                    {
                        query += @"
                        WHERE t.AssignedTo = @UserId 
                           OR t.AssignedTo IS NULL";
                    }

                    query += @" ORDER BY 
                                CASE 
                                    WHEN t.TaskStatus = 4 THEN 1
                                    WHEN t.TaskStatus = 0 THEN 2
                                    WHEN t.TaskStatus = 1 THEN 3
                                    WHEN t.TaskStatus = 2 THEN 4
                                    ELSE 5
                                END ASC, 
                                s.WorkDate DESC, 
                                s.ShiftType ASC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        if (mainWindow.CurrentRole == 1)
                        {
                            cmd.Parameters.Add("@UserId", MySqlDbType.Int32).Value = mainWindow.CurrentUserId;
                        }

                        using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);

                            // Add helper columns
                            dt.Columns.Add("ShiftName", typeof(string));
                            dt.Columns.Add("StatusName", typeof(string));
                            dt.Columns.Add("PrimaryActionText", typeof(string));
                            dt.Columns.Add("ShowPrimaryAction", typeof(bool));
                            dt.Columns.Add("ShowRejectAction", typeof(bool));
                            dt.Columns.Add("CompletedAtText", typeof(string));

                            bool isAdmin = (mainWindow.CurrentRole == 0);

                            foreach (DataRow row in dt.Rows)
                            {
                                int shiftType = Convert.ToInt32(row["ShiftType"]);
                                row["ShiftName"] = shiftType == 0 ? "Ca Sáng" : "Ca Tối";

                                if (row.IsNull("TaskName") || string.IsNullOrWhiteSpace(row["TaskName"].ToString()))
                                {
                                    row["TaskName"] = "Dọn phòng";
                                }

                                if (row.IsNull("AssigneeName") || string.IsNullOrWhiteSpace(row["AssigneeName"].ToString()))
                                {
                                    row["AssigneeName"] = "Chưa nhận việc";
                                }

                                if (!row.IsNull("CompletedAt"))
                                {
                                    DateTime dtVal = Convert.ToDateTime(row["CompletedAt"]);
                                    row["CompletedAtText"] = dtVal.ToString("dd/MM/yyyy HH:mm");
                                }
                                else
                                {
                                    row["CompletedAtText"] = "-";
                                }

                                int status = Convert.ToInt32(row["TaskStatus"]);
                                bool isAssigned = !row.IsNull("AssignedTo");
                                int assignedId = isAssigned ? Convert.ToInt32(row["AssignedTo"]) : 0;
                                bool isMine = isAssigned && assignedId == mainWindow.CurrentUserId;

                                var task = new Hotel_Management.Models.Task { TaskStatus = status };
                                row["StatusName"] = task.StatusName;

                                if (status == 0) // Chờ nhận việc
                                {
                                    row["PrimaryActionText"] = "Nhận việc";
                                    row["ShowPrimaryAction"] = true;
                                    row["ShowRejectAction"] = false;
                                }
                                else if (status == 1) // Đang thực hiện
                                {
                                    if (isMine || isAdmin)
                                    {
                                        row["PrimaryActionText"] = "Báo cáo xong";
                                        row["ShowPrimaryAction"] = true;
                                    }
                                    else
                                    {
                                        row["ShowPrimaryAction"] = false;
                                    }
                                    row["ShowRejectAction"] = false;
                                }
                                else if (status == 2) // Chờ xác nhận
                                {
                                    if (isAdmin)
                                    {
                                        row["PrimaryActionText"] = "Duyệt đạt";
                                        row["ShowPrimaryAction"] = true;
                                        row["ShowRejectAction"] = true;
                                    }
                                    else
                                    {
                                        row["ShowPrimaryAction"] = false;
                                        row["ShowRejectAction"] = false;
                                    }
                                }
                                else if (status == 4) // Yêu cầu làm lại
                                {
                                    if (isMine || isAdmin)
                                    {
                                        row["PrimaryActionText"] = "Làm lại & Báo cáo";
                                        row["ShowPrimaryAction"] = true;
                                    }
                                    else
                                    {
                                        row["ShowPrimaryAction"] = false;
                                    }
                                    row["ShowRejectAction"] = false;
                                }
                                else // status == 3: Hoàn thành
                                {
                                    row["ShowPrimaryAction"] = false;
                                    row["ShowRejectAction"] = false;
                                }
                            }

                            dgTasks.ItemsSource = dt.DefaultView;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1042 || ex.Number == 0)
                    MessageBox.Show("Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show($"Lỗi Cơ Sở Dữ Liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách công việc: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnUpdateTask_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null || btn.DataContext == null) return;
            
            var rowView = btn.DataContext as DataRowView;
            if (rowView == null) return;

            int taskId = Convert.ToInt32(rowView["TaskId"]);
            int currentStatus = Convert.ToInt32(rowView["TaskStatus"]);
            
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null) return;

            string confirmMsg = "Xác nhận thực hiện thao tác này?";
            if (currentStatus == 0) confirmMsg = "Bạn muốn nhận nhiệm vụ này?";
            else if (currentStatus == 1 || currentStatus == 4) confirmMsg = "Xác nhận gửi báo cáo hoàn thành nhiệm vụ?";
            else if (currentStatus == 2) confirmMsg = "Xác nhận nghiệm thu và DUYỆT ĐẠT nhiệm vụ này?";

            var result = MessageBox.Show(confirmMsg, "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            bool isCleaningTask = false;
            try
            {
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            int roomId = 0;
                            string taskName = "";
                            string getTaskInfo = "SELECT RoomId, TaskName FROM Tasks WHERE TaskId = @TaskId";
                            using (MySqlCommand cmdGet = new MySqlCommand(getTaskInfo, conn, trans))
                            {
                                cmdGet.Parameters.Add("@TaskId", MySqlDbType.Int32).Value = taskId;
                                using (var reader = cmdGet.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        roomId = reader.GetInt32("RoomId");
                                        taskName = reader.IsDBNull(reader.GetOrdinal("TaskName")) ? "" : reader.GetString("TaskName");
                                    }
                                }
                            }
                            isCleaningTask = roomId > 0 && IsCleaningTaskName(taskName);

                            if (currentStatus == 0) // Nhận việc
                            {
                                string query = "UPDATE Tasks SET AssignedTo = @UserId, TaskStatus = 1 WHERE TaskId = @TaskId AND (AssignedTo IS NULL OR AssignedTo = @UserId)";
                                using (MySqlCommand cmd = new MySqlCommand(query, conn, trans))
                                {
                                    cmd.Parameters.Add("@UserId", MySqlDbType.Int32).Value = mainWindow.CurrentUserId;
                                    cmd.Parameters.Add("@TaskId", MySqlDbType.Int32).Value = taskId;
                                    if (cmd.ExecuteNonQuery() == 0)
                                        throw new InvalidOperationException("Nhiệm vụ này đã được nhân viên khác nhận!");
                                }

                                if (isCleaningTask)
                                {
                                    string updateRoom = "UPDATE Rooms SET Status = 4 WHERE RoomId = @RoomId AND Status = 3";
                                    using (MySqlCommand cmdRoom = new MySqlCommand(updateRoom, conn, trans))
                                    {
                                        cmdRoom.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = roomId;
                                        cmdRoom.ExecuteNonQuery();
                                    }
                                }
                            }
                            else if (currentStatus == 2 || ((currentStatus == 1 || currentStatus == 4) && mainWindow.CurrentRole == 0))
                            {
                                // Quản lý Duyệt đạt (từ trạng thái 2) hoặc trực tiếp chốt hoàn thành (từ trạng thái 1, 4)
                                string query = "UPDATE Tasks SET TaskStatus = 3, CompletedAt = @CompletedAt WHERE TaskId = @TaskId";
                                using (MySqlCommand cmd = new MySqlCommand(query, conn, trans))
                                {
                                    cmd.Parameters.Add("@CompletedAt", MySqlDbType.DateTime).Value = DateTime.Now;
                                    cmd.Parameters.Add("@TaskId", MySqlDbType.Int32).Value = taskId;
                                    cmd.ExecuteNonQuery();
                                }

                                if (isCleaningTask)
                                {
                                    string updateRoom = "UPDATE Rooms SET Status = 0 WHERE RoomId = @RoomId AND Status IN (3, 4)";
                                    using (MySqlCommand cmdRoom = new MySqlCommand(updateRoom, conn, trans))
                                    {
                                        cmdRoom.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = roomId;
                                        cmdRoom.ExecuteNonQuery();
                                    }
                                }
                            }
                            else if (currentStatus == 1 || currentStatus == 4) // Nhân viên báo cáo xong -> chuyển sang Chờ xác nhận (2)
                            {
                                string query = "UPDATE Tasks SET TaskStatus = 2 WHERE TaskId = @TaskId AND AssignedTo = @UserId";
                                using (MySqlCommand cmd = new MySqlCommand(query, conn, trans))
                                {
                                    cmd.Parameters.Add("@TaskId", MySqlDbType.Int32).Value = taskId;
                                    cmd.Parameters.Add("@UserId", MySqlDbType.Int32).Value = mainWindow.CurrentUserId;
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            trans.Commit();
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }

                string successMsg = "Cập nhật nhiệm vụ thành công!";
                if (currentStatus == 0) successMsg = "Nhận việc thành công!";
                else if (currentStatus == 1 || currentStatus == 4) 
                    successMsg = (mainWindow.CurrentRole == 0) ? "Nhiệm vụ đã hoàn thành!" : "Đã gửi báo cáo! Đang chờ Quản lý nghiệm thu.";
                else if (currentStatus == 2)
                    successMsg = isCleaningTask ? "Nghiệm thu thành công! Phòng đã sẵn sàng đón khách." : "Đã nghiệm thu hoàn thành nhiệm vụ!";

                MessageBox.Show(successMsg, "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1042 || ex.Number == 0)
                    MessageBox.Show("Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show($"Lỗi Cơ Sở Dữ Liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cập nhật nhiệm vụ: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRejectTask_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null || btn.DataContext == null) return;
            var rowView = btn.DataContext as DataRowView;
            if (rowView == null) return;

            int taskId = Convert.ToInt32(rowView["TaskId"]);
            var result = MessageBox.Show("Bạn muốn yêu cầu nhân viên làm lại nhiệm vụ này?", "Xác nhận yêu cầu làm lại", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = "UPDATE Tasks SET TaskStatus = 4 WHERE TaskId = @TaskId";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@TaskId", MySqlDbType.Int32).Value = taskId;
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Đã yêu cầu nhân viên làm lại nhiệm vụ này!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1042 || ex.Number == 0)
                    MessageBox.Show("Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show($"Lỗi Cơ Sở Dữ Liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cập nhật nhiệm vụ: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null && mainWindow.CurrentRole == 0)
            {
                txtTaskName.Clear();
                LoadAssignFormControls();
            }
            LoadData();
        }
    }
}
