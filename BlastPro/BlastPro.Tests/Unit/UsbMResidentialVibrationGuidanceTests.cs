using BlastPro.Api.Services;

namespace BlastPro.Tests.Unit;

public sealed class UsbMResidentialVibrationGuidanceTests
{
    [Theory]
    [InlineData("ResidentialPlaster", 1, 4.79)]
    [InlineData("ResidentialPlaster", 10, 12.77)]
    [InlineData("ResidentialDrywall", 10, 19.05)]
    [InlineData("ResidentialDrywall", 40, 50.80)]
    public void Residential_curve_uses_frequency_and_wall_type(string structure, int frequency, double expected)
    {
        var actual = UsbMResidentialVibrationGuidance.SuggestedLimitMmPerSecond(structure, frequency);
        Assert.NotNull(actual);
        Assert.InRange((double)actual.Value, expected - 0.03, expected + 0.03);
    }

    [Theory]
    [InlineData("Other", 10)]
    [InlineData("ResidentialPlaster", 0)]
    [InlineData("ResidentialDrywall", 101)]
    public void Unsupported_inputs_require_manual_limit(string structure, int frequency)
    {
        Assert.Null(UsbMResidentialVibrationGuidance.SuggestedLimitMmPerSecond(structure, frequency));
    }
}
