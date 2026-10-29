using System;
using System.Windows;
using System.Windows.Controls;
using Hotel_Management.Models;
using MySql.Data.MySqlClient;

namespace Hotel_Management.Pages
{
    public partial class Booking : Page
    {
        private Room _selectedRoom;

        public Booking()
        {
            InitializeComponent();
            CheckPermissions();
        }

        private void CheckPermissions()
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null && mainWindow.CurrentRole == 1)
            {
                btnDatPhong.Visibility = Visibility.Collapsed;
                btnChonPhong.IsEnabled = false;
            }
        }

        private void BtnChonPhong_Click(object sender, RoutedEventArgs e)
        {
            RoomPick roomPickWindow = new RoomPick();
            if (roomPickWindow.ShowDialog() == true)
            {
                _selectedRoom = roomPickWindow.SelectedRoom;
                if (_selectedRoom != null)
                {
                    txtPhong.Text = $"Phòng {_selectedRoom.RoomNumber}";
                    txtSoNguoi.Text = _selectedRoom.NumberPeople.ToString();
                    txtPhongError.Visibility = Visibility.Collapsed;
                    txtSoNguoiError.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void BtnDatPhong_Click(object sender, RoutedEventArgs e)
        {
            bool isValid = true;

            if (_selectedRoom == null)
            {
                txtPhongError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else txtPhongError.Visibility = Visibility.Collapsed;

            string hoVaTen = txtHoVaTen.Text.Trim();
            string email = txtEmail.Text.Trim();
            string phone = txtPhone.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(hoVaTen) || hoVaTen.Length > 50)
            {
                txtHoVaTenError.Text = "Họ và tên không hợp lệ (tối đa 50 ký tự)";
                txtHoVaTenError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else txtHoVaTenError.Visibility = Visibility.Collapsed;

            if (!string.IsNullOrWhiteSpace(email) && (email.Length > 100 || !email.Contains("@") || !email.Contains(".")))
            {
                txtEmailError.Text = "Vui lòng nhập đúng định dạng email (tối đa 100 ký tự)";
                txtEmailError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else txtEmailError.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(phone) || phone.Length < 9 || phone.Length > 12 || !System.Linq.Enumerable.All(phone, char.IsDigit))
            {
                txtPhoneError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else txtPhoneError.Visibility = Visibility.Collapsed;

            if (!dateNgayNhan.SelectedDate.HasValue)
            {
                dateNgayNhanError.Visibility = Visibility.Visible;
                dateNgayNhanError.Text = "Ngày nhận trống";
                isValid = false;
            }
            else if (dateNgayNhan.SelectedDate.Value.Date < DateTime.Today)
            {
                dateNgayNhanError.Visibility = Visibility.Visible;
                dateNgayNhanError.Text = "Ngày nhận không được ở quá khứ";
                isValid = false;
            }
            else dateNgayNhanError.Visibility = Visibility.Collapsed;

            if (!dateNgayTra.SelectedDate.HasValue)
            {
                dateNgayTraError.Visibility = Visibility.Visible;
                dateNgayTraError.Text = "Ngày trả trống";
                isValid = false;
            }
            else dateNgayTraError.Visibility = Visibility.Collapsed;

            int soNguoi = 0;
            if (string.IsNullOrWhiteSpace(txtSoNguoi.Text) || !int.TryParse(txtSoNguoi.Text, out soNguoi) || soNguoi <= 0
                || (_selectedRoom != null && soNguoi > _selectedRoom.NumberPeople))
            {
                txtSoNguoiError.Visibility = Visibility.Visible;
                if (_selectedRoom != null && soNguoi > _selectedRoom.NumberPeople)
                    MessageBox.Show($"Phòng {_selectedRoom.RoomNumber} chỉ chứa tối đa {_selectedRoom.NumberPeople} người!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                isValid = false;
            }
            else txtSoNguoiError.Visibility = Visibility.Collapsed;

            if (dateNgayNhan.SelectedDate.HasValue && dateNgayTra.SelectedDate.HasValue)
            {
                if (dateNgayNhan.SelectedDate.Value >= dateNgayTra.SelectedDate.Value)
                {
                    dateNgayTraError.Text = "Ngày trả phải lớn hơn ngày nhận";
                    dateNgayTraError.Visibility = Visibility.Visible;
                    isValid = false;
                }
            }

            if (!isValid) return;

            int days = (dateNgayTra.SelectedDate.Value - dateNgayNhan.SelectedDate.Value).Days;
            decimal total = days * _selectedRoom.Price;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Giữ phòng trước: chỉ thành công nếu phòng vẫn đang Trống (tránh 2 người đặt trùng 1 phòng)
                            string updateRoomQuery = "UPDATE Rooms SET Status = 1 WHERE RoomId = @RoomId AND Status = 0"; // 1: Đã đặt
                            using (var cmd = new MySqlCommand(updateRoomQuery, conn, transaction))
                            {
                                cmd.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = _selectedRoom.RoomId;
                                if (cmd.ExecuteNonQuery() == 0)
                                    throw new InvalidOperationException($"Phòng {_selectedRoom.RoomNumber} không còn trống. Vui lòng chọn phòng khác!");
                            }

                            string insertBillQuery = @"INSERT INTO Bills (RoomId, FullName, Email, Phone, StartDate, EndDate, NumberPeople, Total, Status) 
                                                       VALUES (@RoomId, @FullName, @Email, @Phone, @StartDate, @EndDate, @NumberPeople, @Total, @Status)";
                            using (var cmd = new MySqlCommand(insertBillQuery, conn, transaction))
                            {
                                cmd.Parameters.Add("@RoomId", MySqlDbType.Int32).Value = _selectedRoom.RoomId;
                                cmd.Parameters.Add("@FullName", MySqlDbType.VarChar, 50).Value = hoVaTen;
                                cmd.Parameters.Add("@Email", MySqlDbType.VarChar, 100).Value = string.IsNullOrWhiteSpace(email) ? DBNull.Value : (object)email;
                                cmd.Parameters.Add("@Phone", MySqlDbType.VarChar, 12).Value = phone;
                                cmd.Parameters.Add("@StartDate", MySqlDbType.DateTime).Value = dateNgayNhan.SelectedDate.Value;
                                cmd.Parameters.Add("@EndDate", MySqlDbType.DateTime).Value = dateNgayTra.SelectedDate.Value;
                                cmd.Parameters.Add("@NumberPeople", MySqlDbType.Int32).Value = soNguoi;
                                cmd.Parameters.Add("@Total", MySqlDbType.Decimal).Value = total;
                                cmd.Parameters.Add("@Status", MySqlDbType.Int32).Value = 0; // Chưa thanh toán

                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            MessageBox.Show($"Đặt phòng thành công! Tổng tiền dự kiến: {total:N0} VND", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                            BtnRefresh_Click(null, null);
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1042 || ex.Number == 0)
                    MessageBox.Show("Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
                else if (ex.Number == 1451 || ex.Number == 1452)
                    MessageBox.Show("Phòng không tồn tại hoặc đã bị xóa!", "Lỗi khóa ngoại", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show($"Lỗi cơ sở dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtHoVaTen.Text = "";
            txtEmail.Text = "";
            txtPhone.Text = "";
            txtSoNguoi.Text = "";
            txtPhong.Text = "Chưa chọn phòng";
            dateNgayNhan.SelectedDate = null;
            dateNgayTra.SelectedDate = null;
            _selectedRoom = null;

            txtPhongError.Visibility = Visibility.Collapsed;
            txtHoVaTenError.Visibility = Visibility.Collapsed;
            txtEmailError.Visibility = Visibility.Collapsed;
            txtPhoneError.Visibility = Visibility.Collapsed;
            dateNgayNhanError.Visibility = Visibility.Collapsed;
            dateNgayTraError.Visibility = Visibility.Collapsed;
            txtSoNguoiError.Visibility = Visibility.Collapsed;
        }
    }
}