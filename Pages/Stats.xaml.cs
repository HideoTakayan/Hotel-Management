using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Hotel_Management.Models;
using MySql.Data.MySqlClient;

namespace Hotel_Management.Pages
{
    // Strongly-typed item cho DataGrid Top Dịch Vụ trong Stats
    internal class TopServiceItem
    {
        public string ServiceName { get; set; }
        public string Unit { get; set; }
        public int Quantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public partial class Stats : Page
    {
        public Stats()
        {
            InitializeComponent();
            dateThoiGian.SelectedDate = DateTime.Now;
        }

        private void LoadData(DateTime selectedDate)
        {
            try
            {
                int month = selectedDate.Month;
                int year = selectedDate.Year;
                int bookingCount = 0;
                decimal totalProfit = 0;
                decimal serviceProfit = 0;

                    var topServices = new List<TopServiceItem>();
                int countTrong = 0, countDangDung = 0, countDaDat = 0, countDonDep = 0;

                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    DatabaseHelper.SyncAllDataToCurrentDate(conn);

                    // 1. Lấy tổng số hóa đơn đã hoàn tất hoặc trả phòng (Đã thanh toán 1 hoặc Chờ thanh toán 4) & tổng doanh thu trong tháng
                    string queryBills = @"SELECT COUNT(BillId) as SoLuong, COALESCE(SUM(Total), 0) as LoiNhuan 
                                          FROM Bills 
                                          WHERE Status IN (1, 4) AND MONTH(CreatedAt) = @month AND YEAR(CreatedAt) = @year";
                    using (var cmd = new MySqlCommand(queryBills, conn))
                    {
                        cmd.Parameters.Add("@month", MySqlDbType.Int32).Value = month;
                        cmd.Parameters.Add("@year", MySqlDbType.Int32).Value = year;
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                bookingCount = Convert.ToInt32(reader["SoLuong"]);
                                totalProfit = Convert.ToDecimal(reader["LoiNhuan"]);
                            }
                        }
                    }

                    // 2. Lấy doanh thu dịch vụ trong tháng
                    string queryServices = @"SELECT COALESCE(SUM(bs.Total), 0) as TienDichVu
                                             FROM Bill_Services bs
                                             JOIN Bills b ON bs.BillId = b.BillId
                                             WHERE b.Status IN (1, 4) AND MONTH(b.CreatedAt) = @month AND YEAR(b.CreatedAt) = @year";
                    using (var cmd = new MySqlCommand(queryServices, conn))
                    {
                        cmd.Parameters.Add("@month", MySqlDbType.Int32).Value = month;
                        cmd.Parameters.Add("@year", MySqlDbType.Int32).Value = year;
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            serviceProfit = Convert.ToDecimal(result);
                        }
                    }

                    // 3. Lấy Top 5 dịch vụ đem lại doanh thu cao nhất
                    string queryTop = @"SELECT s.ServiceName, s.Unit, SUM(bs.Quantity) as TongSoLuong, SUM(bs.Total) as TongTien
                                        FROM Bill_Services bs
                                        JOIN Services s ON bs.ServiceId = s.ServiceId
                                        JOIN Bills b ON bs.BillId = b.BillId
                                        WHERE b.Status IN (1, 4) AND MONTH(b.CreatedAt) = @month AND YEAR(b.CreatedAt) = @year
                                        GROUP BY s.ServiceId, s.ServiceName, s.Unit
                                        ORDER BY TongTien DESC
                                        LIMIT 5";
                    using (var cmd = new MySqlCommand(queryTop, conn))
                    {
                        cmd.Parameters.Add("@month", MySqlDbType.Int32).Value = month;
                        cmd.Parameters.Add("@year", MySqlDbType.Int32).Value = year;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                topServices.Add(new TopServiceItem
                                {
                                    ServiceName = reader.GetString("ServiceName"),
                                    Unit = reader.GetString("Unit"),
                                    Quantity = Convert.ToInt32(reader["TongSoLuong"]),
                                    TotalRevenue = Convert.ToDecimal(reader["TongTien"])
                                });
                            }
                        }
                    }

                    // 4. Lấy tình trạng phòng hiện tại của khách sạn
                    string queryRooms = @"SELECT Status, COUNT(*) as SoLuong FROM Rooms GROUP BY Status";
                    using (var cmd = new MySqlCommand(queryRooms, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int status = Convert.ToInt32(reader["Status"]);
                            int count = Convert.ToInt32(reader["SoLuong"]);
                            switch (status)
                            {
                                case 0: countTrong = count; break;
                                case 1: countDaDat = count; break;
                                case 2: countDangDung = count; break;
                                case 3:
                                case 4: countDonDep += count; break;
                            }
                        }
                    }
                }

                decimal roomProfit = Math.Max(0, totalProfit - serviceProfit);

                txtSoLan.Text = $"{bookingCount} lượt";
                txtTienPhong.Text = $"{roomProfit:N0} VND";
                txtTienDichVu.Text = $"{serviceProfit:N0} VND";
                txtLoiNhuan.Text = $"{totalProfit:N0} VND";

                dgTopServices.ItemsSource = topServices;

                txtPhongTrong.Text = $"{countTrong} phòng";
                txtPhongDangDung.Text = $"{countDangDung} phòng";
                txtPhongDaDat.Text = $"{countDaDat} phòng";
                txtPhongDonDep.Text = $"{countDonDep} phòng";
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

        private void DateThoiGian_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dateThoiGian.SelectedDate.HasValue)
            {
                LoadData(dateThoiGian.SelectedDate.Value);
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (dateThoiGian.SelectedDate.HasValue)
            {
                LoadData(dateThoiGian.SelectedDate.Value);
            }
        }
    }
}