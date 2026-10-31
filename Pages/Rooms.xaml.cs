using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Hotel_Management.Models;
using MySql.Data.MySqlClient;

namespace Hotel_Management.Pages
{
    public partial class Rooms : Page
    {
        public Rooms()
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
                btnAddRoom.Visibility = Visibility.Collapsed;
                btnUpdateRoom.Visibility = Visibility.Collapsed;
                btnDelRoom.Visibility = Visibility.Collapsed;
            }
        }

        private List<Room> _allRooms = new List<Room>();

        private void LoadData()
        {
            try
            {
                var rooms = new List<Room>();
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    DatabaseHelper.SyncAllDataToCurrentDate(conn);
                    string query = "SELECT RoomId, RoomNumber, NumberPeople, NumberBed, Quality, BedType, Price, RoomType, RoomArea, Status FROM Rooms";
                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rooms.Add(new Room
                            {
                                RoomId = reader.GetInt32("RoomId"),
                                RoomNumber = reader.GetString("RoomNumber"),
                                NumberPeople = reader.GetInt32("NumberPeople"),
                                NumberBed = reader.GetInt32("NumberBed"),
                                Quality = reader.GetInt32("Quality"),
                                BedType = reader.GetString("BedType"),
                                Price = reader.GetDecimal("Price"),
                                RoomType = reader.GetString("RoomType"),
                                RoomArea = Convert.ToSingle(reader["RoomArea"]),
                                Status = reader.GetInt32("Status")
                            });
                        }
                    }
                }
                _allRooms = rooms;
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
            if (RoomsDataGrid == null || _allRooms == null) return;
            string keyword = txtSearchRoom?.Text?.Trim().ToLower() ?? "";
            int selectedStatus = -1;

            if (cbFilterRoomStatus?.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                int.TryParse(item.Tag.ToString(), out selectedStatus);
            }

            var filtered = _allRooms.FindAll(r =>
            {
                bool matchKeyword = string.IsNullOrWhiteSpace(keyword) ||
                                    (r.RoomNumber != null && r.RoomNumber.ToLower().Contains(keyword)) ||
                                    (r.RoomType != null && r.RoomType.ToLower().Contains(keyword)) ||
                                    (r.BedType != null && r.BedType.ToLower().Contains(keyword));

                bool matchStatus = (selectedStatus == -1) || (r.Status == selectedStatus);

                return matchKeyword && matchStatus;
            });

            RoomsDataGrid.ItemsSource = filtered;
        }

        private void txtSearchRoom_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (RoomsDataGrid == null || _allRooms == null) return;
            ApplyFilter();
        }

        private void cbFilterRoomStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoomsDataGrid == null || _allRooms == null) return;
            ApplyFilter();
        }

        private bool ValidateInput(out int numberPeople, out int numberBed, out decimal price, out float roomArea, out int quality, out string bedType, out string roomType, out int status)
        {
            numberPeople = 0;
            numberBed = 0;
            price = 0;
            roomArea = 0;
            quality = 0;
            bedType = "";
            roomType = "";
            status = 0;

            bool isValid = true;

            string roomNumber = txtRoomNumber.Text.Trim();
            if (string.IsNullOrWhiteSpace(roomNumber) || roomNumber.Length > 20)
            {
                txtRoomNumberError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else txtRoomNumberError.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(txtNumberPeople.Text) || !int.TryParse(txtNumberPeople.Text, out numberPeople) || numberPeople <= 0)
            {
                txtNumberPeopleError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else txtNumberPeopleError.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(txtNumberBed.Text) || !int.TryParse(txtNumberBed.Text, out numberBed) || numberBed <= 0)
            {
                txtNumberBedError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else txtNumberBedError.Visibility = Visibility.Collapsed;

            string rawPrice = txtPrice.Text.Trim();
            if (rawPrice.EndsWith(".00") || rawPrice.EndsWith(",00"))
                rawPrice = rawPrice.Substring(0, rawPrice.Length - 3);
            rawPrice = rawPrice.Replace(",", "").Replace(".", "");
            if (string.IsNullOrWhiteSpace(rawPrice) || !decimal.TryParse(rawPrice, out price) || price <= 0)
            {
                txtPriceError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else txtPriceError.Visibility = Visibility.Collapsed;

            string rawArea = txtRoomArea.Text.Trim().Replace(',', '.');
            if (string.IsNullOrWhiteSpace(rawArea) || !float.TryParse(rawArea, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out roomArea) || roomArea <= 0)
            {
                txtRoomAreaError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else txtRoomAreaError.Visibility = Visibility.Collapsed;

            if (comboboxQuality.SelectedItem == null)
            {
                txtQualityError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                txtQualityError.Visibility = Visibility.Collapsed;
                if (comboboxQuality.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    int.TryParse(item.Tag.ToString(), out quality);
                }
            }

            if (comboboxBedType.SelectedItem == null)
            {
                txtBedTypeError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                txtBedTypeError.Visibility = Visibility.Collapsed;
                if (comboboxBedType.SelectedItem is ComboBoxItem bedTypeItem && bedTypeItem.Tag != null)
                {
                    bedType = bedTypeItem.Tag.ToString();
                }
            }

            if (comboboxRoomType.SelectedItem == null)
            {
                txtRoomTypeError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                txtRoomTypeError.Visibility = Visibility.Collapsed;
                if (comboboxRoomType.SelectedItem is ComboBoxItem roomTypeItem && roomTypeItem.Tag != null)
                {
                    roomType = roomTypeItem.Tag.ToString();
                }
            }

            if (comboboxStatus.SelectedItem == null)
            {
                txtStatusError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                txtStatusError.Visibility = Visibility.Collapsed;
                if (comboboxStatus.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    int.TryParse(item.Tag.ToString(), out status);
                }
            }

            return isValid;
        }

        private void BtnAddRoom_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput(out int numberPeople, out int numberBed, out decimal price, out float roomArea, out int quality, out string bedType, out string roomType, out int status))
            {
                return;
            }

            if (status == 1 || status == 2)
            {
                MessageBox.Show("Phòng mới tạo chưa có khách đặt hay lưu trú, chỉ có thể đặt trạng thái 'Trống (Sẵn sàng)' hoặc 'Chờ / Đang dọn dẹp'!", "Không hợp lệ với thực tế", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"INSERT INTO Rooms (RoomNumber, NumberPeople, NumberBed, Quality, BedType, Price, RoomType, RoomArea, Status) 
                                     VALUES (@RoomNumber, @NumberPeople, @NumberBed, @Quality, @BedType, @Price, @RoomType, @RoomArea, @Status)";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@RoomNumber", MySqlDbType.VarChar, 20).Value = txtRoomNumber.Text.Trim();
                        cmd.Parameters.Add("@NumberPeople", MySqlDbType.Int32).Value = numberPeople;
                        cmd.Parameters.Add("@NumberBed", MySqlDbType.Int32).Value = numberBed;
                        cmd.Parameters.Add("@Quality", MySqlDbType.Int32).Value = quality;
                        cmd.Parameters.Add("@BedType", MySqlDbType.VarChar, 50).Value = bedType;
                        cmd.Parameters.Add("@Price", MySqlDbType.Decimal).Value = price;
                        cmd.Parameters.Add("@RoomType", MySqlDbType.VarChar, 50).Value = roomType;
                        cmd.Parameters.Add("@RoomArea", MySqlDbType.Float).Value = roomArea;
                        cmd.Parameters.Add("@Status", MySqlDbType.Int32).Value = status;

                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Thêm phòng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
                ClearInputs();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                {
                    MessageBox.Show("Lỗi: Số phòng này đã tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void BtnUpdateRoom_Click(object sender, RoutedEventArgs e)
        {
            if (RoomsDataGrid.SelectedItem is not Room selectedRoom)
            {
                MessageBox.Show("Vui lòng chọn phòng cần cập nhật!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidateInput(out int numberPeople, out int numberBed, out decimal price, out float roomArea, out int quality, out string bedType, out string roomType, out int status))
            {
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    // Tự động đồng bộ trạng thái Hóa đơn nếu quản trị viên thay đổi trạng thái phòng
                    if (status != selectedRoom.Status)
                    {
                        if (status == 2) // Quản trị viên chuyển phòng sang 'Đang ở'
                        {
                            // Nếu phòng có hóa đơn đặt trước (Status = 0), tự động chuyển đơn sang Check-in (Status = 3)
                            string syncBillSql = "UPDATE Bills SET Status = 3 WHERE RoomId = @RoomId AND Status = 0";
                            using (var syncBillCmd = new MySqlCommand(syncBillSql, conn))
                            {
                                syncBillCmd.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = selectedRoom.RoomId;
                                syncBillCmd.ExecuteNonQuery();
                            }
                        }
                        else if (status == 3 || status == 4) // Quản trị viên chuyển sang 'Chờ dọn dẹp' hoặc 'Đang dọn dẹp'
                        {
                            // Nếu phòng đang Check-in (Status = 3), tự động chuyển đơn sang Check-out (Status = 4)
                            string syncBillSql = "UPDATE Bills SET Status = 4 WHERE RoomId = @RoomId AND Status = 3";
                            using (var syncBillCmd = new MySqlCommand(syncBillSql, conn))
                            {
                                syncBillCmd.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = selectedRoom.RoomId;
                                syncBillCmd.ExecuteNonQuery();
                            }
                        }
                        else if (status == 0) // Quản trị viên chuyển phòng về 'Trống (Sẵn sàng)'
                        {
                            // Nếu có đơn đặt trước (0) thì hủy (2); nếu có đơn đang ở (3) hoặc check-out (4) thì thanh toán hoàn tất (1)
                            string syncBillSql = @"
                                UPDATE Bills 
                                SET Status = CASE WHEN Status = 0 THEN 2 ELSE 1 END 
                                WHERE RoomId = @RoomId AND Status IN (0, 3, 4)";
                            using (var syncBillCmd = new MySqlCommand(syncBillSql, conn))
                            {
                                syncBillCmd.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = selectedRoom.RoomId;
                                syncBillCmd.ExecuteNonQuery();
                            }
                        }
                    }

                    string query = @"UPDATE Rooms 
                                     SET RoomNumber = @RoomNumber, NumberPeople = @NumberPeople, NumberBed = @NumberBed, 
                                         Quality = @Quality, BedType = @BedType, Price = @Price, 
                                         RoomType = @RoomType, RoomArea = @RoomArea, Status = @Status 
                                     WHERE RoomId = @RoomId";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@RoomNumber", MySqlDbType.VarChar, 20).Value = txtRoomNumber.Text.Trim();
                        cmd.Parameters.Add("@NumberPeople", MySqlDbType.Int32).Value = numberPeople;
                        cmd.Parameters.Add("@NumberBed", MySqlDbType.Int32).Value = numberBed;
                        cmd.Parameters.Add("@Quality", MySqlDbType.Int32).Value = quality;
                        cmd.Parameters.Add("@BedType", MySqlDbType.VarChar, 50).Value = bedType;
                        cmd.Parameters.Add("@Price", MySqlDbType.Decimal).Value = price;
                        cmd.Parameters.Add("@RoomType", MySqlDbType.VarChar, 50).Value = roomType;
                        cmd.Parameters.Add("@RoomArea", MySqlDbType.Float).Value = roomArea;
                        cmd.Parameters.Add("@Status", MySqlDbType.Int32).Value = status;
                        cmd.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = selectedRoom.RoomId;

                        cmd.ExecuteNonQuery();
                    }

                    // Nếu chuyển trạng thái sang Chờ dọn dẹp (3) hoặc Đang dọn dẹp (4), tự động tạo Task cho nhân viên dọn dẹp
                    if (status == 3 || status == 4)
                    {
                        string checkTaskQuery = "SELECT COUNT(*) FROM Tasks WHERE RoomId = @RoomId AND TaskStatus IN (0, 1, 2, 4)";
                        using (var checkTaskCmd = new MySqlCommand(checkTaskQuery, conn))
                        {
                            checkTaskCmd.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = selectedRoom.RoomId;
                            int activeTaskCount = Convert.ToInt32(checkTaskCmd.ExecuteScalar());
                            if (activeTaskCount == 0)
                            {
                                int shiftId = DatabaseHelper.GetOrCreateCurrentShiftId(conn);
                                int? onDutyEmpId = null;
                                string getDutyEmpQuery = "SELECT Employee1_Id FROM Shifts WHERE ShiftId = @ShiftId";
                                using (var cmdDuty = new MySqlCommand(getDutyEmpQuery, conn))
                                {
                                    cmdDuty.Parameters.Add("@ShiftId", MySqlDbType.Int32).Value = shiftId;
                                    var empRes = cmdDuty.ExecuteScalar();
                                    if (empRes != null && empRes != DBNull.Value) onDutyEmpId = Convert.ToInt32(empRes);
                                }

                                int taskStatus = (status == 4) ? 1 : 0;
                                string insertTaskQuery = @"INSERT INTO Tasks (RoomId, ShiftId, AssignedTo, TaskName, TaskStatus) 
                                                          VALUES (@RoomId, @ShiftId, @AssignedTo, @TaskName, @TaskStatus)";
                                using (var insertTaskCmd = new MySqlCommand(insertTaskQuery, conn))
                                {
                                    insertTaskCmd.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = selectedRoom.RoomId;
                                    insertTaskCmd.Parameters.Add("@ShiftId", MySqlDbType.Int32).Value = shiftId;
                                    insertTaskCmd.Parameters.Add("@AssignedTo", MySqlDbType.Int32).Value = onDutyEmpId.HasValue ? (object)onDutyEmpId.Value : DBNull.Value;
                                    insertTaskCmd.Parameters.Add("@TaskName", MySqlDbType.VarChar, 255).Value = $"Dọn dẹp phòng {txtRoomNumber.Text.Trim()}";
                                    insertTaskCmd.Parameters.Add("@TaskStatus", MySqlDbType.Int32).Value = taskStatus;
                                    insertTaskCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                    else if (status == 0 && (selectedRoom.Status == 3 || selectedRoom.Status == 4))
                    {
                        // Nếu Quản lý chuyển phòng từ Chờ/Đang dọn dẹp sang Trống (Sẵn sàng), tự động hoàn tất các Task dọn phòng đang treo của phòng đó
                        string completeTaskQuery = @"UPDATE Tasks SET TaskStatus = 3, CompletedAt = NOW() 
                                                     WHERE RoomId = @RoomId AND TaskStatus IN (0, 1, 2, 4)";
                        using (var completeTaskCmd = new MySqlCommand(completeTaskQuery, conn))
                        {
                            completeTaskCmd.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = selectedRoom.RoomId;
                            completeTaskCmd.ExecuteNonQuery();
                        }
                    }
                }
                MessageBox.Show("Cập nhật phòng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
                ClearInputs();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                {
                    MessageBox.Show("Lỗi: Số phòng này đã tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void BtnDelRoom_Click(object sender, RoutedEventArgs e)
        {
            if (RoomsDataGrid.SelectedItem is not Room selectedRoom)
            {
                MessageBox.Show("Vui lòng chọn phòng cần xóa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa phòng '{selectedRoom.RoomNumber}' không?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();

                        // Kiểm tra xem phòng đã có hóa đơn hoặc nhiệm vụ liên kết chưa để chặn xóa (tránh ON DELETE CASCADE xóa mất lịch sử)
                        string checkQuery = @"SELECT 
                            (SELECT COUNT(*) FROM Bills WHERE RoomId = @RoomId) +
                            (SELECT COUNT(*) FROM Tasks WHERE RoomId = @RoomId) AS TotalRefs";
                        using (var checkCmd = new MySqlCommand(checkQuery, conn))
                        {
                            checkCmd.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = selectedRoom.RoomId;
                            int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (count > 0)
                            {
                                MessageBox.Show("Không thể xóa phòng này vì đang có dữ liệu liên kết (hóa đơn hoặc nhiệm vụ)!", "Lỗi khóa ngoại", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }

                        string query = "DELETE FROM Rooms WHERE RoomId = @RoomId";
                        using (var cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = selectedRoom.RoomId;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    MessageBox.Show("Xóa phòng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                    ClearInputs();
                }
                catch (MySqlException ex)
                {
                    if (ex.Number == 1451 || ex.Number == 1452)
                    {
                        MessageBox.Show("Không thể xóa phòng này vì đang có dữ liệu liên kết (ví dụ: hóa đơn, nhiệm vụ)!", "Lỗi khóa ngoại", MessageBoxButton.OK, MessageBoxImage.Error);
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
            txtSearchRoom.Text = "";
            cbFilterRoomStatus.SelectedIndex = 0;
            LoadData();
            ClearInputs();
        }

        private void RoomsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoomsDataGrid.SelectedItem is Room room)
            {
                txtRoomNumber.Text = room.RoomNumber;
                txtNumberPeople.Text = room.NumberPeople.ToString();
                txtNumberBed.Text = room.NumberBed.ToString();
                txtPrice.Text = room.Price.ToString("0");
                txtRoomArea.Text = room.RoomArea.ToString();

                SetComboBoxByTag(comboboxQuality, room.Quality.ToString());
                SetComboBoxByTag(comboboxBedType, room.BedType);
                SetComboBoxByTag(comboboxRoomType, room.RoomType);
                SetComboBoxByTag(comboboxStatus, room.Status.ToString());

                HideErrorMessages();
            }
        }

        private void SetComboBoxByTag(ComboBox comboBox, string tagValue)
        {
            if (comboBox == comboboxRoomType && !string.IsNullOrEmpty(tagValue))
            {
                if (tagValue.Equals("Standard", StringComparison.OrdinalIgnoreCase))
                    tagValue = "Phòng đơn";
                else if (tagValue.Equals("Superior", StringComparison.OrdinalIgnoreCase) ||
                         tagValue.Equals("Deluxe", StringComparison.OrdinalIgnoreCase))
                    tagValue = "Phòng đôi";
                else if (tagValue.Equals("Suite", StringComparison.OrdinalIgnoreCase))
                    tagValue = "Phòng gia đình";
            }
            else if (comboBox == comboboxBedType && !string.IsNullOrEmpty(tagValue))
            {
                if (tagValue.Equals("Single", StringComparison.OrdinalIgnoreCase))
                    tagValue = "Giường đơn";
                else if (tagValue.Equals("Double", StringComparison.OrdinalIgnoreCase) ||
                         tagValue.Equals("King", StringComparison.OrdinalIgnoreCase) ||
                         tagValue.Equals("Queen", StringComparison.OrdinalIgnoreCase) ||
                         tagValue.Equals("Western King", StringComparison.OrdinalIgnoreCase) ||
                         tagValue.Equals("Super King", StringComparison.OrdinalIgnoreCase))
                    tagValue = "Giường đôi";
                else if (tagValue.IndexOf("Futon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         tagValue.IndexOf("Tatami", StringComparison.OrdinalIgnoreCase) >= 0)
                    tagValue = "Nệm Futon (Tatami)";
            }

            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Tag?.ToString() == tagValue)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
            comboBox.SelectedItem = null;
        }

        private void ClearInputs()
        {
            txtRoomNumber.Clear();
            txtNumberPeople.Clear();
            txtNumberBed.Clear();
            txtPrice.Clear();
            txtRoomArea.Clear();
            comboboxQuality.SelectedItem = null;
            comboboxBedType.SelectedItem = null;
            comboboxRoomType.SelectedItem = null;
            comboboxStatus.SelectedItem = null;
            RoomsDataGrid.SelectedItem = null;

            HideErrorMessages();
        }

        private void HideErrorMessages()
        {
            txtRoomNumberError.Visibility = Visibility.Collapsed;
            txtNumberPeopleError.Visibility = Visibility.Collapsed;
            txtNumberBedError.Visibility = Visibility.Collapsed;
            txtPriceError.Visibility = Visibility.Collapsed;
            txtRoomAreaError.Visibility = Visibility.Collapsed;
            txtQualityError.Visibility = Visibility.Collapsed;
            txtBedTypeError.Visibility = Visibility.Collapsed;
            txtRoomTypeError.Visibility = Visibility.Collapsed;
            txtStatusError.Visibility = Visibility.Collapsed;
        }
    }
}