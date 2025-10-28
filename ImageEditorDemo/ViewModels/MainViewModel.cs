using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageEditorDemo.Models;
using ImageEditorDemo.Services;

namespace ImageEditorDemo.ViewModels
{
    /// <summary>
    /// Main ViewModel for the Image Editor application.
    /// Manages image state, filter operations, and undo/redo functionality.
    /// Uses CommunityToolkit.Mvvm for MVVM pattern implementation.
    /// </summary>
    public partial class MainViewModel : ViewModelBase
    {
        #region Services

        private readonly IImageService _imageService;
        private readonly IDialogService _dialogService;

        #endregion

        #region Observable Properties

        /// <summary>
        /// Currently displayed image in the editor.
        /// Bound to the UI Image control.
        /// </summary>
        [ObservableProperty]
        private WriteableBitmap? _currentImage;

        /// <summary>
        /// Original loaded image (unchanged).
        /// Used as reference for resetting changes.
        /// </summary>
        [ObservableProperty]
        private WriteableBitmap? _originalImage;

        /// <summary>
        /// Current histogram data for the displayed image.
        /// </summary>
        [ObservableProperty]
        private ImageHistogram? _currentHistogram;

        /// <summary>
        /// Filter parameters (brightness, contrast, blur radius).
        /// Bound to UI sliders.
        /// </summary>
        [ObservableProperty]
        private FilterParameters _filterParameters = new();

        /// <summary>
        /// Currently selected filter type.
        /// </summary>
        [ObservableProperty]
        private FilterType _selectedFilter = FilterType.None;

        /// <summary>
        /// Indicates if an operation is in progress (for UI feedback).
        /// </summary>
        [ObservableProperty]
        private bool _isProcessing;

        /// <summary>
        /// Current file path of the loaded image.
        /// </summary>
        [ObservableProperty]
        private string? _currentFilePath;

        /// <summary>
        /// Indicates if image has unsaved changes.
        /// </summary>
        [ObservableProperty]
        private bool _hasUnsavedChanges;

        /// <summary>
        /// Status message for the status bar.
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = "Ready";

        #endregion

        #region Undo/Redo System

        /// <summary>
        /// Stack for undo operations - stores previous image states.
        /// </summary>
        private readonly Stack<WriteableBitmap> _undoStack = new();

        /// <summary>
        /// Stack for redo operations - stores undone image states.
        /// </summary>
        private readonly Stack<WriteableBitmap> _redoStack = new();

        /// <summary>
        /// Maximum number of undo/redo states to keep in memory.
        /// Prevents excessive memory usage.
        /// </summary>
        private const int MaxHistorySize = 20;

        /// <summary>
        /// Can undo if there are states in the undo stack.
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>
        /// Can redo if there are states in the redo stack.
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;

        #endregion

        #region Available Filters

        /// <summary>
        /// List of all available filters for the filter dropdown.
        /// </summary>
        public ObservableCollection<FilterType> AvailableFilters { get; } = new()
        {
            FilterType.None,
            FilterType.Grayscale,
            FilterType.Sepia,
            FilterType.BrightnessContrast,
            FilterType.GaussianBlur,
            FilterType.EdgeDetection
        };

        #endregion

        #region Constructor

        public MainViewModel(IImageService imageService, IDialogService dialogService)
        {
            _imageService = imageService;
            _dialogService = dialogService;

            // Subscribe to filter parameter changes for live preview
            FilterParameters.PropertyChanged += (s, e) =>
            {
                // When sliders change, update the preview
                if (!IsProcessing && CurrentImage != null)
                {
                    _ = ApplyCurrentFilterAsync();
                }
            };
        }

        /// <summary>
        /// Parameterless constructor for design-time support.
        /// </summary>
        public MainViewModel() : this(new ImageService(), new DialogService())
        {
        }

        #endregion

        #region Commands - File Operations

