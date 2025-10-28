using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageEditorDemo.Models;

namespace ImageEditorDemo.Services;

/// <summary>
/// Service for image loading, saving, and pixel-level manipulation.
/// All filter operations work directly with byte arrays for educational purposes.
/// </summary>
public class ImageService : IImageService
{
    #region Load and Save Operations

    /// <summary>
    /// Loads an image from disk and converts it to a WriteableBitmap for manipulation.
    /// WriteableBitmap allows direct access to pixel data via BackBuffer.
    /// </summary>
    public async Task<WriteableBitmap?> LoadImageAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Load the image using BitmapImage
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Load completely into memory
                bitmap.EndInit();
                bitmap.Freeze(); // Make it thread-safe

                // Convert to WriteableBitmap for pixel manipulation
                // We use Pbgra32 format: 4 bytes per pixel (Blue, Green, Red, Alpha)
                var writeableBitmap = new WriteableBitmap(bitmap);
                writeableBitmap.Freeze();

                return writeableBitmap;
            }
            catch (Exception)
            {
                return null;
            }
        });
    }

    /// <summary>
    /// Saves a WriteableBitmap to disk as PNG or JPEG.
    /// </summary>
    public async Task<bool> SaveImageAsync(WriteableBitmap bitmap, string filePath, int quality = 95)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Determine encoder based on file extension
                BitmapEncoder encoder;
                var extension = Path.GetExtension(filePath).ToLower();

                encoder = extension switch
                {
                    ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = quality },
                    ".bmp" => new BmpBitmapEncoder(),
                    _ => new PngBitmapEncoder() // Default to PNG
                };

                // Save to file
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var stream = new FileStream(filePath, FileMode.Create);
                encoder.Save(stream);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        });
    }

    #endregion

    #region Filter Application

    /// <summary>
    /// Applies the specified filter to the source image.
    /// Each filter works directly with pixel byte arrays.
    /// </summary>
    public async Task<WriteableBitmap> ApplyFilterAsync(WriteableBitmap source, FilterType filterType, FilterParameters parameters)
    {
        // Extract all necessary data on the UI thread before Task.Run
        int width = source.PixelWidth;
        int height = source.PixelHeight;
        double dpiX = source.DpiX;
        double dpiY = source.DpiY;
        var format = source.Format;
        var palette = source.Palette;
        var sourcePixels = GetPixelBytes(source);

        return await Task.Run(() =>
        {
            // Process pixels on background thread
            byte[] processedPixels = filterType switch
            {
                FilterType.Grayscale => ApplyGrayscaleToPixels(sourcePixels),
                FilterType.Brightness => ApplyBrightnessToPixels(sourcePixels, parameters.Brightness),
                FilterType.Contrast => ApplyContrastToPixels(sourcePixels, parameters.Contrast),
                FilterType.BrightnessContrast => ApplyBrightnessContrastToPixels(sourcePixels, parameters.Brightness, parameters.Contrast),
                FilterType.GaussianBlur => ApplyGaussianBlurToPixels(sourcePixels, width, height, parameters.BlurRadius),
                FilterType.EdgeDetection => ApplyEdgeDetectionToPixels(sourcePixels, width, height),
                FilterType.Sepia => ApplySepiaToPixels(sourcePixels),
                _ => sourcePixels
            };

            // Create result bitmap on background thread using extracted metadata
            var result = new WriteableBitmap(width, height, dpiX, dpiY, format, palette);
            SetPixelBytes(result, processedPixels);
            result.Freeze(); // Make thread-safe for return to UI thread

            return result;
        });
    }

    #endregion

    #region Filter Implementations

    /// <summary>
    /// Converts image to grayscale using luminosity method.
    /// Formula: Gray = 0.299*R + 0.587*G + 0.114*B (weighted for human eye sensitivity)
    /// </summary>
    private byte[] ApplyGrayscaleToPixels(byte[] sourcePixels)
    {
        var result = new byte[sourcePixels.Length];
        Array.Copy(sourcePixels, result, sourcePixels.Length);

        // Process each pixel (4 bytes: B, G, R, A in Pbgra32 format)
        for (int i = 0; i < result.Length; i += 4)
        {
            byte blue = result[i];
            byte green = result[i + 1];
            byte red = result[i + 2];
            // Alpha channel at result[i + 3] remains unchanged

            // Calculate grayscale value using luminosity method
            byte gray = (byte)(0.114 * blue + 0.587 * green + 0.299 * red);

            // Set all RGB channels to the same gray value
            result[i] = gray;     // Blue
            result[i + 1] = gray; // Green
            result[i + 2] = gray; // Red
        }

        return result;
    }

    /// <summary>
    /// Adjusts image brightness by adding a constant value to each pixel.
    /// </summary>
    /// <param name="brightness">Brightness adjustment (-100 to +100)</param>
    private byte[] ApplyBrightnessToPixels(byte[] sourcePixels, double brightness)
    {
        var result = new byte[sourcePixels.Length];
        Array.Copy(sourcePixels, result, sourcePixels.Length);

        // Convert brightness from -100..100 to -255..255 range
        int adjustment = (int)(brightness * 2.55);

        for (int i = 0; i < result.Length; i += 4)
        {
            // Apply brightness to RGB channels, skip alpha
            result[i] = ClampByte(result[i] + adjustment);     // Blue
            result[i + 1] = ClampByte(result[i + 1] + adjustment); // Green
            result[i + 2] = ClampByte(result[i + 2] + adjustment); // Red
        }

        return result;
    }

    /// <summary>
    /// Adjusts image contrast using the formula: newValue = factor * (value - 128) + 128
    /// </summary>
    /// <param name="contrast">Contrast adjustment (-100 to +100)</param>
    private byte[] ApplyContrastToPixels(byte[] sourcePixels, double contrast)
    {
        var result = new byte[sourcePixels.Length];
        Array.Copy(sourcePixels, result, sourcePixels.Length);

        // Calculate contrast factor
        // Contrast value of 0 means no change (factor = 1)
        // Positive values increase contrast, negative values decrease it
        double factor = (100.0 + contrast) / 100.0;
        factor = Math.Max(0, factor); // Ensure non-negative

        for (int i = 0; i < result.Length; i += 4)
        {
            // Apply contrast formula to RGB channels
            result[i] = ClampByte((int)(factor * (result[i] - 128) + 128));     // Blue
            result[i + 1] = ClampByte((int)(factor * (result[i + 1] - 128) + 128)); // Green
            result[i + 2] = ClampByte((int)(factor * (result[i + 2] - 128) + 128)); // Red
        }

        return result;
    }

    /// <summary>
    /// Applies both brightness and contrast adjustments in a single pass for efficiency.
    /// </summary>
    private byte[] ApplyBrightnessContrastToPixels(byte[] sourcePixels, double brightness, double contrast)
    {
        var result = new byte[sourcePixels.Length];
        Array.Copy(sourcePixels, result, sourcePixels.Length);

        int brightnessAdj = (int)(brightness * 2.55);
        double contrastFactor = (100.0 + contrast) / 100.0;
        contrastFactor = Math.Max(0, contrastFactor);

        for (int i = 0; i < result.Length; i += 4)
        {
            // Apply contrast first, then brightness
            result[i] = ClampByte((int)(contrastFactor * (result[i] - 128) + 128 + brightnessAdj));
            result[i + 1] = ClampByte((int)(contrastFactor * (result[i + 1] - 128) + 128 + brightnessAdj));
            result[i + 2] = ClampByte((int)(contrastFactor * (result[i + 2] - 128) + 128 + brightnessAdj));
        }

        return result;
    }

    /// <summary>
    /// Applies Gaussian blur using a separable kernel for efficiency.
    /// This is a simplified approximation using box blur passes.
    /// </summary>
    private byte[] ApplyGaussianBlurToPixels(byte[] sourcePixels, int width, int height, int radius)
    {
        if (radius <= 0)
        {
            var result = new byte[sourcePixels.Length];
            Array.Copy(sourcePixels, result, sourcePixels.Length);
            return result;
        }

        var pixels = new byte[sourcePixels.Length];
        Array.Copy(sourcePixels, pixels, sourcePixels.Length);

        // Apply multiple passes of box blur to approximate Gaussian blur
        // More passes = smoother blur but slower
        int passes = 3;
        for (int pass = 0; pass < passes; pass++)
        {
            pixels = BoxBlur(pixels, width, height, radius);
        }

        return pixels;
    }

    /// <summary>
    /// Applies box blur (simple average) in horizontal and vertical passes.
    /// </summary>
    private byte[] BoxBlur(byte[] pixels, int width, int height, int radius)
    {
        var result = new byte[pixels.Length];
        int stride = width * 4; // 4 bytes per pixel

        // Horizontal pass
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int sumB = 0, sumG = 0, sumR = 0, count = 0;

                // Average pixels in horizontal window
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int nx = x + dx;
                    if (nx >= 0 && nx < width)
                    {
                        int idx = (y * stride) + (nx * 4);
                        sumB += pixels[idx];
                        sumG += pixels[idx + 1];
                        sumR += pixels[idx + 2];
                        count++;
                    }
                }

                int resultIdx = (y * stride) + (x * 4);
                result[resultIdx] = (byte)(sumB / count);
                result[resultIdx + 1] = (byte)(sumG / count);
                result[resultIdx + 2] = (byte)(sumR / count);
                result[resultIdx + 3] = pixels[resultIdx + 3]; // Copy alpha
            }
        }

        // Vertical pass
        pixels = result;
        result = new byte[pixels.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int sumB = 0, sumG = 0, sumR = 0, count = 0;

                // Average pixels in vertical window
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int ny = y + dy;
                    if (ny >= 0 && ny < height)
                    {
                        int idx = (ny * stride) + (x * 4);
                        sumB += pixels[idx];
                        sumG += pixels[idx + 1];
                        sumR += pixels[idx + 2];
                        count++;
                    }
                }

                int resultIdx = (y * stride) + (x * 4);
                result[resultIdx] = (byte)(sumB / count);
                result[resultIdx + 1] = (byte)(sumG / count);
                result[resultIdx + 2] = (byte)(sumR / count);
                result[resultIdx + 3] = pixels[resultIdx + 3]; // Copy alpha
            }
        }

        return result;
    }

    /// <summary>
    /// Applies edge detection using Sobel operator.
    /// Detects horizontal and vertical edges and combines them.
    /// </summary>
    private byte[] ApplyEdgeDetectionToPixels(byte[] sourcePixels, int width, int height)
    {
        // First convert to grayscale for simpler edge detection
        var grayscalePixels = ApplyGrayscaleToPixels(sourcePixels);
        var output = new byte[grayscalePixels.Length];

        int stride = width * 4;

        // Sobel kernels for edge detection
        // Gx detects horizontal edges, Gy detects vertical edges
        int[,] sobelX = { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
        int[,] sobelY = { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

        // Process each pixel (skip border pixels)
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int gx = 0, gy = 0;

                // Apply Sobel kernels to 3x3 neighborhood
                for (int ky = -1; ky <= 1; ky++)
                {
                    for (int kx = -1; kx <= 1; kx++)
                    {
                        int idx = ((y + ky) * stride) + ((x + kx) * 4);
                        byte pixelValue = grayscalePixels[idx]; // Using blue channel (they're all the same in grayscale)

                        gx += pixelValue * sobelX[ky + 1, kx + 1];
                        gy += pixelValue * sobelY[ky + 1, kx + 1];
                    }
                }

                // Calculate gradient magnitude
                int magnitude = (int)Math.Sqrt(gx * gx + gy * gy);
                byte edgeValue = ClampByte(magnitude);

                int outputIdx = (y * stride) + (x * 4);
                output[outputIdx] = edgeValue;     // Blue
                output[outputIdx + 1] = edgeValue; // Green
                output[outputIdx + 2] = edgeValue; // Red
                output[outputIdx + 3] = 255;       // Alpha
            }
        }

        return output;
    }

    /// <summary>
    /// Applies sepia tone effect (warm, vintage look).
    /// Uses a standard sepia transformation matrix.
    /// </summary>
    private byte[] ApplySepiaToPixels(byte[] sourcePixels)
    {
        var result = new byte[sourcePixels.Length];
        Array.Copy(sourcePixels, result, sourcePixels.Length);

        for (int i = 0; i < result.Length; i += 4)
        {
            byte blue = result[i];
            byte green = result[i + 1];
            byte red = result[i + 2];

            // Sepia transformation matrix
            // These coefficients create the characteristic warm brown tone
            int newRed = (int)(0.393 * red + 0.769 * green + 0.189 * blue);
            int newGreen = (int)(0.349 * red + 0.686 * green + 0.168 * blue);
            int newBlue = (int)(0.272 * red + 0.534 * green + 0.131 * blue);

            result[i] = ClampByte(newBlue);
            result[i + 1] = ClampByte(newGreen);
            result[i + 2] = ClampByte(newRed);
        }

        return result;
    }

    #endregion

    #region Histogram Calculation

    /// <summary>
    /// Calculates RGB histogram data showing the distribution of color values.
    /// Useful for analyzing image exposure and color balance.
    /// </summary>
    public async Task<ImageHistogram> CalculateHistogramAsync(WriteableBitmap bitmap)
    {
        // Extract pixel data on the calling thread (UI thread)
        var pixels = GetPixelBytes(bitmap);
        
        return await Task.Run(() =>
        {
            var histogram = new ImageHistogram();
            
            // Count occurrences of each intensity value (0-255) for each channel
            for (int i = 0; i < pixels.Length; i += 4)
            {
                histogram.BlueChannel[pixels[i]]++;
                histogram.GreenChannel[pixels[i + 1]]++;
                histogram.RedChannel[pixels[i + 2]]++;
            }

            // Find maximum value for normalization in UI
            histogram.MaxValue = Math.Max(
                histogram.RedChannel.Max(),
                Math.Max(histogram.GreenChannel.Max(), histogram.BlueChannel.Max()));
            
            return histogram;
        });
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a deep copy of a WriteableBitmap.
    /// Essential for undo/redo functionality.
    /// </summary>
    public WriteableBitmap CloneBitmap(WriteableBitmap source)
    {
        var clone = new WriteableBitmap(source.PixelWidth, source.PixelHeight,
            source.DpiX, source.DpiY, source.Format, source.Palette);

        // Copy pixel data
        var pixels = GetPixelBytes(source);
        SetPixelBytes(clone, pixels);

        return clone;
    }

    /// <summary>
    /// Extracts pixel data from WriteableBitmap into a byte array.
    /// Format: BGRA (4 bytes per pixel) for Pbgra32 format.
    /// </summary>
    private byte[] GetPixelBytes(WriteableBitmap bitmap)
    {
        int stride = bitmap.PixelWidth * 4; // 4 bytes per pixel (BGRA)
        int size = stride * bitmap.PixelHeight;
        byte[] pixels = new byte[size];

        bitmap.CopyPixels(pixels, stride, 0);
        return pixels;
    }

    /// <summary>
    /// Writes pixel data from byte array back to WriteableBitmap.
    /// </summary>
    private void SetPixelBytes(WriteableBitmap bitmap, byte[] pixels)
    {
        int stride = bitmap.PixelWidth * 4;
        bitmap.WritePixels(
            new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
            pixels, 
            stride, 
            0);
    }

    /// <summary>
    /// Clamps an integer value to valid byte range (0-255).
    /// Prevents overflow/underflow in pixel calculations.
    /// </summary>
    private byte ClampByte(int value)
    {
        if (value < 0) return 0;
        if (value > 255) return 255;
        return (byte)value;
    }

    #endregion
}
