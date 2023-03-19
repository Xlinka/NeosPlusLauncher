using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Octokit;
using System.Globalization;
using System.Text.Json;
using NeosPlusInstaller.ViewModels;
using Avalonia.Controls;
using Avalonia;

namespace NeosPlusInstaller
{
    public class MainWindow : Window
    {
        public static readonly StyledProperty<string> StatusTextProperty =
            AvaloniaProperty.Register<MainWindow, string>(nameof(StatusText));

        public string StatusText
        {
            get { return GetValue(StatusTextProperty); }
            set { SetValue(StatusTextProperty, value); }
        }

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