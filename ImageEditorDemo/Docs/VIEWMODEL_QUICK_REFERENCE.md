# MainViewModel Quick Reference

## Constructor

```csharp
public MainViewModel(
    IImageService imageService,
    IDialogService dialogService)
```

## Public Properties

### Image State
- `WriteableBitmap? CurrentImage` - Currently displayed image
- `WriteableBitmap? OriginalImage` - Unmodified original
- `ImageHistogram? CurrentHistogram` - RGB histogram data

### Filter State
- `FilterParameters FilterParameters` - Brightness, Contrast, BlurRadius
- `FilterType SelectedFilter` - Currently selected filter
- `ObservableCollection<FilterType> AvailableFilters` - Filter dropdown source

### UI State
- `bool IsProcessing` - Shows loading/busy indicator
- `string? CurrentFilePath` - Path of loaded file
- `bool HasUnsavedChanges` - Dirty flag
- `string StatusMessage` - Status bar text

### History State
- `bool CanUndo` - True if undo stack has items
- `bool CanRedo` - True if redo stack has items

## Public Commands

### File Operations
```csharp
IAsyncRelayCommand OpenImageAsyncCommand    // Opens file dialog
IAsyncRelayCommand SaveImageAsyncCommand  // Saves with dialog
IAsyncRelayCommand ResetImageAsyncCommand   // Reverts to original
```

### Filter Operations
```csharp
IAsyncRelayCommand ApplyFilterAsyncCommand  // Applies selected filter
```

### Edit Operations
```csharp
IAsyncRelayCommand UndoAsyncCommand         // Undo last change
IAsyncRelayCommand RedoAsyncCommand  // Redo last undo
```

## XAML Binding Examples

### Basic Bindings
```xml
<!-- Image Display -->
<Image Source="{Binding CurrentImage}" 
       Stretch="Uniform" />

<!-- Status Bar -->
<StatusBar>
    <TextBlock Text="{Binding StatusMessage}" />
</StatusBar>

<!-- Loading Indicator -->
<ProgressBar IsIndeterminate="True"
  Visibility="{Binding IsProcessing, 
      Converter={StaticResource BoolToVis}}" />
```

### Commands
```xml
<!-- Toolbar Buttons -->
<Button Content="Open" 
        Command="{Binding OpenImageAsyncCommand}" />
        
<Button Content="Save" 
   Command="{Binding SaveImageAsyncCommand}" />
     
<Button Content="Undo" 
        Command="{Binding UndoAsyncCommand}"
      ToolTip="Undo (Ctrl+Z)" />
        
<Button Content="Redo" 
   Command="{Binding RedoAsyncCommand}"
    ToolTip="Redo (Ctrl+Y)" />
        
<Button Content="Reset" 
 Command="{Binding ResetImageAsyncCommand}"
    ToolTip="Reset to Original" />
```

### Filter Controls
```xml
<!-- Filter Selection -->
<ComboBox ItemsSource="{Binding AvailableFilters}"
      SelectedItem="{Binding SelectedFilter}"
   Width="150" />

<!-- Apply Button -->
<Button Content="Apply Filter" 
    Command="{Binding ApplyFilterAsyncCommand}" />

<!-- Brightness Slider -->
<StackPanel Orientation="Horizontal">
    <TextBlock Text="Brightness:" Width="80" />
    <Slider Value="{Binding FilterParameters.Brightness}"
     Minimum="-100" Maximum="100"
       Width="200"
            TickFrequency="10"
        TickPlacement="BottomRight" />
    <TextBlock Text="{Binding FilterParameters.Brightness, 
  StringFormat='{}{0:F0}'}"
         Width="40" />
</StackPanel>

<!-- Contrast Slider -->
<StackPanel Orientation="Horizontal">
    <TextBlock Text="Contrast:" Width="80" />
    <Slider Value="{Binding FilterParameters.Contrast}"
    Minimum="-100" Maximum="100"
      Width="200" />
    <TextBlock Text="{Binding FilterParameters.Contrast, 
StringFormat='{}{0:F0}'}" />
</StackPanel>

<!-- Blur Radius Slider -->
<StackPanel Orientation="Horizontal">
    <TextBlock Text="Blur Radius:" Width="80" />
  <Slider Value="{Binding FilterParameters.BlurRadius}"
    Minimum="1" Maximum="10"
            Width="200"
    IsSnapToTickEnabled="True"
     TickFrequency="1" />
    <TextBlock Text="{Binding FilterParameters.BlurRadius}" />
</StackPanel>
```

