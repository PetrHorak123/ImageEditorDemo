using System.IO;
using ImageEditorDemo.Models;
using ImageEditorDemo.Services;

namespace ImageEditorDemo.Examples;

/// <summary>
/// Example usage of ImageService for educational purposes.
/// This class demonstrates how to use the service programmatically.
/// </summary>
public class ImageServiceExample
{
    private readonly IImageService _imageService;

    public ImageServiceExample()
    {
        _imageService = new ImageService();
    }

    /// <summary>
    /// Example: Load an image, apply filters, and save it.
    /// </summary>
    public async Task ProcessImageExample()
    {
        // 1. Load an image
        var image = await _imageService.LoadImageAsync("C:\\path\\to\\image.jpg");
        if (image == null)
        {
            Console.WriteLine("Failed to load image");
            return;
        }

        // 2. Apply grayscale filter
        var grayscaleImage = await _imageService.ApplyFilterAsync(
            image,
            FilterType.Grayscale,
            new FilterParameters());
        

        // 3. Apply brightness and contrast
        var adjustedImage = await _imageService.ApplyFilterAsync(
            grayscaleImage,
            FilterType.BrightnessContrast,
            new FilterParameters
            {
                Brightness = 20,  // Increase brightness by 20
                Contrast = 15     // Increase contrast by 15
            });
        

        // 4. Apply blur
        var blurredImage = await _imageService.ApplyFilterAsync(
            adjustedImage,
            FilterType.GaussianBlur,
            new FilterParameters
            {
                BlurRadius = 5
            });
        
        // 5. Calculate histogram
        var histogram = await _imageService.CalculateHistogramAsync(blurredImage);
        Console.WriteLine($"Histogram max value: {histogram.MaxValue}");

        // 6. Save the result
        await _imageService.SaveImageAsync(blurredImage, "C:\\path\\to\\output.png");
    }

    /// <summary>
    /// Example: Apply all filters to see the effects.
    /// </summary>
    public async Task ApplyAllFiltersExample(string inputPath, string outputFolder)
    {
        var original = await _imageService.LoadImageAsync(inputPath);
        if (original == null) return;

        var parameters = new FilterParameters
        {
            Brightness = 10,
            Contrast = 20,
            BlurRadius = 3
        };

        // Apply each filter and save
        var filters = new[]
        {
            (FilterType.Grayscale, "grayscale.png"),
            (FilterType.Sepia, "sepia.png"),
            (FilterType.EdgeDetection, "edges.png"),
            (FilterType.BrightnessContrast, "adjusted.png"),
            (FilterType.GaussianBlur, "blurred.png")
        };

        foreach (var (filterType, filename) in filters)
        {
            var filtered = await _imageService.ApplyFilterAsync(original, filterType, parameters);
            await _imageService.SaveImageAsync(filtered, Path.Combine(outputFolder, filename));
        }
    }
}
