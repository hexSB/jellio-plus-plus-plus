using System.Net;
using System.Text;
using Jellyfin.Plugin.Jellio.Services;

namespace Jellyfin.Plugin.Jellio.Tests.Services;

public class JellyseerrStatusServiceTests
{
    [Theory]
    [InlineData(1, "unknown")]
    [InlineData(2, "pending")]
    [InlineData(3, "processing")]
    [InlineData(4, "partially_available")]
    [InlineData(5, "available")]
    [InlineData(6, "deleted")]
    public void MapMediaStatus_MapsJellyseerrStatusValues(int rawStatus, string expected)
    {
        Assert.Equal(expected, JellyseerrStatusService.MapMediaStatus(rawStatus));
    }

    [Theory]
    [InlineData("pending", true)]
    [InlineData("processing", true)]
    [InlineData("partially_available", true)]
    [InlineData("available", true)]
    [InlineData("unknown", false)]
    [InlineData("deleted", false)]
    public void HasExistingRequestOrMedia_OnlyBlocksActiveOrAvailableMedia(string status, bool expected)
    {
        Assert.Equal(expected, JellyseerrStatusService.HasExistingRequestOrMedia(status));
    }

    [Theory]
    [InlineData("movie", "http://jellyseerr.test/api/v1/movie/123")]
    [InlineData("tv", "http://jellyseerr.test/api/v1/tv/123")]
    public async Task GetMediaStatus_UsesTypedJellyseerrDetailsEndpoint(string type, string expectedUri)
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"mediaInfo":{"status":2}}""");
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://jellyseerr.test/")
        };

        var status = await JellyseerrStatusService.GetMediaStatus(client, 123, type);

        Assert.Equal("pending", status);
        Assert.Equal(expectedUri, handler.LastRequestUri);
    }

    [Fact]
    public async Task GetMediaStatus_ReturnsLocalStatusWhenJellyseerrDoesNotReturnMediaInfo()
    {
        JellyseerrStatusService.MarkLocalStatus(456, "movie", "pending");
        using var client = new HttpClient(new StubHandler(HttpStatusCode.NotFound, string.Empty))
        {
            BaseAddress = new Uri("http://jellyseerr.test/")
        };

        var status = await JellyseerrStatusService.GetMediaStatus(client, 456, "movie");

        Assert.Equal("pending", status);
    }

    private sealed class StubHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        public string? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri?.ToString();
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }
}
