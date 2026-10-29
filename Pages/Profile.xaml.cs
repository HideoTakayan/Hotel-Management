using System;
using System.Windows;
using System.Windows.Controls;
using Hotel_Management.Models;
using MySql.Data.MySqlClient;

namespace Hotel_Management.Pages
{
    public partial class Profile : Page
    {
        private int _userId;

        public Profile(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadProfile();
        }

        private void LoadProfile()
        {
            try
            {
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT FullName, Email, Phone FROM Users WHERE Id = @Id";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@Id", MySqlDbType.Int32).Value = _userId;
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtFullName.Text = reader.GetString("FullName");
                                txtEmail.Text = reader.IsDBNull(reader.GetOrdinal("Email")) ? "" : reader.GetString("Email");
                                txtPhone.Text = reader.IsDBNull(reader.GetOrdinal("Phone")) ? "" : reader.GetString("Phone");
                            }
                        }
                    }
                }
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
                MessageBox.Show("Lỗi tải thông tin cá nhân: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string fullName = txtFullName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string newPassword = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 50)
            {
                MessageBox.Show("Họ và Tên không được để trống và không quá 50 ký tự!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(email) || email.Length > 100 || !email.Contains("@"))
            {
                MessageBox.Show("Email không hợp lệ hoặc vượt quá 100 ký tự!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(phone) && (phone.Length < 9 || phone.Length > 12 || !System.Linq.Enumerable.All(phone, char.IsDigit)))
            {
                MessageBox.Show("Số điện thoại không hợp lệ (phải từ 9 đến 12 chữ số)! Vui lòng kiểm tra lại.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(newPassword) && newPassword.Length < 4)
            {
                MessageBox.Show("Mật khẩu mới phải có ít nhất 4 ký tự!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = "UPDATE Users SET FullName = @FullName, Email = @Email, Phone = @Phone";
                    
                    if (!string.IsNullOrWhiteSpace(newPassword))
                    {
                        query += ", Password = @Password";
                    }
                    
                    query += " WHERE Id = @Id";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@FullName", MySqlDbType.VarChar, 50).Value = fullName;
                        cmd.Parameters.Add("@Email", MySqlDbType.VarChar, 100).Value = email;
                        cmd.Parameters.Add("@Phone", MySqlDbType.VarChar, 12).Value = string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone;
                        cmd.Parameters.Add("@Id", MySqlDbType.Int32).Value = _userId;

                        if (!string.IsNullOrWhiteSpace(newPassword))
                        {
                            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
                            cmd.Parameters.Add("@Password", MySqlDbType.VarChar, 255).Value = hashedPassword;
                        }

                        cmd.ExecuteNonQuery();
                    }
                }
                
                // Cập nhật tên hiển thị ở góc dưới thanh menu
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.SetFullName(fullName);
                }

                txtPassword.Clear();
                MessageBox.Show("Cập nhật thông tin thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                    MessageBox.Show("Email hoặc số điện thoại này đã được sử dụng bởi tài khoản khác!", "Lỗi Trùng Lặp", MessageBoxButton.OK, MessageBoxImage.Error);
                else if (ex.Number == 1042 || ex.Number == 0)
                    MessageBox.Show("Mất kết nối đến máy chủ CSDL. Vui lòng kiểm tra lại mạng!", "Lỗi Kết Nối", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show($"Lỗi cơ sở dữ liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi cập nhật thông tin: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnResetProfile_Click(object sender, RoutedEventArgs e)
        {
            txtPassword.Clear();
            LoadProfile();
        }
    }
}
