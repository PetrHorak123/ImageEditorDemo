namespace ImageEditorDemo.Models;

/// <summary>
/// Represents RGB histogram data for an image
/// </summary>
public class ImageHistogram
{
    /// <summary>
    /// Red channel histogram (0-255 intensity values)
    /// </summary>
    public int[] RedChannel { get; set; } = new int[256];

    /// <summary>
    /// Green channel histogram (0-255 intensity values)
    /// </summary>
    public int[] GreenChannel { get; set; } = new int[256];

    /// <summary>
    /// Blue channel histogram (0-255 intensity values)
    /// </summary>
    public int[] BlueChannel { get; set; } = new int[256];

    /// <summary>
    /// Maximum value across all channels (for normalization in UI)
    /// </summary>
    public int MaxValue { get; set; }
}