        /// <summary>
        /// Opens an image file dialog and loads the selected image.
        /// </summary>
        [RelayCommand]
        private async Task OpenImageAsync()
        {
            try
            {
                // Show file dialog
                var filePath = _dialogService.ShowOpenFileDialog();
                if (string.IsNullOrEmpty(filePath))
                    return;

                IsProcessing = true;
                StatusMessage = $"Loading {Path.GetFileName(filePath)}...";

                // Load image
                var image = await _imageService.LoadImageAsync(filePath);
                if (image == null)
                {
                    _dialogService.ShowError("Failed to load image. The file may be corrupted or in an unsupported format.");
                    StatusMessage = "Failed to load image";
                    return;
                }

                // Store original and current image
                OriginalImage = image;
                CurrentImage = _imageService.CloneBitmap(image);
                CurrentFilePath = filePath;
                HasUnsavedChanges = false;

                // Clear undo/redo history
                _undoStack.Clear();
                _redoStack.Clear();

                // Calculate initial histogram
                await UpdateHistogramAsync();

                // Reset filter parameters
                FilterParameters = new FilterParameters();
                SelectedFilter = FilterType.None;

                StatusMessage = $"Loaded: {Path.GetFileName(filePath)} ({image.PixelWidth}×{image.PixelHeight})";

                // Update command states
                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Error loading image: {ex.Message}");
                StatusMessage = "Error loading image";
            }
            finally
            {
                IsProcessing = false;
            }

            NotifyEverythingChanged();
        }

