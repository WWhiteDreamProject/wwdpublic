using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._White.VPN;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Configuration;

namespace Content.Server._White.VPN
{
    /// <summary>
    /// Command to set the API key for VPN detection
    /// </summary>
    [AdminCommand(AdminFlags.Host)]
    public sealed class VPNSetApiKeyCommand : IConsoleCommand
    {
        public string Command => "vpnsetapikey";
        public string Description => Loc.GetString("Установить API ключ для сервиса обнаружения VPN");
        public string Help => Loc.GetString("Использование: vpnsetapikey <ключ API>");
        
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError(Loc.GetString("Ошибка: Неверное количество аргументов. Использование: vpnsetapikey <ключ API>"));
                return;
            }

            var apiKey = args[0];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                shell.WriteError(Loc.GetString("Ошибка: API ключ не может быть пустым"));
                return;
            }
            
            var cfg = IoCManager.Resolve<IConfigurationManager>();
            cfg.SetCVar(VPNDetectionCVars.VPNApiKey, apiKey);
            
            shell.WriteLine(Loc.GetString("API ключ для обнаружения VPN успешно установлен."));
        }
    }

    /// <summary>
    /// Command to enable/disable VPN inspection
    /// </summary>
    [AdminCommand(AdminFlags.Host)]
    public sealed class VPNToggleCommand : IConsoleCommand
    {
        public string Command => "vpntoggle";
        public string Description => Loc.GetString("Включить/выключить обнаружение VPN");
        public string Help => Loc.GetString("Использование: vpntoggle <on/off>");
        
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1 || (args[0] != "on" && args[0] != "off"))
            {
                shell.WriteError(Loc.GetString("Ошибка: Неверный аргумент. Использование: vpntoggle <on/off>"));
                return;
            }

            var enabled = args[0] == "on";
            
            var cfg = IoCManager.Resolve<IConfigurationManager>();
            cfg.SetCVar(VPNDetectionCVars.VPNDetectionEnabled, enabled);
            
            if (enabled)
                shell.WriteLine(Loc.GetString("Обнаружение VPN включено."));
            else
                shell.WriteLine(Loc.GetString("Обнаружение VPN отключено."));
        }
    }
    
    /// <summary>
    /// Command for test mode control
    /// When test mode is enabled, local IPs are tested using the test IP
    /// </summary>
    [AdminCommand(AdminFlags.Host)]
    public sealed class VPNTestModeCommand : IConsoleCommand
    {
        public string Command => "vpntestmode";
        public string Description => Loc.GetString("Включить/выключить тестовый режим проверки VPN");
        public string Help => Loc.GetString("Использование: vpntestmode <on/off>");
        
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1 || (args[0] != "on" && args[0] != "off"))
            {
                shell.WriteError(Loc.GetString("Ошибка: Неверный аргумент. Использование: vpntestmode <on/off>"));
                return;
            }

            var enabled = args[0] == "on";
            
            var cfg = IoCManager.Resolve<IConfigurationManager>();
            cfg.SetCVar(VPNDetectionCVars.VPNTestMode, enabled);
            
            if (enabled)
                shell.WriteLine(Loc.GetString("Тестовый режим VPN включен."));
            else
                shell.WriteLine(Loc.GetString("Тестовый режим VPN выключен."));
        }
    }
    
    /// <summary>
    /// Command to set the test IP address
    /// </summary>
    [AdminCommand(AdminFlags.Host)]
    public sealed class VPNSetTestIPCommand : IConsoleCommand
    {
        public string Command => "vpnsettestip";
        public string Description => Loc.GetString("Установить тестовый IP для проверки VPN");
        public string Help => Loc.GetString("Использование: vpnsettestip <IP адрес>");
        
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError(Loc.GetString("Ошибка: Неверное количество аргументов. Использование: vpnsettestip <IP адрес>"));
                return;
            }

            var testIp = args[0];
            if (!System.Net.IPAddress.TryParse(testIp, out _))
            {
                shell.WriteError(Loc.GetString("Ошибка: Некорректный IP адрес"));
                return;
            }
            
            var cfg = IoCManager.Resolve<IConfigurationManager>();
            cfg.SetCVar(VPNDetectionCVars.VPNTestIP, testIp);
            
            shell.WriteLine(Loc.GetString($"Тестовый IP для проверки VPN установлен на: {testIp}"));
        }
    }

    /// <summary>
    /// Command to view the current settings of the VPN system
    /// </summary>
    [AdminCommand(AdminFlags.Host)]
    public sealed class VPNStatusCommand : IConsoleCommand
    {
        public string Command => "vpnstatus";
        public string Description => Loc.GetString("Проверить статус системы обнаружения VPN");
        public string Help => Loc.GetString("Использование: vpnstatus");
        
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var cfg = IoCManager.Resolve<IConfigurationManager>();
            
            var enabled = cfg.GetCVar(VPNDetectionCVars.VPNDetectionEnabled);
            var testMode = cfg.GetCVar(VPNDetectionCVars.VPNTestMode);
            var testIp = cfg.GetCVar(VPNDetectionCVars.VPNTestIP);
            var apiKey = cfg.GetCVar(VPNDetectionCVars.VPNApiKey);
            
            shell.WriteLine(Loc.GetString("Статус обнаружения VPN:"));
            shell.WriteLine(Loc.GetString($"- Включено: {(enabled ? "Да" : "Нет")}"));
            shell.WriteLine(Loc.GetString($"- API URL: {VPNDetectionCVars.VpnApiUrl}"));
            shell.WriteLine(Loc.GetString($"- API ключ установлен: {(!string.IsNullOrEmpty(apiKey) ? "Да" : "Нет")}"));
            shell.WriteLine(Loc.GetString($"- Тестовый режим: {(testMode ? "Включен" : "Выключен")}"));
            
            if (testMode)
            {
                shell.WriteLine(Loc.GetString($"- Тестовый IP: {testIp}"));
            }
        }
    }
} 