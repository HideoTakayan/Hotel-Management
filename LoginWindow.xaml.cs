using System;
using System.Windows;
using System.Windows.Input;
using MySql.Data.MySqlClient;
using Hotel_Management.Models;

namespace Hotel_Management
{
    public partial class LoginWindow : Window
    {
        private bool _isPasswordVisible = false;

        public LoginWindow()
        {
            InitializeComponent();
            var themeSource = Application.Current.Resources.MergedDictionaries[0].Source?.OriginalString ?? "";
            if (themeSource.Contains("LightMode"))
                iconTheme.Kind = MahApps.Metro.IconPacks.PackIconBootstrapIconsKind.SunFill;
        }

        private void btnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            if (_isPasswordVisible)
            {
                txtPasswordVisible.Text = txtPassword.Password;
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Visibility = Visibility.Visible;
                iconEye.Kind = MahApps.Metro.IconPacks.PackIconBootstrapIconsKind.EyeFill;
                iconEye.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "PrimaryBlueColor");
                txtPasswordVisible.Focus();
                txtPasswordVisible.CaretIndex = txtPasswordVisible.Text.Length;
            }
            else
            {
                txtPassword.Password = txtPasswordVisible.Text;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                iconEye.Kind = MahApps.Metro.IconPacks.PackIconBootstrapIconsKind.EyeSlashFill;
                iconEye.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "PrimaryGrayColor");
                txtPassword.Focus();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            var currentSource = Application.Current.Resources.MergedDictionaries[0].Source?.OriginalString ?? "";
            bool isDarkMode = currentSource.Contains("LightMode");
            var newTheme = isDarkMode ? "Themes/DarkMode.xaml" : "Themes/LightMode.xaml";
            var dict = new ResourceDictionary { Source = new Uri(newTheme, UriKind.Relative) };

            Application.Current.Resources.MergedDictionaries.RemoveAt(0);
            Application.Current.Resources.MergedDictionaries.Insert(0, dict);

            iconTheme.Kind = isDarkMode
                ? MahApps.Metro.IconPacks.PackIconBootstrapIconsKind.MoonFill
                : MahApps.Metro.IconPacks.PackIconBootstrapIconsKind.SunFill;
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            txtError.Visibility = Visibility.Collapsed;

            string username = txtUsername.Text.Trim();
            string password = _isPasswordVisible ? txtPasswordVisible.Text : txtPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                txtError.Text = "Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu!";
                txtError.Visibility = Visibility.Visible;
                return;
            }

            btnLogin.IsEnabled = false;

            try
            {
                var authResult = await System.Threading.Tasks.Task.Run(() =>
                {
                    using (MySqlConnection conn = DatabaseHelper.GetConnection())
                    {
                        DatabaseHelper.OpenConnection(conn);
                        string query = "SELECT Id, Username, FullName, Email, Role, Password FROM Users WHERE Username = @User LIMIT 1";

                        using (MySqlCommand cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.Add("@User", MySqlDbType.VarChar, 50).Value = username;

                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    string storedHash = reader.GetString("Password");
                                    bool isMatch = false;

                                    if (storedHash.StartsWith("$2"))
                                    {
                                        isMatch = BCrypt.Net.BCrypt.Verify(password, storedHash);
                                    }
                                    else
                                    {
                                        isMatch = (password == storedHash);
                                    }

                                    if (isMatch)
                                    {
                                        int userId = reader.GetInt32("Id");
                                        string loginName = reader.GetString("Username");
                                        string fullName = reader.IsDBNull(reader.GetOrdinal("FullName")) ? "" : reader.GetString("FullName");
                                        string email = reader.IsDBNull(reader.GetOrdinal("Email")) ? "" : reader.GetString("Email");
                                        int role = reader.GetInt32("Role");
                                        return (success: true, userId, loginName, fullName, email, role);
                                    }
                                }
                            }
                        }
                    }
                    return (success: false, userId: 0, loginName: "", fullName: "", email: "", role: 0);
                });

                if (authResult.success)
                {
                    MainWindow main = new MainWindow(authResult.userId, authResult.loginName, authResult.fullName, authResult.email, authResult.role);
                    Application.Current.MainWindow = main;
                    main.Show();
                    this.Close();
                }
                else
                {
                    txtError.Text = "Sai tài khoản hoặc mật khẩu!";
                    txtError.Visibility = Visibility.Visible;
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1042 || ex.Number == 0)
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
                MessageBox.Show($"Lỗi Hệ Thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnLogin.IsEnabled = true;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && WindowState == WindowState.Normal)
                DragMove();
        }
    }
}
