using System.Windows;
using ImageEditorDemo.Services;
using ImageEditorDemo.ViewModels;

namespace ImageEditorDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Initialize services
            var imageService = new ImageService();
            var dialogService = new DialogService();

            // Create and set ViewModel as DataContext
            DataContext = new MainViewModel(imageService, dialogService);
        }

        /// <summary>
        /// Access the ViewModel from code-behind if needed
        /// </summary>
        private MainViewModel ViewModel => (MainViewModel)DataContext;

        /// <summary>
        /// Handle window closing - check for unsaved changes
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            if (ViewModel.HasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Do you want to close anyway?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}