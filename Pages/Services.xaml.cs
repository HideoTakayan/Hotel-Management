using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Hotel_Management.Models;
using MySql.Data.MySqlClient;

namespace Hotel_Management.Pages
{
    public partial class Services : Page
    {
        public Services()
        {
            InitializeComponent();
            CheckPermissions();
            LoadData();
        }

        private void CheckPermissions()
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null && mainWindow.CurrentRole == 1)
            {
                btnAdd.Visibility = Visibility.Collapsed;
                btnUpdate.Visibility = Visibility.Collapsed;
                btnDel.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadData()
        {
            LoadServiceList();
            LoadOrderComboboxes();
        }

        private void LoadServiceList()
        {
            try
            {
                var services = new List<Service>();
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    DatabaseHelper.EnsureDefaultOnsenServices(conn);
                    string query = "SELECT ServiceId, ServiceName, Price, Unit FROM Services ORDER BY ServiceId ASC";
                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            services.Add(new Service
                            {
                                ServiceId = reader.GetInt32("ServiceId"),
                                ServiceName = reader.GetString("ServiceName"),
                                Price = reader.GetDecimal("Price"),
                                Unit = reader.GetString("Unit")
                            });
                        }
                    }
                }
                ServicesDataGrid.ItemsSource = services;
                cbServices.ItemsSource = services;
                if (services.Count > 0)
                {
                    cbServices.SelectedIndex = 0;
                }
                UpdateStaffAssignmentUI();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1042 || ex.Number == 0)
                    MessageBox.Show("Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show($"Lỗi cơ sở dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadOrderComboboxes()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    
                    // Load active rooms with their active BillId
                    string roomQuery = @"
                        SELECT b.BillId, r.RoomId, r.RoomNumber 
                        FROM Bills b 
                        JOIN Rooms r ON b.RoomId = r.RoomId 
                        WHERE b.Status = 3"; // Chỉ phòng đang có khách ở (Check-in) mới gọi được dịch vụ
                    var roomList = new List<OrderRoomItem>();
                    using (var cmd = new MySqlCommand(roomQuery, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            roomList.Add(new OrderRoomItem { BillId = reader.GetInt32("BillId"), RoomId = reader.GetInt32("RoomId"), RoomNumber = "Phòng " + reader.GetString("RoomNumber") });
                        }
                    }
                    cbRooms.ItemsSource = roomList;

                    if (roomList.Count == 0)
                    {
                        txtNoRoomsNotice.Visibility = Visibility.Visible;
                        btnOrderService.IsEnabled = false;
                    }
                    else
                    {
                        txtNoRoomsNotice.Visibility = Visibility.Collapsed;
                        btnOrderService.IsEnabled = true;
                        cbRooms.SelectedIndex = 0;
                    }

                    // Load staff (Role = 1) và ưu tiên hiển thị nhân viên đang trong ca trực hiện tại
                    int currentShiftType = (DateTime.Now.Hour >= 15) ? 1 : 0;
                    string staffQuery = @"
                        SELECT u.Id, u.FullName,
                               EXISTS(
                                   SELECT 1 FROM Shifts s 
                                   WHERE s.WorkDate = CURDATE() AND s.ShiftType = @ShiftType 
                                     AND (s.Employee1_Id = u.Id OR s.Employee2_Id = u.Id)
                               ) AS IsOnDuty
                        FROM Users u 
                        WHERE u.Role = 1 
                        ORDER BY IsOnDuty DESC, u.FullName ASC";
                    var staffList = new List<User>();
                    using (var cmd = new MySqlCommand(staffQuery, conn))
                    {
                        cmd.Parameters.Add("@ShiftType", MySqlDbType.Int32).Value = currentShiftType;
                        using (var reader = cmd.ExecuteReader())
                        {
                            staffList.Add(new User { Id = 0, FullName = "-- Chung (Cả ca) --" });
                            while (reader.Read())
                            {
                                bool onDuty = Convert.ToInt32(reader["IsOnDuty"]) == 1;
                                string name = reader.GetString("FullName") + (onDuty ? " (Đang trực)" : "");
                                staffList.Add(new User { Id = reader.GetInt32("Id"), FullName = name });
                            }
                        }
                    }
                    cbStaff.ItemsSource = staffList;
                    if (staffList.Count > 0)
                    {
                        cbStaff.SelectedIndex = 0;
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1042 || ex.Number == 0)
                    MessageBox.Show("Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show($"Lỗi cơ sở dữ liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu combobox: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput(out decimal price)
        {
            price = 0;
            bool isValid = true;

            string serviceName = txtServiceName.Text.Trim();
            if (string.IsNullOrWhiteSpace(serviceName) || serviceName.Length > 100)
            {
                txtServiceNameError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                txtServiceNameError.Visibility = Visibility.Collapsed;
            }

            string rawPrice = txtPrice.Text.Trim();
            if (rawPrice.EndsWith(".00") || rawPrice.EndsWith(",00"))
                rawPrice = rawPrice.Substring(0, rawPrice.Length - 3);
            rawPrice = rawPrice.Replace(",", "").Replace(".", "");
            if (string.IsNullOrWhiteSpace(rawPrice) || !decimal.TryParse(rawPrice, out price) || price <= 0)
            {
                txtPriceError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                txtPriceError.Visibility = Visibility.Collapsed;
            }

            string unit = txtUnit.Text.Trim();
            if (string.IsNullOrWhiteSpace(unit) || unit.Length > 20)
            {
                txtUnitError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                txtUnitError.Visibility = Visibility.Collapsed;
            }

            return isValid;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput(out decimal price)) return;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = "INSERT INTO Services (ServiceName, Price, Unit) VALUES (@ServiceName, @Price, @Unit)";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@ServiceName", MySqlDbType.VarChar, 100).Value = txtServiceName.Text.Trim();
                        cmd.Parameters.Add("@Price", MySqlDbType.Decimal).Value = price;
                        cmd.Parameters.Add("@Unit", MySqlDbType.VarChar, 20).Value = txtUnit.Text.Trim();
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Thêm dịch vụ thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
                ClearInputs();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                {
                    MessageBox.Show("Lỗi: Tên dịch vụ này đã tồn tại!", "Lỗi Trùng Lặp", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (ex.Number == 1042 || ex.Number == 0)
                {
                    MessageBox.Show("Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show($"Lỗi cơ sở dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (ServicesDataGrid.SelectedItem is not Service selectedService)
            {
                MessageBox.Show("Vui lòng chọn dịch vụ cần cập nhật!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidateInput(out decimal price)) return;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = "UPDATE Services SET ServiceName = @ServiceName, Price = @Price, Unit = @Unit WHERE ServiceId = @ServiceId";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@ServiceName", MySqlDbType.VarChar, 100).Value = txtServiceName.Text.Trim();
                        cmd.Parameters.Add("@Price", MySqlDbType.Decimal).Value = price;
                        cmd.Parameters.Add("@Unit", MySqlDbType.VarChar, 20).Value = txtUnit.Text.Trim();
                        cmd.Parameters.Add("@ServiceId", MySqlDbType.Int32).Value = selectedService.ServiceId;
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Cập nhật dịch vụ thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
                ClearInputs();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                {
                    MessageBox.Show("Lỗi: Tên dịch vụ này đã tồn tại!", "Lỗi Trùng Lặp", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (ex.Number == 1042 || ex.Number == 0)
                {
                    MessageBox.Show("Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show($"Lỗi cơ sở dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDel_Click(object sender, RoutedEventArgs e)
        {
            if (ServicesDataGrid.SelectedItem is not Service selectedService)
            {
                MessageBox.Show("Vui lòng chọn dịch vụ cần xóa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa dịch vụ '{selectedService.ServiceName}' không?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();

                        // Kiểm tra xem dịch vụ đã được sử dụng trong hóa đơn nào chưa để chặn xóa (tránh ON DELETE CASCADE xóa mất lịch sử)
                        string checkQuery = "SELECT COUNT(*) FROM Bill_Services WHERE ServiceId = @ServiceId";
                        using (var checkCmd = new MySqlCommand(checkQuery, conn))
                        {
                            checkCmd.Parameters.Add("@ServiceId", MySqlDbType.Int32).Value = selectedService.ServiceId;
                            int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (count > 0)
                            {
                                MessageBox.Show("Không thể xóa dịch vụ này vì đã có hóa đơn sử dụng nó!", "Lỗi khóa ngoại", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }

                        string query = "DELETE FROM Services WHERE ServiceId = @ServiceId";
                        using (var cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.Add("@ServiceId", MySqlDbType.Int32).Value = selectedService.ServiceId;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    MessageBox.Show("Xóa dịch vụ thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                    ClearInputs();
                }
                catch (MySqlException ex)
                {
                    if (ex.Number == 1451 || ex.Number == 1452)
                    {
                        MessageBox.Show("Không thể xóa dịch vụ này vì đang có hóa đơn sử dụng nó!", "Lỗi khóa ngoại", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (ex.Number == 1042 || ex.Number == 0)
                    {
                        MessageBox.Show("Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show($"Lỗi cơ sở dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            ClearInputs();
        }

        private void ServicesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ServicesDataGrid.SelectedItem is Service service)
            {
                txtServiceName.Text = service.ServiceName;
                txtPrice.Text = service.Price.ToString("0");
                txtUnit.Text = service.Unit;

                txtServiceNameError.Visibility = Visibility.Collapsed;
                txtPriceError.Visibility = Visibility.Collapsed;
                txtUnitError.Visibility = Visibility.Collapsed;
            }
        }

        private void ClearInputs()
        {
            txtServiceName.Clear();
            txtPrice.Clear();
            txtUnit.Clear();
            txtServiceNameError.Visibility = Visibility.Collapsed;
            txtPriceError.Visibility = Visibility.Collapsed;
            txtUnitError.Visibility = Visibility.Collapsed;
            ServicesDataGrid.SelectedItem = null;
        }

        private void cbServices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateStaffAssignmentUI();
        }

        private void UpdateStaffAssignmentUI()
        {
            if (cbServices?.SelectedItem is Service service)
            {
                if (service.RequiresStaffDelivery)
                {
                    if (gridStaffSelect != null) gridStaffSelect.Visibility = Visibility.Visible;
                    if (cbStaff != null) cbStaff.IsEnabled = true;
                    if (txtServiceStaffHint != null)
                    {
                        txtServiceStaffHint.Text = "🚚 Dịch vụ phục vụ tại phòng: Hệ thống sẽ tự động tạo nhiệm vụ cho nhân viên mang đồ đến phòng.";
                        txtServiceStaffHint.Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryColor");
                        txtServiceStaffHint.Visibility = Visibility.Visible;
                    }
                    if (btnOrderService != null) btnOrderService.Content = "Xác nhận & Giao việc";
                }
                else
                {
                    if (gridStaffSelect != null) gridStaffSelect.Visibility = Visibility.Collapsed;
                    if (cbStaff != null) cbStaff.IsEnabled = false;
                    if (txtServiceStaffHint != null)
                    {
                        txtServiceStaffHint.Text = "✨ Dịch vụ trải nghiệm & giải trí Onsen: Tự động ghi vào hóa đơn phòng, khách tự chơi / trải nghiệm (không cần nhân viên giao).";
                        txtServiceStaffHint.Foreground = (System.Windows.Media.Brush)FindResource("PrimaryBlueColor");
                        txtServiceStaffHint.Visibility = Visibility.Visible;
                    }
                    if (btnOrderService != null) btnOrderService.Content = "Ghi Vào Hóa Đơn";
                }
            }
            else
            {
                if (txtServiceStaffHint != null) txtServiceStaffHint.Visibility = Visibility.Collapsed;
            }
        }

        private void btnOrderService_Click(object sender, RoutedEventArgs e)
        {
            if (cbRooms.SelectedItem is not OrderRoomItem selectedRoom)
            {
                MessageBox.Show("Hiện chưa có phòng nào đang ở (Check-in) để gọi dịch vụ!\nVui lòng vào mục 'Hóa đơn' và chuyển khách sang 'Đã nhận phòng (Check-in)' trước.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cbServices.SelectedItem is not Service selectedService)
            {
                MessageBox.Show("Vui lòng chọn dịch vụ cần thêm!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool requiresDelivery = selectedService.RequiresStaffDelivery;
            User selectedStaff = cbStaff.SelectedItem as User;

            if (requiresDelivery && selectedStaff == null)
            {
                MessageBox.Show("Dịch vụ này phục vụ tại phòng, vui lòng chọn nhân viên thực hiện (hoặc chọn '-- Chung (Cả ca) --')!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Số lượng không hợp lệ!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int billId = selectedRoom.BillId;
            int roomId = selectedRoom.RoomId;
            int serviceId = selectedService.ServiceId;
            string serviceName = selectedService.ServiceName;
            decimal price = selectedService.Price;
            int? staffId = (requiresDelivery && selectedStaff != null && selectedStaff.Id > 0) ? selectedStaff.Id : null;
            decimal total = price * quantity;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. Thêm vào Bill_Services
                            string query1 = @"INSERT INTO Bill_Services (BillId, ServiceId, Quantity, Total) 
                                              VALUES (@BillId, @ServiceId, @Quantity, @Total)";
                            using (var cmd1 = new MySqlCommand(query1, conn, trans))
                            {
                                cmd1.Parameters.Add("@BillId", MySqlDbType.Int32).Value = billId;
                                cmd1.Parameters.Add("@ServiceId", MySqlDbType.Int32).Value = serviceId;
                                cmd1.Parameters.Add("@Quantity", MySqlDbType.Int32).Value = quantity;
                                cmd1.Parameters.Add("@Total", MySqlDbType.Decimal).Value = total;
                                cmd1.ExecuteNonQuery();
                            }

                            // 2. Cập nhật tổng tiền của Bill an toàn (xử lý trường hợp Total bị NULL)
                            string queryUpdateBill = "UPDATE Bills SET Total = COALESCE(Total, 0) + @Amount WHERE BillId = @BillId";
                            using (var cmdUpdateBill = new MySqlCommand(queryUpdateBill, conn, trans))
                            {
                                cmdUpdateBill.Parameters.Add("@Amount", MySqlDbType.Decimal).Value = total;
                                cmdUpdateBill.Parameters.Add("@BillId", MySqlDbType.Int32).Value = billId;
                                cmdUpdateBill.ExecuteNonQuery();
                            }

                            // 3. Chỉ tạo Task giao việc NẾU dịch vụ yêu cầu nhân viên giao tận phòng
                            if (requiresDelivery)
                            {
                                int shiftId = DatabaseHelper.GetOrCreateCurrentShiftId(conn, trans);
                                int initialTaskStatus = staffId.HasValue ? 1 : 0;
                                string taskName = $"Giao dịch vụ: {serviceName} (SL: {quantity})";
                                string query2 = @"INSERT INTO Tasks (RoomId, AssignedTo, ShiftId, TaskStatus, TaskName) 
                                                  VALUES (@RoomId, @AssignedTo, @ShiftId, @TaskStatus, @TaskName)";
                                using (var cmd2 = new MySqlCommand(query2, conn, trans))
                                {
                                    cmd2.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = roomId;
                                    cmd2.Parameters.Add("@AssignedTo", MySqlDbType.Int32).Value = staffId.HasValue ? staffId.Value : (object)DBNull.Value;
                                    cmd2.Parameters.Add("@ShiftId", MySqlDbType.Int32).Value = shiftId;
                                    cmd2.Parameters.Add("@TaskStatus", MySqlDbType.Int32).Value = initialTaskStatus;
                                    cmd2.Parameters.Add("@TaskName", MySqlDbType.VarChar, 255).Value = taskName;
                                    cmd2.ExecuteNonQuery();
                                }
                            }

                            trans.Commit();

                            string successMessage = requiresDelivery
                                ? $"Đã thêm dịch vụ vào hóa đơn và tạo nhiệm vụ giao việc cho {(staffId.HasValue ? selectedStaff.FullName : "nhân viên ca trực")} thành công!"
                                : $"Đã thêm dịch vụ '{serviceName}' vào hóa đơn {selectedRoom.RoomNumber} thành công!\n(Dịch vụ trải nghiệm & giải trí tự động ghi vào hóa đơn, không cần nhân viên giao phòng).";

                            MessageBox.Show(successMessage, "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // Reset form & tải lại danh sách
                            txtQuantity.Text = "1";
                            LoadOrderComboboxes();
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
                    MessageBox.Show($"Lỗi cơ sở dữ liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class OrderRoomItem
    {
        public int BillId { get; set; }
        public int RoomId { get; set; }
        public string RoomNumber { get; set; }
        public override string ToString() => RoomNumber ?? string.Empty;
    }
}
