# Image Processing Concepts - Educational Guide

This document explains the pixel-level image processing techniques used in this project.

## 🎨 Pixel Format: BGRA (Pbgra32)

WPF's `WriteableBitmap` uses the **Pbgra32** format by default:
- Each pixel = 4 bytes (32 bits)
- Order: **Blue, Green, Red, Alpha**
- Values: 0-255 for each channel

```
Pixel Array: [B, G, R, A, B, G, R, A, B, G, R, A, ...]
             └─ Pixel 0 ┘└─ Pixel 1 ┘└─ Pixel 2 ┘
```

## 🔍 Filter Algorithms Explained

### 1. Grayscale (Luminosity Method)

Converts color to grayscale using weighted averages based on human eye sensitivity:

```
Gray = 0.299 × Red + 0.587 × Green + 0.114 × Blue
```

**Why these weights?**
- Human eyes are most sensitive to green light
- Least sensitive to blue light
- This creates more natural-looking grayscale images

### 2. Brightness

Simply adds a constant value to each RGB channel:

```
New_R = Clamp(Old_R + brightness)
New_G = Clamp(Old_G + brightness)
New_B = Clamp(Old_B + brightness)
```

- Positive values = brighter
- Negative values = darker
- Must clamp to 0-255 range

### 3. Contrast

Uses midpoint (128) as reference:

```
Factor = (100 + contrast) / 100
New_Value = Factor × (Old_Value - 128) + 128
```

**How it works:**
- Pixels lighter than 128 become lighter
- Pixels darker than 128 become darker
- Increases difference between light and dark areas

### 4. Gaussian Blur (Box Blur Approximation)

This implementation uses **multiple box blur passes** to approximate Gaussian blur:

**Box Blur:**
1. For each pixel, calculate average of surrounding pixels
2. Use a square window (radius parameter)
3. Apply horizontally, then vertically (separable)

**Why multiple passes?**
- Single box blur = rough, blocky results
- 3 passes ≈ smooth Gaussian-like blur
- Much simpler to implement than true Gaussian

### 5. Edge Detection (Sobel Operator)

Uses two 3×3 kernels to detect edges:

**Horizontal edges (Sobel X):**
```
[-1  0  +1]
[-2  0  +2]
[-1  0  +1]
```

**Vertical edges (Sobel Y):**
```
[-1  -2  -1]
[ 0   0   0]
[+1  +2  +1]
```

**Algorithm:**
1. Convert image to grayscale
2. Apply both kernels to each pixel
3. Calculate magnitude: `√(Gx² + Gy²)`
4. Result shows edges as bright lines

### 6. Sepia Tone

Applies a transformation matrix to create warm, vintage look:

```
New_R = 0.393×R + 0.769×G + 0.189×B
New_G = 0.349×R + 0.686×G + 0.168×B
New_B = 0.272×R + 0.534×G + 0.131×B
```

These coefficients create characteristic brown/amber tones.

## 📊 Histogram

A histogram shows the distribution of pixel intensities:

- **X-axis**: Intensity value (0-255)
- **Y-axis**: Number of pixels with that value
- Separate histograms for Red, Green, and Blue channels

**What it tells you:**
- **Left-heavy**: Dark image (underexposed)
- **Right-heavy**: Bright image (overexposed)
- **Centered**: Well-balanced exposure
- **Peaks at both ends**: High contrast

## 💾 Memory and Performance

### Cloning Images
For undo/redo **deep copies** of pixel data are created:
```csharp
byte[] original = GetPixelBytes(bitmap);
byte[] copy = new byte[original.Length];
Array.Copy(original, copy, original.Length);
```

### Async Operations
Long operations run on background threads:
```csharp
await Task.Run(() => {
    // Process pixels here
});
```

This keeps the UI responsive during processing.

---

*This implementation prioritizes clarity and education over performance.*
*Production systems would use optimized libraries (OpenCV, ImageSharp, etc.)*