        /// <summary>
        /// Saves the current image to a file.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSaveImage))]
        private async Task SaveImageAsync()
        {
            try
            {
                if (CurrentImage == null)
                    return;

                // Show save dialog with default filename
                var defaultName = !string.IsNullOrEmpty(CurrentFilePath)
                  ? Path.GetFileNameWithoutExtension(CurrentFilePath) + "_edited" + Path.GetExtension(CurrentFilePath)
                   : "edited_image.png";

                var filePath = _dialogService.ShowSaveFileDialog(defaultName);
                if (string.IsNullOrEmpty(filePath))
                    return;

                IsProcessing = true;
                StatusMessage = "Saving image...";

                // Save image
                var success = await _imageService.SaveImageAsync(CurrentImage, filePath);
                if (success)
                {
                    HasUnsavedChanges = false;
                    CurrentFilePath = filePath;
                    StatusMessage = $"Saved: {Path.GetFileName(filePath)}";
                }
                else
                {
                    _dialogService.ShowError("Failed to save image.");
                    StatusMessage = "Failed to save image";
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Error saving image: {ex.Message}");
                StatusMessage = "Error saving image";
            }
            finally
            {
                IsProcessing = false;
            }

            NotifyEverythingChanged();
        }

        private bool CanSaveImage() => CurrentImage != null && !IsProcessing;

        /// <summary>
        /// Resets the image to its original state (before any filters).
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanResetImage))]
        private async Task ResetImageAsync()
        {
            if (OriginalImage == null)
                return;

            try
            {
                IsProcessing = true;
                StatusMessage = "Resetting image...";

                // Save current state for undo
                if (CurrentImage != null)
                {
                    PushToUndoStack(CurrentImage);
                }

                // Reset to original
                CurrentImage = _imageService.CloneBitmap(OriginalImage);
                FilterParameters = new FilterParameters();
                SelectedFilter = FilterType.None;
                HasUnsavedChanges = false;

                await UpdateHistogramAsync();

                StatusMessage = "Image reset to original";
            }
            finally
            {
                IsProcessing = false;
            }

            NotifyEverythingChanged();
        }

        private bool CanResetImage() => OriginalImage != null && !IsProcessing;

        #endregion

        #region Commands - Filter Operations

        /// <summary>
        /// Applies the currently selected filter with current parameters.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanApplyFilter))]
        private async Task ApplyFilterAsync()
        {
            if (CurrentImage == null || SelectedFilter == FilterType.None)
                return;

            try
            {
                IsProcessing = true;
                StatusMessage = $"Applying {SelectedFilter} filter...";

                // Save current state for undo
                PushToUndoStack(CurrentImage);

                // Apply filter
                var filtered = await _imageService.ApplyFilterAsync(
                    CurrentImage,
                    SelectedFilter,
                    FilterParameters);
                
                CurrentImage = filtered;
                HasUnsavedChanges = true;

                // Update histogram
                await UpdateHistogramAsync();

                StatusMessage = $"Applied {SelectedFilter} filter";
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Error applying filter: {ex.Message}");
                StatusMessage = "Error applying filter";
            }
            finally
            {
                IsProcessing = false;
            }

            NotifyEverythingChanged();
        }

        private bool CanApplyFilter() => CurrentImage != null && SelectedFilter != FilterType.None && !IsProcessing;

        /// <summary>
        /// Applies the current filter for live preview (called when sliders change).
        /// </summary>
        private async Task ApplyCurrentFilterAsync()
        {
            // Only auto-apply for filters that use parameters
            if (SelectedFilter == FilterType.BrightnessContrast || SelectedFilter == FilterType.GaussianBlur)
            {
                await ApplyFilterAsync();
            }
        }

        #endregion

        #region Commands - Undo/Redo

        /// <summary>
        /// Undoes the last operation.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanUndo))]
        private async Task UndoAsync()
        {
            if (!CanUndo || CurrentImage == null)
                return;

            try
            {
                IsProcessing = true;
                StatusMessage = "Undoing...";

                // Push current state to redo stack
                _redoStack.Push(_imageService.CloneBitmap(CurrentImage));

                // Pop from undo stack
                CurrentImage = _undoStack.Pop();
                HasUnsavedChanges = _undoStack.Count > 0;

                await UpdateHistogramAsync();

                StatusMessage = "Undo complete";

                // Update command states
                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
            }
            finally
            {
                IsProcessing = false;
            }

            NotifyEverythingChanged();
        }

        /// <summary>
        /// Redoes the last undone operation.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRedo))]
        private async Task RedoAsync()
        {
            if (!CanRedo || CurrentImage == null)
                return;

            try
            {
                IsProcessing = true;
                StatusMessage = "Redoing...";

                // Push current state to undo stack
                PushToUndoStack(CurrentImage, false);

                // Pop from redo stack
                CurrentImage = _redoStack.Pop();
                HasUnsavedChanges = true;

                await UpdateHistogramAsync();

                StatusMessage = "Redo complete";

                // Update command states
                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
            }
            finally
            {
                IsProcessing = false;
            }

            NotifyEverythingChanged();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Pushes current image state to the undo stack.
        /// Manages stack size to prevent excessive memory usage.
        /// </summary>
        private void PushToUndoStack(WriteableBitmap image, bool clearRedoStack = true)
        {
            // Clear redo stack when new action is performed
            if (clearRedoStack)
            {
                _redoStack.Clear();
            }

            // Clone and push to undo stack
            _undoStack.Push(_imageService.CloneBitmap(image));

            // Limit stack size
            if (_undoStack.Count > MaxHistorySize)
            {
                // Remove oldest state
                var stack = _undoStack.ToArray();
                _undoStack.Clear();
                for (int i = 0; i < MaxHistorySize; i++)
                {
                    _undoStack.Push(stack[i]);
                }
            }

            // Update command states
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        /// <summary>
        /// Updates the histogram for the current image.
        /// </summary>
        private async Task UpdateHistogramAsync()
        {
            if (CurrentImage == null)
            {
                CurrentHistogram = null;
                return;
            }

            try
            {
                CurrentHistogram = await _imageService.CalculateHistogramAsync(CurrentImage);
            }
            catch (Exception)
            {
                // Silently fail histogram update - it's not critical
                CurrentHistogram = null;
            }
        }

        private void NotifyEverythingChanged()
        {
            SaveImageCommand.NotifyCanExecuteChanged();
            ResetImageCommand.NotifyCanExecuteChanged();
            ApplyFilterCommand.NotifyCanExecuteChanged();
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region Property Change Handlers

        /// <summary>
        /// Called when CurrentImage changes - updates dependent properties.
        /// </summary>
        partial void OnCurrentImageChanged(WriteableBitmap? value)
        {
            // Update command can-execute states
            // Notify the generated RelayCommands to re-evaluate their CanExecute methods
            SaveImageCommand.NotifyCanExecuteChanged();
            ResetImageCommand.NotifyCanExecuteChanged();
            ApplyFilterCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Called when SelectedFilter changes - resets parameters.
        /// </summary>
        partial void OnSelectedFilterChanged(FilterType value)
        {
            // Reset parameters when filter changes
            FilterParameters = new FilterParameters();
            ApplyFilterCommand.NotifyCanExecuteChanged();
        }

        #endregion
    }
}
