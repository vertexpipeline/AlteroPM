using System;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using Altero.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO.Compression;

using AlteroShared.Packaging;
using AlteroShared;
using Altero.Repositories;
using AlteroShared.Packaging;

using System.Reflection;

using static Altero.RichConsole;

namespace Altero
{
    class Program
    {
        static LocationsInfo _locations;
        static SettingsModel _settings;
        static Dictionary<string, string> _i10n;
        static string _path = new System.IO.FileInfo(new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath).Directory.FullName;

        static void LoadSettings()
        {
            var settingsFile = File.ReadAllText(_path+"\\settings.json");
            var settings = JsonConvert.DeserializeObject<Altero.Models.SettingsModel>(settingsFile);

            _locations = settings.locations.First(e => e.os == Environment.OSVersion.Platform.ToString());
            _settings = settings;
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
                        if (Regex.IsMatch(arg, _settings.path_regex))
                            arguments.Add(new Argument { key = "PATH", parameter = trimmedArg });
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

            dynamic file = JsonConvert.DeserializeObject(File.ReadAllText(_path+"\\i10n.json"));
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
                var formed = entry.Value;
                foreach(Match mt in Regex.Matches(entry.Value, @"%(?<name>\w+)%")) {
                    var name = mt.Groups["name"].Value;
                    if (_i10n.ContainsKey(name)) {
                        formed = formed.Replace(mt.Value, _i10n[name]);
                    } 
                }
                temp.Add(entry.Key, formed);
            }

            _i10n = temp;
        }

        static void Create(string path, string name, PackageVersion ver)
        {
            if (Directory.Exists(path)) {
                try {
                    var dir = $"{path}\\{name}~{ver}";
                    
                    Directory.CreateDirectory(dir);
                    var meta = JsonConvert.SerializeObject(new PackageMeta { name = name, version = ver}, Formatting.Indented);
                    File.WriteAllText(dir+"\\metadata.json", meta);
                }catch(InvalidOperationException ex) {
                    Write(_i10n["cannot_create"]);
                }
            } else {
                Write(_i10n["dir_not_exists"]);
            }
        }

        static PackageInfo AssemblyPackage(PackageMeta meta, string path, string root="\\root")
        {
            var pkg = new PackageInfo();
            pkg.meta = meta;

            var pkgName = meta.name + meta.version.ToString();
            var file = Path.GetTempPath() + "\\" + Path.GetRandomFileName();

            ZipFile.CreateFromDirectory(path, file);

            pkg.packagePath = file;
            return pkg;
        }

        static IPackagesRepository LoadRepo(List<Argument> args)
        {
            IPackagesRepository repo = OnlineRepository.Load();
            foreach (var arg in args) {
                if (arg.key == "r") {
                    var localRepo = LocalRepository.Load(arg.parameter);
                    if (localRepo != null)
                        repo = localRepo;
                }
            }
            return repo;
        }
        static (string, PackageVersion) ParseCompositeName(List<Argument> args, int pos)
        {
            var parts = new int[] { 0, 0, 0, 0 };
            var name = "";
            try {
                var nameSplit = args.First(arg => arg.key == pos.ToString()).parameter.Split('~');
            //read version
            
                nameSplit[1].Split('.').ForEach((part, i) =>
                {
                    parts[i] = int.Parse(part);
                });
                name = nameSplit[0];
            }
            catch (Exception ex) {
                return (null, null);
            }
            var version = new PackageVersion(parts[0], parts[1], parts[2], parts[3]);
            return (name, version);
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
                    case "create": {
                            if (arguments.Count(arg => arg.key == "0") < 1)
                                Write(_i10n["mis_name"]);
                            else {
                                var path = "";
                                arguments.ForEach((arg) =>
                                {
                                    if (arg.key == "PATH")
                                        path = arg.parameter;
                                });

                                var (name, version) = ParseCompositeName(arguments, 0);

                                Create(path, name, version);
                            }
                            break;
                        }
                    case "make": {
                            var repo = LoadRepo(arguments);
                            
                            var pathArg = arguments.FirstOrDefault(a => a.key == "PATH");
                            var fileArg = arguments.FirstOrDefault(a => a.key == "0");

                            if (pathArg != default(Argument)) {
                                var pkg = default(PackageInfo);
                                try {
                                    var path = pathArg.parameter;
                                    var metaFile = File.ReadAllText(path + "\\metadata.json");
                                    var meta = JsonConvert.DeserializeObject<PackageMeta>(metaFile);

                                    if(fileArg != default(Argument)) {
                                        //TODO add logging support
                                    } else {
                                        if (Directory.Exists(path + "\\root")) {
                                            var root = path + "\\root";
                                            void addFolder(string relPath)
                                            {
                                                meta.files.Add(new FileMeta { type = "folder", destinationLocation = "%root%" + relPath });
                                                var dInfo = new DirectoryInfo(root + "\\" + relPath);
                                                foreach(var file in dInfo.GetFiles()) {
                                                    meta.files.Add(new FileMeta() { rootLocation = "root\\"+relPath, destinationLocation = "%root%" + relPath });
                                                    Write($"{_i10n["adding"]} {file.FullName}\n");
                                                }
                                                foreach(var folder in dInfo.GetDirectories()) {
                                                    addFolder(relPath+"\\"+folder.Name);
                                                }
                                            }
                                            WriteLine(_i10n["fetching_root"]);
                                            addFolder("");
                                        }
                                    }
                                    WriteLine(_i10n["assembling"]);
                                    pkg = AssemblyPackage(meta, pathArg.parameter);
                                }
                                catch (FileNotFoundException ex) {
                                    Write(_i10n["meta_not_exists"]);
                                }
                                catch (Exception ex) {
                                    Write(ex);
                                    Write(_i10n["package_not_found"]);
                                }

                                if (pkg != default(PackageInfo)) {
                                    repo.Send(pkg);
                                }
                                
                            } else {
                                Write(_i10n["dir_not_spec"]);
                            }
                            break;
                        }
                    case "makerepo": {
                            var path = arguments.FirstOrDefault(a => a.key == "PATH");
                            if(path != default(Argument)) {
                                Directory.CreateDirectory(path.parameter);
                                File.WriteAllText(path.parameter + "\\meta.json", JsonConvert.SerializeObject(new LocalRepositoryInfo(), Formatting.Indented));
                                WriteLine("<green>Local repository was created</>");
                            } else {
                                WriteLine("<red>Write path</>");
                            }
                            break;
                        }
                    case "install": {
                            var repo = LoadRepo(arguments);

                            var n = 0;
                            

                            break;
                        }
                }
            }
        }
    }
}