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
                lblTitle.Content = "Quản lý nhiệm vụ";
                AdminAssignPanel.Visibility = Visibility.Visible;
                LoadAssignFormControls();
            }
            else
            {
                AdminAssignPanel.Visibility = Visibility.Collapsed;
            }
            LoadData();
        }

        private async void LoadAssignFormControls()
        {
            try
            {
                int currentShiftType = (DateTime.Now.Hour >= 15) ? 1 : 0;
                var (rooms, staffList) = await System.Threading.Tasks.Task.Run(() =>
                {
                    var roomList = new System.Collections.Generic.List<Room>();
                    var staff = new System.Collections.Generic.List<User>();

                    using (MySqlConnection conn = DatabaseHelper.GetConnection())
                    {
                        DatabaseHelper.OpenConnection(conn);
                        string queryRooms = "SELECT RoomId, RoomNumber FROM Rooms ORDER BY RoomNumber";
                        using (MySqlCommand cmd = new MySqlCommand(queryRooms, conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                roomList.Add(new Room { RoomId = reader.GetInt32("RoomId"), RoomNumber = reader.GetString("RoomNumber") });
                            }
                        }

                        staff.Add(new User { Id = 0, FullName = "-- Ai rảnh nhận việc --" });
                        string queryStaff = @"
                            SELECT u.Id, u.FullName,
                                   EXISTS(
                                       SELECT 1 FROM Shifts s 
                                       WHERE s.WorkDate = CURDATE() AND s.ShiftType = @ShiftType 
                                         AND (s.Employee1_Id = u.Id OR s.Employee2_Id = u.Id)
                                   ) AS IsOnDuty
                            FROM Users u 
                            WHERE u.Role = 1 
                            ORDER BY IsOnDuty DESC, u.FullName ASC";

                        using (MySqlCommand cmd = new MySqlCommand(queryStaff, conn))
                        {
                            cmd.Parameters.Add("@ShiftType", MySqlDbType.Int32).Value = currentShiftType;
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    bool onDuty = Convert.ToInt32(reader["IsOnDuty"]) == 1;
                                    string name = reader.GetString("FullName") + (onDuty ? " (Đang trực)" : "");
                                    staff.Add(new User { Id = reader.GetInt32("Id"), FullName = name });
                                }
                            }
                        }
                    }
                    return (roomList, staff);
                });

                cbTaskRoom.ItemsSource = rooms;
                if (rooms.Count > 0) cbTaskRoom.SelectedIndex = 0;
                cbTaskStaff.ItemsSource = staffList;
                cbTaskStaff.SelectedIndex = 0;
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

            string rawInputTaskName = txtTaskName.Text.Trim();
            string taskName = string.IsNullOrWhiteSpace(rawInputTaskName)
                ? $"Dọn dẹp phòng {selectedRoom.RoomNumber}"
                : rawInputTaskName;
            bool isCleaning = IsCleaningTaskName(taskName);

            try
            {
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    DatabaseHelper.OpenConnection(conn);
                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            if (isCleaning && string.IsNullOrWhiteSpace(rawInputTaskName))
                            {
                                string checkDupQuery = "SELECT COUNT(*) FROM Tasks WHERE RoomId = @RoomId AND TaskStatus IN (0, 1, 2, 4)";
                                using (MySqlCommand checkCmd = new MySqlCommand(checkDupQuery, conn, trans))
                                {
                                    checkCmd.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = roomId;
                                    if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                                    {
                                        trans.Rollback();
                                        MessageBox.Show($"Phòng {selectedRoom.RoomNumber} hiện đã có nhiệm vụ đang thực hiện!\nNếu muốn giao thêm việc phụ khác, vui lòng nhập rõ tên công việc vào ô 'Nhiệm vụ'.", "Cảnh báo trùng việc", MessageBoxButton.OK, MessageBoxImage.Warning);
                                        return;
                                    }
                                }
                            }

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

                            if (isCleaning)
                            {
                                int newRoomStatus = assignedTo.HasValue ? 4 : 3;
                                string updateRoom = "UPDATE Rooms SET Status = @NewStatus WHERE RoomId = @RoomId AND Status IN (0, 3)";
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

        internal async void LoadData()
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
            {
                MessageBox.Show("Không xác định được người dùng đang đăng nhập!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int currentUserId = mainWindow.CurrentUserId;
            int currentRole = mainWindow.CurrentRole;
            bool isAdmin = (currentRole == 0);

            try
            {
                var dt = await System.Threading.Tasks.Task.Run(() =>
                {
                    DataTable dataTable = new DataTable();
                    using (MySqlConnection conn = DatabaseHelper.GetConnection())
                    {
                        DatabaseHelper.OpenConnection(conn);
                        string query = @"
                            SELECT t.TaskId, t.TaskName, s.WorkDate, s.ShiftType, r.RoomNumber, t.TaskStatus, t.AssignedTo, u.FullName as AssigneeName, t.CompletedAt
                            FROM Tasks t
                            JOIN Shifts s ON t.ShiftId = s.ShiftId
                            JOIN Rooms r ON t.RoomId = r.RoomId
                            LEFT JOIN Users u ON t.AssignedTo = u.Id";

                        if (currentRole == 1)
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
                            if (currentRole == 1)
                            {
                                cmd.Parameters.Add("@UserId", MySqlDbType.Int32).Value = currentUserId;
                            }

                            using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                            {
                                da.Fill(dataTable);
                            }
                        }
                    }

                    dataTable.Columns.Add("ShiftName", typeof(string));
                    dataTable.Columns.Add("StatusName", typeof(string));
                    dataTable.Columns.Add("StatusBgColor", typeof(string));
                    dataTable.Columns.Add("StatusFgColor", typeof(string));
                    dataTable.Columns.Add("PrimaryActionText", typeof(string));
                    dataTable.Columns.Add("PrimaryActionBgColor", typeof(string));
                    dataTable.Columns.Add("ShowPrimaryAction", typeof(bool));
                    dataTable.Columns.Add("ShowRejectAction", typeof(bool));
                    dataTable.Columns.Add("ShowDeleteAction", typeof(bool));
                    dataTable.Columns.Add("CompletedAtText", typeof(string));

                    foreach (DataRow row in dataTable.Rows)
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
                        bool isMine = isAssigned && assignedId == currentUserId;
                        bool isUnassigned = !isAssigned;

                        var task = new Hotel_Management.Models.Task { TaskStatus = status };
                        row["StatusName"] = task.StatusName;

                        switch (status)
                        {
                            case 0:
                                row["StatusBgColor"] = "#25E67E22";
                                row["StatusFgColor"] = "#E67E22";
                                break;
                            case 1:
                                row["StatusBgColor"] = "#253498DB";
                                row["StatusFgColor"] = "#3498DB";
                                break;
                            case 2:
                                row["StatusBgColor"] = "#25F39C12";
                                row["StatusFgColor"] = "#F39C12";
                                break;
                            case 3:
                                row["StatusBgColor"] = "#252ECC71";
                                row["StatusFgColor"] = "#2ECC71";
                                break;
                            case 4:
                                row["StatusBgColor"] = "#25E74C3C";
                                row["StatusFgColor"] = "#E74C3C";
                                break;
                            default:
                                row["StatusBgColor"] = "#2595A5A6";
                                row["StatusFgColor"] = "#95A5A6";
                                break;
                        }

                        if (isAdmin)
                        {
                            if (status == 2)
                            {
                                row["PrimaryActionText"] = "Duyệt";
                                row["PrimaryActionBgColor"] = "#10B981";
                                row["ShowPrimaryAction"] = true;
                                row["ShowRejectAction"] = true;
                            }
                            else
                            {
                                row["ShowPrimaryAction"] = false;
                                row["ShowRejectAction"] = false;
                            }

                            row["ShowDeleteAction"] = (status != 3);
                        }
                        else
                        {
                            row["ShowRejectAction"] = false;
                            row["ShowDeleteAction"] = false;

                            if (status == 0)
                            {
                                if (isUnassigned || isMine)
                                {
                                    row["PrimaryActionText"] = "Nhận việc";
                                    row["PrimaryActionBgColor"] = "#3B82F6";
                                    row["ShowPrimaryAction"] = true;
                                }
                                else
                                {
                                    row["ShowPrimaryAction"] = false;
                                }
                            }
                            else if (status == 1)
                            {
                                if (isMine)
                                {
                                    row["PrimaryActionText"] = "Báo cáo";
                                    row["PrimaryActionBgColor"] = "#10B981";
                                    row["ShowPrimaryAction"] = true;
                                }
                                else
                                {
                                    row["ShowPrimaryAction"] = false;
                                }
                            }
                            else if (status == 4)
                            {
                                if (isMine)
                                {
                                    row["PrimaryActionText"] = "Báo cáo";
                                    row["PrimaryActionBgColor"] = "#10B981";
                                    row["ShowPrimaryAction"] = true;
                                }
                                else
                                {
                                    row["ShowPrimaryAction"] = false;
                                }
                            }
                            else
                            {
                                row["ShowPrimaryAction"] = false;
                            }
                        }
                    }

                    return dataTable;
                });

                dgTasks.ItemsSource = dt.DefaultView;
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

            if (currentStatus == 0 && mainWindow.CurrentRole == 0)
            {
                MessageBox.Show("Tài khoản Quản trị viên chỉ quản lý và giao việc, không trực tiếp nhận việc!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if ((currentStatus == 1 || currentStatus == 4) && mainWindow.CurrentRole == 0)
            {
                MessageBox.Show("Chỉ nhân viên phụ trách mới thực hiện báo cáo hoàn thành nhiệm vụ!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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
                    DatabaseHelper.OpenConnection(conn);
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

                            if (currentStatus == 0)
                            {
                                string query = @"UPDATE Tasks 
                                                 SET AssignedTo = COALESCE(AssignedTo, @UserId), TaskStatus = 1 
                                                 WHERE TaskId = @TaskId AND (AssignedTo IS NULL OR AssignedTo = @UserId OR @IsAdmin = 1)";
                                using (MySqlCommand cmd = new MySqlCommand(query, conn, trans))
                                {
                                    cmd.Parameters.Add("@UserId", MySqlDbType.Int32).Value = mainWindow.CurrentUserId;
                                    cmd.Parameters.Add("@TaskId", MySqlDbType.Int32).Value = taskId;
                                    cmd.Parameters.Add("@IsAdmin", MySqlDbType.Int32).Value = (mainWindow.CurrentRole == 0) ? 1 : 0;
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
                            else if (currentStatus == 1 || currentStatus == 4)
                            {
                                string query = @"UPDATE Tasks 
                                                 SET TaskStatus = 2 
                                                 WHERE TaskId = @TaskId AND (AssignedTo = @UserId OR @IsAdmin = 1)";
                                using (MySqlCommand cmd = new MySqlCommand(query, conn, trans))
                                {
                                    cmd.Parameters.Add("@TaskId", MySqlDbType.Int32).Value = taskId;
                                    cmd.Parameters.Add("@UserId", MySqlDbType.Int32).Value = mainWindow.CurrentUserId;
                                    cmd.Parameters.Add("@IsAdmin", MySqlDbType.Int32).Value = (mainWindow.CurrentRole == 0) ? 1 : 0;
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else if (currentStatus == 2 && mainWindow.CurrentRole == 0)
                            {
                                string query = "UPDATE Tasks SET TaskStatus = 3, CompletedAt = @CompletedAt WHERE TaskId = @TaskId";
                                using (MySqlCommand cmd = new MySqlCommand(query, conn, trans))
                                {
                                    cmd.Parameters.Add("@CompletedAt", MySqlDbType.DateTime).Value = DateTime.Now;
                                    cmd.Parameters.Add("@TaskId", MySqlDbType.Int32).Value = taskId;
                                    cmd.ExecuteNonQuery();
                                }

                                if (isCleaningTask)
                                {
                                    string checkRemaining = "SELECT COUNT(*) FROM Tasks WHERE RoomId = @RoomId AND TaskStatus IN (0, 1, 2, 4)";
                                    int remainingTasks = 0;
                                    using (MySqlCommand cmdCheck = new MySqlCommand(checkRemaining, conn, trans))
                                    {
                                        cmdCheck.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = roomId;
                                        remainingTasks = Convert.ToInt32(cmdCheck.ExecuteScalar());
                                    }

                                    if (remainingTasks == 0)
                                    {
                                        string updateRoom = "UPDATE Rooms SET Status = 0 WHERE RoomId = @RoomId AND Status IN (3, 4)";
                                        using (MySqlCommand cmdRoom = new MySqlCommand(updateRoom, conn, trans))
                                        {
                                            cmdRoom.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = roomId;
                                            cmdRoom.ExecuteNonQuery();
                                        }
                                    }
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
                    successMsg = "Đã gửi báo cáo hoàn thành! Nhiệm vụ chuyển sang trạng thái 'Chờ xác nhận'.";
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
                    DatabaseHelper.OpenConnection(conn);
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

        private void btnDeleteTask_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null || mainWindow.CurrentRole != 0)
            {
                MessageBox.Show("Chỉ Quản trị viên mới có quyền hủy nhiệm vụ!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var btn = sender as Button;
            if (btn == null || btn.DataContext == null) return;
            var rowView = btn.DataContext as DataRowView;
            if (rowView == null) return;

            int taskId = Convert.ToInt32(rowView["TaskId"]);
            string taskName = rowView["TaskName"]?.ToString() ?? "";

            var result = MessageBox.Show($"Xác nhận HỦY nhiệm vụ '{taskName}'?", "Xác nhận hủy", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    DatabaseHelper.OpenConnection(conn);
                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            int roomId = 0;
                            string getRoom = "SELECT RoomId FROM Tasks WHERE TaskId = @TaskId";
                            using (var cmdGet = new MySqlCommand(getRoom, conn, trans))
                            {
                                cmdGet.Parameters.Add("@TaskId", MySqlDbType.Int32).Value = taskId;
                                var res = cmdGet.ExecuteScalar();
                                if (res != null && res != DBNull.Value) roomId = Convert.ToInt32(res);
                            }

                            string query = "DELETE FROM Tasks WHERE TaskId = @TaskId";
                            using (MySqlCommand cmd = new MySqlCommand(query, conn, trans))
                            {
                                cmd.Parameters.Add("@TaskId", MySqlDbType.Int32).Value = taskId;
                                cmd.ExecuteNonQuery();
                            }

                            if (roomId > 0)
                            {
                                string countActive = "SELECT COUNT(*) FROM Tasks WHERE RoomId = @RoomId AND TaskStatus IN (0, 1, 2, 4)";
                                int activeCount = 0;
                                using (var cmdCount = new MySqlCommand(countActive, conn, trans))
                                {
                                    cmdCount.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = roomId;
                                    activeCount = Convert.ToInt32(cmdCount.ExecuteScalar());
                                }

                                if (activeCount == 0)
                                {
                                    string releaseRoom = "UPDATE Rooms SET Status = 0 WHERE RoomId = @RoomId AND Status IN (3, 4)";
                                    using (var cmdRel = new MySqlCommand(releaseRoom, conn, trans))
                                    {
                                        cmdRel.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = roomId;
                                        cmdRel.ExecuteNonQuery();
                                    }
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
                MessageBox.Show("Đã hủy nhiệm vụ thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("Lỗi khi hủy nhiệm vụ: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
