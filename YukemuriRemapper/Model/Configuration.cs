using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace YukemuriRemapper.Model
{
    public class Configuration
    {
        public List<ProcessConfiguration> Configurations { get; set; } = new();


        public Configuration Clone()
        {
            return JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(this));
        }

        public static readonly string DefaultPath = "config.json";
        public static Configuration Load(string path)
        {
            return JsonSerializer.Deserialize<Configuration>(File.ReadAllText(path));
        }

        public static void Save(string path, Configuration config)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(config));
        }
    }

    public class ProcessConfiguration
    {
        public string Name { get; set; }
        public bool IsEnabled { get; set; } = true;

        public string ProcessName { get; set; }

        public List<KeyBind> KeyBinds { get; set; } = new();
    }

    public class KeyBind
    {
        public int From { get; set; }
        public int To { get; set; }
    }

}
