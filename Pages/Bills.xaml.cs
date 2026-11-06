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
        private int? _targetBillId;

        public Bills() : this(null)
        {
        }

        public Bills(int? targetBillId)
        {
            _targetBillId = targetBillId;
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
        private decimal _currentServiceTotal = 0;
        private decimal _currentRoomTotal = 0;

        internal async void LoadData()
        {
            int? preservedSelectedId = _targetBillId ?? (BillsDataGrid?.SelectedItem as Bill)?.BillId;

            try
            {
                var bills = await System.Threading.Tasks.Task.Run(() =>
                {
                    var list = new List<Bill>();
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        DatabaseHelper.OpenConnection(conn);
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
                                list.Add(new Bill
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
                    return list;
                });

                _allBills = bills;
                _targetBillId = preservedSelectedId;
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

            if (_targetBillId.HasValue)
            {
                var target = _allBills.Find(b => b.BillId == _targetBillId.Value);
                if (target != null)
                {
                    if (!filtered.Contains(target))
                    {
                        if (cbFilterStatus != null) cbFilterStatus.SelectedIndex = 0;
                        if (txtSearchBill != null) txtSearchBill.Text = "";
                        filtered = _allBills;
                        BillsDataGrid.ItemsSource = filtered;
                    }
                    BillsDataGrid.SelectedItem = target;
                    BillsDataGrid.ScrollIntoView(target);
                    LoadBillServices(target);
                }
                _targetBillId = null;
            }
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
                txtPhone.Text = bill.Phone;
                txtThoiGianLuuTru.Text = $"{bill.StartDate:dd/MM} - {bill.EndDate:dd/MM/yyyy}";
                txtSoNguoi.Text = $"{bill.NumberPeople} người";

                LoadBillServices(bill);
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
                txtPhone.Text = "---";
                txtThoiGianLuuTru.Text = "---";
                txtSoNguoi.Text = "---";
                txtTienPhongDetail.Text = "0 VND";
                txtTienDichVuDetail.Text = "0 VND";
                txtTongTien.Text = "0 VND";
                _currentServiceTotal = 0;
                _currentRoomTotal = 0;

                comboboxTrangThai.SelectedIndex = -1;
                txtStatusLockedHint.Visibility = Visibility.Collapsed;

                comboboxTrangThai.IsEnabled = false;
                btnUpdateBill.IsEnabled = false;
            }
        }

        private async void LoadBillServices(Bill bill)
        {
            if (bill == null) return;

            try
            {
                int targetBillId = bill.BillId;
                decimal initialTotal = bill.Total ?? 0;

                var result = await System.Threading.Tasks.Task.Run(() =>
                {
                    decimal totalBill = initialTotal;
                    decimal tienDichVu = 0;

                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        DatabaseHelper.OpenConnection(conn);
                        string queryBillTotal = "SELECT Total FROM Bills WHERE BillId = @BillId";
                        using (var cmdTotal = new MySqlCommand(queryBillTotal, conn))
                        {
                            cmdTotal.Parameters.Add("@BillId", MySqlDbType.Int32).Value = targetBillId;
                            var res = cmdTotal.ExecuteScalar();
                            if (res != null && res != DBNull.Value)
                            {
                                totalBill = Convert.ToDecimal(res);
                            }
                        }

                        string queryServices = "SELECT COALESCE(SUM(Total), 0) FROM Bill_Services WHERE BillId = @BillId";
                        using (var cmd = new MySqlCommand(queryServices, conn))
                        {
                            cmd.Parameters.Add("@BillId", MySqlDbType.Int32).Value = targetBillId;
                            var res = cmd.ExecuteScalar();
                            if (res != null && res != DBNull.Value)
                            {
                                tienDichVu = Convert.ToDecimal(res);
                            }
                        }
                    }
                    return (totalBill, tienDichVu);
                });

                if (BillsDataGrid?.SelectedItem is Bill currentSelected && currentSelected.BillId == targetBillId)
                {
                    bill.Total = result.totalBill;
                    _currentServiceTotal = result.tienDichVu;
                    _currentRoomTotal = Math.Max(0, result.totalBill - result.tienDichVu);

                    txtTienPhongDetail.Text = $"{_currentRoomTotal:N0} VND";
                    txtTienDichVuDetail.Text = $"{_currentServiceTotal:N0} VND";
                    txtTongTien.Text = $"{result.totalBill:N0} VND";
                }
            }
            catch { }
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

            bool isValidTransition = selectedBill.Status switch
            {
                0 => newStatus == 3 || newStatus == 2,
                3 => newStatus == 4 || newStatus == 1,
                4 => newStatus == 1,
                _ => false
            };
            if (!isValidTransition)
            {
                string hint = selectedBill.Status switch
                {
                    0 => "Khách mới đặt phòng ('Chưa thanh toán') chỉ có thể chuyển sang 'Đã nhận phòng (Check-in)' khi khách đến, hoặc 'Đã hủy' nếu khách hủy đặt.\n(Không thể tất toán/trả phòng khi khách chưa nhận phòng).",
                    3 => "Khách đang lưu trú ('Check-in') chỉ có thể chuyển sang 'Trả phòng (Chờ thanh toán)' hoặc 'Đã thanh toán' (trả phòng & thanh toán luôn).\n(Không thể hủy đơn khi khách đã nhận phòng).",
                    4 => "Hóa đơn đang 'Chờ thanh toán' chỉ có thể chuyển sang 'Đã thanh toán' sau khi thu tiền hoàn tất.",
                    _ => "Hóa đơn đã thanh toán hoặc đã hủy thì không thể thay đổi trạng thái nữa."
                };
                MessageBox.Show(hint, "Thao tác không hợp lệ với thực tế", MessageBoxButton.OK, MessageBoxImage.Warning);
                SetComboBoxStatusByTag(selectedBill.Status);
                return;
            }

            decimal tienDichVu = _currentServiceTotal;
            decimal tongTien = selectedBill.Total ?? 0;
            decimal tienPhong = Math.Max(0, tongTien - tienDichVu);

            string statusText = ((ComboBoxItem)comboboxTrangThai.SelectedItem).Content.ToString();
            string confirmMsg = $"Xác nhận cập nhật hóa đơn ID {selectedBill.BillId} (Phòng {selectedBill.Room?.RoomNumber}) sang '{statusText}'?\n\n"
                              + $"• Tiền phòng: {tienPhong:N0} VND\n"
                              + $"• Tiền dịch vụ: {tienDichVu:N0} VND\n"
                              + $"• TỔNG CỘNG HÓA ĐƠN: {tongTien:N0} VND";

            var result = MessageBox.Show(confirmMsg, "Xác Nhận Hóa Đơn", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        DatabaseHelper.OpenConnection(conn);
                        using (var trans = conn.BeginTransaction())
                        {
                            try
                            {
                                string updateBill = "UPDATE Bills SET Status = @Status WHERE BillId = @BillId";
                                using (var cmd = new MySqlCommand(updateBill, conn, trans))
                                {
                                    cmd.Parameters.Add("@Status", MySqlDbType.Int32).Value = newStatus;
                                    cmd.Parameters.Add("@BillId", MySqlDbType.Int32).Value = selectedBill.BillId;
                                    cmd.ExecuteNonQuery();
                                }

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

                                if (newStatus == 3)
                                {
                                    roomStatus = 2;
                                }
                                else if (newStatus == 4 || newStatus == 1)
                                {
                                    if (currentRoomStatus == 2)
                                    {
                                        roomStatus = 3;
                                        createCleaningTask = true;
                                    }
                                }
                                else if (newStatus == 2)
                                {
                                    if (currentRoomStatus == 1)
                                    {
                                        roomStatus = 0;
                                    }
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
                                        int? onDutyEmpId = null;
                                        string getDutyEmpQuery = "SELECT Employee1_Id FROM Shifts WHERE ShiftId = @ShiftId";
                                        using (var cmdDuty = new MySqlCommand(getDutyEmpQuery, conn, trans))
                                        {
                                            cmdDuty.Parameters.Add("@ShiftId", MySqlDbType.Int32).Value = shiftId;
                                            var empRes = cmdDuty.ExecuteScalar();
                                            if (empRes != null && empRes != DBNull.Value) onDutyEmpId = Convert.ToInt32(empRes);
                                        }

                                        string insertTask = @"INSERT INTO Tasks (RoomId, ShiftId, AssignedTo, TaskName, TaskStatus) 
                                                              VALUES (@RoomId, @ShiftId, @AssignedTo, @TaskName, 0)";
                                        using (var cmd = new MySqlCommand(insertTask, conn, trans))
                                        {
                                            cmd.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = selectedBill.RoomId;
                                            cmd.Parameters.Add("@ShiftId", MySqlDbType.Int32).Value = shiftId;
                                            cmd.Parameters.Add("@AssignedTo", MySqlDbType.Int32).Value = onDutyEmpId.HasValue ? (object)onDutyEmpId.Value : DBNull.Value;
                                            cmd.Parameters.Add("@TaskName", MySqlDbType.VarChar, 255).Value = $"Dọn dẹp phòng {selectedBill.Room?.RoomNumber ?? ""}";
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

            if (selectedBill.Status == 0)
            {
                MessageBox.Show("Hóa đơn đang giữ phòng đặt trước ('Chưa thanh toán').\nNếu khách hủy đặt, vui lòng cập nhật trạng thái thành 'Đã hủy' trước khi xóa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedBill.Status == 3)
            {
                MessageBox.Show("Khách đang lưu trú trong phòng ('Check-in'). Không thể xóa hóa đơn lúc này!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedBill.Status == 4)
            {
                MessageBox.Show("Khách đã trả phòng ('Chờ thanh toán') nhưng chưa hoàn tất thanh toán.\nVui lòng cập nhật sang 'Đã thanh toán' sau khi thu tiền trước khi xóa lịch sử!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Xác nhận XÓA hóa đơn ID {selectedBill.BillId}?\nHành động này không thể hoàn tác!", "Cảnh báo nguy hiểm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        DatabaseHelper.OpenConnection(conn);
                        using (var trans = conn.BeginTransaction())
                        {
                            try
                            {
                                string deleteBillServices = "DELETE FROM Bill_Services WHERE BillId = @BillId";
                                using (var cmd = new MySqlCommand(deleteBillServices, conn, trans))
                                {
                                    cmd.Parameters.Add("@BillId", MySqlDbType.Int32).Value = selectedBill.BillId;
                                    cmd.ExecuteNonQuery();
                                }

                                string deleteBill = "DELETE FROM Bills WHERE BillId = @BillId";
                                using (var cmd = new MySqlCommand(deleteBill, conn, trans))
                                {
                                    cmd.Parameters.Add("@BillId", MySqlDbType.Int32).Value = selectedBill.BillId;
                                    cmd.ExecuteNonQuery();
                                }

                                if (selectedBill.Status == 0)
                                {
                                    string releaseRoom = "UPDATE Rooms SET Status = 0 WHERE RoomId = @RoomId AND Status = 1";
                                    using (var cmdRelease = new MySqlCommand(releaseRoom, conn, trans))
                                    {
                                        cmdRelease.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = selectedBill.RoomId;
                                        cmdRelease.ExecuteNonQuery();
                                    }
                                }
                                else if (selectedBill.Status == 3)
                                {
                                    string roomToCleaning = "UPDATE Rooms SET Status = 3 WHERE RoomId = @RoomId AND Status = 2";
                                    using (var cmdRoomClean = new MySqlCommand(roomToCleaning, conn, trans))
                                    {
                                        cmdRoomClean.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = selectedBill.RoomId;
                                        cmdRoomClean.ExecuteNonQuery();
                                    }

                                    int shiftId = DatabaseHelper.GetOrCreateCurrentShiftId(conn, trans);
                                    string insertTask = @"INSERT INTO Tasks (RoomId, ShiftId, TaskName, TaskStatus) 
                                                          VALUES (@RoomId, @ShiftId, @TaskName, 0)";
                                    using (var cmdTask = new MySqlCommand(insertTask, conn, trans))
                                    {
                                        cmdTask.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = selectedBill.RoomId;
                                        cmdTask.Parameters.Add("@ShiftId", MySqlDbType.Int32).Value = shiftId;
                                        cmdTask.Parameters.Add("@TaskName", MySqlDbType.VarChar, 255).Value = $"Dọn dẹp phòng {selectedBill.Room?.RoomNumber ?? ""}";
                                        cmdTask.ExecuteNonQuery();
                                    }
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
            comboboxTrangThai.IsEnabled = false;
            btnUpdateBill.IsEnabled = false;
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