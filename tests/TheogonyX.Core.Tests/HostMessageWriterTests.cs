using System.Text.Json;
using TheogonyX.Core.Protocol;

namespace TheogonyX.Core.Tests;

public class HostMessageWriterTests
{
    [Fact]
    [Trait("TS", "TS-companion-pairing-008")]
    public void Paired_sessionToken_PAIREDのJSONになる()
    {
        using var json = JsonDocument.Parse(HostMessageWriter.Paired("Xk2pSession"));
        var root = json.RootElement;

        Assert.Equal("PAIRED", root.GetProperty("type").GetString());
        Assert.Equal(1, root.GetProperty("version").GetInt32());
        Assert.Equal("Xk2pSession", root.GetProperty("sessionToken").GetString());
    }

    [Fact]
    [Trait("TS", "TS-companion-pairing-008")]
    public void Error_上限に達した_ERRORのJSONになる()
    {
        using var json = JsonDocument.Parse(HostMessageWriter.Error(HostMessageWriter.MaxConnectionsReached));
        var root = json.RootElement;

        Assert.Equal("ERROR", root.GetProperty("type").GetString());
        Assert.Equal(1, root.GetProperty("version").GetInt32());
        Assert.Equal("MAX_CONNECTIONS_REACHED", root.GetProperty("reason").GetString());
    }
}
