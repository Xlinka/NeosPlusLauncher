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

        private static readonly string LogFilePath = "log.txt";

        public static async Task<bool> DownloadAndInstallNeosPlus(string neosPath, string neosPlusDirectory, TextBlock statusTextBlock, Button installButton)
        {
            string dllFileName = "NeosPlus.dll";
            string localFilePath = Path.Combine(neosPlusDirectory, dllFileName);

            Release latestRelease = await GetLatestRelease();
            string latestReleaseUrl = latestRelease.Assets[0].BrowserDownloadUrl;

            Log("Downloading NeosPlus...");
            await UpdateStatusText(statusTextBlock, "Downloading NeosPlus...");

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(latestReleaseUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        Log("Failed to download NeosPlus.");
                        await UpdateStatusText(statusTextBlock, "Failed to download NeosPlus.");
                        EnableInstallButton(installButton);
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

                Log("NeosPlus downloaded successfully.");
                await UpdateStatusText(statusTextBlock, "NeosPlus downloaded successfully.");

                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to download NeosPlus: {ex.Message}");
                await UpdateStatusText(statusTextBlock, $"Failed to download NeosPlus: {ex.Message}");
                EnableInstallButton(installButton);
                return false;
            }
        }

        private static async Task<Release> GetLatestRelease()
        {
            GitHubClient gitHubClient = new GitHubClient(new Octokit.ProductHeaderValue("NeosPlusInstaller"));
            return await gitHubClient.Repository.Release.GetLatest(RepositoryOwner, RepositoryName);
        }

        private static void Log(string message)
        {
            File.AppendAllText(LogFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
        }

        private static void LogError(string message)
        {
            File.AppendAllText(LogFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] - {message}\n");
        }

        private static Task UpdateStatusText(TextBlock statusTextBlock, string text)
        {
            var tcs = new TaskCompletionSource<object>();

            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                statusTextBlock.Text = text;
                tcs.SetResult(null);
            });

            return tcs.Task;
        }

        private static Task EnableInstallButton(Button installButton)
        {
            var tcs = new TaskCompletionSource<object>();

            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                installButton.IsEnabled = true;
                tcs.SetResult(null);
            });

            return tcs.Task;
        }
    }
}
