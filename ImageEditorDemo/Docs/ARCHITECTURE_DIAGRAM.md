# ViewModel Architecture Diagram

## Component Structure

```
┌─────────────────────────────────────────────────────────────┐
│                      MainViewModel                          │
│                                                             │
│           Inherits: ViewModelBase (ObservableObject)        │
└─────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┴─────────────────────┐
        │                                           │
        ▼                                           ▼
┌──────────────────┐                        ┌──────────────────┐
│  Dependencies    │                        │   Observable     │
│                  │                        │   Properties     │
│ • IImageService  │                        │                  │
│ • IDialogService │                        │ • CurrentImage   │
└──────────────────┘                        │ • OriginalImage  │
                                            │ • Histogram      │
                                            │ • FilterParams   │
                                            │ • IsProcessing   │
                                            │ • StatusMessage  │
                                            └──────────────────┘
                                                     │
                        ┌────────────────────────────┴──────────────────────────┐
                        │                                                       │
                        ▼                                                       ▼
                ┌──────────────────┐                                  ┌──────────────────┐
                │    Commands      │                                  │      State       │
                │   (ICommand)     │                                  │   Management     │
                │                  │                                  │                  │
                │ • OpenImageAsync │◄─────────────────────────────────│ • Undo Stack     │
                │ • SaveImageAsync │                                  │ • Redo Stack     │
                │ • ApplyFilterAsy │                                  │ • History Size   │
                │ • UndoAsync      │                                  │                  │
                │ • RedoAsync      │                                  └──────────────────┘
                │ • ResetImageAsyn │
                └──────────────────┘
```

## Data Flow

### Opening an Image

```
User Action                  ViewModel                    Service Layer
───────────                  ─────────                    ─────────────
        
Click Open  ─────────►  OpenImageAsyncCommand
                                 │
                                 ▼
                         ShowOpenFileDialog()  ────────►  DialogService
                                 │                              │
                                 │◄─────────────────────────────┘
                                 │        (returns file path)
                                 ▼
                         LoadImageAsync(path)  ────────►  ImageService
                                 │                              │
                                 │◄─────────────────────────────┘
                                 │      (returns WriteableBitmap)
                                 ▼
                         Set OriginalImage
                         Clone to CurrentImage
                         Clear Undo/Redo
                         Calculate Histogram
                         Update UI bindings
```

### Applying a Filter

```
User Action               ViewModel                Service Layer
───────────               ─────────                ─────────────

Adjust Slider ──►  FilterParameters.Brightness
                     (PropertyChanged event)
                              │
                              ▼
                    ApplyCurrentFilterAsync()
                              │
                              ▼
                      Push to Undo Stack
                              │
                              ▼
                      ApplyFilterAsync() ──────────► ImageService
                              │                           │
                              │     (pixel manipulation)  │
                              │◄──────────────────────────┘
                              │  (returns filtered bitmap)
                              ▼
                         CurrentImage = filtered
                         HasUnsavedChanges = true
                         Update Histogram
```

### Undo Operation

```
User Action          ViewModel               Memory
───────────          ─────────               ──────

Click Undo ──────► UndoAsyncCommand
                          │
                          ▼
                    Check CanUndo
                          │
                          ▼
                  Pop from _undoStack ────► [State1]
                          │                 [State2] ←
                          │                 [State3]
                          ▼
                  Push to _redoStack  ────► [CurrentState]
                          │
                          ▼
                  CurrentImage = State2
                      Update UI
```

## Memory Management

```
┌─────────────────────────────────────────────┐
│       Image States in Memory                │
├─────────────────────────────────────────────┤
│                                             │
│  OriginalImage (1x)                         │
│  ┌─────────────┐                            │
│  │   8.3 MB    │  (1920×1080 RGBA)          │
│  └─────────────┘                            │
│                                             │
│  CurrentImage (1x)                          │
│  ┌─────────────┐                            │
│  │   8.3 MB    │                            │
│  └─────────────┘                            │
│                                             │
│  Undo Stack (max 20)                        │
│  ┌─────────────┐                            │
│  │   8.3 MB    │  State 20                  │
│  ├─────────────┤                            │
│  │   8.3 MB    │  State 19                  │
│  ├─────────────┤                            │
│  │     ...     │                            │
│  ├─────────────┤                            │
│  │   8.3 MB    │  State 1                   │
│  └─────────────┘                            │
│                                             │
│  Redo Stack (varies)                        │
│  ┌─────────────┐                            │
│  │   8.3 MB    │  Undone states             │
│  └─────────────┘                            │
│                                             │
│  Total Max: ~182 MB (22 states)             │
└─────────────────────────────────────────────┘
```

## Command State Machine

```
          ┌────────────────┐
          │   No Image     │
          │   Loaded       │
          └────────┬───────┘
                   │ 
                   │ OpenImageAsync
                   │
                   ▼
         ┌────────────────┐
     ┌───┤  Image Loaded  │
     │   │  (Clean State) │
     │   └────────┬───────┘
     │            │ 
     │            │ ApplyFilterAsync
     │            │
     │            ▼
     │      ┌────────────────┐
ResetAsync  │     Edited     │
     │      │ (Dirty State)  │
     │      └─────┬──────────┘
     │            │
     │            │ Undo/Redo
     │            │
     │            ▼
     │   ┌────────────────┐
     └───┤   Modified     │
         │ (With History) │
         └────────┬───────┘
                  │ 
                  │ SaveAsync
                  │
                  ▼
         ┌────────────────┐
         │     Saved      │
         │ (Clean Again)  │
         └────────────────┘
```

## Command Availability Matrix

| State        | Open | Save | Undo | Redo | Apply | Reset |
|--------------|------|------|------|------|-------|-------|
| No Image     | ✅   | ❌   | ❌   | ❌   | ❌    | ❌    |
| Fresh Load   | ✅   | ✅   | ❌   | ❌   | ✅*   | ❌    |
| After Filter | ✅   | ✅   | ✅   | ❌   | ✅*   | ✅    |
| After Undo   | ✅   | ✅   | ✅** | ✅   | ✅*   | ✅    |
| Processing   | ❌   | ❌   | ❌   | ❌   | ❌    | ❌    |

`*` Only if filter is selected
`**` If undo stack not empty

## Property Change Notifications

```
┌─────────────────────────────────────────────┐
│   CommunityToolkit.Mvvm Magic               │
├─────────────────────────────────────────────┤
│                                             │
│  [ObservableProperty]                       │
│  private WriteableBitmap? _currentImage;    │
│                                             │
│  ↓ Source Generator Creates ↓               │
│                                             │
│  public WriteableBitmap? CurrentImage       │
│  {                                          │
│      get => _currentImage;                  │
│      set                                    │
│      {                                      │
│          if (SetProperty(ref _currentImage, │
│                          value))            │
│          {                                  │
│              OnCurrentImageChanged(value);  │
│          }                                  │
│      }                                      │
│  }                                          │
│                                             │
│  partial void OnCurrentImageChanged(        │
│      WriteableBitmap? value);               │
│  // ↑ We can implement this hook            │
└─────────────────────────────────────────────┘
```

---

This architecture provides clean separation of concerns, testability, and maintainability!
