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

        private void LoadData()
        {
            int? preservedSelectedId = _targetBillId ?? (BillsDataGrid?.SelectedItem as Bill)?.BillId;

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
                    // Nếu target đang bị bộ lọc ẩn đi, reset lại bộ lọc để người dùng nhìn thấy hóa đơn
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
                dgBillServices.ItemsSource = null;
                dgBillServices.Visibility = Visibility.Collapsed;
                txtNoServicesHint.Visibility = Visibility.Visible;

                comboboxTrangThai.SelectedIndex = -1;
                txtStatusLockedHint.Visibility = Visibility.Collapsed;

                comboboxTrangThai.IsEnabled = false;
                btnUpdateBill.IsEnabled = false;
            }
        }

        private void LoadBillServices(Bill bill)
        {
            if (bill == null) return;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    // Lấy Total mới nhất từ DB để đồng bộ thời gian thực nếu vừa có dịch vụ thêm
                    decimal totalBill = bill.Total ?? 0;
                    string queryBillTotal = "SELECT Total FROM Bills WHERE BillId = @BillId";
                    using (var cmdTotal = new MySqlCommand(queryBillTotal, conn))
                    {
                        cmdTotal.Parameters.Add("@BillId", MySqlDbType.Int32).Value = bill.BillId;
                        var res = cmdTotal.ExecuteScalar();
                        if (res != null && res != DBNull.Value)
                        {
                            totalBill = Convert.ToDecimal(res);
                            bill.Total = totalBill;
                        }
                    }

                    string queryServices = @"
                        SELECT s.ServiceName, 
                               CASE WHEN bs.Quantity > 0 THEN (bs.Total / bs.Quantity) ELSE s.Price END AS Price, 
                               bs.Quantity, 
                               bs.Total
                        FROM Bill_Services bs
                        JOIN Services s ON bs.ServiceId = s.ServiceId
                        WHERE bs.BillId = @BillId";

                    var servicesList = new List<BillService>();
                    decimal tienDichVu = 0;

                    using (var cmd = new MySqlCommand(queryServices, conn))
                    {
                        cmd.Parameters.Add("@BillId", MySqlDbType.Int32).Value = bill.BillId;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                decimal itemTotal = reader.GetDecimal("Total");
                                tienDichVu += itemTotal;
                                servicesList.Add(new BillService
                                {
                                    ServiceName = reader.GetString("ServiceName"),
                                    Price = reader.GetDecimal("Price"),
                                    Quantity = reader.GetInt32("Quantity"),
                                    Total = itemTotal
                                });
                            }
                        }
                    }

                    dgBillServices.ItemsSource = servicesList;
                    if (servicesList.Count > 0)
                    {
                        txtNoServicesHint.Visibility = Visibility.Collapsed;
                        dgBillServices.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        txtNoServicesHint.Visibility = Visibility.Visible;
                        dgBillServices.Visibility = Visibility.Collapsed;
                    }

                    decimal tienPhong = Math.Max(0, totalBill - tienDichVu);
                    txtTienPhongDetail.Text = $"{tienPhong:N0} VND";
                    txtTienDichVuDetail.Text = $"{tienDichVu:N0} VND";
                    txtTongTien.Text = $"{totalBill:N0} VND";
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1042 || ex.Number == 0)
                    MessageBox.Show("Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show($"Lỗi tải dịch vụ hóa đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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

            // Quy trình nghiệp vụ khách sạn thực tế:
            // - Chưa thanh toán / Đặt trước (0) -> Chỉ được chuyển sang: Nhận phòng Check-in (3) hoặc Đã hủy (2).
            //   (Khách chưa nhận phòng thì không thể nhảy cóc sang Chờ thanh toán (4) hay Đã thanh toán (1) làm giải phóng phòng).
            // - Đang ở Check-in (3) -> Chỉ được chuyển sang: Trả phòng (Chờ thanh toán) (4) hoặc Đã thanh toán (1 - trả phòng & tất toán luôn).
            //   (Khách đã vào ở và sử dụng phòng/dịch vụ thì không thể bấm 'Đã hủy' (2) để biến phòng đang ở thành phòng trống chưa dọn).
            // - Chờ thanh toán (4) -> Chỉ được chuyển sang: Đã thanh toán (1).
            // - Đã thanh toán (1), Đã hủy (2): Trạng thái kết thúc, đã khóa.
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

            decimal tienDichVu = 0;
            if (dgBillServices.ItemsSource is List<BillService> list)
            {
                foreach (var item in list) tienDichVu += item.Total;
            }
            decimal tongTien = selectedBill.Total ?? 0;
            decimal tienPhong = Math.Max(0, tongTien - tienDichVu);

            string statusText = ((ComboBoxItem)comboboxTrangThai.SelectedItem).Content.ToString();
            string confirmMsg = $"Xác nhận cập nhật hóa đơn ID {selectedBill.BillId} (Phòng {selectedBill.Room?.RoomNumber}) sang '{statusText}'?\n\n"
                              + $"• Tiền phòng: {tienPhong:N0} VND\n"
                              + $"• Tiền dịch vụ ({(dgBillServices.ItemsSource as List<BillService>)?.Count ?? 0} món): {tienDichVu:N0} VND\n"
                              + $"• TỔNG CỘNG HÓA ĐƠN: {tongTien:N0} VND";

            var result = MessageBox.Show(confirmMsg, "Xác Nhận Hóa Đơn", MessageBoxButton.YesNo, MessageBoxImage.Question);
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
                                }
                                else if (newStatus == 2) // Hủy đặt phòng trước khi ở
                                {
                                    if (currentRoomStatus == 1)
                                    {
                                        roomStatus = 0; // Giải phóng phòng về Trống
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
                        conn.Open();
                        using (var trans = conn.BeginTransaction())
                        {
                            try
                            {
                                // 1. Xóa các dịch vụ đã gọi của hóa đơn này (tránh lỗi khóa ngoại)
                                string deleteBillServices = "DELETE FROM Bill_Services WHERE BillId = @BillId";
                                using (var cmd = new MySqlCommand(deleteBillServices, conn, trans))
                                {
                                    cmd.Parameters.Add("@BillId", MySqlDbType.Int32).Value = selectedBill.BillId;
                                    cmd.ExecuteNonQuery();
                                }

                                // 2. Xóa hóa đơn
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