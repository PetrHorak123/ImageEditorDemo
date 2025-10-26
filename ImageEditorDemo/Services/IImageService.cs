using System.Windows.Media.Imaging;
using ImageEditorDemo.Models;

namespace ImageEditorDemo.Services;

/// <summary>
/// Interface for image processing operations
/// Handles loading, saving, and pixel-level manipulations
/// </summary>
public interface IImageService
{
    /// <summary>
    /// Loads an image from file path and returns a WriteableBitmap
    /// </summary>
    /// <param name="filePath">Full path to the image file</param>
    /// <returns>WriteableBitmap ready for manipulation</returns>
    Task<WriteableBitmap?> LoadImageAsync(string filePath);

    /// <summary>
    /// Saves a WriteableBitmap to a file
    /// </summary>
    /// <param name="bitmap">Image to save</param>
    /// <param name="filePath">Destination file path</param>
    /// <param name="quality">JPEG quality (1-100), ignored for PNG</param>
    Task<bool> SaveImageAsync(WriteableBitmap bitmap, string filePath, int quality = 95);

    /// <summary>
    /// Applies a filter to the image using manual pixel manipulation
    /// </summary>
    /// <param name="source">Source image</param>
    /// <param name="filterType">Type of filter to apply</param>
    /// <param name="parameters">Filter parameters (brightness, contrast, etc.)</param>
    /// <returns>New WriteableBitmap with filter applied</returns>
    Task<WriteableBitmap> ApplyFilterAsync(WriteableBitmap source, FilterType filterType, FilterParameters parameters);

    /// <summary>
    /// Calculates histogram data for RGB channels
    /// </summary>
    /// <param name="bitmap">Source image</param>
    /// <returns>Histogram data</returns>
    Task<ImageHistogram> CalculateHistogramAsync(WriteableBitmap bitmap);

    /// <summary>
    /// Creates a deep copy of a WriteableBitmap
    /// </summary>
    /// <param name="source">Source bitmap</param>
    /// <returns>Cloned bitmap</returns>
    WriteableBitmap CloneBitmap(WriteableBitmap source);
}
