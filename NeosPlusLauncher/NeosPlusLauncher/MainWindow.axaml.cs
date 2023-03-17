using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Octokit;

namespace NeosPlusInstaller
{
    public class MainWindow : Window
    {
        private const string RepositoryOwner = "Xlinka";
        private const string RepositoryName = "NeosPlus";
        private const string NeosExePath = @"C:\Path\To\Your\Neos\neos.exe";
        private const string NeosPlusFolder = @"C:\Path\To\Your\Neos\Libraries\NeosPlus";
        private const string VersionFilePath = @"C:\Path\To\Your\Neos\Libraries\NeosPlus\version.txt";

        private Button InstallButton;
        private TextBlock StatusTextBlock;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            InstallButton = this.FindControl<Button>("InstallButton");
            StatusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            InstallButton.IsEnabled = false;
            StatusTextBlock.Text = "Checking for updates...";

            string currentVersion = string.Empty;
            if (File.Exists(VersionFilePath))
            {
                currentVersion = await File.ReadAllTextAsync(VersionFilePath);
            }

            GitHubClient gitHubClient = new(new Octokit.ProductHeaderValue("NeosPlusInstaller"));
            Release latestRelease = await gitHubClient.Repository.Release.GetLatest(RepositoryOwner, RepositoryName);

            if (currentVersion != latestRelease.TagName)
            {
                string latestReleaseUrl = latestRelease.Assets[0].BrowserDownloadUrl;
                string localZipFilePath = Path.Combine(Path.GetTempPath(), "NeosPlus.zip");

                StatusTextBlock.Text = "Downloading NeosPlus...";
                using (WebClient webClient = new())
                {
                    await webClient.DownloadFileTaskAsync(new Uri(latestReleaseUrl), localZipFilePath);
                }

                StatusTextBlock.Text = "Extracting NeosPlus...";
                if (Directory.Exists(NeosPlusFolder))
                {
                    Directory.Delete(NeosPlusFolder, true);
                }

                Directory.CreateDirectory(NeosPlusFolder);
                ZipFile.ExtractToDirectory(localZipFilePath, NeosPlusFolder);

                File.Delete(localZipFilePath);

                await File.WriteAllTextAsync(VersionFilePath, latestRelease.TagName);
            }

            StatusTextBlock.Text = "Starting Neos with NeosPlus...";
            System.Diagnostics.Process.Start(NeosExePath, "-LoadAssembly Libraries\\NeosPlus\\NEOSPlus.dll");

            StatusTextBlock.Text = "Done!";
            InstallButton.IsEnabled = true;
        }
    }
}