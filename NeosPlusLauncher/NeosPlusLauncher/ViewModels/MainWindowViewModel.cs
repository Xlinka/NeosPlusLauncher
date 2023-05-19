using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Octokit;

namespace NeosPlusInstaller.ViewModels
{
    public class MainWindowViewModel
    {
        private const string ConfigFilePath = "./Assets/Config.json";

        public class Config
        {
            public string LauncherArguments { get; set; }
            public string CustomInstallDir { get; set; }
        }

        private Config LoadConfig()
        {
            try
            {
                string json = File.ReadAllText(ConfigFilePath);
                return JsonSerializer.Deserialize<Config>(json);
            }
            catch (FileNotFoundException)
            {
                // Return a new empty configuration if the file doesn't exist
                return new Config();
            }
        }

        private void SaveConfig(Config config)
        {
            string json = JsonSerializer.Serialize(config);
            File.WriteAllText(ConfigFilePath, json);
        }

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

            // Check if CustomInstallDir is set in the configuration
            Config config = LoadConfig();
            if (!string.IsNullOrEmpty(config.CustomInstallDir) && Directory.Exists(config.CustomInstallDir))
            {
                existingPaths.Insert(0, config.CustomInstallDir);
            }

            return existingPaths.ToArray();
        }

        private const string RepositoryOwner = "Xlinka";
        private const string RepositoryName = "NeosPlus";

        private readonly MainWindow mainWindow;
        private readonly Button installButton;
        private readonly TextBlock statusTextBlock;
        private readonly TextBox launcherArgumentsTextBox;

        public MainWindowViewModel(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;

            installButton = mainWindow.FindControl<Button>("InstallButton");
            statusTextBlock = mainWindow.FindControl<TextBlock>("StatusTextBlock");
            launcherArgumentsTextBox = mainWindow.FindControl<TextBox>("LauncherArgumentsTextBox");

            installButton.Click += InstallButton_Click;

            // Load the configuration when the application starts
            Config config = LoadConfig();

            // Set the LauncherArgumentsTextBox to the value in the configuration
            launcherArgumentsTextBox.Text = config.LauncherArguments;
        }


        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            installButton.IsEnabled = false;
            statusTextBlock.Text = "Checking for updates...";

            string[] neosPaths = GetNeosPaths();
            string neosPath = null;

            if (neosPaths.Length > 0)
                neosPath = neosPaths[0];
            else
            {
                var dialog = new OpenFolderDialog { Title = "Select Directory", Directory = "." };
                var result = await dialog.ShowAsync(new Window());

                if (result == null)
                {
                    statusTextBlock.Text = "No Neos directory selected.";
                    installButton.IsEnabled = true;
                    return;
                }

                neosPath = result;

                // Save the custom directory to the configuration
                Config config = LoadConfig();
                config.CustomInstallDir = neosPath;
                SaveConfig(config);
            }

            string neosPlusDirectory = Path.Combine(neosPath, "Libraries", "NeosPlus");
            string versionFilePath = Path.Combine(neosPlusDirectory, "version.txt");

            // Create the NeosPlus directory if it doesn't exist
            if (!Directory.Exists(neosPlusDirectory))
                Directory.CreateDirectory(neosPlusDirectory);

            // Read the current version from the version.txt file or set it to an empty string if not found
            string currentVersion = File.Exists(versionFilePath) ? File.ReadAllText(versionFilePath) : "";

            GitHubClient gitHubClient = new GitHubClient(new Octokit.ProductHeaderValue("NeosPlusInstaller"));
            Release latestRelease = await gitHubClient.Repository.Release.GetLatest(RepositoryOwner, RepositoryName);

            if (currentVersion != latestRelease.TagName)
            {
                string latestReleaseUrl = latestRelease.Assets[0].BrowserDownloadUrl;
                string localZipFilePath = Path.Combine(neosPlusDirectory, $"NeosPlus_{latestRelease.TagName}.zip");

                statusTextBlock.Text = "Downloading NeosPlus...";
                using (HttpClient httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(latestReleaseUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        statusTextBlock.Text = "Failed to download NeosPlus.";
                        installButton.IsEnabled = true;
                        return;
                    }

                    using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                                stream = new FileStream(localZipFilePath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        await contentStream.CopyToAsync(stream);
                    }
                }

                // Extract the zip file to the NeosPlus directory
                ZipFile.ExtractToDirectory(localZipFilePath, neosPlusDirectory, true);

                // Delete the zip file after extraction
                File.Delete(localZipFilePath);

                // Update the version information in the version.txt file
                await File.WriteAllTextAsync(versionFilePath, latestRelease.TagName);
            }

            statusTextBlock.Text = "Starting Neos with NeosPlus...";
            LaunchNeosPlus(neosPath, neosPlusDirectory);

            statusTextBlock.Text = "Done";
            installButton.IsEnabled = true;
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
                Config config = LoadConfig();
                config.LauncherArguments = launcherArguments;
                SaveConfig(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to launch NeosVR: {ex.Message}");
            }
        }
    }
}

