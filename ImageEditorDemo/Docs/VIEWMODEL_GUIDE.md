# MainViewModel Documentation

## Overview

`MainViewModel` is the core ViewModel for the Image Editor application, implementing the MVVM pattern using CommunityToolkit.Mvvm.

## Architecture

### CommunityToolkit.Mvvm Features Used

#### 1. **[ObservableProperty]** Attribute
Automatically generates property change notifications:

```csharp
[ObservableProperty]
private WriteableBitmap? _currentImage;

// Generates:
// - Property: CurrentImage
// - INotifyPropertyChanged implementation
// - OnCurrentImageChanged() partial method hook
```

#### 2. **[RelayCommand]** Attribute
Automatically generates ICommand implementations:

```csharp
[RelayCommand(CanExecute = nameof(CanSaveImage))]
private async Task SaveImageAsync()

// Generates:
// - Property: SaveImageAsyncCommand (IAsyncRelayCommand)
// - Automatic CanExecute binding
// - Thread-safe execution
```

## Key Components

### Observable Properties

| Property | Type | Purpose |
|----------|------|---------|
| `CurrentImage` | WriteableBitmap? | Currently displayed/edited image |
| `OriginalImage` | WriteableBitmap? | Unmodified original for reset |
| `CurrentHistogram` | ImageHistogram? | RGB histogram data |
| `FilterParameters` | FilterParameters | Slider values (brightness, contrast, blur) |
| `SelectedFilter` | FilterType | Active filter selection |
| `IsProcessing` | bool | Shows loading/busy state |
| `CurrentFilePath` | string? | Path of loaded file |
| `HasUnsavedChanges` | bool | Tracks dirty state |
| `StatusMessage` | string | Status bar text |

### Commands

#### File Operations
- **OpenImageAsync**: Opens file dialog and loads image
- **SaveImageAsync**: Saves current image with file dialog
- **ResetImageAsync**: Reverts to original image

#### Filter Operations
- **ApplyFilterAsync**: Applies selected filter with parameters
- **ApplyCurrentFilterAsync** (private): Auto-applies for live preview

#### Edit Operations
- **UndoAsync**: Reverts to previous state
- **RedoAsync**: Restores undone state

## Undo/Redo System

### Implementation Details

```csharp
Stack<WriteableBitmap> _undoStack;  // Previous states
Stack<WriteableBitmap> _redoStack;  // Undone states
const int MaxHistorySize = 20;      // Memory limit
```

### How It Works

