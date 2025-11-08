namespace Jellyfin.Plugin.Jellio.Models;

public class SaveConfigRequest
{
    public bool JellyseerrEnabled { get; set; }
    public string? JellyseerrUrl { get; set; }
    public string? JellyseerrApiKey { get; set; }
    public string? PublicBaseUrl { get; set; }
}
