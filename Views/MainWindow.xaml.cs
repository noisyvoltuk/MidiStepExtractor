using MidiStepExtractor.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace MidiStepExtractor.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            Closed += (_, _) => ViewModel?.Dispose();
        }

        /// <summary>
        /// Handle step button clicks — routed up via Tag containing StepIndex.
        /// We use code-behind here since ItemsControl DataTemplate buttons
        /// don't have easy access to the parent VM toggle command with index.
        /// </summary>
        private void StepButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int stepIndex)
                ViewModel.ToggleStep(stepIndex);
        }
    }
}
