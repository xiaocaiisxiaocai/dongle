using LicenseProtection.Services;
using LicenseProtection.ViewModels;
using System.Windows;

namespace LicenseProtection.Views
{
    public partial class LogViewerWindow : Window
    {
        private readonly LogViewerViewModel _viewModel;

        public LogViewerWindow(ILogService logService)
        {
            InitializeComponent();
            
            _viewModel = new LogViewerViewModel(logService);
            DataContext = _viewModel;
            
            Loaded += async (s, e) => await _viewModel.InitializeAsync();
            Closing += (s, e) => _viewModel.Dispose();
        }
    }
}