using System.Windows;
using Microsoft.Win32;

namespace ImageEditorDemo.Services
{
    /// <summary>
    /// Implementation of dialog service for file operations
    /// </summary>
    public class DialogService : IDialogService
    {
        private const string ImageFilter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp|PNG Files|*.png|JPEG Files|*.jpg;*.jpeg|BMP Files|*.bmp|All Files|*.*";

        public string? ShowOpenFileDialog()
        {
            var dialog = new OpenFileDialog
            {
                Filter = ImageFilter,
                Title = "Open Image",
                CheckFileExists = true,
                CheckPathExists = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowSaveFileDialog(string defaultFileName = "image.png")
        {
            var dialog = new SaveFileDialog
            {
                Filter = ImageFilter,
                Title = "Save Image",
                FileName = defaultFileName,
                DefaultExt = ".png",
                AddExtension = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public void ShowError(string message, string title = "Error")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowInformation(string message, string title = "Information")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
