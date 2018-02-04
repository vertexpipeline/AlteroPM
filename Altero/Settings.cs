using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Reflection;
using System.Linq;
using AlteroShared.Packaging;
using Altero.Models;

namespace Altero
{
    class Settings
    {
        static string _path = new System.IO.FileInfo(new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath).Directory.FullName;
        public static LocationsInfo Locations { get; set; } = new LocationsInfo();
        public static string Path => _path;

        private static SettingsModel _file = new SettingsModel();

        public static string PathRegex => _file.path_regex;

        public static string LocalizationFile => Path + "\\" + _file.localization_path;

        public static List<PackageMeta> Installed => _file.installed;

        public static string ServerURL => _file.serverURL;

        static Settings()
        {
            var settingsFile = File.ReadAllText(_path + "\\settings.json");
            var settings = JsonConvert.DeserializeObject<Altero.Models.SettingsModel>(settingsFile);

            _file = settings;
            Locations = settings.locations.First(e => e.os == Environment.OSVersion.Platform.ToString());
        }

        public static void Save()
        {
            File.WriteAllText(_path + "\\settings.json", JsonConvert.SerializeObject(_file));
        }
    }
}
