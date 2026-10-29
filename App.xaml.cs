using System.Configuration;
using System.Data;
using System.Windows;

namespace Hotel_Management
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Đã xảy ra lỗi giao diện:\n{e.Exception.Message}\n\nChi tiết: {e.Exception.InnerException?.Message}", 
                            "Thông Báo Lỗi", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Warning);
            e.Handled = true; // Ngăn không cho ứng dụng bị crash đột ngột
        }
    }
}
