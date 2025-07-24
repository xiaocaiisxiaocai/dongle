using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using LicenseProtection.Models;
using LicenseProtection.Services;
using LicenseProtection.Utils;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows;

namespace LicenseProtection.ViewModels
{
    public partial class LicenseManagerViewModel : ObservableObject
    {
        private readonly ILicenseService _licenseService;

        [ObservableProperty]
        private ObservableCollection<LicenseInfo> _licenses = new();

        [ObservableProperty]
        private LicenseInfo? _selectedLicense;

        [ObservableProperty]
        private string _currentPath = "";

        [ObservableProperty]
        private string _statusMessage = "就绪";

        public ICommand CreateLicenseCommand { get; }
        public ICommand EditLicenseCommand { get; }
        public ICommand DeleteLicenseCommand { get; }
        public ICommand ImportLicenseCommand { get; }
        public ICommand ExportLicenseCommand { get; }
        public ICommand SelectPathCommand { get; }
        public ICommand RefreshCommand { get; }

        public LicenseManagerViewModel(ILicenseService licenseService)
        {
            _licenseService = licenseService;

            CreateLicenseCommand = new AsyncRelayCommand(CreateLicenseAsync);
            EditLicenseCommand = new AsyncRelayCommand<LicenseInfo>(EditLicenseAsync);
            DeleteLicenseCommand = new AsyncRelayCommand<LicenseInfo>(DeleteLicenseAsync);
            ImportLicenseCommand = new AsyncRelayCommand(ImportLicenseAsync);
            ExportLicenseCommand = new AsyncRelayCommand(ExportLicenseAsync);
            SelectPathCommand = new RelayCommand(SelectPath);
            RefreshCommand = new AsyncRelayCommand(RefreshLicensesAsync);

            // 默认设置当前路径
            CurrentPath = Directory.GetCurrentDirectory();
        }

        public async Task InitializeAsync()
        {
            await RefreshLicensesAsync();
        }

        private async Task CreateLicenseAsync()
        {
            try
            {
                StatusMessage = "创建新授权...";
                
                var newLicense = LicenseGenerator.CreateTrialLicense("新客户", "产品名称");
                var editViewModel = new LicenseEditViewModel(newLicense, true);
                var editWindow = new Views.LicenseEditWindow { DataContext = editViewModel };
                
                if (editWindow.ShowDialog() == true)
                {
                    // 保存到当前路径
                    var success = await _licenseService.SaveLicenseAsync(CurrentPath, editViewModel.License);
                    if (success)
                    {
                        await RefreshLicensesAsync();
                        StatusMessage = "授权创建成功";
                    }
                    else
                    {
                        StatusMessage = "授权创建失败";
                    }
                }
                else
                {
                    StatusMessage = "已取消创建";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"创建失败: {ex.Message}";
            }
        }

        private async Task EditLicenseAsync(LicenseInfo? license)
        {
            if (license == null) return;

            try
            {
                StatusMessage = "编辑授权...";
                
                var editViewModel = new LicenseEditViewModel(license.Clone(), false);
                var editWindow = new Views.LicenseEditWindow { DataContext = editViewModel };
                
                if (editWindow.ShowDialog() == true)
                {
                    // 更新原授权
                    var index = Licenses.IndexOf(license);
                    if (index >= 0)
                    {
                        Licenses[index] = editViewModel.License;
                        
                        // 保存到文件
                        var success = await _licenseService.SaveLicenseAsync(CurrentPath, editViewModel.License);
                        StatusMessage = success ? "授权更新成功" : "授权更新失败";
                    }
                }
                else
                {
                    StatusMessage = "已取消编辑";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"编辑失败: {ex.Message}";
            }
        }

        private async Task DeleteLicenseAsync(LicenseInfo? license)
        {
            if (license == null) return;

            var result = MessageBox.Show(
                $"确认删除授权 {license.SerialNumber}？", 
                "确认删除", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Licenses.Remove(license);
                    
                    // 删除对应的授权文件
                    var licensePath = Path.Combine(CurrentPath, $"license_{license.SerialNumber}.dat");
                    if (File.Exists(licensePath))
                    {
                        File.Delete(licensePath);
                    }
                    
                    StatusMessage = "授权删除成功";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"删除失败: {ex.Message}";
                }
            }

            await Task.CompletedTask;
        }

        private async Task ImportLicenseAsync()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "选择授权文件",
                    Filter = "授权文件|*.dat|所有文件|*.*",
                    Multiselect = true
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    StatusMessage = "导入授权文件...";
                    int successCount = 0;
                    
                    foreach (var fileName in openFileDialog.FileNames)
                    {
                        try
                        {
                            var directory = Path.GetDirectoryName(fileName) ?? "";
                            var license = await _licenseService.LoadLicenseAsync(directory);
                            
                            if (license != null)
                            {
                                // 复制到当前路径
                                var targetPath = Path.Combine(CurrentPath, Path.GetFileName(fileName));
                                File.Copy(fileName, targetPath, true);
                                successCount++;
                            }
                        }
                        catch
                        {
                            // 忽略单个文件的导入失败
                        }
                    }
                    
                    await RefreshLicensesAsync();
                    StatusMessage = $"成功导入 {successCount} 个授权文件";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"导入失败: {ex.Message}";
            }
        }

