using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Hotel_Management.Models;
using MySql.Data.MySqlClient;

namespace Hotel_Management.Pages
{
    public partial class Bills : Page
    {
        public Bills()
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
                btnUpdateBill.Visibility = Visibility.Collapsed;
                btnDelBill.Visibility = Visibility.Collapsed;
                comboboxTrangThai.IsEnabled = false;
            }
        }

        private List<Bill> _allBills = new List<Bill>();

        private void LoadData()
        {
            try
            {
                var bills = new List<Bill>();
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT b.BillId, b.RoomId, r.RoomNumber, b.CreatedAt, b.FullName, b.Email, b.Phone, 
                                            b.StartDate, b.EndDate, b.NumberPeople, b.Total, b.Status 
                                     FROM Bills b
                                     JOIN Rooms r ON b.RoomId = r.RoomId
                                     ORDER BY b.CreatedAt DESC";
                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bills.Add(new Bill
                            {
                                BillId = reader.GetInt32("BillId"),
                                RoomId = reader.GetInt32("RoomId"),
                                Room = new Room { RoomId = reader.GetInt32("RoomId"), RoomNumber = reader.GetString("RoomNumber") },
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                FullName = reader.GetString("FullName"),
                                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? "" : reader.GetString("Email"),
                                Phone = reader.GetString("Phone"),
                                StartDate = reader.GetDateTime("StartDate"),
                                EndDate = reader.GetDateTime("EndDate"),
                                NumberPeople = reader.GetInt32("NumberPeople"),
                                Total = reader.IsDBNull(reader.GetOrdinal("Total")) ? 0 : reader.GetDecimal("Total"),
                                Status = reader.GetInt32("Status")
                            });
                        }
                    }
                }
                _allBills = bills;
                ApplyFilter();
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

        private void ApplyFilter()
        {
            if (BillsDataGrid == null || _allBills == null) return;
            string keyword = txtSearchBill?.Text?.Trim().ToLower() ?? "";
            int selectedStatus = -1;

            if (cbFilterStatus?.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                int.TryParse(item.Tag.ToString(), out selectedStatus);
            }

            var filtered = _allBills.FindAll(b =>
            {
                bool matchKeyword = string.IsNullOrWhiteSpace(keyword) ||
                                    b.BillId.ToString().Contains(keyword) ||
                                    (b.FullName != null && b.FullName.ToLower().Contains(keyword)) ||
                                    (b.Phone != null && b.Phone.Contains(keyword)) ||
                                    (b.Room != null && b.Room.RoomNumber != null && b.Room.RoomNumber.ToLower().Contains(keyword));

                bool matchStatus = (selectedStatus == -1) || (b.Status == selectedStatus);

                return matchKeyword && matchStatus;
            });

            BillsDataGrid.ItemsSource = filtered;
        }

        private void txtSearchBill_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (BillsDataGrid == null || _allBills == null) return;
            ApplyFilter();
        }

        private void cbFilterStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BillsDataGrid == null || _allBills == null) return;
            ApplyFilter();
        }

        private void BillsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            bool isStaff = (mainWindow != null && mainWindow.CurrentRole == 1);

            if (BillsDataGrid.SelectedItem is Bill bill)
            {
                txtKhachHang.Text = bill.FullName;
                txtPhong.Text = bill.Room?.RoomNumber ?? "";
                txtEmail.Text = bill.Email;
                txtPhone.Text = bill.Phone;
                txtNgayNhan.Text = bill.StartDate.ToString("dd/MM/yyyy HH:mm");
                txtNgayTra.Text = bill.EndDate.ToString("dd/MM/yyyy HH:mm");
                txtSoNguoi.Text = bill.NumberPeople.ToString();
                txtTongTien.Text = bill.Total?.ToString("N0") + " VND";
                SetComboBoxStatusByTag(bill.Status);

                bool isFinal = (bill.Status == 1 || bill.Status == 2);
                if (isStaff)
                {
                    comboboxTrangThai.IsEnabled = false;
                    btnUpdateBill.IsEnabled = false;
                    txtStatusLockedHint.Visibility = Visibility.Collapsed;
                }
                else
                {
                    comboboxTrangThai.IsEnabled = !isFinal;
                    btnUpdateBill.IsEnabled = !isFinal;
                    txtStatusLockedHint.Visibility = isFinal ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            else
            {
                txtKhachHang.Text = "---";
                txtPhong.Text = "---";
                txtEmail.Text = "---";
                txtPhone.Text = "---";
                txtNgayNhan.Text = "---";
                txtNgayTra.Text = "---";
                txtSoNguoi.Text = "---";
                txtTongTien.Text = "---";
                comboboxTrangThai.SelectedIndex = -1;
                txtStatusLockedHint.Visibility = Visibility.Collapsed;

                if (!isStaff)
                {
                    comboboxTrangThai.IsEnabled = true;
                    btnUpdateBill.IsEnabled = true;
                }
            }
        }

        private void SetComboBoxStatusByTag(int status)
        {
            foreach (ComboBoxItem item in comboboxTrangThai.Items)
            {
                if (item.Tag?.ToString() == status.ToString())
                {
                    comboboxTrangThai.SelectedItem = item;
                    return;
                }
            }
            comboboxTrangThai.SelectedIndex = -1;
        }

        private void UpdateStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (BillsDataGrid.SelectedItem is not Bill selectedBill)
            {
                MessageBox.Show("Vui lòng chọn hóa đơn cần cập nhật!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int newStatus = -1;
            if (comboboxTrangThai.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                int.TryParse(selectedItem.Tag.ToString(), out newStatus);
            }

            if (newStatus == -1 || newStatus == selectedBill.Status)
            {
                MessageBox.Show("Trạng thái không thay đổi hoặc chưa hợp lệ!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Quy trình hợp lệ: Chưa thanh toán(0) -> Check-in(3) / Đã thanh toán(1) / Hủy(2)
            //                   Check-in(3) -> Check-out(4) / Đã thanh toán(1) / Hủy(2)
            //                   Check-out(4) -> Đã thanh toán(1)
            //                   Đã thanh toán(1), Đã hủy(2): trạng thái cuối, không đổi được
            bool isValidTransition = selectedBill.Status switch
            {
                0 => newStatus == 3 || newStatus == 2 || newStatus == 1,
                3 => newStatus == 4 || newStatus == 1 || newStatus == 2,
                4 => newStatus == 1,
                _ => false
            };
            if (!isValidTransition)
            {
                string hint = selectedBill.Status switch
                {
                    0 => "Hóa đơn 'Chưa thanh toán' có thể chuyển sang 'Check-in', 'Đã thanh toán' hoặc 'Đã hủy'.",
                    3 => "Khách đang ở chỉ có thể chuyển sang 'Check-out', 'Đã thanh toán' hoặc 'Đã hủy'.",
                    4 => "Hóa đơn đã Check-out chỉ có thể chuyển sang 'Đã thanh toán'.",
                    _ => "Hóa đơn đã thanh toán hoặc đã hủy thì không thể thay đổi trạng thái nữa."
                };
                MessageBox.Show(hint, "Thao tác không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                SetComboBoxStatusByTag(selectedBill.Status);
                return;
            }

            var result = MessageBox.Show($"Xác nhận đổi trạng thái hóa đơn ID {selectedBill.BillId} thành {((ComboBoxItem)comboboxTrangThai.SelectedItem).Content}?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        using (var trans = conn.BeginTransaction())
                        {
                            try
                            {
                                // Cập nhật Bill
                                string updateBill = "UPDATE Bills SET Status = @Status WHERE BillId = @BillId";
                                using (var cmd = new MySqlCommand(updateBill, conn, trans))
                                {
                                    cmd.Parameters.Add("@Status", MySqlDbType.Int32).Value = newStatus;
                                    cmd.Parameters.Add("@BillId", MySqlDbType.Int32).Value = selectedBill.BillId;
                                    cmd.ExecuteNonQuery();
                                }

                                // Lấy trạng thái phòng hiện tại để tránh tạo nhiều Task trùng lặp
                                int currentRoomStatus = 0;
                                string getRoomStatusQuery = "SELECT Status FROM Rooms WHERE RoomId = @RoomId";
                                using (var cmdGetRoom = new MySqlCommand(getRoomStatusQuery, conn, trans))
                                {
                                    cmdGetRoom.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = selectedBill.RoomId;
                                    var res = cmdGetRoom.ExecuteScalar();
                                    if (res != null) currentRoomStatus = Convert.ToInt32(res);
                                }

                                int roomStatus = -1;
                                bool createCleaningTask = false;

                                if (newStatus == 3) // Check-in
                                {
                                    roomStatus = 2; // Đang sử dụng
                                }
                                else if (newStatus == 4 || newStatus == 1) // Check-out hoặc Đã thanh toán
                                {
                                    // Chỉ cập nhật và tạo dọn phòng nếu phòng đang ở trạng thái 'Đang sử dụng'
                                    if (currentRoomStatus == 2)
                                    {
                                        roomStatus = 3; // Chờ dọn dẹp
                                        createCleaningTask = true;
                                    }
                                    else if (currentRoomStatus == 1)
                                    {
                                        roomStatus = 0; // Giải phóng phòng về Trống nếu thanh toán thẳng từ Đã đặt
                                    }
                                }
                                else if (newStatus == 2) // Hủy
                                {
                                    roomStatus = 0; // Trống
                                }
                                else if (newStatus == 0) // Chưa thanh toán
                                {
                                    roomStatus = 1; // Đã đặt
                                }

                                if (roomStatus != -1)
                                {
                                    string updateRoom = "UPDATE Rooms SET Status = @RoomStatus WHERE RoomId = @RoomId";
                                    using (var cmd = new MySqlCommand(updateRoom, conn, trans))
                                    {
                                        cmd.Parameters.Add("@RoomStatus", MySqlDbType.Int32).Value = roomStatus;
                                        cmd.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = selectedBill.RoomId;
                                        cmd.ExecuteNonQuery();
                                    }
                                }

                                if (createCleaningTask)
                                {
                                    string checkTaskQuery = "SELECT COUNT(*) FROM Tasks WHERE RoomId = @RoomId AND TaskStatus IN (0, 1, 2, 4)";
                                    int activeTaskCount = 0;
                                    using (var checkTaskCmd = new MySqlCommand(checkTaskQuery, conn, trans))
                                    {
                                        checkTaskCmd.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = selectedBill.RoomId;
                                        activeTaskCount = Convert.ToInt32(checkTaskCmd.ExecuteScalar());
                                    }

                                    if (activeTaskCount == 0)
                                    {
                                        int shiftId = DatabaseHelper.GetOrCreateCurrentShiftId(conn, trans);

                                        string insertTask = @"INSERT INTO Tasks (RoomId, ShiftId, TaskName, TaskStatus) 
                                                              VALUES (@RoomId, @ShiftId, 'Dọn phòng sau Check-out', 0)";
                                        using (var cmd = new MySqlCommand(insertTask, conn, trans))
                                        {
                                            cmd.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = selectedBill.RoomId;
                                            cmd.Parameters.Add("@ShiftId", MySqlDbType.Int32).Value = shiftId;
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }

                                trans.Commit();
                                MessageBox.Show("Cập nhật trạng thái thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnDelBill_Click(object sender, RoutedEventArgs e)
        {
            if (BillsDataGrid.SelectedItem is not Bill selectedBill)
            {
                MessageBox.Show("Vui lòng chọn hóa đơn cần xóa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedBill.Status == 3)
            {
                MessageBox.Show("Khách đang ở phòng (Check-in). Hãy Check-out trước, không thể xóa hóa đơn lúc này!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Xác nhận XÓA hóa đơn ID {selectedBill.BillId}?\nHành động này không thể hoàn tác!", "Cảnh báo nguy hiểm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        using (var trans = conn.BeginTransaction())
                        {
                            try
                            {
                                // 1. Giải phóng phòng (Chỉ giải phóng nếu hóa đơn đang kích hoạt: Chưa thanh toán hoặc Đang Check-in)
                                // Tránh lỗi: Xóa hóa đơn cũ từ tháng trước làm phòng đang có khách hiện tại tự dưng thành phòng trống.
                                if (selectedBill.Status == 0 || selectedBill.Status == 3)
                                {
                                    string updateRoom = "UPDATE Rooms SET Status = 0 WHERE RoomId = @RoomId";
                                    using (var cmd = new MySqlCommand(updateRoom, conn, trans))
                                    {
                                        cmd.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = selectedBill.RoomId;
                                        cmd.ExecuteNonQuery();
                                    }
                                }

                                // 2. Xóa các dịch vụ đã gọi của hóa đơn này (tránh lỗi khóa ngoại)
                                string deleteBillServices = "DELETE FROM Bill_Services WHERE BillId = @BillId";
                                using (var cmd = new MySqlCommand(deleteBillServices, conn, trans))
                                {
                                    cmd.Parameters.Add("@BillId", MySqlDbType.Int32).Value = selectedBill.BillId;
                                    cmd.ExecuteNonQuery();
                                }

                                // 3. Xóa hóa đơn
                                string deleteBill = "DELETE FROM Bills WHERE BillId = @BillId";
                                using (var cmd = new MySqlCommand(deleteBill, conn, trans))
                                {
                                    cmd.Parameters.Add("@BillId", MySqlDbType.Int32).Value = selectedBill.BillId;
                                    cmd.ExecuteNonQuery();
                                }

                                trans.Commit();
                                MessageBox.Show("Xóa hóa đơn thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    if (ex.Number == 1451 || ex.Number == 1452)
                        MessageBox.Show("Không thể xóa hóa đơn vì đang có dữ liệu liên kết khác!", "Lỗi Khóa Ngoại", MessageBoxButton.OK, MessageBoxImage.Error);
                    else if (ex.Number == 1042 || ex.Number == 0)
                        MessageBox.Show("Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
                    else
                        MessageBox.Show($"Lỗi Cơ Sở Dữ Liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearchBill.Text = "";
            cbFilterStatus.SelectedIndex = 0;
            BillsDataGrid.SelectedItem = null;
            txtStatusLockedHint.Visibility = Visibility.Collapsed;
            LoadData();
        }

        private void BtnViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (BillsDataGrid.SelectedItem is not Bill selectedBill)
            {
                MessageBox.Show("Vui lòng chọn hóa đơn cần xem chi tiết!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.NavigationService.Navigate(new BillDetails(selectedBill.BillId));
        }
    }
}