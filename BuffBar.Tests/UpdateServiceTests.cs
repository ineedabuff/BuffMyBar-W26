using System;
using BuffBar.Services;
using Xunit;

namespace BuffBar.Tests;

/// <summary>
/// Tests de la logique pure de comparaison de versions (UpdateService).
/// L'appel réseau n'est pas testé ; seule la décision « plus récent ? » l'est.
/// </summary>
public class UpdateServiceTests
{
    [Theory]
    [InlineData("v0.10.0", true)]   // 0.10 > 0.9
    [InlineData("1.0.0", true)]
    [InlineData("v0.9.1", true)]
    [InlineData("v0.9.0", false)]   // identique
    [InlineData("0.8.5", false)]    // plus ancien
    [InlineData("garbage", false)]  // illisible -> pas de mise à jour
    public void IsNewer_ComparesAgainstCurrent(string latestTag, bool expected)
    {
        var current = new Version(0, 9, 0);
        Assert.Equal(expected, UpdateService.IsNewer(current, latestTag));
    }

    [Theory]
    [InlineData("v0.9.0", 0, 9, 0)]
    [InlineData("0.9.0", 0, 9, 0)]
    [InlineData("1.2.3-beta", 1, 2, 3)]   // suffixe ignoré
    [InlineData("V2.0.1", 2, 0, 1)]
    public void ParseVersion_ReadsSemanticTag(string tag, int major, int minor, int build)
    {
        Version? v = UpdateService.ParseVersion(tag);
        Assert.Equal(new Version(major, minor, build), v);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("vX")]
    [InlineData("beta")]
    public void ParseVersion_ReturnsNullForJunk(string tag)
    {
        Assert.Null(UpdateService.ParseVersion(tag));
    }
}
