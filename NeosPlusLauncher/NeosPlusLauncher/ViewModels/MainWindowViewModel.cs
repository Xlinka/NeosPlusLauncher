using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

   

        private const string RepositoryOwner = "Xlinka";
        private const string RepositoryName = "NeosPlus";
        private string logFileName;

        private readonly MainWindow mainWindow;
        private readonly Button installButton;
        private readonly TextBlock statusTextBlock;
        private readonly TextBox launcherArgumentsTextBox;

        public MainWindowViewModel(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;

            // Initialize logFileName based on the current time
            logFileName = $"log-{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.txt";

            installButton = mainWindow.FindControl<Button>("InstallButton");
            statusTextBlock = mainWindow.FindControl<TextBlock>("StatusTextBlock");
            launcherArgumentsTextBox = mainWindow.FindControl<TextBox>("LauncherArgumentsTextBox");

            installButton.Click += InstallButton_Click;
        }

        private async Task LogAsync(string message)
        {
            string localeFolder = Path.Combine(Directory.GetCurrentDirectory(), "Locale");
            var currentLocale = System.Globalization.CultureInfo.CurrentCulture.Name;
            var localeFilePath = Path.Combine(localeFolder, $"{currentLocale}.json");
            if (!File.Exists(localeFilePath))
            {
                Console.WriteLine($"Locale file for {currentLocale} not found.");
                return;
            }

            var langFile = File.ReadAllText(localeFilePath);

            var langData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(langFile);

            if (langData.ContainsKey(message))
            {
                message = langData[message];
            }

            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"log-{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.txt");
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logMessage = $"{timestamp} - {message}{Environment.NewLine}";

            await File.AppendAllTextAsync(logFilePath, logMessage);
        }
        private string GetLocalizedString(string key)
        {
            string localeFolder = Path.Combine(Directory.GetCurrentDirectory(), "Locale");
            var currentLocale = CultureInfo.CurrentCulture.Name;
            var localeFilePath = Path.Combine(localeFolder, $"{currentLocale}.json");
            if (!File.Exists(localeFilePath))
            {
                Console.WriteLine($"Locale file for {currentLocale} not found.");
                return key;
            }

            var langFile = File.ReadAllText(localeFilePath);

            var langData = JsonSerializer.Deserialize<Dictionary<string, string>>(langFile);

            if (langData.ContainsKey(key))
            {
                return langData[key];
            }
            else
            {
                Console.WriteLine($"Localization key '{key}' not found in locale file for {currentLocale}.");
                return key;
            }
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            installButton.IsEnabled = false;
            statusTextBlock.Text = GetLocalizedString("CheckingForUpdates");

            string[] neosPaths = GetNeosPaths();

            string neosPath = null;

            if (neosPaths.Length > 0)
            {
                neosPath = neosPaths[0];
            }
            else
            {
                var dialog = new OpenFolderDialog
                {
                    Title = GetLocalizedString("SelectDirectory"),
                    Directory = "."
                };

                var result = await dialog.ShowAsync(new Window());

                if (result != null)
                {
                    neosPath = result;
                }
            }

            if (neosPath == null)
            {
                statusTextBlock.Text = GetLocalizedString("NoNeosDirectory");
                installButton.IsEnabled = true;
                return;
            }



            string neosPlusDirectory = Path.Combine(neosPath, "Libraries", "NeosPlus");
            string versionFilePath = Path.Combine(neosPlusDirectory, "version.txt");
            string dllFilePath = Path.Combine(neosPlusDirectory, "NeosPlus.dll");

            // Create the NeosPlus directory if it doesn't exist
            if (!Directory.Exists(neosPlusDirectory))
            {
                Directory.CreateDirectory(neosPlusDirectory);
            }

            // Read the current version from the version.txt file or set to empty string if not found
            string currentVersion = "";
            if (File.Exists(versionFilePath))
            {
                currentVersion = File.ReadAllText(versionFilePath);
            }

            GitHubClient gitHubClient = new(new Octokit.ProductHeaderValue("NeosPlusInstaller"));
            Release latestRelease = await gitHubClient.Repository.Release.GetLatest(RepositoryOwner, RepositoryName);


            // Create the NeosPlus directory if it doesn't exist
            if (!Directory.Exists(neosPlusDirectory))
            {
                Directory.CreateDirectory(neosPlusDirectory);
            }

            // Read the current version from the version.txt file or set to empty string if not found
            if (File.Exists(versionFilePath))
            {
                currentVersion = await File.ReadAllTextAsync(versionFilePath);
            }

            if (currentVersion != latestRelease.TagName || !File.Exists(dllFilePath))
            {
                string latestReleaseUrl = latestRelease.Assets[0].BrowserDownloadUrl;
                await LogAsync($"Latest Release URL: {latestReleaseUrl}"); // Log the asset URL

                string localDllFilePath = Path.Combine(neosPlusDirectory, $"NeosPlus_{latestRelease.TagName}.dll");
                await LogAsync($"Local Dll File Path: {localDllFilePath}"); // Log the local dll file path

                statusTextBlock.Text = GetLocalizedString("DownloadingNeosPlus");
                using (HttpClient httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(latestReleaseUrl);
                    if (response.IsSuccessStatusCode) // Check if the response is successful
                    {
                        using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                                 stream = new FileStream(localDllFilePath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                        {
                            await contentStream.CopyToAsync(stream);
                        }
                    }
                    else
                    {
                        statusTextBlock.Text = GetLocalizedString("FailedToDownloadNeosPlus");
                        installButton.IsEnabled = true;
                        return;
                    }
                }

                // Replace the current DLL with the new one
                if (File.Exists(dllFilePath))
                {
                    File.Delete(dllFilePath);
                }
                File.Move(localDllFilePath, dllFilePath);

                // Update the version information in the version.txt file
                await File.WriteAllTextAsync(versionFilePath, latestRelease.TagName);
            }
            statusTextBlock.Text = GetLocalizedString("StartingNeosWithNeosPlus");
            LaunchNeosPlus(neosPath, neosPlusDirectory);

            statusTextBlock.Text = GetLocalizedString("Done");
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to launch NeosVR: {ex.Message}");
            }
        }
    }
}