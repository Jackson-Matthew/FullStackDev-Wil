namespace BlastPro.Api.Services;

/// <summary>
/// USBM RI 8507 Appendix B residential screening curve. This is US research
/// guidance for houses, not a South African statutory or site-approved limit.
/// The 0.03 in and 0.008 in displacement branches are converted from
/// displacement to sinusoidal peak velocity using 2*pi*f*d.
/// Source: https://stacks.cdc.gov/view/cdc/201726/cdc_201726_DS1.pdf
/// </summary>
public static class UsbMResidentialVibrationGuidance
{
    public static decimal? SuggestedLimitMmPerSecond(string? structureType, decimal? frequencyHz)
    {
        if (structureType is not ("ResidentialPlaster" or "ResidentialDrywall") ||
            frequencyHz is null or <= 0 or > 100)
            return null;

        var plateau = structureType == "ResidentialPlaster" ? 0.50 : 0.75;
        var frequency = (double)frequencyHz.Value;
        var velocityInchesPerSecond = Math.Min(2.0,
            Math.Min(2 * Math.PI * frequency * 0.03,
                Math.Max(plateau, 2 * Math.PI * frequency * 0.008)));
        return Math.Round((decimal)(velocityInchesPerSecond * 25.4), 2);
    }
}
