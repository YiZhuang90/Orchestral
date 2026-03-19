using System;
using ExperimentalControlPlatform.Core.Artifacts;
using Xunit;

namespace ExperimentalControlPlatform.Core.Tests.Artifacts;

public sealed class ArtifactIdTests
{
    [Fact]
    public void Constructor_Rejects_Blank_Value()
    {
        Assert.Throws<ArgumentException>(() => new ArtifactId(""));
        Assert.Throws<ArgumentException>(() => new ArtifactId(" "));
    }

    [Fact]
    public void Default_Value_Cannot_Be_Used_As_A_Valid_Identifier()
    {
        var artifactId = default(ArtifactId);

        var valueException = Assert.Throws<InvalidOperationException>(() => _ = artifactId.Value);
        Assert.Contains("default", valueException.Message, StringComparison.OrdinalIgnoreCase);

        var stringException = Assert.Throws<InvalidOperationException>(() => artifactId.ToString());
        Assert.Contains("default", stringException.Message, StringComparison.OrdinalIgnoreCase);
    }
}
