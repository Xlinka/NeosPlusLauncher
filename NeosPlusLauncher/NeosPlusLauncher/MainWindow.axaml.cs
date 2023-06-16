using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NeosPlusLauncher.ViewModels;

namespace NeosPlusLauncher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            DataContext = new MainWindowViewModel(this);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
