using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Hotel_Management.Helpers;
using Hotel_Management.Models;
using MySql.Data.MySqlClient;

namespace Hotel_Management.Pages
{
    public partial class RoomPick : Window
    {
        public Room SelectedRoom { get; private set; }
        private List<Room> _allRooms = new List<Room>();

        public RoomPick()
        {
            InitializeComponent();
            WindowMaximizeHelper.Attach(this);
            LoadRooms();
        }

        private async void LoadRooms()
        {
            try
            {
                var rooms = await System.Threading.Tasks.Task.Run(() =>
                {
                    var list = new List<Room>();
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        DatabaseHelper.OpenConnection(conn);
                        string query = "SELECT RoomId, RoomNumber, NumberPeople, NumberBed, Quality, BedType, Price, RoomType, RoomArea, Status FROM Rooms ORDER BY RoomNumber ASC";
                        using (var cmd = new MySqlCommand(query, conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new Room
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
                    return list;
                });

                _allRooms = rooms;
                ApplyFilter();
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

        private void ApplyFilter()
        {
            if (RoomsDataGrid == null || _allRooms == null) return;

            int selectedStatus = -1;
            if (cbFilterStatus?.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                int.TryParse(item.Tag.ToString(), out selectedStatus);
            }

            if (selectedStatus == -1)
            {
                RoomsDataGrid.ItemsSource = _allRooms;
            }
            else
            {
                RoomsDataGrid.ItemsSource = _allRooms.FindAll(r => r.Status == selectedStatus);
            }
        }

        private void cbFilterStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ConfirmRoomSelection(Room selectedRoom)
        {
            if (selectedRoom == null) return;

            if (selectedRoom.Status != 0)
            {
                MessageBox.Show($"Phòng {selectedRoom.RoomNumber} hiện đang ở trạng thái '{selectedRoom.StatusName}'.\nChỉ có thể chọn các phòng đang 'Trống (Sẵn sàng)' để đặt phòng mới!", "Không Thể Chọn Phòng", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedRoom = selectedRoom;
            DialogResult = true;
            Close();
        }

        private void RoomsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (RoomsDataGrid.SelectedItem is Room selectedRoom)
            {
                ConfirmRoomSelection(selectedRoom);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            WindowState = (WindowState == WindowState.Normal) ? WindowState.Maximized : WindowState.Normal;
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnPickRoom_Click(object sender, RoutedEventArgs e)
        {
            if (RoomsDataGrid.SelectedItem is Room selectedRoom)
            {
                ConfirmRoomSelection(selectedRoom);
            }
            else
            {
                MessageBox.Show("Hãy chọn một phòng trống trong danh sách!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            RoomsDataGrid.SelectedItem = null;
        }

        private void btnCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed && WindowState == WindowState.Normal)
                DragMove();
        }
    }
}
