using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Hotel_Management.Models;
using MySql.Data.MySqlClient;

namespace Hotel_Management.Pages
{
    public partial class WorkSchedule : Page
    {
        public WorkSchedule()
        {
            InitializeComponent();
            CheckPermissions();
            LoadEmployees();
            LoadData();
        }

        private void CheckPermissions()
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null || mainWindow.CurrentRole == 1) // Nhân viên (hoặc không xác định) -> chỉ được xem
            {
                AdminPanel.Visibility = Visibility.Collapsed;     // Ẩn form phân ca
                btnAddShift.Visibility = Visibility.Collapsed;    // Ẩn nút tạo ca mới
                btnUpdateShift.Visibility = Visibility.Collapsed; // Ẩn nút cập nhật ca
                btnDelShift.Visibility = Visibility.Collapsed;    // Ẩn nút xóa ca
            }
        }

        private void LoadEmployees()
        {
            try
            {
                var employees = new List<User>();
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT Id, FullName FROM Users WHERE Role = 1 ORDER BY FullName";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            employees.Add(new User
                            {
                                Id = reader.GetInt32("Id"),
                                FullName = reader.GetString("FullName")
                            });
                        }
                    }
                }

                comboboxEmp1.ItemsSource = employees;
                comboboxEmp1.DisplayMemberPath = "FullName";
                comboboxEmp1.SelectedValuePath = "Id";

                comboboxEmp2.ItemsSource = new List<User>(employees);
                comboboxEmp2.DisplayMemberPath = "FullName";
                comboboxEmp2.SelectedValuePath = "Id";
            }
            catch (MySqlException ex)
            {
                ShowDbError(ex);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tải được danh sách nhân viên: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData()
        {
            try
            {
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                        SELECT s.ShiftId, s.WorkDate, s.ShiftType,
                               s.Employee1_Id, s.Employee2_Id,
                               u1.FullName AS Employee1_Name, 
                               u2.FullName AS Employee2_Name 
                        FROM Shifts s
                        JOIN Users u1 ON s.Employee1_Id = u1.Id
                        JOIN Users u2 ON s.Employee2_Id = u2.Id
                        ORDER BY s.WorkDate DESC, s.ShiftType ASC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        // Add helper columns for DataGrid display
                        dt.Columns.Add("ShiftName", typeof(string));
                        dt.Columns.Add("TimeRange", typeof(string));

                        foreach (DataRow row in dt.Rows)
                        {
                            int shiftType = Convert.ToInt32(row["ShiftType"]);
                            row["ShiftName"] = shiftType == 0 ? "Ca Sáng" : "Ca Tối";
                            row["TimeRange"] = shiftType == 0 ? "07:00 - 15:00" : "15:00 - 23:00";
                        }

                        dgShifts.ItemsSource = dt.DefaultView;
                    }
                }
            }
            catch (MySqlException ex)
            {
                ShowDbError(ex);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu ca làm việc: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgShifts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgShifts.SelectedItem is DataRowView rowView)
            {
                if (rowView["WorkDate"] != DBNull.Value)
                {
                    dpWorkDate.SelectedDate = Convert.ToDateTime(rowView["WorkDate"]);
                }

                string shiftTypeStr = rowView["ShiftType"]?.ToString();
                foreach (ComboBoxItem item in comboboxShiftType.Items)
                {
                    if (item.Tag?.ToString() == shiftTypeStr)
                    {
                        comboboxShiftType.SelectedItem = item;
                        break;
                    }
                }

                if (rowView["Employee1_Id"] != DBNull.Value)
                {
                    comboboxEmp1.SelectedValue = Convert.ToInt32(rowView["Employee1_Id"]);
                }
                if (rowView["Employee2_Id"] != DBNull.Value)
                {
                    comboboxEmp2.SelectedValue = Convert.ToInt32(rowView["Employee2_Id"]);
                }
            }
        }

        private void ShowDbError(MySqlException ex)
        {
            string msg = (ex.Number == 1042 || ex.Number == 0)
                ? "Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!"
                : $"Lỗi Cơ Sở Dữ Liệu: {ex.Message}";
            MessageBox.Show(msg, "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void btnAddShift_Click(object sender, RoutedEventArgs e)
        {
            if (dpWorkDate.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn ngày làm việc!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (dpWorkDate.SelectedDate.Value.Date < DateTime.Today)
            {
                MessageBox.Show("Không thể phân ca cho ngày trong quá khứ!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (comboboxShiftType.SelectedItem is not ComboBoxItem shiftItem || !int.TryParse(shiftItem.Tag?.ToString(), out int shiftType))
            {
                MessageBox.Show("Vui lòng chọn ca làm việc!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (comboboxEmp1.SelectedValue == null || comboboxEmp2.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn đầy đủ 2 nhân viên!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int emp1 = (int)comboboxEmp1.SelectedValue;
            int emp2 = (int)comboboxEmp2.SelectedValue;

            if (emp1 == emp2)
            {
                MessageBox.Show("Không thể chọn 2 nhân viên giống nhau!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    // Kiểm tra xem ca làm việc này đã được phân trong ngày chưa
                    string checkQuery = "SELECT COUNT(*) FROM Shifts WHERE WorkDate = @Date AND ShiftType = @Type";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.Add("@Date", MySqlDbType.Date).Value = dpWorkDate.SelectedDate.Value.Date;
                        checkCmd.Parameters.Add("@Type", MySqlDbType.Int32).Value = shiftType;
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            MessageBox.Show("Ca làm việc này đã tồn tại trong ngày!", "Lỗi Trùng Lặp", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    string query = "INSERT INTO Shifts (WorkDate, ShiftType, Employee1_Id, Employee2_Id) VALUES (@Date, @Type, @Emp1, @Emp2)";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@Date", MySqlDbType.Date).Value = dpWorkDate.SelectedDate.Value.Date;
                        cmd.Parameters.Add("@Type", MySqlDbType.Int32).Value = shiftType;
                        cmd.Parameters.Add("@Emp1", MySqlDbType.Int32).Value = emp1;
                        cmd.Parameters.Add("@Emp2", MySqlDbType.Int32).Value = emp2;
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Đã thêm ca làm việc thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
                ClearInputs();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                    MessageBox.Show("Ca làm việc này đã tồn tại trong ngày!", "Lỗi Trùng Lặp", MessageBoxButton.OK, MessageBoxImage.Error);
                else if (ex.Number == 1451 || ex.Number == 1452)
                    MessageBox.Show("Nhân viên được chọn không tồn tại hoặc đã bị xóa!", "Lỗi Khóa Ngoại", MessageBoxButton.OK, MessageBoxImage.Error);
                else if (ex.Number == 1042 || ex.Number == 0)
                    MessageBox.Show("Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show($"Lỗi Cơ Sở Dữ Liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi thêm ca: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnUpdateShift_Click(object sender, RoutedEventArgs e)
        {
            if (dgShifts.SelectedItem is not DataRowView rowView)
            {
                MessageBox.Show("Vui lòng chọn ca làm việc cần cập nhật từ danh sách!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dpWorkDate.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn ngày làm việc!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DateTime oldWorkDate = rowView["WorkDate"] != DBNull.Value ? Convert.ToDateTime(rowView["WorkDate"]).Date : DateTime.Today;
            if (dpWorkDate.SelectedDate.Value.Date < DateTime.Today && dpWorkDate.SelectedDate.Value.Date != oldWorkDate)
            {
                MessageBox.Show("Không thể chuyển ca làm việc sang ngày trong quá khứ!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (comboboxShiftType.SelectedItem is not ComboBoxItem shiftItem || !int.TryParse(shiftItem.Tag?.ToString(), out int shiftType))
            {
                MessageBox.Show("Vui lòng chọn ca làm việc!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (comboboxEmp1.SelectedValue == null || comboboxEmp2.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn đầy đủ 2 nhân viên!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int shiftId = Convert.ToInt32(rowView["ShiftId"]);
            int emp1 = (int)comboboxEmp1.SelectedValue;
            int emp2 = (int)comboboxEmp2.SelectedValue;

            if (emp1 == emp2)
            {
                MessageBox.Show("Không thể chọn 2 nhân viên giống nhau!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string checkQuery = "SELECT COUNT(*) FROM Shifts WHERE WorkDate = @Date AND ShiftType = @Type AND ShiftId <> @ShiftId";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.Add("@Date", MySqlDbType.Date).Value = dpWorkDate.SelectedDate.Value.Date;
                        checkCmd.Parameters.Add("@Type", MySqlDbType.Int32).Value = shiftType;
                        checkCmd.Parameters.Add("@ShiftId", MySqlDbType.Int32).Value = shiftId;
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            MessageBox.Show("Ca làm việc này đã tồn tại trong ngày!", "Lỗi Trùng Lặp", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    string updateQuery = @"UPDATE Shifts 
                                           SET WorkDate = @Date, ShiftType = @Type, Employee1_Id = @Emp1, Employee2_Id = @Emp2 
                                           WHERE ShiftId = @ShiftId";
                    using (MySqlCommand cmd = new MySqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.Add("@Date", MySqlDbType.Date).Value = dpWorkDate.SelectedDate.Value.Date;
                        cmd.Parameters.Add("@Type", MySqlDbType.Int32).Value = shiftType;
                        cmd.Parameters.Add("@Emp1", MySqlDbType.Int32).Value = emp1;
                        cmd.Parameters.Add("@Emp2", MySqlDbType.Int32).Value = emp2;
                        cmd.Parameters.Add("@ShiftId", MySqlDbType.Int32).Value = shiftId;
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Cập nhật ca làm việc thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
                ClearInputs();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                    MessageBox.Show("Ca làm việc này đã tồn tại trong ngày!", "Lỗi Trùng Lặp", MessageBoxButton.OK, MessageBoxImage.Error);
                else if (ex.Number == 1451 || ex.Number == 1452)
                    MessageBox.Show("Nhân viên được chọn không tồn tại hoặc đã bị xóa!", "Lỗi Khóa Ngoại", MessageBoxButton.OK, MessageBoxImage.Error);
                else if (ex.Number == 1042 || ex.Number == 0)
                    MessageBox.Show("Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show($"Lỗi Cơ Sở Dữ Liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi cập nhật ca: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDelShift_Click(object sender, RoutedEventArgs e)
        {
            if (dgShifts.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn ca làm việc cần xóa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var rowView = dgShifts.SelectedItem as DataRowView;
            if (rowView == null) return;
            
            int shiftId = Convert.ToInt32(rowView["ShiftId"]);

            var result = MessageBox.Show($"Xác nhận XÓA ca làm việc ID {shiftId}?", "Cảnh báo", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (MySqlConnection conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();

                        // Cấm xóa nếu ca làm việc này đã có nhiệm vụ (tránh mất nhiệm vụ dọn phòng do CASCADE)
                        string checkQuery = "SELECT COUNT(*) FROM Tasks WHERE ShiftId = @ShiftId";
                        using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn))
                        {
                            checkCmd.Parameters.Add("@ShiftId", MySqlDbType.Int32).Value = shiftId;
                            int taskCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (taskCount > 0)
                            {
                                MessageBox.Show("Không thể xóa ca này vì đang có các nhiệm vụ dọn phòng/giao dịch vụ liên kết!", "Lỗi Khóa Ngoại", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }

                        string query = "DELETE FROM Shifts WHERE ShiftId = @ShiftId";
                        using (MySqlCommand cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.Add("@ShiftId", MySqlDbType.Int32).Value = shiftId;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    MessageBox.Show("Đã xóa ca làm việc thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                    ClearInputs();
                }
                catch (MySqlException ex)
                {
                    if (ex.Number == 1451 || ex.Number == 1452)
                    {
                        MessageBox.Show("Không thể xóa ca này vì đã có nhiệm vụ liên kết!", "Lỗi Khóa Ngoại", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (ex.Number == 1042 || ex.Number == 0)
                    {
                        MessageBox.Show("Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show($"Lỗi Cơ Sở Dữ Liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi xóa ca: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearInputs()
        {
            dpWorkDate.SelectedDate = null;
            comboboxShiftType.SelectedIndex = -1;
            comboboxEmp1.SelectedIndex = -1;
            comboboxEmp2.SelectedIndex = -1;
            dgShifts.SelectedItem = null;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            ClearInputs();
            LoadEmployees();
            LoadData();
        }
    }
}
