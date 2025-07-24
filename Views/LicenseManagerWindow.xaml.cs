using LicenseProtection.Services;
using LicenseProtection.ViewModels;
using System.Windows;

namespace LicenseProtection.Views
{
    public partial class LicenseManagerWindow : Window
    {
        public LicenseManagerWindow()
        {
            InitializeComponent();
            
            var licenseService = new LicenseService();
            var viewModel = new LicenseManagerViewModel(licenseService);
            DataContext = viewModel;
            
            Loaded += async (s, e) => await viewModel.InitializeAsync();
        }
    }
}