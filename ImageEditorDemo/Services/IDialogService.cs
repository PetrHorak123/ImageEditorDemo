namespace ImageEditorDemo.Services;

/// <summary>
/// Helper service for file dialogs
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows an open file dialog for images
    /// </summary>
    /// <returns>Selected file path or null if canceled</returns>
    string? ShowOpenFileDialog();

    /// <summary>
    /// Shows a save file dialog for images
    /// </summary>
    /// <param name="defaultFileName">Default filename</param>
    /// <returns>Selected file path or null if canceled</returns>
    string? ShowSaveFileDialog(string defaultFileName = "image.png");

    /// <summary>
    /// Shows an error message box
    /// </summary>
    void ShowError(string message, string title = "Error");

    /// <summary>
    /// Shows an information message box
    /// </summary>
    void ShowInformation(string message, string title = "Information");
}
