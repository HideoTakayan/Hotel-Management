using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Hotel_Management.Models;
using MySql.Data.MySqlClient;

namespace Hotel_Management.Pages
{
    public partial class Users : Page
    {
        public Users()
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
                btnAddUser.Visibility = Visibility.Collapsed;
                btnUpdateUser.Visibility = Visibility.Collapsed;
                btnDelUser.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadData()
        {
            try
            {
                var users = new List<User>();
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT Id, Username, FullName, Email, Phone, Role FROM Users";
                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new User
                            {
                                Id = reader.GetInt32("Id"),
                                Username = reader.GetString("Username"),
                                FullName = reader.GetString("FullName"),
                                Email = reader.GetString("Email"),
                                Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString("Phone"),
                                Role = reader.GetInt32("Role")
                            });
                        }
                    }
                }
                UsersDataGrid.ItemsSource = users;
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

        private bool ValidateInput(bool isUpdate, out int role)
        {
            role = 0;
            bool isValid = true;

            string username = txtUsername.Text.Trim();
            if (string.IsNullOrWhiteSpace(username) || username.Length > 50)
            {
                txtUsernameError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else txtUsernameError.Visibility = Visibility.Collapsed;

            string fullName = txtFullName.Text.Trim();
            if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 50)
            {
                txtFullNameError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else txtFullNameError.Visibility = Visibility.Collapsed;

            string email = txtEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(email) || email.Length > 100 || !email.Contains("@"))
            {
                txtEmailError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else txtEmailError.Visibility = Visibility.Collapsed;

            if (!isUpdate && string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                txtPasswordError.Text = "Mật khẩu không được để trống";
                txtPasswordError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else if (!string.IsNullOrWhiteSpace(txtPassword.Password) && txtPassword.Password.Length < 4)
            {
                txtPasswordError.Text = "Mật khẩu phải có ít nhất 4 ký tự";
                txtPasswordError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else txtPasswordError.Visibility = Visibility.Collapsed;

            // Số điện thoại không bắt buộc - chỉ validate định dạng nếu có nhập
            string phoneText = txtPhone.Text.Trim();
            if (!string.IsNullOrWhiteSpace(phoneText) && (phoneText.Length < 9 || phoneText.Length > 12 || !System.Linq.Enumerable.All(phoneText, char.IsDigit)))
            {
                txtPhoneError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else txtPhoneError.Visibility = Visibility.Collapsed;

            if (comboboxRole.SelectedItem == null)
            {
                txtRoleError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                txtRoleError.Visibility = Visibility.Collapsed;
                if (comboboxRole.SelectedItem is ComboBoxItem roleItem && roleItem.Tag != null)
                {
                    int.TryParse(roleItem.Tag.ToString(), out role);
                }
            }

            return isValid;
        }

        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput(false, out int role)) return;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"INSERT INTO Users (Username, Password, FullName, Email, Phone, Role) 
                                     VALUES (@Username, @Password, @FullName, @Email, @Phone, @Role)";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@Username", MySqlDbType.VarChar, 50).Value = txtUsername.Text.Trim();
                        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(txtPassword.Password);
                        cmd.Parameters.Add("@Password", MySqlDbType.VarChar, 255).Value = hashedPassword;
                        cmd.Parameters.Add("@FullName", MySqlDbType.VarChar, 50).Value = txtFullName.Text.Trim();
                        cmd.Parameters.Add("@Email", MySqlDbType.VarChar, 100).Value = txtEmail.Text.Trim();
                        cmd.Parameters.Add("@Phone", MySqlDbType.VarChar, 12).Value = string.IsNullOrWhiteSpace(txtPhone.Text) ? DBNull.Value : txtPhone.Text.Trim();
                        cmd.Parameters.Add("@Role", MySqlDbType.Int32).Value = role;

                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Thêm người dùng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
                ClearInputs();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                {
                    MessageBox.Show("Lỗi: Tên tài khoản này đã tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void BtnUpdateUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is not User selectedUser)
            {
                MessageBox.Show("Vui lòng chọn người dùng cần cập nhật!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidateInput(true, out int role)) return;

            var currentMain = Application.Current.MainWindow as MainWindow;
            if (currentMain != null && currentMain.CurrentUserId == selectedUser.Id && role != selectedUser.Role)
            {
                MessageBox.Show("Bạn không thể tự thay đổi quyền của chính mình khi đang đăng nhập!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query;
                    bool updatePassword = !string.IsNullOrWhiteSpace(txtPassword.Password);

                    if (updatePassword)
                    {
                        query = @"UPDATE Users SET Username = @Username, Password = @Password, FullName = @FullName, 
                                  Email = @Email, Phone = @Phone, Role = @Role WHERE Id = @Id";
                    }
                    else
                    {
                        query = @"UPDATE Users SET Username = @Username, FullName = @FullName, 
                                  Email = @Email, Phone = @Phone, Role = @Role WHERE Id = @Id";
                    }

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@Username", MySqlDbType.VarChar, 50).Value = txtUsername.Text.Trim();
                        if (updatePassword)
                        {
                            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(txtPassword.Password);
                            cmd.Parameters.Add("@Password", MySqlDbType.VarChar, 255).Value = hashedPassword;
                        }
                        cmd.Parameters.Add("@FullName", MySqlDbType.VarChar, 50).Value = txtFullName.Text.Trim();
                        cmd.Parameters.Add("@Email", MySqlDbType.VarChar, 100).Value = txtEmail.Text.Trim();
                        cmd.Parameters.Add("@Phone", MySqlDbType.VarChar, 12).Value = string.IsNullOrWhiteSpace(txtPhone.Text) ? DBNull.Value : txtPhone.Text.Trim();
                        cmd.Parameters.Add("@Role", MySqlDbType.Int32).Value = role;
                        cmd.Parameters.Add("@Id", MySqlDbType.Int32).Value = selectedUser.Id;

                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Cập nhật người dùng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
                ClearInputs();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                {
                    MessageBox.Show("Lỗi: Tên tài khoản này đã tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void BtnDelUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is not User selectedUser)
            {
                MessageBox.Show("Vui lòng chọn người dùng cần xóa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null && mainWindow.CurrentUserId == selectedUser.Id)
            {
                MessageBox.Show("Bạn không thể tự xóa tài khoản của chính mình khi đang đăng nhập!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa người dùng '{selectedUser.Username}' không?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();

                        // Kiểm tra xem nhân viên đã có nhiệm vụ hoặc ca làm việc chưa để chặn xóa
                        string checkQuery = @"SELECT 
                            (SELECT COUNT(*) FROM Tasks WHERE AssignedTo = @Id) +
                            (SELECT COUNT(*) FROM Shifts WHERE Employee1_Id = @Id OR Employee2_Id = @Id) AS TotalRefs";
                        using (var checkCmd = new MySqlCommand(checkQuery, conn))
                        {
                            checkCmd.Parameters.Add("@Id", MySqlDbType.Int32).Value = selectedUser.Id;
                            int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (count > 0)
                            {
                                MessageBox.Show("Không thể xóa người dùng này vì đang có dữ liệu liên kết (nhiệm vụ hoặc ca làm việc)!", "Lỗi ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }

                        string query = "DELETE FROM Users WHERE Id = @Id";
                        using (var cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.Add("@Id", MySqlDbType.Int32).Value = selectedUser.Id;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    MessageBox.Show("Xóa người dùng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                    ClearInputs();
                }
                catch (MySqlException ex)
                {
                    if (ex.Number == 1451 || ex.Number == 1452)
                    {
                        MessageBox.Show("Không thể xóa người dùng này vì đang có dữ liệu liên kết (ví dụ: ca làm việc, nhiệm vụ)!", "Lỗi khóa ngoại", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            ClearInputs();
        }

        private void UsersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is User user)
            {
                txtUsername.Text = user.Username;
                txtFullName.Text = user.FullName;
                txtEmail.Text = user.Email;
                txtPhone.Text = user.Phone;
                txtPassword.Password = ""; // Clear password for security

                foreach (ComboBoxItem item in comboboxRole.Items)
                {
                    if (item.Tag?.ToString() == user.Role.ToString())
                    {
                        comboboxRole.SelectedItem = item;
                        break;
                    }
                }

                HideErrorMessages();
            }
        }

        private void ClearInputs()
        {
            txtUsername.Clear();
            txtFullName.Clear();
            txtEmail.Clear();
            txtPassword.Clear();
            txtPhone.Clear();
            comboboxRole.SelectedItem = null;
            UsersDataGrid.SelectedItem = null;

            HideErrorMessages();
        }

        private void HideErrorMessages()
        {
            txtUsernameError.Visibility = Visibility.Collapsed;
            txtFullNameError.Visibility = Visibility.Collapsed;
            txtEmailError.Visibility = Visibility.Collapsed;
            txtPasswordError.Visibility = Visibility.Collapsed;
            txtPhoneError.Visibility = Visibility.Collapsed;
            txtRoleError.Visibility = Visibility.Collapsed;
        }
    }
}
