using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared._White.VPN;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Player;
using Robust.Shared.Enums;

namespace Content.Server._White.VPN
{
    /// <summary>
    /// VPN/Proxy detection system(with vpnapi.io API)
    /// </summary>
    public sealed class VPNDetectionSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IChatManager _chat = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private readonly HttpClient _httpClient = new HttpClient();
        private bool _enabled = false;
        private string _apiKey = string.Empty;

        public override void Initialize()
        {
            base.Initialize();
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _cfg.OnValueChanged(VPNDetectionCVars.VPNDetectionEnabled, enabled => _enabled = enabled, true);
            _cfg.OnValueChanged(VPNDetectionCVars.VPNApiKey, UpdateApiKey, true);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
            _cfg.UnsubValueChanged(VPNDetectionCVars.VPNDetectionEnabled, enabled => _enabled = enabled);
            _cfg.UnsubValueChanged(VPNDetectionCVars.VPNApiKey, UpdateApiKey);
            _httpClient.Dispose();
        }

        /// <summary>
        /// Updates API key when CVar is changed
        /// </summary>
        private void UpdateApiKey(string apiKey)
        {
            _apiKey = apiKey;
            Logger.InfoS("vpn", "API ключ обновлен." + (!string.IsNullOrEmpty(apiKey) ? " Ключ установлен." : " Ключ не установлен."));
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (!_enabled)
                return;

            // Checking IP on initial connection
            if (e.NewStatus == SessionStatus.Connected)
            {
                _ = CheckPlayerIPAsync(e.Session);
            }
        }

        /// <summary>
        /// Gets the IP and checks it for signs of a VPN
        /// </summary>
        private async Task CheckPlayerIPAsync(ICommonSession session)
        {
            try
            {
                if (session.Channel == null)
                    return;

                if (string.IsNullOrEmpty(_apiKey))
                {
                    Logger.ErrorS("vpn", "Проверка VPN невозможна: API ключ не установлен. Используйте vpnsetapikey для установки ключа.");
                    return;
                }

                var endPoint = session.Channel.RemoteEndPoint;
                var ipAddress = endPoint.Address.ToString();
                
                var result = await CheckVPN(ipAddress);
                
                if (result.IsVpn)
                {
                    NotifyAdmins(session, ipAddress, result);
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorS("vpn", $"Ошибка при проверке IP игрока {session.Name}: {ex}");
            }
        }

        /// <summary>
        /// Request to the vpnapi.io API
        /// </summary>
        /// <remarks>
        /// Support test mode for local IP
        /// </remarks>
        private async Task<VpnApiResponse> CheckVPN(string ipAddress)
        {
            try
            {
                string testIp = ipAddress;
                // If it is a local IP and test mode is enabled - use test IP
                if (ipAddress == "::1" || ipAddress == "127.0.0.1")
                {
                    if (_cfg.GetCVar(VPNDetectionCVars.VPNTestMode))
                    {
                        testIp = _cfg.GetCVar(VPNDetectionCVars.VPNTestIP);
                    }
                }

                var url = $"{VPNDetectionCVars.VpnApiUrl}{testIp}?key={_apiKey}";
                var response = await _httpClient.GetStringAsync(url);

                using (var doc = JsonDocument.Parse(response))
                {
                    // Check if there is a message (usually indicates an error or special case)
                    if (doc.RootElement.TryGetProperty("message", out var messageElement))
                    {
                        var messageText = messageElement.GetString() ?? string.Empty;
                        
                        // Local IP addresses in the API response
                        if (messageText.Contains("loopback") || messageText.Contains("private"))
                        {
                            return new VpnApiResponse { IP = ipAddress, IsLoopback = true };
                        }
                        // Problem with API key
                        else if (messageText.Contains("API key"))
                        {
                            Logger.ErrorS("vpn", $"Проблема с API ключом: {messageText}");
                            return new VpnApiResponse { IP = ipAddress, ErrorMessage = messageText };
                        }
                        return new VpnApiResponse { IP = ipAddress, ErrorMessage = messageText };
                    }

                    // VPN data response
                    var result = JsonSerializer.Deserialize<VpnApiResponse>(response, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result != null)
                    {
                        result.IP = ipAddress;
                        return result;
                    }
                    return new VpnApiResponse { IP = ipAddress };
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorS("vpn", $"Ошибка при запросе к VPN API: {ex}");
                return new VpnApiResponse { IP = ipAddress, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// VPN notification to administrators
        /// </summary>
        private void NotifyAdmins(ICommonSession session, string ipAddress, VpnApiResponse result)
        {
            var message = $"ВНИМАНИЕ: Игрок {session.Name} ({ipAddress}) скрывается под маской VPN/Proxy.";
            
            if (_cfg.GetCVar(VPNDetectionCVars.VPNTestMode))
            {
                 message += $"\n(Тестовый режим: проверялся IP {_cfg.GetCVar(VPNDetectionCVars.VPNTestIP)} вместо локального {ipAddress})";
            }
            _chat.SendAdminAnnouncement(message);
        }

        /// <summary>
        /// Response model from the vpnapi.io API
        /// </summary>
        public class VpnApiResponse
        {
            public string IP { get; set; } = string.Empty;
            public SecurityInfo? Security { get; set; }
            public string? ErrorMessage { get; set; }
            public bool IsLoopback { get; set; }
            //  IP is considered a VPN if Security contains positive flags for Vpn or Proxy
            public bool IsVpn => Security != null && (Security.Vpn || Security.Proxy);
        }

        public class SecurityInfo
        {
            public bool Vpn { get; set; }
            public bool Proxy { get; set; }
        }
    }
} 