        private async Task ExportLicenseAsync()
        {
            if (SelectedLicense == null)
            {
                StatusMessage = "请选择要导出的授权";
                return;
            }

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "导出授权文件",
                    Filter = "授权文件|*.dat",
                    FileName = $"license_{SelectedLicense.SerialNumber}.dat"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    StatusMessage = "导出授权文件...";
                    
                    var success = await _licenseService.SaveLicenseAsync(
                        Path.GetDirectoryName(saveFileDialog.FileName) ?? "", 
                        SelectedLicense);
                    
                    StatusMessage = success ? "导出成功" : "导出失败";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"导出失败: {ex.Message}";
            }
        }

        private void SelectPath()
        {
            try
            {
                using var dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog
                {
                    Title = "选择授权文件目录",
                    IsFolderPicker = true,
                    InitialDirectory = CurrentPath
                };

                if (dialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
                {
                    CurrentPath = dialog.FileName;
                    Task.Run(async () => await RefreshLicensesAsync());
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"选择路径失败: {ex.Message}";
                MessageBox.Show($"路径选择失败: {ex.Message}\n\n当前路径: {CurrentPath}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task RefreshLicensesAsync()
        {
            try
            {
                StatusMessage = "刷新授权列表...";
                Licenses.Clear();

                if (!Directory.Exists(CurrentPath))
                {
                    StatusMessage = "目录不存在";
                    return;
                }

                var licenseFiles = Directory.GetFiles(CurrentPath, "*.dat");
                int loadedCount = 0;

                foreach (var file in licenseFiles)
                {
                    try
                    {
                        var directory = Path.GetDirectoryName(file) ?? "";
                        var license = await _licenseService.LoadLicenseAsync(directory);
                        
                        if (license != null)
                        {
                            Licenses.Add(license);
                            loadedCount++;
                        }
                    }
                    catch
                    {
                        // 忽略无法加载的文件
                    }
                }

                StatusMessage = $"加载了 {loadedCount} 个授权文件";
            }
            catch (Exception ex)
            {
                StatusMessage = $"刷新失败: {ex.Message}";
            }
        }
    }

    // 扩展方法：克隆授权对象
    public static class LicenseInfoExtensions
    {
        public static LicenseInfo Clone(this LicenseInfo source)
        {
            return new LicenseInfo
            {
                SerialNumber = source.SerialNumber,
                CustomerName = source.CustomerName,
                ProductName = source.ProductName,
                Type = source.Type,
                ExpirationDate = source.ExpirationDate,
                MaxUsageHours = source.MaxUsageHours,
                UsedHours = source.UsedHours,
                IsActive = source.IsActive
            };
        }
    }
}