### Unsaved Changes Indicator
```xml
<TextBlock>
    <Run Text="*" 
         Visibility="{Binding HasUnsavedChanges, 
    Converter={StaticResource BoolToVis}}"
         Foreground="Red" 
         FontWeight="Bold" />
    <Run Text="{Binding CurrentFilePath}" />
</TextBlock>
```

## Code-Behind Usage

### Setting DataContext
```csharp
// In MainWindow.xaml.cs
public partial class MainWindow : Window
{
    public MainWindow()
{
InitializeComponent();
   
        // Create services
 var imageService = new ImageService();
 var dialogService = new DialogService();
        
 // Create and set ViewModel
DataContext = new MainViewModel(imageService, dialogService);
}
}
```

### Accessing ViewModel from Code-Behind
```csharp
private MainViewModel ViewModel => (MainViewModel)DataContext;

private async void Window_Loaded(object sender, RoutedEventArgs e)
{
    // Can access ViewModel properties/methods
    if (ViewModel.CurrentImage == null)
    {
     // Prompt to open image
 }
}
```

## Common Patterns

### Triggering Commands from Keyboard
```xml
<Window.InputBindings>
    <KeyBinding Key="O" Modifiers="Ctrl" 
                Command="{Binding OpenImageAsyncCommand}" />
    <KeyBinding Key="S" Modifiers="Ctrl" 
   Command="{Binding SaveImageAsyncCommand}" />
 <KeyBinding Key="Z" Modifiers="Ctrl" 
  Command="{Binding UndoAsyncCommand}" />
 <KeyBinding Key="Y" Modifiers="Ctrl" 
     Command="{Binding RedoAsyncCommand}" />
</Window.InputBindings>
```

### Context Menu
```xml
<Image.ContextMenu>
    <ContextMenu>
        <MenuItem Header="Reset to Original" 
                  Command="{Binding ResetImageAsyncCommand}" />
   <Separator />
  <MenuItem Header="Undo" 
              Command="{Binding UndoAsyncCommand}" />
        <MenuItem Header="Redo" 
          Command="{Binding RedoAsyncCommand}" />
    </ContextMenu>
</Image.ContextMenu>
```

### Disabling UI During Processing
```xml
<Grid IsEnabled="{Binding IsProcessing, 
          Converter={StaticResource InverseBool}}">
    <!-- All controls automatically disabled when IsProcessing=true -->
</Grid>
```

## Property Change Flow

```
User moves slider
    ↓
FilterParameters.Brightness changes
    ↓
PropertyChanged event fires
    ↓
ViewModel's event handler triggered
    ↓
ApplyCurrentFilterAsync() called (if applicable)
    ↓
Filter applied automatically (live preview)
    ↓
CurrentImage updates
    ↓
UI refreshes (via binding)
```

## Memory Considerations

- **CurrentImage**: 1 instance (8-10MB for Full HD)
- **OriginalImage**: 1 instance (8-10MB)
- **Undo Stack**: Up to 20 instances (160-200MB max)
- **Redo Stack**: Varies (cleared on new action)

**Total Peak**: ~220MB for Full HD images

## Tips

1. **Auto-save**: Check `HasUnsavedChanges` before closing window
2. **Progress**: Bind `IsProcessing` to show loading spinners
3. **Validation**: Commands handle CanExecute automatically
4. **Threading**: All async operations are thread-safe
5. **Testing**: Use constructor injection for easy mocking

---

**Ready for Step 4**: Wire this ViewModel to MainWindow XAML!
