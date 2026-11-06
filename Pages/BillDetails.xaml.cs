using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Hotel_Management.Models;
using MySql.Data.MySqlClient;

namespace Hotel_Management.Pages
{
    public partial class BillDetails : Page
    {
        public BillDetails(int billId)
        {
            InitializeComponent();
            LoadBillDetails(billId);
        }

        private async void LoadBillDetails(int billId)
        {
            try
            {
                var data = await System.Threading.Tasks.Task.Run(() =>
                {
                    string customerName = "";
                    string roomNumber = "";
                    decimal tongTienDB = 0;
                    var servicesList = new List<BillService>();
                    decimal tienDichVu = 0;

                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        DatabaseHelper.OpenConnection(conn);

                        string queryBill = @"
                            SELECT b.FullName, r.RoomNumber, b.Total 
                            FROM Bills b
                            JOIN Rooms r ON b.RoomId = r.RoomId
                            WHERE b.BillId = @BillId";

                        using (var cmd = new MySqlCommand(queryBill, conn))
                        {
                            cmd.Parameters.Add("@BillId", MySqlDbType.Int32).Value = billId;
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    customerName = reader.GetString("FullName");
                                    roomNumber = reader.GetString("RoomNumber");
                                    tongTienDB = reader.IsDBNull(reader.GetOrdinal("Total")) ? 0 : reader.GetDecimal("Total");
                                }
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

                        using (var cmd = new MySqlCommand(queryServices, conn))
                        {
                            cmd.Parameters.Add("@BillId", MySqlDbType.Int32).Value = billId;
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    decimal totalItem = reader.GetDecimal("Total");
                                    tienDichVu += totalItem;

                                    servicesList.Add(new BillService
                                    {
                                        ServiceName = reader.GetString("ServiceName"),
                                        Price = reader.GetDecimal("Price"),
                                        Quantity = reader.GetInt32("Quantity"),
                                        Total = totalItem
                                    });
                                }
                            }
                        }
                    }

                    return (customerName, roomNumber, tongTienDB, servicesList, tienDichVu);
                });

                txtKhachHang.Text = "Khách hàng: " + data.customerName;
                txtPhong.Text = "Phòng: " + data.roomNumber;
                dgServices.ItemsSource = data.servicesList;

                decimal tienPhong = Math.Max(0, data.tongTienDB - data.tienDichVu);
                txtTienPhong.Text = tienPhong.ToString("N0") + " VND";
                txtTienDichVu.Text = data.tienDichVu.ToString("N0") + " VND";
                txtTongCong.Text = data.tongTienDB.ToString("N0") + " VND";
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
                MessageBox.Show($"Lỗi khi tải chi tiết hóa đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                NavigationService.Navigate(new Bills());
            }
        }
    }
}
