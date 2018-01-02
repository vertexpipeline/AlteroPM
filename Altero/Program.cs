using System;
using System.IO;
using static Altero.RichConsole;
using Newtonsoft.Json;
using System.Linq;
using Altero.Models;
using System.Collections.Generic;

using AlteroShared.Packaging;
using AlteroShared;
using System.Text.RegularExpressions;


namespace Altero
{
    class Program
    {
        static LocationsInfo _locations;
        static Dictionary<string, string> _i10n;

        static void LoadSettings()
        {
            var settingsFile = File.ReadAllText("settings.json");
            var settings = JsonConvert.DeserializeObject<Altero.Models.SettingsModel>(settingsFile);

            _locations = settings.locations.First(e => e.os == Environment.OSVersion.Platform.ToString());
        }

        static List<Argument> ParseArgs(IEnumerable<string> args)
        {
            string key = "";
            int n = 0;
            var arguments = new List<Argument>();
            foreach (var arg in args) {
                var trimmedArg = arg.Trim();
                if (trimmedArg.StartsWith('/')) {
                    if (key != "")
                        arguments.Add(new Argument { key = key });
                    key = trimmedArg.Substring(1);
                } else {
                    if (key == "") {
                        if (key.Count(c => c == '.' || c == '\\') != 0)
                            arguments.Add(new Argument { key = "FILE", parameter = trimmedArg });
                        else {
                            arguments.Add(new Argument { key = n.ToString(), parameter = trimmedArg });
                            n++;
                        }
                    } else {
                        arguments.Add(new Argument { key = key, parameter = trimmedArg });
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
            _i10n = new Dictionary<string, string>();

            dynamic file = JsonConvert.DeserializeObject(File.ReadAllText("i10n.json"));
            var cult = System.Globalization.CultureInfo.CurrentCulture.Name;

            foreach (dynamic entry in file) {
                dynamic findLang(string name)
                {
                    foreach (var l in entry.values)
                        if (l.lang.Value == name)
                            return l;
                    return null;
                }

                var lang = findLang(cult);
                if (lang == null)
                    lang = findLang("en-Us");

                _i10n.Add(entry.key.Value, lang.value.Value.Replace("\\n", "\n"));
            }

            var temp = new Dictionary<string, string>();

            foreach(KeyValuePair<string, string> entry in _i10n) {
                foreach(Match mt in Regex.Matches(entry.Value, @"%(?<name>\w+)%")) {
                    var name = mt.Groups["name"].Value;
                    if (_i10n.ContainsKey(name)) {
                        temp.Add(entry.Key,entry.Value.Replace(mt.Value, _i10n[name]));
                    }
                }
            }

            _i10n = temp;
        }

        static void Create(string path, string name, PackageVersion ver)
        {

        }

        static void Main(string[] args)
        {
            LoadSettings();
            LoadI10n();
            
            if (args.Length == 0) {
                Write(_i10n["intro"]);
            } else {
                var command = args[0].ToLower();
                var arguments = ParseArgs(args.Skip(1));

                switch (command) {
                    case "create":
                        if (arguments.Count < 1)
                            Write(_i10n["mis_name"]);
                        break;
                }
            }
        }
    }
}