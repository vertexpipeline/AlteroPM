using System;
using System.IO;
using static Altero.RichConsole;
using Newtonsoft.Json;
using System.Linq;
using Altero.Models;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Altero
{
    class Program
    {
        static LocationsInfo _locations;
        static Dictionary<string, string> i10n;

        static void LoadSettings()
        {
            var settingsFile = File.ReadAllText("settings.json");
            var settings = JsonConvert.DeserializeObject<Altero.Models.SettingsModel>(settingsFile);

            _locations = settings.locations.First(e => e.os == Environment.OSVersion.Platform.ToString());
        }

        static List<Argument> ParseArgs(IEnumerable<string> args)
        {
            string key = "";
            var arguments = new List<Argument>();
            foreach (var arg in args) {
                var trimmedArg = arg.Trim();
                if (trimmedArg.StartsWith('/')) {
                    if (key != "")
                        arguments.Add(new Argument { key = key });
                    key = trimmedArg.Substring(1);
                }
                else {
                    if (key == "")
                        arguments.Add(new Argument { key = "FILE", parameter = arg });
                    else {
                        arguments.Add(new Argument { key = key, parameter = arg });
                        key = "";
                    }
                }
            }
            if (key != "")
                arguments.Add(new Argument { key = key });
            return arguments;
        }

        static void LoadI10n()
        {
            i10n = new Dictionary<string, string>();

            dynamic file = JsonConvert.DeserializeObject(File.ReadAllText("i10n.json"));
            var cult = System.Globalization.CultureInfo.CurrentCulture.Name;
            
            dynamic findLang(string name)
            {
                foreach (var l in file)
                    if (l.lang.Value == name)
                        return l;
                return null; 
            }

            var lang = findLang(cult);
            if (lang == null)
                lang = findLang("en-Us");

            foreach (dynamic entry in lang.entries) {
                i10n.Add(entry.key.Value, entry.value.Value.Replace("\\n","\n"));
            }
        }

        static void Main(string[] args)
        {
            LoadSettings();
            LoadI10n();
            
            if (args.Length == 0) {
                Write(i10n["intro"]);
            }
            else {
                var command = args[0];
                var arguments = ParseArgs(args.Skip(1));
            }
        }
    }
}