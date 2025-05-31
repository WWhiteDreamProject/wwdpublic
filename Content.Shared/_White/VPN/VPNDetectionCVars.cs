using Robust.Shared.Configuration;

namespace Content.Shared._White.VPN;

/// <summary>
/// Configuration constants for the VPN discovery system
/// </summary>
[CVarDefs]
public sealed class VPNDetectionCVars
{
    /// <summary>
    /// VPN API URL
    /// </summary>
    public const string VpnApiUrl = "https://vpnapi.io/api/";
    
    /// <summary>
    /// VPN API key
    /// </summary>
    public static readonly CVarDef<string> VPNApiKey =
        CVarDef.Create("vpn.api_key", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);
    
    /// <summary>
    /// Enable or disable VPN discovery.
    /// </summary>
    public static readonly CVarDef<bool> VPNDetectionEnabled =
        CVarDef.Create("vpn.enabled", true, CVar.SERVERONLY);
    
    /// <summary>
    /// Test Mode - If active, local IPs are replaced with the test IP. (For testing on the local server)
    /// </summary>
    public static readonly CVarDef<bool> VPNTestMode =
        CVarDef.Create("vpn.test_mode", false, CVar.SERVERONLY);
    
    /// <summary>
    /// Test IP to check when connecting from localhost.
    /// </summary>
    public static readonly CVarDef<string> VPNTestIP =
        CVarDef.Create("vpn.test_ip", "8.8.8.8", CVar.SERVERONLY);
} 
