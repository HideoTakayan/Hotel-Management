using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hotel_Management
{
    public partial class MainWindow : Window
    {
        public int CurrentUserId { get; private set; }
        public string CurrentUsername { get; private set; }
        public string CurrentFullName { get; private set; }
        public string CurrentEmail { get; private set; }
        public int CurrentRole { get; private set; }

        public MainWindow()
        {
            Application.Current.MainWindow = this;
            InitializeComponent();
        }

        public MainWindow(int userId, string username, string fullName, string email, int role = 1)
        {
            Application.Current.MainWindow = this;
            InitializeComponent();
            CurrentUserId = userId;
            CurrentUsername = username;
            SetFullName(fullName);
            CurrentEmail = email;
            CurrentRole = role;
            ApplyRolePermissions();

            // Đồng bộ icon theme với theme đang dùng (trường hợp đăng xuất/đăng nhập lại)
            var themeSource = Application.Current.Resources.MergedDictionaries[0].Source?.OriginalString ?? "";
            if (themeSource.Contains("LightMode"))
                iconTheme.Kind = MahApps.Metro.IconPacks.PackIconBootstrapIconsKind.SunFill;

            // Mở trang phù hợp với vai trò người dùng
            placeHolder.Visibility = Visibility.Collapsed;
            if (CurrentRole == 1)
            {
                rdTasks.IsChecked = true;
                PagesNavigation.Navigate(new Uri("Pages/MyTasks.xaml", UriKind.RelativeOrAbsolute));
            }
            else
            {
                rdHome.IsChecked = true;
                PagesNavigation.Navigate(new Uri("Pages/Home.xaml", UriKind.RelativeOrAbsolute));
            }
        }

        private void ApplyRolePermissions()
        {
            // Role 0: Quản lý (Admin kiêm Lễ tân) - Toàn quyền quản trị, đặt phòng, thu ngân, giao việc
            // Role 1: Nhân viên - Chỉ xem lịch làm việc, nhận việc dọn dẹp và báo cáo hoàn thành
            if (CurrentRole == 1)
            {
                rdRooms.Visibility = Visibility.Collapsed;
                rdBooking.Visibility = Visibility.Collapsed;
                rdBills.Visibility = Visibility.Collapsed;
                rdServices.Visibility = Visibility.Collapsed;
                rdUsers.Visibility = Visibility.Collapsed;
                rdStats.Visibility = Visibility.Collapsed;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
                MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
            }
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void NavigateToPage(string pageUri)
        {
            placeHolder.Visibility = Visibility.Collapsed;
            PagesNavigation.Navigate(new Uri(pageUri, UriKind.RelativeOrAbsolute));
        }

        private void rdHome_Click(object sender, RoutedEventArgs e) => NavigateToPage("Pages/Home.xaml");
        private void rdRooms_Click(object sender, RoutedEventArgs e) => NavigateToPage("Pages/Rooms.xaml");
        private void rdBills_Click(object sender, RoutedEventArgs e) => NavigateToPage("Pages/Bills.xaml");
        private void rdUsers_Click(object sender, RoutedEventArgs e) => NavigateToPage("Pages/Users.xaml");
        private void rdServices_Click(object sender, RoutedEventArgs e) => NavigateToPage("Pages/Services.xaml");
        private void rdStats_Click(object sender, RoutedEventArgs e) => NavigateToPage("Pages/Stats.xaml");
        private void rdBooking_Click(object sender, RoutedEventArgs e) => NavigateToPage("Pages/Booking.xaml");
        private void rdShifts_Click(object sender, RoutedEventArgs e) => NavigateToPage("Pages/WorkSchedule.xaml");
        private void rdTasks_Click(object sender, RoutedEventArgs e) => NavigateToPage("Pages/MyTasks.xaml");

        public void SetFullName(string fullName)
        {
            txtUsername.Text = fullName;
            CurrentFullName = fullName;
        }

        private void rdLogout_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            Application.Current.MainWindow = login;
            login.Show();
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && WindowState == WindowState.Normal)
                DragMove();
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

        private void btnProfile_Click(object sender, MouseButtonEventArgs e)
        {
            rdHome.IsChecked = false;
            rdRooms.IsChecked = false;
            rdBooking.IsChecked = false;
            rdShifts.IsChecked = false;
            rdTasks.IsChecked = false;
            rdBills.IsChecked = false;
            rdServices.IsChecked = false;
            rdUsers.IsChecked = false;
            rdStats.IsChecked = false;
            placeHolder.Visibility = Visibility.Collapsed;
            PagesNavigation.Navigate(new Pages.Profile(CurrentUserId));
        }

        #region Thu Phóng Nội Dung (Zoom)
        private double _currentZoom = 1.0;
        private const double MinZoom = 0.7;
        private const double MaxZoom = 1.8;
        private const double ZoomStep = 0.1;

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                    ZoomIn();
                else if (e.Delta < 0)
                    ZoomOut();

                e.Handled = true;
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.OemPlus || e.Key == Key.Add)
                {
                    ZoomIn();
                    e.Handled = true;
                }
                else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
                {
                    ZoomOut();
                    e.Handled = true;
                }
                else if (e.Key == Key.D0 || e.Key == Key.NumPad0)
                {
                    ResetZoom();
                    e.Handled = true;
                }
            }
        }

        public void ZoomIn() => ApplyZoom(_currentZoom + ZoomStep);
        public void ZoomOut() => ApplyZoom(_currentZoom - ZoomStep);
        public void ResetZoom() => ApplyZoom(1.0);

        private void ApplyZoom(double newZoom)
        {
            _currentZoom = Math.Round(Math.Clamp(newZoom, MinZoom, MaxZoom), 1);
            if (pageScaleTransform != null)
            {
                pageScaleTransform.ScaleX = _currentZoom;
                pageScaleTransform.ScaleY = _currentZoom;
            }
            if (txtZoomPercent != null)
            {
                txtZoomPercent.Text = $"{(int)(_currentZoom * 100)}%";
            }
        }

        private void btnZoomIn_Click(object sender, RoutedEventArgs e) => ZoomIn();
        private void btnZoomOut_Click(object sender, RoutedEventArgs e) => ZoomOut();
        private void btnZoomReset_Click(object sender, RoutedEventArgs e) => ResetZoom();
        #endregion
    }
}
