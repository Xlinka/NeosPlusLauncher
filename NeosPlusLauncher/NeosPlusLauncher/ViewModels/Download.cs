using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Octokit;

namespace NeosPlusLauncher.ViewModels
{
    public static class Download
    {
        private const string RepositoryOwner = "Xlinka";
        private const string RepositoryName = "NeosPlus";

        public static async Task<bool> DownloadAndInstallNeosPlus(string neosPath, string neosPlusDirectory, TextBlock statusTextBlock, Button installButton)
        {
            string neosPlusDir = Path.Combine(neosPath, "Libraries", "NeosPlus");
            Directory.CreateDirectory(neosPlusDir);
            string versionFilePath = Path.Combine(neosPlusDir, "version.txt");

            GitHubClient gitHubClient = new GitHubClient(new Octokit.ProductHeaderValue("NeosPlusInstaller"));
            Release latestRelease = await gitHubClient.Repository.Release.GetLatest(RepositoryOwner, RepositoryName);

            // Read the current version from the version.txt file or set it to an empty string if not found
            string currentVersion = File.Exists(versionFilePath) ? File.ReadAllText(versionFilePath) : "";

            if (currentVersion != latestRelease.TagName)
            {
                string latestReleaseUrl = latestRelease.Assets[0].BrowserDownloadUrl;
                string localZipFilePath = Path.Combine(neosPlusDir, $"NeosPlus_{latestRelease.TagName}.zip");

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    statusTextBlock.Text = "Downloading NeosPlus...";
                });

                try
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        var response = await httpClient.GetAsync(latestReleaseUrl);

                        if (!response.IsSuccessStatusCode)
                        {
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                statusTextBlock.Text = "Failed to download NeosPlus.";
                                installButton.IsEnabled = true;
                            });
                            return false;
                        }

                        using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                                stream = new FileStream(localZipFilePath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                        {
                            await contentStream.CopyToAsync(stream);
                        }
                    }

                    // Extract the zip file to the NeosPlus directory
                    ZipFile.ExtractToDirectory(localZipFilePath, neosPlusDir, true);

                    // Delete the zip file after extraction
                    File.Delete(localZipFilePath);

                    // Update the version information in the version.txt file
                    await File.WriteAllTextAsync(versionFilePath, latestRelease.TagName);

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        statusTextBlock.Text = "NeosPlus downloaded and installed successfully.";
                    });

                    return true;
                }
                catch (Exception ex)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        statusTextBlock.Text = $"Failed to download or install NeosPlus: {ex.Message}";
                        installButton.IsEnabled = true;
                    });
                    return false;
                }
            }
            else
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    statusTextBlock.Text = "NeosPlus is up-to-date.";
                });

                return true;
            }
        }
    }
}
