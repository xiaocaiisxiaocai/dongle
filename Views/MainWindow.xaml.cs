using LicenseProtection.Services;
using LicenseProtection.ViewModels;
using System;
using System.Windows;

namespace LicenseProtection.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // 初始化服务
            var logService = new LogService();
            var errorHandlingService = new ErrorHandlingService(logService);
            var hardwareService = new HardwareDetectionService();
            var licenseService = new LicenseService();
            
            // 设置全局异常处理
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var exception = e.ExceptionObject as Exception;
                errorHandlingService.HandleException(exception ?? new Exception("未知异常"), "全局异常处理", true);
            };
            
            Application.Current.DispatcherUnhandledException += (s, e) =>
            {
                errorHandlingService.HandleException(e.Exception, "UI线程异常", true);
                e.Handled = true; // 防止程序崩溃
            };

            var viewModel = new MainViewModel(hardwareService, licenseService, logService, errorHandlingService);
            DataContext = viewModel;
            
            Loaded += async (s, e) => await viewModel.InitializeAsync();
            Closing += async (s, e) => await hardwareService.StopDetectionAsync();
            
            logService.LogInfo("应用程序启动完成", "MainWindow");
        }
    }
}