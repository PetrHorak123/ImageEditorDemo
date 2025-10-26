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
        return await Task.Run(() =>
          {
              return filterType switch
              {
                  FilterType.Grayscale => ApplyGrayscale(source),
                  FilterType.Brightness => ApplyBrightness(source, parameters.Brightness),
                  FilterType.Contrast => ApplyContrast(source, parameters.Contrast),
                  FilterType.BrightnessContrast => ApplyBrightnessContrast(source, parameters.Brightness, parameters.Contrast),
                  FilterType.GaussianBlur => ApplyGaussianBlur(source, parameters.BlurRadius),
                  FilterType.EdgeDetection => ApplyEdgeDetection(source),
                  FilterType.Sepia => ApplySepia(source),
                  _ => CloneBitmap(source)
              };
          });
    }

    #endregion

    #region Filter Implementations

    /// <summary>
    /// Converts image to grayscale using luminosity method.
    /// Formula: Gray = 0.299*R + 0.587*G + 0.114*B (weighted for human eye sensitivity)
    /// </summary>
    private WriteableBitmap ApplyGrayscale(WriteableBitmap source)
    {
        var result = CloneBitmap(source);
        var pixels = GetPixelBytes(result);

        // Process each pixel (4 bytes: B, G, R, A in Pbgra32 format)
        for (int i = 0; i < pixels.Length; i += 4)
        {
            byte blue = pixels[i];
            byte green = pixels[i + 1];
            byte red = pixels[i + 2];
            // Alpha channel at pixels[i + 3] remains unchanged

            // Calculate grayscale value using luminosity method
            byte gray = (byte)(0.114 * blue + 0.587 * green + 0.299 * red);

            // Set all RGB channels to the same gray value
            pixels[i] = gray;     // Blue
            pixels[i + 1] = gray; // Green
            pixels[i + 2] = gray; // Red
        }

        SetPixelBytes(result, pixels);
        return result;
    }

    /// <summary>
    /// Adjusts image brightness by adding a constant value to each pixel.
    /// </summary>
    /// <param name="brightness">Brightness adjustment (-100 to +100)</param>
    private WriteableBitmap ApplyBrightness(WriteableBitmap source, double brightness)
    {
        var result = CloneBitmap(source);
        var pixels = GetPixelBytes(result);

        // Convert brightness from -100..100 to -255..255 range
        int adjustment = (int)(brightness * 2.55);

        for (int i = 0; i < pixels.Length; i += 4)
        {
            // Apply brightness to RGB channels, skip alpha
            pixels[i] = ClampByte(pixels[i] + adjustment);     // Blue
            pixels[i + 1] = ClampByte(pixels[i + 1] + adjustment); // Green
            pixels[i + 2] = ClampByte(pixels[i + 2] + adjustment); // Red
        }

        SetPixelBytes(result, pixels);
        return result;
    }

    /// <summary>
    /// Adjusts image contrast using the formula: newValue = factor * (value - 128) + 128
    /// </summary>
    /// <param name="contrast">Contrast adjustment (-100 to +100)</param>
    private WriteableBitmap ApplyContrast(WriteableBitmap source, double contrast)
    {
        var result = CloneBitmap(source);
        var pixels = GetPixelBytes(result);

        // Calculate contrast factor
        // Contrast value of 0 means no change (factor = 1)
        // Positive values increase contrast, negative values decrease it
        double factor = (100.0 + contrast) / 100.0;
        factor = Math.Max(0, factor); // Ensure non-negative

        for (int i = 0; i < pixels.Length; i += 4)
        {
            // Apply contrast formula to RGB channels
            pixels[i] = ClampByte((int)(factor * (pixels[i] - 128) + 128));     // Blue
            pixels[i + 1] = ClampByte((int)(factor * (pixels[i + 1] - 128) + 128)); // Green
            pixels[i + 2] = ClampByte((int)(factor * (pixels[i + 2] - 128) + 128)); // Red
        }

        SetPixelBytes(result, pixels);
        return result;
    }

    /// <summary>
    /// Applies both brightness and contrast adjustments in a single pass for efficiency.
    /// </summary>
    private WriteableBitmap ApplyBrightnessContrast(WriteableBitmap source, double brightness, double contrast)
    {
        var result = CloneBitmap(source);
        var pixels = GetPixelBytes(result);

        int brightnessAdj = (int)(brightness * 2.55);
        double contrastFactor = (100.0 + contrast) / 100.0;
        contrastFactor = Math.Max(0, contrastFactor);

        for (int i = 0; i < pixels.Length; i += 4)
        {
            // Apply contrast first, then brightness
            pixels[i] = ClampByte((int)(contrastFactor * (pixels[i] - 128) + 128 + brightnessAdj));
            pixels[i + 1] = ClampByte((int)(contrastFactor * (pixels[i + 1] - 128) + 128 + brightnessAdj));
            pixels[i + 2] = ClampByte((int)(contrastFactor * (pixels[i + 2] - 128) + 128 + brightnessAdj));
        }

        SetPixelBytes(result, pixels);
        return result;
    }

    /// <summary>
    /// Applies Gaussian blur using a separable kernel for efficiency.
    /// This is a simplified approximation using box blur passes.
    /// </summary>
    private WriteableBitmap ApplyGaussianBlur(WriteableBitmap source, int radius)
    {
        if (radius <= 0) return CloneBitmap(source);

        var result = CloneBitmap(source);
        int width = result.PixelWidth;
        int height = result.PixelHeight;
        var pixels = GetPixelBytes(result);

        // Apply multiple passes of box blur to approximate Gaussian blur
        // More passes = smoother blur but slower
        int passes = 3;
        for (int pass = 0; pass < passes; pass++)
        {
            pixels = BoxBlur(pixels, width, height, radius);
        }

        SetPixelBytes(result, pixels);
        return result;
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
    private WriteableBitmap ApplyEdgeDetection(WriteableBitmap source)
    {
        // First convert to grayscale for simpler edge detection
        var grayscale = ApplyGrayscale(source);
        var result = CloneBitmap(grayscale);

        int width = result.PixelWidth;
        int height = result.PixelHeight;
        var pixels = GetPixelBytes(grayscale);
        var output = new byte[pixels.Length];

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
                        byte pixelValue = pixels[idx]; // Using blue channel (they're all the same in grayscale)

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

        SetPixelBytes(result, output);
        return result;
    }

    /// <summary>
    /// Applies sepia tone effect (warm, vintage look).
    /// Uses a standard sepia transformation matrix.
    /// </summary>
    private WriteableBitmap ApplySepia(WriteableBitmap source)
    {
        var result = CloneBitmap(source);
        var pixels = GetPixelBytes(result);

        for (int i = 0; i < pixels.Length; i += 4)
        {
            byte blue = pixels[i];
            byte green = pixels[i + 1];
            byte red = pixels[i + 2];

            // Sepia transformation matrix
            // These coefficients create the characteristic warm brown tone
            int newRed = (int)(0.393 * red + 0.769 * green + 0.189 * blue);
            int newGreen = (int)(0.349 * red + 0.686 * green + 0.168 * blue);
            int newBlue = (int)(0.272 * red + 0.534 * green + 0.131 * blue);

            pixels[i] = ClampByte(newBlue);
            pixels[i + 1] = ClampByte(newGreen);
            pixels[i + 2] = ClampByte(newRed);
        }

        SetPixelBytes(result, pixels);
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
        return await Task.Run(() =>
        {
            var histogram = new ImageHistogram();
            var pixels = GetPixelBytes(bitmap);

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
