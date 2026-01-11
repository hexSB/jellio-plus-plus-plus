using System;
using System.Linq;
using System.Security.Claims;
using Jellyfin.Data.Queries;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;

namespace Jellyfin.Plugin.Jellio.Helpers;

public static class RequestHelpers
{
    internal static Guid? GetCurrentUserId(ClaimsPrincipal claimsPrincipal)
    {
        var userIdString = claimsPrincipal.FindFirstValue("Jellyfin-UserId");

        if (string.IsNullOrEmpty(userIdString))
        {
            return null;
        }

        if (!Guid.TryParse(userIdString, out var userIdGuid))
        {
            return null;
        }

        return userIdGuid;
    }

    internal static Guid? GetUserIdByAuthToken(
        string authToken,
        IDeviceManager deviceManager,
        ISessionManager? sessionManager = null
    )
    {
        // First, try to find the token in devices
        var items = deviceManager
            .GetDevices(new DeviceQuery { AccessToken = authToken, Limit = 1 })
            .Items;

        if (items.Count > 0)
        {
            return items[0].UserId;
        }

        // If not found in devices, try to find it in active sessions
        if (sessionManager != null)
        {
            var sessions = sessionManager.Sessions;
            var session = sessions.FirstOrDefault(s => 
                s.AuthenticationToken != null && 
                s.AuthenticationToken.Equals(authToken, StringComparison.OrdinalIgnoreCase));
            
            if (session != null)
            {
                return session.UserId;
            }
        }

        return null;
    }
}
