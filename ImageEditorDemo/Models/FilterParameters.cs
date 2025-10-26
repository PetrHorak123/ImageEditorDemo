using CommunityToolkit.Mvvm.ComponentModel;

namespace ImageEditorDemo.Models;

/// <summary>
/// Parameters for various image filters
/// Observable to support live preview when slider values change.
/// </summary>
public partial class FilterParameters : ObservableObject
{
    /// <summary>
    /// Brightness adjustment (-100 to +100)
    /// </summary>
    [ObservableProperty]
    private double _brightness = 0;

    /// <summary>
    /// Contrast adjustment (-100 to +100)
    /// </summary>
    [ObservableProperty]
    private double _contrast = 0;

    /// <summary>
    /// Blur radius for Gaussian blur (1-10)
    /// </summary>
    [ObservableProperty]
    private int _blurRadius = 3;

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
