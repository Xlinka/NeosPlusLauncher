using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Octokit;

namespace NeosPlusInstaller
{
    public class MainWindow : Window
    {
        private const string RepositoryOwner = "Xlinka";
        private const string RepositoryName = "NeosPlus";

        private string[] GetNeosPaths()
        {
            string[] defaultPaths =
            {
                @"C:\Program Files (x86)\Steam\steamapps\common\NeosVR\",
                @"C:\Neos\app\",
                @"/.steam/steam/steamapps/common/NeosVR/",
                @"/mnt/LocalDisk/SteamLibrary/steamapps/common/NeosVR/"
            };

            List<string> existingPaths = new List<string>();
            foreach (string path in defaultPaths)
            {
                if (Directory.Exists(path))
                {
                    existingPaths.Add(path);
                }
            }

            return existingPaths.ToArray();
        }

        private Button InstallButton;
        private TextBlock StatusTextBlock;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            InstallButton = this.FindControl<Button>("InstallButton");
            StatusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");
            InstallButton.Click += InstallButton_Click;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            InstallButton.IsEnabled = false;
            StatusTextBlock.Text = "Checking for updates...";

            string[] neosPaths = GetNeosPaths();

            if (neosPaths.Length == 0)
            {
                StatusTextBlock.Text = "No Neos directory found. Aborting installation.";
                InstallButton.IsEnabled = true;
                return;
            }

            string neosPath = neosPaths[0];

            if (neosPaths.Length > 1)
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select a directory",
                    Directory = neosPath ?? "", // Set initial directory to the current Neos path, if available
                    AllowMultiple = false,
                    InitialFileName = ".",
                    Filters = new List<FileDialogFilter> { new FileDialogFilter() { Name = "Directories", Extensions = { "" } } },
                };

                var result = await dialog.ShowAsync(this);

                if (result != null && result.Length > 0)
                {
                    neosPath = result[0];
                }
            }
            GitHubClient gitHubClient = new(new Octokit.ProductHeaderValue("NeosPlusInstaller"));
            Release latestRelease = await gitHubClient.Repository.Release.GetLatest(RepositoryOwner, RepositoryName);

            string neosPlusDirectory = Path.Combine(neosPath, "Libraries", "NeosPlus");
            string versionFilePath = Path.Combine(neosPlusDirectory, "version.txt");

            // Create the NeosPlus directory if it doesn't exist
            if (!Directory.Exists(neosPlusDirectory))
            {
                Directory.CreateDirectory(neosPlusDirectory);
            }

            if (!File.Exists(versionFilePath))
            {
                // Create the version.txt file with the latest release version
                await File.WriteAllTextAsync(versionFilePath, latestRelease.TagName);
            }
            // Read the current version from the version.txt file
            string currentVersion = await File.ReadAllTextAsync(versionFilePath);


            if (currentVersion != latestRelease.TagName)
            {
                string latestReleaseUrl = latestRelease.Assets[0].BrowserDownloadUrl;
                string localZipFilePath = Path.Combine(Path.GetTempPath(), "NeosPlus.zip");

                StatusTextBlock.Text = "Downloading NeosPlus...";
                using (HttpClient httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(latestReleaseUrl);
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                                 stream = new FileStream(localZipFilePath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        await contentStream.CopyToAsync(stream);
                    }
                }

                StatusTextBlock.Text = "Extracting NeosPlus...";
                string neosPlusFolder = Path.Combine(neosPath, "Libraries", "NeosPlus");
                if (Directory.Exists(neosPlusFolder))
                {
                    Directory.Delete(neosPlusFolder, true);
                }

                Directory.CreateDirectory(neosPlusFolder);
                ZipFile.ExtractToDirectory(localZipFilePath, neosPlusFolder);

                File.Delete(localZipFilePath);

                await File.WriteAllTextAsync(versionFilePath, latestRelease.TagName);
            }

            StatusTextBlock.Text = "Starting Neos with NeosPlus...";
            string neosExePath = Path.Combine(neosPath, "neos.exe");
            System.Diagnostics.Process.Start(neosExePath, "-LoadAssembly Libraries\\NeosPlus\\NEOSPlus.dll");

            StatusTextBlock.Text = "Done!";
            InstallButton.IsEnabled = true;
        }
    }
}