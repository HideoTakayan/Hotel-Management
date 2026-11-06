using System;
using System.Collections.Generic;
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
            InitDefaultDates();
            LoadRoomStatusBoard();
        }

        private void InitDefaultDates()
        {
            dateNgayNhan.DisplayDateStart = DateTime.Today;
            dateNgayTra.DisplayDateStart = DateTime.Today.AddDays(1);
            dateNgayNhan.SelectedDate = DateTime.Today;
            dateNgayTra.SelectedDate = DateTime.Today.AddDays(1);
        }

                private void dateNgayNhan_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dateNgayNhan.SelectedDate.HasValue)
            {
                var minCheckout = dateNgayNhan.SelectedDate.Value.AddDays(1);
                dateNgayTra.DisplayDateStart = minCheckout;
                if (!dateNgayTra.SelectedDate.HasValue || dateNgayTra.SelectedDate.Value <= dateNgayNhan.SelectedDate.Value)
                {
                    dateNgayTra.SelectedDate = minCheckout;
                }
            }
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

        public async void LoadRoomStatusBoard()
        {
            try
            {
                var roomCards = await System.Threading.Tasks.Task.Run(() =>
                {
                    var cards = new List<RoomCardItem>();
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        DatabaseHelper.OpenConnection(conn);

                        string query = @"
                            SELECT r.RoomId, r.RoomNumber, r.NumberPeople, r.NumberBed, r.BedType, r.Price, r.RoomType, r.RoomArea, r.Quality, r.Status,
                                   b.BillId, b.FullName AS CustomerName, b.Phone AS CustomerPhone, b.Email AS CustomerEmail,
                                   b.StartDate, b.EndDate, b.NumberPeople AS BookingGuests, b.Total AS BillTotal, b.Status AS BillStatus
                            FROM Rooms r
                            LEFT JOIN (
                                SELECT b1.*
                                FROM Bills b1
                                INNER JOIN (
                                    SELECT RoomId, 
                                           CAST(SUBSTRING_INDEX(
                                               GROUP_CONCAT(BillId ORDER BY 
                                                   CASE 
                                                       WHEN Status = 3 THEN 1
                                                       WHEN Status = 4 THEN 2
                                                       WHEN Status = 0 THEN 3
                                                       ELSE 4
                                                   END ASC, BillId DESC), ',', 1
                                           ) AS UNSIGNED) AS PreferredBillId
                                    FROM Bills
                                    WHERE Status IN (0, 3, 4)
                                    GROUP BY RoomId
                                ) latest ON b1.BillId = latest.PreferredBillId
                            ) b ON r.RoomId = b.RoomId
                            ORDER BY r.RoomNumber ASC";

                        using (var cmd = new MySqlCommand(query, conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int status = reader.GetInt32("Status");
                                int numPeople = reader.GetInt32("NumberPeople");
                                string customer = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? "" : reader.GetString("CustomerName");

                                int? billId = reader.IsDBNull(reader.GetOrdinal("BillId")) ? (int?)null : reader.GetInt32("BillId");
                                string customerPhone = reader.IsDBNull(reader.GetOrdinal("CustomerPhone")) ? "" : reader.GetString("CustomerPhone");
                                string customerEmail = reader.IsDBNull(reader.GetOrdinal("CustomerEmail")) ? "" : reader.GetString("CustomerEmail");
                                DateTime? startDate = reader.IsDBNull(reader.GetOrdinal("StartDate")) ? (DateTime?)null : reader.GetDateTime("StartDate");
                                DateTime? endDate = reader.IsDBNull(reader.GetOrdinal("EndDate")) ? (DateTime?)null : reader.GetDateTime("EndDate");
                                int? bookingGuests = reader.IsDBNull(reader.GetOrdinal("BookingGuests")) ? (int?)null : reader.GetInt32("BookingGuests");
                                decimal? billTotal = reader.IsDBNull(reader.GetOrdinal("BillTotal")) ? (decimal?)null : reader.GetDecimal("BillTotal");
                                int? billStatus = reader.IsDBNull(reader.GetOrdinal("BillStatus")) ? (int?)null : reader.GetInt32("BillStatus");

                                string statusText;
                                string statusColor;
                                string statusBg;
                                string displaySubtitle;

                                switch (status)
                                {
                                    case 0:
                                        statusText = "Trống";
                                        statusColor = "#2ECC71";
                                        statusBg = "#102ECC71";
                                        displaySubtitle = $"Tối đa {numPeople} người";
                                        break;
                                    case 1:
                                        statusText = "Đã đặt";
                                        statusColor = "#F39C12";
                                        statusBg = "#10F39C12";
                                        displaySubtitle = !string.IsNullOrWhiteSpace(customer) ? $"Khách: {customer}" : "Đã giữ chỗ";
                                        break;
                                    case 2:
                                        statusText = "Đang ở";
                                        statusColor = "#3498DB";
                                        statusBg = "#103498DB";
                                        displaySubtitle = !string.IsNullOrWhiteSpace(customer) ? $"Khách: {customer}" : "Đang lưu trú";
                                        break;
                                    case 3:
                                    case 4:
                                        statusText = "Chờ dọn";
                                        statusColor = "#E74C3C";
                                        statusBg = "#10E74C3C";
                                        displaySubtitle = "Cần dọn dẹp phòng";
                                        break;
                                    default:
                                        statusText = "Khác";
                                        statusColor = "#95A5A6";
                                        statusBg = "#1095A5A6";
                                        displaySubtitle = "";
                                        break;
                                }

                                cards.Add(new RoomCardItem
                                {
                                    RoomId = reader.GetInt32("RoomId"),
                                    RoomNumber = reader.GetString("RoomNumber"),
                                    NumberPeople = numPeople,
                                    NumberBed = reader.GetInt32("NumberBed"),
                                    Quality = reader.GetInt32("Quality"),
                                    BedType = reader.GetString("BedType"),
                                    Price = reader.GetDecimal("Price"),
                                    RoomType = reader.GetString("RoomType"),
                                    RoomArea = Convert.ToSingle(reader["RoomArea"]),
                                    Status = status,
                                    StatusText = statusText,
                                    StatusColor = statusColor,
                                    StatusBg = statusBg,
                                    CustomerName = customer,
                                    DisplaySubtitle = displaySubtitle,
                                    BillId = billId,
                                    CustomerPhone = customerPhone,
                                    CustomerEmail = customerEmail,
                                    StartDate = startDate,
                                    EndDate = endDate,
                                    BookingGuests = bookingGuests,
                                    BillTotal = billTotal,
                                    BillStatus = billStatus
                                });
                            }
                        }
                    }
                    return cards;
                });

                icRoomStatus.ItemsSource = roomCards;

                var dueRooms = roomCards.FindAll(r => r.IsDueToday);
                if (dueRooms.Count > 0)
                {
                    borderCheckoutNotice.Visibility = Visibility.Visible;
                    string roomNames = string.Join(", ", dueRooms.ConvertAll(r => $"Phòng {r.RoomNumber} ({r.CustomerName})"));
                    txtCheckoutNoticeTitle.Text = $"{dueRooms.Count} phòng đến hạn trả hôm nay ({DateTime.Now:dd/MM/yyyy})";
                    txtCheckoutNoticeDesc.Text = $"Danh sách phòng: {roomNames}.";
                }
                else
                {
                    borderCheckoutNotice.Visibility = Visibility.Collapsed;
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1042 || ex.Number == 0)
                    MessageBox.Show("Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show($"Lỗi tải sơ đồ phòng: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnViewDueCheckouts_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                var dueRooms = (icRoomStatus.ItemsSource as List<RoomCardItem>)?.FindAll(r => r.IsDueToday);
                int? targetBillId = (dueRooms != null && dueRooms.Count > 0) ? dueRooms[0].BillId : null;
                mainWindow.NavigateToBills(targetBillId);
            }
        }

        private void RoomCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is RoomCardItem card)
            {
                if (card.Status == 0)
                {
                    _selectedRoom = new Room
                    {
                        RoomId = card.RoomId,
                        RoomNumber = card.RoomNumber,
                        NumberPeople = card.NumberPeople,
                        NumberBed = card.NumberBed,
                        Quality = card.Quality,
                        BedType = card.BedType,
                        Price = card.Price,
                        RoomType = card.RoomType,
                        RoomArea = card.RoomArea,
                        Status = card.Status
                    };
                    txtPhong.Text = $"Phòng {_selectedRoom.RoomNumber} ({_selectedRoom.Price:N0}đ/đêm)";
                    txtSoNguoi.Text = _selectedRoom.NumberPeople.ToString();
                    txtPhongError.Visibility = Visibility.Collapsed;
                    txtSoNguoiError.Visibility = Visibility.Collapsed;
                }
                else if (card.Status == 1 || card.Status == 2)
                {
                    ShowBookedRoomDetails(card);
                }
                else if (card.Status == 3 || card.Status == 4)
                {
                    if (card.BillId.HasValue)
                    {
                        ShowBookedRoomDetails(card);
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Phòng {card.RoomNumber} ({card.RoomType}) hiện đang ở trạng thái 'Chờ dọn dẹp'.\n\n" +
                            "• Khách trước đã trả phòng.\n" +
                            "• Phòng đang chờ nhân viên buồng phòng hoàn tất vệ sinh để chuyển về 'Trống (Sẵn sàng)'.",
                            $"Thông Tin Phòng {card.RoomNumber}",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
        }

        private void ShowBookedRoomDetails(RoomCardItem card)
        {
            string customer = !string.IsNullOrWhiteSpace(card.CustomerName) ? card.CustomerName : "---";
            string phone = !string.IsNullOrWhiteSpace(card.CustomerPhone) ? card.CustomerPhone : "---";
            string email = !string.IsNullOrWhiteSpace(card.CustomerEmail) ? card.CustomerEmail : "(Không có)";
            string guests = card.BookingGuests.HasValue ? $"{card.BookingGuests.Value} người" : $"{card.NumberPeople} người";
            string stayPeriod = card.StayDurationText;
            string checkoutStatus = card.CheckoutStatusText;
            string totalText = card.BillTotal.HasValue ? $"{card.BillTotal.Value:N0} VND" : "---";

            string dueNotice = card.IsDueToday ? "\nPhòng đến hạn trả phòng hôm nay." : "";

            string detailMessage =
                $"THÔNG TIN CHI TIẾT ĐẶT PHÒNG - PHÒNG {card.RoomNumber}\n" +
                $"────────────────────────────────────────\n" +
                $"• Khách hàng: {customer}\n" +
                $"• Số điện thoại: {phone}\n" +
                $"• Email: {email}\n" +
                $"• Số khách lưu trú: {guests}\n" +
                $"• Thời gian lưu trú: {stayPeriod}\n" +
                $"• Trạng thái phòng: {card.StatusText}\n" +
                $"• Tình trạng trả phòng: {checkoutStatus}\n" +
                $"• Tổng tiền hóa đơn: {totalText}\n" +
                dueNotice + "\n" +
                $"────────────────────────────────────────\n" +
                $"Bạn có muốn chuyển sang mục 'Quản Lý Hóa Đơn' để xem chi tiết hoặc thanh toán phòng này không?";

            var result = MessageBox.Show(detailMessage, $"Chi Tiết Khách Đặt Phòng {card.RoomNumber}", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.NavigateToBills(card.BillId);
                }
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
                    txtPhong.Text = $"Phòng {_selectedRoom.RoomNumber} ({_selectedRoom.Price:N0}đ/đêm)";
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
                if (dateNgayNhan.SelectedDate.Value.Date >= dateNgayTra.SelectedDate.Value.Date)
                {
                    dateNgayTraError.Text = "Ngày trả phải sau ngày nhận ít nhất 1 đêm";
                    dateNgayTraError.Visibility = Visibility.Visible;
                    isValid = false;
                }
                else if ((dateNgayTra.SelectedDate.Value.Date - dateNgayNhan.SelectedDate.Value.Date).Days > 365)
                {
                    dateNgayTraError.Text = "Thời gian đặt phòng tối đa là 365 ngày";
                    dateNgayTraError.Visibility = Visibility.Visible;
                    isValid = false;
                }
            }

            if (!isValid) return;

            int days = (dateNgayTra.SelectedDate.Value.Date - dateNgayNhan.SelectedDate.Value.Date).Days;
            decimal total = days * _selectedRoom.Price;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    DatabaseHelper.OpenConnection(conn);
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string updateRoomQuery = "UPDATE Rooms SET Status = 1 WHERE RoomId = @RoomId AND Status = 0";
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
                                cmd.Parameters.Add("@Status", MySqlDbType.Int32).Value = 0;

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
            InitDefaultDates();
            _selectedRoom = null;

            txtPhongError.Visibility = Visibility.Collapsed;
            txtHoVaTenError.Visibility = Visibility.Collapsed;
            txtEmailError.Visibility = Visibility.Collapsed;
            txtPhoneError.Visibility = Visibility.Collapsed;
            dateNgayNhanError.Visibility = Visibility.Collapsed;
            dateNgayTraError.Visibility = Visibility.Collapsed;
            txtSoNguoiError.Visibility = Visibility.Collapsed;

            LoadRoomStatusBoard();
        }
    }

    public class RoomCardItem
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; }
        public string RoomType { get; set; }
        public decimal Price { get; set; }
        public int NumberPeople { get; set; }
        public int NumberBed { get; set; }
        public string BedType { get; set; }
        public float RoomArea { get; set; }
        public int Quality { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; }
        public string StatusColor { get; set; }
        public string StatusBg { get; set; }
        public string CustomerName { get; set; }
        public string DisplaySubtitle { get; set; }

        public int? BillId { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? BookingGuests { get; set; }
        public decimal? BillTotal { get; set; }
        public int? BillStatus { get; set; }

        public bool IsDueToday => EndDate.HasValue && EndDate.Value.Date <= DateTime.Now.Date && (BillStatus == 3 || Status == 2);
        public Visibility DueWarningVisibility => IsDueToday ? Visibility.Visible : Visibility.Collapsed;

        public string StayDurationText
        {
            get
            {
                if (StartDate.HasValue && EndDate.HasValue)
                {
                    int nights = Math.Max(1, (EndDate.Value.Date - StartDate.Value.Date).Days);
                    return $"{StartDate.Value:dd/MM/yyyy HH:mm} - {EndDate.Value:dd/MM/yyyy HH:mm} ({nights} đêm)";
                }
                return "---";
            }
        }

        public string CheckoutStatusText
        {
            get
            {
                return BillStatus switch
                {
                    0 => "Chưa nhận phòng (Đã đặt trước)",
                    3 => "Chưa trả phòng (Khách đang lưu trú)",
                    4 => "Đã trả phòng (Chờ thanh toán)",
                    1 => "Đã thanh toán (Hoàn tất)",
                    _ => "---"
                };
            }
        }
    }
}