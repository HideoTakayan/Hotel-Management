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

        internal void LoadData()
        {
            LoadServiceList();
            LoadOrderComboboxes();
        }

        private async void LoadServiceList()
        {
            try
            {
                var services = await System.Threading.Tasks.Task.Run(() =>
                {
                    var list = new List<Service>();
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        DatabaseHelper.OpenConnection(conn);
                        DatabaseHelper.EnsureDefaultOnsenServices(conn);
                        string query = "SELECT ServiceId, ServiceName, Price, Unit FROM Services ORDER BY ServiceId ASC";
                        using (var cmd = new MySqlCommand(query, conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new Service
                                {
                                    ServiceId = reader.GetInt32("ServiceId"),
                                    ServiceName = reader.GetString("ServiceName"),
                                    Price = reader.GetDecimal("Price"),
                                    Unit = reader.GetString("Unit")
                                });
                            }
                        }
                    }
                    return list;
                });

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

        private async void LoadOrderComboboxes()
        {
            try
            {
                int currentShiftType = (DateTime.Now.Hour >= 15) ? 1 : 0;
                var (roomList, staffList) = await System.Threading.Tasks.Task.Run(() =>
                {
                    var rooms = new List<OrderRoomItem>();
                    var staff = new List<User>();

                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        DatabaseHelper.OpenConnection(conn);
                        
                        string roomQuery = @"
                            SELECT b.BillId, r.RoomId, r.RoomNumber 
                            FROM Bills b 
                            JOIN Rooms r ON b.RoomId = r.RoomId 
                            WHERE b.Status = 3";
                        using (var cmd = new MySqlCommand(roomQuery, conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rooms.Add(new OrderRoomItem { BillId = reader.GetInt32("BillId"), RoomId = reader.GetInt32("RoomId"), RoomNumber = "Phòng " + reader.GetString("RoomNumber") });
                            }
                        }

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

                        using (var cmd = new MySqlCommand(staffQuery, conn))
                        {
                            cmd.Parameters.Add("@ShiftType", MySqlDbType.Int32).Value = currentShiftType;
                            using (var reader = cmd.ExecuteReader())
                            {
                                staff.Add(new User { Id = 0, FullName = "-- Chung (Cả ca) --" });
                                while (reader.Read())
                                {
                                    bool onDuty = Convert.ToInt32(reader["IsOnDuty"]) == 1;
                                    string name = reader.GetString("FullName") + (onDuty ? " (Đang trực)" : "");
                                    staff.Add(new User { Id = reader.GetInt32("Id"), FullName = name });
                                }
                            }
                        }
                    }

                    return (rooms, staff);
                });

                cbRooms.ItemsSource = roomList;
                if (roomList.Count == 0)
                {
                    btnOrderService.IsEnabled = false;
                    if (txtNoOccupiedRoomsHint != null) txtNoOccupiedRoomsHint.Visibility = Visibility.Visible;
                }
                else
                {
                    btnOrderService.IsEnabled = true;
                    if (txtNoOccupiedRoomsHint != null) txtNoOccupiedRoomsHint.Visibility = Visibility.Collapsed;
                    cbRooms.SelectedIndex = 0;
                }

                cbStaff.ItemsSource = staffList;
                if (staffList.Count > 0)
                {
                    cbStaff.SelectedIndex = 0;
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
                    DatabaseHelper.OpenConnection(conn);
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
                    DatabaseHelper.OpenConnection(conn);
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
                        DatabaseHelper.OpenConnection(conn);

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
                    if (btnOrderService != null) btnOrderService.Content = "Thêm dịch vụ";
                }
                else
                {
                    if (gridStaffSelect != null) gridStaffSelect.Visibility = Visibility.Collapsed;
                    if (cbStaff != null) cbStaff.IsEnabled = false;
                    if (btnOrderService != null) btnOrderService.Content = "Thêm dịch vụ";
                }
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
                    DatabaseHelper.OpenConnection(conn);
                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
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

                            string queryUpdateBill = "UPDATE Bills SET Total = COALESCE(Total, 0) + @Amount WHERE BillId = @BillId AND Status = 3";
                            using (var cmdUpdateBill = new MySqlCommand(queryUpdateBill, conn, trans))
                            {
                                cmdUpdateBill.Parameters.Add("@Amount", MySqlDbType.Decimal).Value = total;
                                cmdUpdateBill.Parameters.Add("@BillId", MySqlDbType.Int32).Value = billId;
                                if (cmdUpdateBill.ExecuteNonQuery() == 0)
                                {
                                    throw new InvalidOperationException("Hóa đơn của phòng này đã chuyển sang trả phòng hoặc đã thanh toán, không thể gọi thêm dịch vụ!");
                                }
                            }

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
