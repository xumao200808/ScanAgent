using Microsoft.Win32;
using System;
using System.IO;

namespace ScanAgent.Utils;

public class RegistrationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public static class TwainRegistryHelper
{
    public static RegistrationResult RegisterKodakDriver()
    {
        var result = new RegistrationResult();
        Console.WriteLine("========================================");
        Console.WriteLine("[TwainRegistry] Starting Kodak driver registration...");
        Console.WriteLine("========================================");

        try
        {
            // 检查管理员权限
            bool isAdmin = IsAdministrator();
            Console.WriteLine($"[TwainRegistry] Is Administrator: {isAdmin}");

            if (!isAdmin)
            {
                string errorMsg = "需要以管理员身份运行才能修改注册表！\n\n请右键点击 start.bat，选择'以管理员身份运行'。";
                Console.WriteLine($"[TwainRegistry] ERROR: {errorMsg}");
                result.Success = false;
                result.Message = errorMsg;
                return result;
            }

            // 检查系统架构
            bool is64Bit = Environment.Is64BitOperatingSystem;
            bool is64BitProcess = Environment.Is64BitProcess;
            Console.WriteLine($"[TwainRegistry] System: {(is64Bit ? "64-bit" : "32-bit")}, Process: {(is64BitProcess ? "64-bit" : "32-bit")}");

            // 可能的Kodak驱动路径
            string[] possiblePaths = new[]
            {
                @"C:\Windows\twain_32\Kodak\kds_i1400.ds",
                @"C:\Windows\twain_32\Kodak\kds_i1400.dll",
                @"C:\Windows\twain_32\KODAK\kds_i1400.ds",
                @"C:\Windows\twain_32\KODAK\kds_i1400.dll"
            };

            Console.WriteLine("[TwainRegistry] Looking for Kodak driver in:");
            foreach (var path in possiblePaths)
            {
                Console.WriteLine($"  - {path} (exists: {File.Exists(path)})");
            }

            string? driverPath = null;
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    driverPath = path;
                    Console.WriteLine($"[TwainRegistry] Found driver at: {driverPath}");
                    break;
                }
            }

            if (driverPath == null)
            {
                string errorMsg = "未找到 Kodak 驱动文件！\n\n请检查以下路径是否存在：\n" + string.Join("\n", possiblePaths);
                Console.WriteLine($"[TwainRegistry] ERROR: {errorMsg}");
                result.Success = false;
                result.Message = errorMsg;
                return result;
            }

            // 注册到32位TWAIN注册表
            Console.WriteLine("[TwainRegistry] Registering to 32-bit TWAIN registry...");
            RegisterTwainSource("Kodak i1405", driverPath, is64Bit: false);

            // 如果是64位系统，也尝试注册到64位TWAIN注册表
            if (is64Bit)
            {
                Console.WriteLine("[TwainRegistry] Registering to 64-bit TWAIN registry...");
                RegisterTwainSource("Kodak i1405", driverPath, is64Bit: true);
            }

            Console.WriteLine("========================================");
            Console.WriteLine("[TwainRegistry] Registration completed!");
            Console.WriteLine("[TwainRegistry] Please restart ScanAgent for changes to take effect");
            Console.WriteLine("========================================");

