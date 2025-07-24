# 加密狗授权管理系统

基于WPF+MVVM架构的软件授权保护系统，用于防止软件盗版和管理使用时长。

## 功能特性

### 🔒 硬件检测
- 自动检测USB加密狗设备
- 实时监控设备连接状态
- 支持设备热插拔检测

### 📋 授权管理
- 支持多种授权类型（试用版、标准版、专业版、企业版）
- 时长限制和使用统计
- 授权过期检测
- 加密授权文件存储

### 🛡️ 安全保护
- AES-256加密算法保护授权文件
- SHA-256校验和验证
- 防篡改授权信息

### 🖥️ 用户界面
- 现代化WPF界面设计
- MVVM架构模式
- 实时状态显示
- 友好的用户体验

## 技术架构

```
LicenseProtection/
├── Models/              # 数据模型
│   └── LicenseInfo.cs  # 授权信息模型
├── Services/            # 业务服务
│   ├── IHardwareDetectionService.cs
│   ├── HardwareDetectionService.cs
│   ├── ILicenseService.cs
│   ├── LicenseService.cs
│   ├── IEncryptionService.cs
│   └── EncryptionService.cs
├── ViewModels/          # 视图模型
│   └── MainViewModel.cs
├── Views/              # 用户界面
│   ├── MainWindow.xaml
│   └── MainWindow.xaml.cs
├── Utils/              # 工具类
│   └── LicenseGenerator.cs
├── Examples/           # 示例代码
│   └── LicenseCreator.cs
└── Converters/         # 数据转换器
    └── InverseBooleanToVisibilityConverter.cs
```

## 核心组件

### 硬件检测服务 (HardwareDetectionService)
- 监控USB设备变化
- 检测license.dat文件存在
- 事件驱动的设备状态通知

### 授权服务 (LicenseService)
- 加载和验证授权文件
- 使用时长统计和更新
- 加密存储授权信息

### 加密服务 (EncryptionService)
- AES-256数据加密/解密
- 密钥派生和安全存储
- 文件完整性校验

## 使用方法

### 1. 编译项目
```bash
dotnet build
dotnet run
```

### 2. 创建授权文件
```csharp
// 创建试用版授权
var trialLicense = LicenseGenerator.CreateTrialLicense(
    "客户名称", 
    "产品名称", 
    trialDays: 30, 
    maxHours: 100);

// 保存到设备
await LicenseGenerator.GenerateLicenseFileAsync("E:\\license.dat", trialLicense);
```

### 3. 授权类型说明

| 类型 | 说明 | 时长限制 | 过期时间 |
|------|------|----------|----------|
| Trial | 试用版 | 可设置 | 短期 |
| Standard | 标准版 | 可设置 | 1年 |
| Professional | 专业版 | 无限制 | 多年 |
| Enterprise | 企业版 | 无限制 | 永久 |

### 4. 集成到应用程序

```csharp
// 在应用启动时检查授权
var hardwareService = new HardwareDetectionService();
var licenseService = new LicenseService();

await hardwareService.StartDetectionAsync();
var hardware = await hardwareService.GetConnectedHardwareAsync();

if (hardware != null)
{
    var license = await licenseService.LoadLicenseAsync(hardware.DeviceId);
    var isValid = await licenseService.ValidateLicenseAsync(license);
    
    if (!isValid)
    {
        // 显示授权无效提示
        Application.Current.Shutdown();
    }
}
```

## 安全注意事项

1. **密钥管理**: 生产环境中应使用安全的密钥管理方案
2. **代码混淆**: 建议对发布的程序进行代码混淆
3. **防调试**: 可添加反调试和反逆向工程保护
4. **网络验证**: 可结合在线验证增强安全性

## 系统要求

- .NET 6.0 或更高版本
- Windows 10/11
- USB端口支持

## 依赖包

- Microsoft.Toolkit.Mvvm (MVVM框架)
- System.Management (硬件检测)
- Newtonsoft.Json (JSON序列化)

## 许可证

本项目仅供学习和合法的软件保护用途使用。