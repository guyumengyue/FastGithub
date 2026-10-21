using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace FastGithub.UI
{
    internal sealed class UiSettings
    {
        private static readonly string settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FastGithub", "ui-settings.json");
        public bool AutoStart { get; set; } = true;
        public bool EnablePacketIntercept { get; set; } = true;

        public static UiSettings Load()
        {
            if (File.Exists(settingsPath))
                return JsonConvert.DeserializeObject<UiSettings>(File.ReadAllText(settingsPath)) ?? new UiSettings();
            return new UiSettings { EnablePacketIntercept = ReadCoreConfiguration()?["EnablePacketIntercept"]?.Value<bool>() ?? true };
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
            var temporary = settingsPath + ".tmp";
            File.WriteAllText(temporary, JsonConvert.SerializeObject(this, Formatting.Indented));
            if (File.Exists(settingsPath)) File.Replace(temporary, settingsPath, null);
            else File.Move(temporary, settingsPath);
        }

        public static int ReadProxyPort() => ReadCoreConfiguration()?["HttpProxyPort"]?.Value<int>() ?? 38457;
        private static JObject? ReadCoreConfiguration()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            return File.Exists(path) ? JObject.Parse(File.ReadAllText(path))["FastGithub"] as JObject : null;
        }
    }
}
