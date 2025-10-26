namespace ImageEditorDemo.Models;

/// <summary>
/// Parameters for various image filters
/// </summary>
public class FilterParameters
{
    /// <summary>
    /// Brightness adjustment (-100 to +100)
    /// </summary>
    public double Brightness { get; set; } = 0;

    /// <summary>
    /// Contrast adjustment (-100 to +100)
    /// </summary>
    public double Contrast { get; set; } = 0;

    /// <summary>
    /// Blur radius for Gaussian blur (1-10)
    /// </summary>
    public int BlurRadius { get; set; } = 3;

    /// <summary>
    /// Creates a copy of the parameters
    /// </summary>
    public FilterParameters Clone()
    {
        return new FilterParameters
        {
            Brightness = this.Brightness,
            Contrast = this.Contrast,
            BlurRadius = this.BlurRadius
        };
    }
}
