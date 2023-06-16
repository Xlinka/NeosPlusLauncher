using Avalonia.Controls;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace NeosPlusLauncher.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set => this.RaiseAndSetIfChanged(ref _statusText, value);
        }

        private string _launcherArguments;
        public string LauncherArguments
        {
            get => _launcherArguments;
            set => this.RaiseAndSetIfChanged(ref _launcherArguments, value);
        }

        public ReactiveCommand<Unit, Unit> InstallCommand { get; }

        private MainWindow mainWindow;
        private Button installButton;
        private TextBlock statusTextBlock;
        private TextBox launcherArgumentsTextBox;

        public MainWindowViewModel(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;

            InitializeControls();

            InstallCommand = ReactiveCommand.CreateFromTask(ExecuteInstall);
        }

        private void InitializeControls()
        {
            installButton = mainWindow.FindControl<Button>("InstallButton");
            statusTextBlock = mainWindow.FindControl<TextBlock>("StatusTextBlock");
            launcherArgumentsTextBox = mainWindow.FindControl<TextBox>("LauncherArgumentsTextBox");

            installButton.Click += InstallButton_Click;
        }

        private async Task ExecuteInstall()
        {
            try
            {
                installButton.IsEnabled = false;
                statusTextBlock.Text = "Checking for updates...";

                string[] neosPaths = NeosPathHelper.GetNeosPaths();
                string neosPath = null;

                if (neosPaths.Length > 0)
                {
                    neosPath = neosPaths[0];
                }
                else
                {
                    var dialog = new OpenFolderDialog { Title = "Select Directory", Directory = "." };
                    var result = await dialog.ShowAsync(mainWindow);

                    if (result == null)
                    {
                        statusTextBlock.Text = "No Neos directory selected.";
                        installButton.IsEnabled = true;
                        return;
                    }

                    neosPath = result;

                    // Save the custom directory to the configuration
                    Config config = Config.LoadConfig();
                    config.CustomInstallDir = neosPath;
                    config.SaveConfig();
                }

                string neosPlusDirectory = Path.Combine(neosPath, "Libraries", "NeosPlus");

                bool downloadSuccess = await DownloadNeosPlus(neosPath, neosPlusDirectory);

                if (!downloadSuccess)
                {
                    installButton.IsEnabled = true;
                    return;
                }

                statusTextBlock.Text = "Starting Neos with NeosPlus...";

                LaunchNeosPlus(neosPath, neosPlusDirectory);

                statusTextBlock.Text = "Done";
                installButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to execute install: {ex.Message}");
            }
        }

        private async Task<bool> DownloadNeosPlus(string neosPath, string neosPlusDirectory)
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    statusTextBlock.Text = "Downloading NeosPlus...";
                });

                bool downloadSuccess = await Download.DownloadAndInstallNeosPlus(neosPath, neosPlusDirectory, statusTextBlock, installButton);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (downloadSuccess)
                    {
                        statusTextBlock.Text = "NeosPlus downloaded successfully.";
                    }
                    else
                    {
                        statusTextBlock.Text = "Failed to download NeosPlus.";
                    }
                });

                return downloadSuccess;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during NeosPlus download: {ex}");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    statusTextBlock.Text = $"Error during NeosPlus download: {ex.Message}";
                });
                return false;
            }
        }


        private void LaunchNeosPlus(string neosPath, string neosPlusDirectory)
        {
            string neosExePath = Path.Combine(neosPath, "neos.exe");
            string neosPlusDllPath = Path.Combine(neosPlusDirectory, "NeosPlus.dll");
            string arguments = $"-LoadAssembly \"{neosPlusDllPath}\"";

            // Get the value of the LauncherArgumentsTextBox and add it as an argument
            string launcherArguments = launcherArgumentsTextBox?.Text?.Trim();
            if (!string.IsNullOrEmpty(launcherArguments))
            {
                arguments += $" {launcherArguments}";
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(neosExePath, arguments);
            startInfo.WorkingDirectory = neosPath;

            try
            {
                Process.Start(startInfo);

                // Save the configuration after launching NeosVR
                Config config = Config.LoadConfig();
                config.LauncherArguments = launcherArguments;
                config.SaveConfig();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to launch NeosVR: {ex.Message}");
            }
        }

        private void InstallButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _ = ExecuteInstall();
        }
    }
}