            result.Success = true;
            result.Message = "Kodak 驱动注册成功！\n\n请重启 ScanAgent 以使更改生效。";
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TwainRegistry] ERROR: {ex.Message}");
            Console.WriteLine($"[TwainRegistry] Stack trace: {ex.StackTrace}");
            result.Success = false;
            result.Message = $"注册失败：\n{ex.Message}";
            return result;
        }
    }

    private static bool IsAdministrator()
    {
        try
        {
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
        }
        catch
        {
            return false;
        }
    }

    private static void RegisterTwainSource(string sourceName, string driverPath, bool is64Bit)
    {
        try
        {
            string registryPath = is64Bit ? @"SOFTWARE\Twain_64" : @"SOFTWARE\Twain_32";
            Console.WriteLine($"[TwainRegistry] Registering to {(is64Bit ? "64-bit" : "32-bit")} registry: {registryPath}\\{sourceName}");

            using (var baseKey = Registry.LocalMachine.CreateSubKey(registryPath))
            {
                if (baseKey == null)
                {
                    Console.WriteLine($"[TwainRegistry] WARNING: Could not create/open registry key: {registryPath}");
                    return;
                }

                // 删除旧的键（如果存在）
                try
                {
                    baseKey.DeleteSubKeyTree(sourceName, throwOnMissingSubKey: false);
                    Console.WriteLine($"[TwainRegistry] Deleted old key: {sourceName}");
                }
                catch { }

                // 创建新的数据源键
                using (var sourceKey = baseKey.CreateSubKey(sourceName))
                {
                    if (sourceKey == null)
                    {
                        Console.WriteLine($"[TwainRegistry] WARNING: Could not create source key: {sourceName}");
                        return;
                    }

                    // 设置默认值为数据源名称
                    sourceKey.SetValue("", sourceName, RegistryValueKind.String);

                    // 设置DLL路径
                    sourceKey.SetValue("DLL", driverPath, RegistryValueKind.String);

                    Console.WriteLine($"[TwainRegistry] SUCCESS: Registered {sourceName} -> {driverPath}");
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            string errorMsg = "访问被拒绝！请以管理员身份运行。";
            Console.WriteLine($"[TwainRegistry] ERROR: {errorMsg}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TwainRegistry] ERROR registering {(is64Bit ? "64-bit" : "32-bit")}: {ex.Message}");
        }
    }

    public static RegistrationResult ListRegisteredSources()
    {
        var result = new RegistrationResult();
        Console.WriteLine("========================================");
        Console.WriteLine("[TwainRegistry] Listing registered TWAIN sources...");
        Console.WriteLine("========================================");

        try
        {
            bool is64Bit = Environment.Is64BitOperatingSystem;

            Console.WriteLine("\n--- 32-bit TWAIN sources ---");
            ListRegisteredSourcesForArch(false);
            Console.WriteLine();

            if (is64Bit)
            {
                Console.WriteLine("--- 64-bit TWAIN sources ---");
                ListRegisteredSourcesForArch(true);
                Console.WriteLine();
            }

            result.Success = true;
            result.Message = "已列出 TWAIN 数据源！\n\n请查看命令行窗口了解详细信息。";
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TwainRegistry] ERROR: {ex.Message}");
            result.Success = false;
            result.Message = $"查询失败：\n{ex.Message}";
            return result;
        }
    }

    public static RegistrationResult UnregisterKodakDriver()
    {
        var result = new RegistrationResult();
        Console.WriteLine("========================================");
        Console.WriteLine("[TwainRegistry] Starting Kodak driver unregistration...");
        Console.WriteLine("========================================");

        try
        {
            // 检查管理员权限
            bool isAdmin = IsAdministrator();
            Console.WriteLine($"[TwainRegistry] Is Administrator: {isAdmin}");

            if (!isAdmin)
            {
                string errorMsg = "需要以管理员身份运行才能修改注册表！\n\n请右键点击 start.bat，选择'以管理员身份运行'。";
                Console.WriteLine($"[TwainRegistry] ERROR: {errorMsg}");
                result.Success = false;
                result.Message = errorMsg;
                return result;
            }

            bool is64Bit = Environment.Is64BitOperatingSystem;
            bool is64BitProcess = Environment.Is64BitProcess;
            Console.WriteLine($"[TwainRegistry] System: {(is64Bit ? "64-bit" : "32-bit")}, Process: {(is64BitProcess ? "64-bit" : "32-bit")}");

            string[] sourceNames = new[] { "Kodak i1405", "Kodak i1400", "Kodak" };

            // 从32位TWAIN注册表中取消注册
            Console.WriteLine("[TwainRegistry] Unregistering from 32-bit TWAIN registry...");
            foreach (var sourceName in sourceNames)
            {
                UnregisterTwainSource(sourceName, is64Bit: false);
            }

            // 如果是64位系统，也从64位TWAIN注册表中取消注册
            if (is64Bit)
            {
                Console.WriteLine("[TwainRegistry] Unregistering from 64-bit TWAIN registry...");
                foreach (var sourceName in sourceNames)
                {
                    UnregisterTwainSource(sourceName, is64Bit: true);
                }
            }

            Console.WriteLine("========================================");
            Console.WriteLine("[TwainRegistry] Unregistration completed!");
            Console.WriteLine("[TwainRegistry] Please restart ScanAgent for changes to take effect");
            Console.WriteLine("========================================");

            result.Success = true;
            result.Message = "Kodak 驱动取消注册成功！\n\n请重启 ScanAgent 以使更改生效。";
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TwainRegistry] ERROR: {ex.Message}");
            Console.WriteLine($"[TwainRegistry] Stack trace: {ex.StackTrace}");
            result.Success = false;
            result.Message = $"取消注册失败：\n{ex.Message}";
            return result;
        }
    }

    private static void UnregisterTwainSource(string sourceName, bool is64Bit)
    {
        try
        {
            string registryPath = is64Bit ? @"SOFTWARE\Twain_64" : @"SOFTWARE\Twain_32";
            Console.WriteLine($"[TwainRegistry] Unregistering from {(is64Bit ? "64-bit" : "32-bit")} registry: {registryPath}\\{sourceName}");

            using (var baseKey = Registry.LocalMachine.OpenSubKey(registryPath, writable: true))
            {
                if (baseKey == null)
                {
                    Console.WriteLine($"[TwainRegistry] Registry key not found: {registryPath}");
                    return;
                }

                // 检查源是否存在
                string[] subKeys = baseKey.GetSubKeyNames();
                if (!subKeys.Contains(sourceName))
                {
                    Console.WriteLine($"[TwainRegistry] Source not registered: {sourceName}");
                    return;
                }

                // 删除源
                try
                {
                    baseKey.DeleteSubKeyTree(sourceName, throwOnMissingSubKey: false);
                    Console.WriteLine($"[TwainRegistry] SUCCESS: Unregistered {sourceName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TwainRegistry] WARNING: Could not unregister {sourceName}: {ex.Message}");
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            string errorMsg = "访问被拒绝！请以管理员身份运行。";
            Console.WriteLine($"[TwainRegistry] ERROR: {errorMsg}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TwainRegistry] ERROR unregistering {(is64Bit ? "64-bit" : "32-bit")}: {ex.Message}");
        }
    }

    private static void ListRegisteredSourcesForArch(bool is64Bit)
    {
        try
        {
            string registryPath = is64Bit ? @"SOFTWARE\Twain_64" : @"SOFTWARE\Twain_32";

            using (var baseKey = Registry.LocalMachine.OpenSubKey(registryPath))
            {
                if (baseKey == null)
                {
                    Console.WriteLine("  (No registry key found)");
                    return;
                }

                string[] sourceNames = baseKey.GetSubKeyNames();
                if (sourceNames.Length == 0)
                {
                    Console.WriteLine("  (No sources registered)");
                    return;
                }

                foreach (var sourceName in sourceNames)
                {
                    try
                    {
                        using (var sourceKey = baseKey.OpenSubKey(sourceName))
                        {
                            if (sourceKey == null) continue;

                            string? dllPath = sourceKey.GetValue("DLL") as string;
                            Console.WriteLine($"  - {sourceName}");
                            if (!string.IsNullOrEmpty(dllPath))
                            {
                                Console.WriteLine($"    DLL: {dllPath}");
                            }
                        }
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR: {ex.Message}");
        }
    }
}
