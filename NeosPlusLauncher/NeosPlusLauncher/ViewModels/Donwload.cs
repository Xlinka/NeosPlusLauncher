using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Octokit;
using Avalonia.Threading;

namespace NeosPlusLauncher.ViewModels
{
    public static class Download
    {
        private const string RepositoryOwner = "Xlinka";
        private const string RepositoryName = "NeosPlus";

        public static async Task<bool> DownloadAndInstallNeosPlus(string neosPath, string neosPlusDirectory, TextBlock statusTextBlock, Button installButton)
        {
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

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    statusTextBlock.Text = "Downloading NeosPlus...";
                });

                using (HttpClient httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(latestReleaseUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
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
                ZipFile.ExtractToDirectory(localZipFilePath, neosPlusDirectory, true);

                // Delete the zip file after extraction
                File.Delete(localZipFilePath);

                // Update the version information in the version.txt file
                await File.WriteAllTextAsync(versionFilePath, latestRelease.TagName);
            }

            return true;
        }
    }
}
