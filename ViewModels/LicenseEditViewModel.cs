using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using LicenseProtection.Models;
using LicenseProtection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace LicenseProtection.ViewModels
{
    public partial class LicenseEditViewModel : ObservableObject
    {
        public LicenseInfo License { get; }

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _serialNumber;

        [ObservableProperty]
        private string _customerName;

        [ObservableProperty]
        private string _productName;

        [ObservableProperty]
        private LicenseType _licenseType;

        [ObservableProperty]
        private DateTime _expirationDate;

        [ObservableProperty]
        private int _maxUsageHours;

        [ObservableProperty]
        private int _usedHours;

        [ObservableProperty]
        private bool _isActive;

        [ObservableProperty]
        private bool _isSerialNumberReadOnly;

        public List<LicenseType> AvailableLicenseTypes { get; } = 
            Enum.GetValues<LicenseType>().ToList();

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ApplyTrialCommand { get; }
        public ICommand ApplyStandardCommand { get; }
        public ICommand ApplyProfessionalCommand { get; }
        public ICommand ApplyEnterpriseCommand { get; }

        public LicenseEditViewModel(LicenseInfo license, bool isNew)
        {
            License = license;
            Title = isNew ? "创建新授权" : "编辑授权";
            IsSerialNumberReadOnly = !isNew;

            // 绑定属性
            SerialNumber = license.SerialNumber;
            CustomerName = license.CustomerName;
            ProductName = license.ProductName;
            LicenseType = license.Type;
            ExpirationDate = license.ExpirationDate;
            MaxUsageHours = license.MaxUsageHours;
            UsedHours = license.UsedHours;
            IsActive = license.IsActive;

            // 初始化命令
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
            ApplyTrialCommand = new RelayCommand(ApplyTrial);
            ApplyStandardCommand = new RelayCommand(ApplyStandard);
            ApplyProfessionalCommand = new RelayCommand(ApplyProfessional);
            ApplyEnterpriseCommand = new RelayCommand(ApplyEnterprise);

            // 监听属性变化，同步到License对象
            PropertyChanged += (s, e) => UpdateLicense();
        }

        private void UpdateLicense()
        {
            License.SerialNumber = SerialNumber;
            License.CustomerName = CustomerName;
            License.ProductName = ProductName;
            License.Type = LicenseType;
            License.ExpirationDate = ExpirationDate;
            License.MaxUsageHours = MaxUsageHours;
            License.UsedHours = UsedHours;
            License.IsActive = IsActive;
        }

        private void Save()
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(SerialNumber))
            {
                MessageBox.Show("序列号不能为空", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(CustomerName))
            {
                MessageBox.Show("客户名称不能为空", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(ProductName))
            {
                MessageBox.Show("产品名称不能为空", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ExpirationDate <= DateTime.Now && LicenseType != LicenseType.Enterprise)
            {
                var result = MessageBox.Show(
                    "过期时间已过，授权将无法使用。是否继续？", 
                    "确认", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.No)
                    return;
            }

            // 关闭窗口并返回成功
            CloseWindow(true);
        }

        private void Cancel()
        {
            CloseWindow(false);
        }

        private void ApplyTrial()
        {
            LicenseType = LicenseType.Trial;
            ExpirationDate = DateTime.Now.AddDays(30);
            MaxUsageHours = 50;
            IsActive = true;
        }

        private void ApplyStandard()
        {
            LicenseType = LicenseType.Standard;
            ExpirationDate = DateTime.Now.AddYears(1);
            MaxUsageHours = 1000;
            IsActive = true;
        }

        private void ApplyProfessional()
        {
            LicenseType = LicenseType.Professional;
            ExpirationDate = DateTime.Now.AddYears(2);
            MaxUsageHours = 0; // 无限制
            IsActive = true;
        }

        private void ApplyEnterprise()
        {
            LicenseType = LicenseType.Enterprise;
            ExpirationDate = DateTime.MaxValue;
            MaxUsageHours = 0; // 无限制
            IsActive = true;
        }

        private void CloseWindow(bool dialogResult)
        {
            // 查找当前窗口并关闭
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.DialogResult = dialogResult;
                    window.Close();
                    break;
                }
            }
        }
    }
}