1. **Before any operation**: Current state pushed to undo stack
2. **Undo**: Pop from undo ? push to redo
3. **Redo**: Pop from redo ? push to undo
4. **New action**: Clear redo stack (can't redo after new changes)

### Memory Management

- Limits to 20 states to prevent excessive memory usage
- Each state stores full bitmap clone (~4 bytes per pixel)
- Example: 1920×1080 image = ~8MB per state = ~160MB max

## Live Preview Feature

### How It Works

```csharp
// In constructor:
FilterParameters.PropertyChanged += (s, e) =>
{
    if (!IsProcessing && CurrentImage != null)
  {
        _ = ApplyCurrentFilterAsync();
    }
};
```

**Behavior:**
- Only activates for `BrightnessContrast` and `GaussianBlur`
- Automatically applies filter as sliders move
- Disabled during processing to prevent conflicts

## State Management

### Loading an Image

```
1. User clicks Open
2. ShowOpenFileDialog()
3. LoadImageAsync(path)
4. Store OriginalImage (unchanged reference)
5. Clone to CurrentImage (working copy)
6. Clear undo/redo stacks
7. Calculate initial histogram
8. Reset filter parameters
9. Update status bar
```

### Applying a Filter

```
1. User selects filter + adjusts sliders
2. Clicks Apply (or sliders auto-trigger)
3. Push CurrentImage to undo stack
4. Call ImageService.ApplyFilterAsync()
5. Replace CurrentImage with result
6. Mark HasUnsavedChanges = true
7. Update histogram
8. Clear redo stack
9. Update status bar
```

### Undo Operation

```
1. Pop from _undoStack ? oldState
2. Push CurrentImage to _redoStack
3. Set CurrentImage = oldState
4. Update histogram
5. Update CanUndo/CanRedo
```

## Command CanExecute Logic

### SaveImageAsync
```csharp
CanSaveImage() => CurrentImage != null && !IsProcessing
```
Enabled when: Image loaded AND not busy

### ApplyFilterAsync
```csharp
CanApplyFilter() => CurrentImage != null 
               && SelectedFilter != None 
        && !IsProcessing
```
Enabled when: Image loaded AND filter selected AND not busy

### UndoAsync / RedoAsync
```csharp
CanUndo => _undoStack.Count > 0
CanRedo => _redoStack.Count > 0
```
Enabled when: States available in respective stacks

## Dependency Injection

### Services Required

```csharp
public MainViewModel(
    IImageService imageService,
    IDialogService dialogService)
```

**Benefits:**
- Testable (can mock services)
- Decoupled from implementation
- Design-time support with parameterless constructor

## Error Handling

### Pattern Used
```csharp
try
{
    IsProcessing = true;
    StatusMessage = "Loading...";
    
    // Operation here
    
    StatusMessage = "Success!";
}
catch (Exception ex)
{
    _dialogService.ShowError($"Error: {ex.Message}");
    StatusMessage = "Error occurred";
}
finally
{
    IsProcessing = false;
}
```

**Always:**
- Set IsProcessing = true at start
- Set IsProcessing = false in finally
- Update StatusMessage for user feedback
- Show dialog for critical errors

## Usage in XAML

### Data Binding Examples

```xml
<!-- Image Display -->
<Image Source="{Binding CurrentImage}" />

<!-- Filter Dropdown -->
<ComboBox ItemsSource="{Binding AvailableFilters}"
 SelectedItem="{Binding SelectedFilter}" />

<!-- Brightness Slider -->
<Slider Value="{Binding FilterParameters.Brightness}"
        Minimum="-100" Maximum="100" />

<!-- Command Buttons -->
<Button Content="Open" Command="{Binding OpenImageAsyncCommand}" />
<Button Content="Save" Command="{Binding SaveImageAsyncCommand}" />
<Button Content="Undo" Command="{Binding UndoAsyncCommand}" />

<!-- Loading Indicator -->
<ProgressBar IsIndeterminate="True"
             Visibility="{Binding IsProcessing, 
      Converter={StaticResource BoolToVisibility}}" />

<!-- Status Bar -->
<TextBlock Text="{Binding StatusMessage}" />
```

## Threading Considerations

### Async Operations
- All heavy operations use `Task.Run()` in ImageService
- UI thread only updates properties
- WriteableBitmap must be frozen for cross-thread access

### Thread Safety
```csharp
// In ImageService:
bitmap.Freeze(); // Make thread-safe after loading
```

## Performance Tips

1. **Histogram Updates**: Only calculated after filter application
2. **Live Preview**: Limited to parameterized filters only
3. **Cloning**: Deep copy only when necessary (undo states)
4. **Stack Size**: Limited to 20 states for memory efficiency

## Testing Considerations

### Unit Testing Example
```csharp
[Fact]
public async Task OpenImage_ValidPath_LoadsImage()
{
 // Arrange
    var mockImageService = new Mock<IImageService>();
    var mockDialogService = new Mock<IDialogService>();
  mockDialogService.Setup(x => x.ShowOpenFileDialog())
          .Returns("test.png");
    mockImageService.Setup(x => x.LoadImageAsync(It.IsAny<string>()))
 .ReturnsAsync(CreateTestBitmap());
    
    var vm = new MainViewModel(mockImageService.Object, 
    mockDialogService.Object);
    
    // Act
  await vm.OpenImageAsyncCommand.ExecuteAsync(null);
    
    // Assert
    Assert.NotNull(vm.CurrentImage);
    Assert.NotNull(vm.OriginalImage);
    Assert.False(vm.HasUnsavedChanges);
}
```

---

**Next Step**: Wire this ViewModel to the MainWindow UI in Step 4!
