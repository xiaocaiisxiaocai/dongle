using LicenseProtection.Models;
using LicenseProtection.Utils;
using System;
using System.Threading.Tasks;

namespace LicenseProtection.Examples
{
    public static class LicenseCreator
    {
        public static async Task CreateExampleLicensesAsync()
        {
            Console.WriteLine("开始创建示例授权文件...");

            var trialLicense = LicenseGenerator.CreateTrialLicense(
                "测试客户", 
                "演示软件", 
                trialDays: 30, 
                maxHours: 50);

            var standardLicense = LicenseGenerator.CreateStandardLicense(
                "标准客户", 
                "标准版软件", 
                DateTime.Now.AddYears(1), 
                maxHours: 1000);

            var professionalLicense = LicenseGenerator.CreateProfessionalLicense(
                "专业客户", 
                "专业版软件", 
                DateTime.Now.AddYears(2));

            var enterpriseLicense = LicenseGenerator.CreateEnterpriseLicense(
                "企业客户", 
                "企业版软件");

            Console.WriteLine($"试用版授权: {trialLicense.SerialNumber}");
            Console.WriteLine($"标准版授权: {standardLicense.SerialNumber}");
            Console.WriteLine($"专业版授权: {professionalLicense.SerialNumber}");
            Console.WriteLine($"企业版授权: {enterpriseLicense.SerialNumber}");

            Console.WriteLine("\n授权文件创建完成！");
            Console.WriteLine("请将生成的 license.dat 文件复制到加密狗设备的根目录。");
        }

        public static void PrintLicenseInfo(LicenseInfo license)
        {
            Console.WriteLine($"序列号: {license.SerialNumber}");
            Console.WriteLine($"客户名称: {license.CustomerName}");
            Console.WriteLine($"产品名称: {license.ProductName}");
            Console.WriteLine($"授权类型: {license.Type}");
            Console.WriteLine($"过期时间: {license.ExpirationDate:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"最大使用时长: {(license.MaxUsageHours > 0 ? license.MaxUsageHours + " 小时" : "无限制")}");
            Console.WriteLine($"已使用时长: {license.UsedHours} 小时");
            Console.WriteLine($"激活状态: {(license.IsActive ? "已激活" : "未激活")}");
            Console.WriteLine();
        }
    }
}