# Image Editor Demo - WPF .NET 8

A simple WPF image editor demonstrating pixel-level image processing for educational purposes.

## Project Structure

```
ImageEditorDemo/
??? Models/
?   ??? FilterType.cs           # Enum for available filters
?   ??? FilterParameters.cs      # Parameters for filter adjustments
???? ImageHistogram.cs    # RGB histogram data structure
?
??? ViewModels/
?   ??? ViewModelBase.cs # Base class using CommunityToolkit.Mvvm
?
??? Services/
?   ??? IImageService.cs   # Interface for image operations
? ??? IDialogService.cs        # Interface for file dialogs
?   ??? DialogService.cs         # File dialog implementation
?
??? Views/
?   ??? (To be created)
?
??? App.xaml         # Application entry point
??? MainWindow.xaml   # Main window view
```

## Dependencies

- **CommunityToolkit.Mvvm 8.4.0** - MVVM framework with source generators
- **.NET 8.0-windows** - Target framework
- **WPF** - UI framework

## Features (Planned)

### Core Features
- ? MVVM Architecture
- ? Open/Save images (PNG, JPG, BMP)
- ? Display images with zoom/pan
- ? Undo/Redo functionality

### Filters (Manual Pixel Manipulation)
- ? Grayscale
- ? Brightness/Contrast (with sliders)
- ? Gaussian Blur
- ? Edge Detection (Sobel operator)
- ? Sepia tone

### Additional Features
- ? RGB Histogram viewer
- ? Async/await for responsive UI
- ? Progress indicators

## Implementation Steps

- [x] **Step 1**: Setup & Infrastructure
  - [x] Add CommunityToolkit.Mvvm package
  - [x] Create folder structure
  - [x] Define base models and interfaces
  
- [ ] **Step 2**: Core Service Layer (ImageService)
- [ ] **Step 3**: ViewModel Foundation (MainViewModel)
- [ ] **Step 4**: Basic UI (MainWindow redesign)
- [ ] **Step 5**: Filter Implementation Part 1
- [ ] **Step 6**: Filter Implementation Part 2
- [ ] **Step 7**: Undo/Redo System
- [ ] **Step 8**: Histogram View
- [ ] **Step 9**: Polish & Optimization

## Educational Purpose

This project demonstrates:
- Direct pixel manipulation using byte arrays
- MVVM pattern in WPF
- Asynchronous operations in UI applications
- Basic image processing algorithms
- Clean architecture principles
