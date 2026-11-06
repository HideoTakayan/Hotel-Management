using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hotel_Management.Helpers;
using Hotel_Management.Models;

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
            WindowMaximizeHelper.Attach(this);
        }

        public MainWindow(int userId, string username, string fullName, string email, int role = 1)
        {
            Application.Current.MainWindow = this;
            InitializeComponent();
            WindowMaximizeHelper.Attach(this);
            CurrentUserId = userId;
            CurrentUsername = username;
            SetFullName(fullName);
            CurrentEmail = email;
            CurrentRole = role;
            ApplyRolePermissions();

            var themeSource = Application.Current.Resources.MergedDictionaries[0].Source?.OriginalString ?? "";
            if (themeSource.Contains("LightMode"))
                iconTheme.Kind = MahApps.Metro.IconPacks.PackIconBootstrapIconsKind.SunFill;

            placeHolder.Visibility = Visibility.Collapsed;
            if (CurrentRole == 1)
            {
                rdTasks.IsChecked = true;
                NavigateToPage("Pages/MyTasks.xaml");
            }
            else
            {
                rdHome.IsChecked = true;
                NavigateToPage("Pages/Home.xaml");
            }

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        DatabaseHelper.OpenConnection(conn);
                        DatabaseHelper.SyncAllDataToCurrentDate(conn, force: false);
                    }
                }
                catch { }
            });
        }

        private void ApplyRolePermissions()
        {
            txtRole.Text = CurrentRole == 0 ? "Quản lý" : "Nhân viên";
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
            WindowState = (WindowState == WindowState.Normal) ? WindowState.Maximized : WindowState.Normal;
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private readonly System.Collections.Generic.Dictionary<string, Page> _pageCache = new System.Collections.Generic.Dictionary<string, Page>();

        private Page GetOrCreatePage(string key, Func<Page> factory)
        {
            if (!_pageCache.TryGetValue(key, out var page))
            {
                page = factory();
                _pageCache[key] = page;
            }
            return page;
        }

        private void NavigateToPage(string pageUri)
        {
            placeHolder.Visibility = Visibility.Collapsed;
            Page targetPage = null;

            switch (pageUri)
            {
                case "Pages/Home.xaml":
                    targetPage = GetOrCreatePage("Home", () => new Pages.Home());
                    break;
                case "Pages/Rooms.xaml":
                    var roomsPage = (Pages.Rooms)GetOrCreatePage("Rooms", () => new Pages.Rooms());
                    roomsPage.LoadData();
                    targetPage = roomsPage;
                    break;
                case "Pages/Bills.xaml":
                    var billsPage = (Pages.Bills)GetOrCreatePage("Bills", () => new Pages.Bills());
                    billsPage.LoadData();
                    targetPage = billsPage;
                    break;
                case "Pages/Users.xaml":
                    var usersPage = (Pages.Users)GetOrCreatePage("Users", () => new Pages.Users());
                    usersPage.LoadData();
                    targetPage = usersPage;
                    break;
                case "Pages/Services.xaml":
                    var servicesPage = (Pages.Services)GetOrCreatePage("Services", () => new Pages.Services());
                    servicesPage.LoadData();
                    targetPage = servicesPage;
                    break;
                case "Pages/Stats.xaml":
                    var statsPage = (Pages.Stats)GetOrCreatePage("Stats", () => new Pages.Stats());
                    statsPage.TriggerLoadData();
                    targetPage = statsPage;
                    break;
                case "Pages/Booking.xaml":
                    var bookingPage = (Pages.Booking)GetOrCreatePage("Booking", () => new Pages.Booking());
                    bookingPage.LoadRoomStatusBoard();
                    targetPage = bookingPage;
                    break;
                case "Pages/WorkSchedule.xaml":
                    var schedulePage = (Pages.WorkSchedule)GetOrCreatePage("WorkSchedule", () => new Pages.WorkSchedule());
                    schedulePage.LoadData();
                    targetPage = schedulePage;
                    break;
                case "Pages/MyTasks.xaml":
                    var tasksPage = (Pages.MyTasks)GetOrCreatePage("MyTasks", () => new Pages.MyTasks());
                    tasksPage.LoadData();
                    targetPage = tasksPage;
                    break;
                default:
                    PagesNavigation.Navigate(new Uri(pageUri, UriKind.RelativeOrAbsolute));
                    return;
            }

            PagesNavigation.Navigate(targetPage);
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

        public void NavigateToBills(int? selectedBillId = null)
        {
            rdBills.IsChecked = true;
            placeHolder.Visibility = Visibility.Collapsed;
            if (selectedBillId.HasValue)
            {
                var billsPage = new Pages.Bills(selectedBillId);
                _pageCache["Bills"] = billsPage;
                PagesNavigation.Navigate(billsPage);
            }
            else
            {
                var page = (Pages.Bills)GetOrCreatePage("Bills", () => new Pages.Bills());
                page.LoadData();
                PagesNavigation.Navigate(page);
            }
        }

        public void NavigateToBooking()
        {
            rdBooking.IsChecked = true;
            NavigateToPage("Pages/Booking.xaml");
        }

        public void NavigateToRooms()
        {
            rdRooms.IsChecked = true;
            NavigateToPage("Pages/Rooms.xaml");
        }

        public void NavigateToTasks()
        {
            rdTasks.IsChecked = true;
            NavigateToPage("Pages/MyTasks.xaml");
        }

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
            var page = GetOrCreatePage("Profile", () => new Pages.Profile(CurrentUserId));
            PagesNavigation.Navigate(page);
        }

        #region Thu Phóng Nội Dung (Zoom)
        private double _currentZoom = 1.0;
        private const double MinZoom = 0.7;
        private const double MaxZoom = 1.8;
        private const double ZoomStep = 0.1;

        private const double MinPageWidth = 760;
        private const double MinPageHeight = 540;

        private void borderPageHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePageSize();
        }

        private void UpdatePageSize()
        {
            if (borderPageHost == null || pageScroller == null || PagesNavigation == null) return;
            if (borderPageHost.ActualWidth <= 0 || borderPageHost.ActualHeight <= 0) return;

            double unscaledWidth = borderPageHost.ActualWidth / _currentZoom;
            double unscaledHeight = borderPageHost.ActualHeight / _currentZoom;

            bool needHScroll = unscaledWidth < MinPageWidth;
            bool needVScroll = unscaledHeight < MinPageHeight;

            if (!needHScroll && !needVScroll)
            {
                pageScroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                pageScroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                PagesNavigation.Width = double.NaN;
                PagesNavigation.Height = double.NaN;
                PagesNavigation.HorizontalAlignment = HorizontalAlignment.Stretch;
                PagesNavigation.VerticalAlignment = VerticalAlignment.Stretch;
            }
            else
            {
                pageScroller.HorizontalScrollBarVisibility = needHScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
                pageScroller.VerticalScrollBarVisibility = needVScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;

                double availW = Math.Max(0, borderPageHost.ActualWidth - (needVScroll ? 18 : 0));
                double availH = Math.Max(0, borderPageHost.ActualHeight - (needHScroll ? 18 : 0));

                PagesNavigation.HorizontalAlignment = HorizontalAlignment.Left;
                PagesNavigation.VerticalAlignment = VerticalAlignment.Top;
                PagesNavigation.Width = Math.Max(Math.Floor(availW / _currentZoom), MinPageWidth);
                PagesNavigation.Height = Math.Max(Math.Floor(availH / _currentZoom), MinPageHeight);
            }
        }

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
            UpdatePageSize();
        }

        private void btnZoomIn_Click(object sender, RoutedEventArgs e) => ZoomIn();
        private void btnZoomOut_Click(object sender, RoutedEventArgs e) => ZoomOut();
        private void btnZoomReset_Click(object sender, RoutedEventArgs e) => ResetZoom();
        #endregion
    }
}
