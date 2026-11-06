using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Hotel_Management.Models;
using MySql.Data.MySqlClient;

namespace Hotel_Management.Pages
{
    internal class TopServiceItem
    {
        public string ServiceName { get; set; }
        public string Unit { get; set; }
        public int Quantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public partial class Stats : Page
    {
        private bool _isInitialized = false;

        public Stats()
        {
            InitializeComponent();
            InitMonthYearFilter();
            _isInitialized = true;
            TriggerLoadData();
        }

        private void InitMonthYearFilter()
        {
            for (int m = 1; m <= 12; m++)
            {
                cbMonth.Items.Add(new ComboBoxItem { Content = $"Tháng {m:D2}", Tag = m });
            }

            int currentYear = DateTime.Now.Year;
            for (int y = currentYear - 4; y <= currentYear + 2; y++)
            {
                cbYear.Items.Add(new ComboBoxItem { Content = y.ToString(), Tag = y });
            }

            int currentMonth = DateTime.Now.Month;
            cbMonth.SelectedIndex = currentMonth - 1;

            for (int i = 0; i < cbYear.Items.Count; i++)
            {
                if (cbYear.Items[i] is ComboBoxItem item && Convert.ToInt32(item.Tag) == currentYear)
                {
                    cbYear.SelectedIndex = i;
                    break;
                }
            }
        }

        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            TriggerLoadData();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            TriggerLoadData();
        }

        internal void TriggerLoadData()
        {
            int month = DateTime.Now.Month;
            int year = DateTime.Now.Year;

            if (cbMonth?.SelectedItem is ComboBoxItem mItem && mItem.Tag != null)
            {
                month = Convert.ToInt32(mItem.Tag);
            }

            if (cbYear?.SelectedItem is ComboBoxItem yItem && yItem.Tag != null)
            {
                year = Convert.ToInt32(yItem.Tag);
            }

            LoadData(month, year);
        }

        private async void LoadData(int month, int year)
        {
            try
            {

                var data = await System.Threading.Tasks.Task.Run(() =>
                {
                    int bookingCount = 0;
                    decimal totalProfit = 0;
                    decimal serviceProfit = 0;
                    var topServices = new List<TopServiceItem>();
                    int countTrong = 0, countDangDung = 0, countDaDat = 0, countDonDep = 0;

                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        DatabaseHelper.OpenConnection(conn);

                        string queryBills = @"SELECT COUNT(BillId) as SoLuong, COALESCE(SUM(Total), 0) as LoiNhuan 
                                              FROM Bills 
                                              WHERE Status IN (1, 4) AND MONTH(COALESCE(EndDate, CreatedAt)) = @month AND YEAR(COALESCE(EndDate, CreatedAt)) = @year";
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

                        string queryServices = @"SELECT COALESCE(SUM(bs.Total), 0) as TienDichVu
                                                 FROM Bill_Services bs
                                                 JOIN Bills b ON bs.BillId = b.BillId
                                                 WHERE b.Status IN (1, 4) AND MONTH(COALESCE(b.EndDate, b.CreatedAt)) = @month AND YEAR(COALESCE(b.EndDate, b.CreatedAt)) = @year";
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

                        string queryTop = @"SELECT s.ServiceName, s.Unit, SUM(bs.Quantity) as TongSoLuong, SUM(bs.Total) as TongTien
                                            FROM Bill_Services bs
                                            JOIN Services s ON bs.ServiceId = s.ServiceId
                                            JOIN Bills b ON bs.BillId = b.BillId
                                            WHERE b.Status IN (1, 4) AND MONTH(COALESCE(b.EndDate, b.CreatedAt)) = @month AND YEAR(COALESCE(b.EndDate, b.CreatedAt)) = @year
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
                    return (bookingCount, roomProfit, serviceProfit, totalProfit, topServices, countTrong, countDangDung, countDaDat, countDonDep);
                });

                txtSoLan.Text = $"{data.bookingCount} lượt";
                txtTienPhong.Text = $"{data.roomProfit:N0} VND";
                txtTienDichVu.Text = $"{data.serviceProfit:N0} VND";
                txtLoiNhuan.Text = $"{data.totalProfit:N0} VND";

                dgTopServices.ItemsSource = data.topServices;

                txtPhongTrong.Text = $"{data.countTrong} phòng";
                txtPhongDangDung.Text = $"{data.countDangDung} phòng";
                txtPhongDaDat.Text = $"{data.countDaDat} phòng";
                txtPhongDonDep.Text = $"{data.countDonDep} phòng";
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

    }
}