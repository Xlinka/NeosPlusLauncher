using System.IO;
using System.Text.Json;

namespace NeosPlusLauncher.ViewModels
{
    public class Config
    {
        private const string ConfigFilePath = "./Assets/Config.json";

        public string LauncherArguments { get; set; }
        public string CustomInstallDir { get; set; }

        public static Config LoadConfig()
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

        public void SaveConfig()
        {
            string json = JsonSerializer.Serialize(this);
            File.WriteAllText(ConfigFilePath, json);
        }
    }
}
