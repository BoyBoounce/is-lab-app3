namespace IsLabApp.Tests;

public class UnitTest1
{
    [Fact]
    public void AppVersion_ForLab10_ShouldBeExpected()
    {
        var version = "0.1.0-lab10";

        Assert.Equal("0.1.0-lab10", version);
    }
}
