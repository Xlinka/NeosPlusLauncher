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
            string dllFileName = "NeosPlus.dll";
            string localFilePath = Path.Combine(neosPlusDirectory, dllFileName);

            GitHubClient gitHubClient = new GitHubClient(new Octokit.ProductHeaderValue("NeosPlusInstaller"));
            Release latestRelease = await gitHubClient.Repository.Release.GetLatest(RepositoryOwner, RepositoryName);
            string latestReleaseUrl = latestRelease.Assets[0].BrowserDownloadUrl;

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

                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (FileStream fileStream = new FileStream(localFilePath, System.IO.FileMode.Create))
                        {
                            await contentStream.CopyToAsync(fileStream);
                        }
                    }
                }

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    statusTextBlock.Text = "NeosPlus downloaded successfully.";
                });

                return true;
            }
            catch (Exception ex)
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    statusTextBlock.Text = $"Failed to download NeosPlus: {ex.Message}";
                    installButton.IsEnabled = true;
                });
                return false;
            }
        }
    }
}
