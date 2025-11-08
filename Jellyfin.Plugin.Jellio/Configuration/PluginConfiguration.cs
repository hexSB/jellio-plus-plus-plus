using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Jellio.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public bool JellyseerrEnabled { get; set; } = false;
    public string JellyseerrUrl { get; set; } = string.Empty;
    public string JellyseerrApiKey { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
}
