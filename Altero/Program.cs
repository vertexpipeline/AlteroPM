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
using System.Security.Permissions;

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
            var settingsFile = File.ReadAllText(_path + "\\settings.json");
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
                }
                else {
                    if (key == "") {
                        if (Regex.IsMatch(arg, _settings.path_regex))
                            arguments.Add(new Argument { key = "PATH", parameter = trimmedArg });
                        else {
                            arguments.Add(new Argument { key = n.ToString(), parameter = trimmedArg });
                            n++;
                        }
                    }
                    else {
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

            dynamic file = JsonConvert.DeserializeObject(File.ReadAllText(_path + "\\i10n.json"));
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

            foreach (KeyValuePair<string, string> entry in _i10n) {
                var formed = entry.Value;
                foreach (Match mt in Regex.Matches(entry.Value, @"%(?<name>\w+)%")) {
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
                    var meta = JsonConvert.SerializeObject(new PackageMeta { name = name, version = ver }, Formatting.Indented);
                    File.WriteAllText(dir + "\\metadata.json", meta);
                }
                catch (InvalidOperationException ex) {
                    Write(_i10n["cannot_create"]);
                }
            }
            else {
                Write(_i10n["dir_not_exists"]);
            }
        }

        static PackageInfo AssemblyPackage(PackageMeta meta, string path, string root = "\\root")
        {
            var pkg = new PackageInfo();
            pkg.meta = meta;

            var file = Path.GetTempPath() + "\\" + Path.GetRandomFileName();

            File.WriteAllText(path + "\\metadata.json", JsonConvert.SerializeObject(pkg.meta, Formatting.Indented));

            if (File.Exists(file))
                File.Delete(file);

            ZipFile.CreateFromDirectory(path, file);

            pkg.packagePath = file;
            return pkg;
        }

        static Dictionary<string, string> MakeFiller(PackageMeta meta, IPackagesRepository repo)
        {
            var filler = new Dictionary<string, string>();
            filler.Add("os_drive", Path.GetPathRoot(Environment.SystemDirectory));
            filler.Add("root", _locations.apps.Fill(filler) + "\\" + meta.name);
            filler.Add("user", Path.GetPathRoot(Environment.SystemDirectory) + "//users//" + Environment.UserName);
            if(repo != null && repo.GetType() == typeof(LocalRepository))
                filler.Add("std_repo", (repo as LocalRepository).Path);
            return filler;
        }

        static bool InstallPackage(PackageInfo info, IPackagesRepository repo)
        {
            foreach (var iP in _settings.installed) {
                if (info.meta.Equals(iP)) {
                    WriteLine("<yellow>Package already installed</>");
                    return true;
                }
                else if (info.meta.name == iP.name) {
                    if (info.meta.version.CompareTo(iP.version) == 1) {
                        WriteLine("<Cyan>Идет обновление пакета</>");
                        //TODO updating
                        return true;
                    }
                    else {
                        WriteLine("<green>Latest version of package already installed</>");
                        return false;
                    }
                }
            } //listen installed

            WriteLine("<cyan>Installing started...</>");

            WriteLine("<cyan>Resolving dependencies...</>");

            var resolved = new List<PackageInfo>();

            foreach(var dep in info.meta.dependencies) {
                var (name, ver) = ParseCompositeName(dep.package);
                var pkg = repo.Get(name, ver);
                if(pkg != default(PackageInfo)) {
                    WriteLine($"<cyan>Installing dependency {pkg.meta.name}</>");
                    if(InstallPackage(pkg, repo)) {
                        WriteLine("<green>Resolved</>");
                        resolved.Add(pkg);
                    }
                    else {
                        WriteLine("<red>Can't resolve dependencies. Revert changes</>");
                        resolved.ForEach(p => UninstallPackage(p.meta.name));
                        return false;
                    }
                }
            }

            var tempPath = _path + "\\installingTEMP";
            
            Directory.CreateDirectory(tempPath);
            WriteLine("<cyan>Extracting...</>");
            ZipFile.ExtractToDirectory(info.packagePath, tempPath);
            WriteLine("<cyan>Distributing...</>");

            var filler = MakeFiller(info.meta, repo);
            if (info.meta.type == "app") {
                var appDir = "%root%".Fill(filler);
                Directory.CreateDirectory(appDir);

                try {
                    foreach (var entry in info.meta.files.Where(pkg => pkg.type == "folder")) {
                        if (entry.destinationLocation != "%root%\\") {
                            var filledPath = entry.destinationLocation.Fill(filler);
                            Directory.CreateDirectory(filledPath);
                        }
                    }

                    foreach (var entry in info.meta.files.Where(pkg => pkg.type == "file")) {
                        File.Move(tempPath + "\\" + entry.rootLocation + "\\" + entry.name, entry.destinationLocation.Fill(filler) + "\\" + entry.name);
                    }
                }
                catch (Exception ex) {
                    return false;
                }
                finally {
                    Directory.Delete(tempPath, true);
                }

                foreach (var launcher in info.meta.launchers) {
                    if (launcher.isShortcut) {
                        if (File.Exists(appDir + "\\" + launcher.icon) && File.Exists(appDir + "\\" + launcher.file)) {
                            File.WriteAllText("%user%\\desktop\\".Fill(filler) + launcher.name + ".url", $"[InternetShortcut]\n" +
                                $"URL=file:///{appDir + "\\" + launcher.file}\n" +
                                $"IconIndex=0\n" +
                                $"IconFile={appDir + "\\" + launcher.icon}");
                        }
                        else {
                            WriteLine("Icon not found");
                        }
                    }
                    else {
                        var shortcut = "%os_drive%\\Shortcuts\\".Fill(filler) + launcher.name + ".bat";
                        File.WriteAllText(shortcut,
                                    "@echo off \n" +
                                    $@"{$"%root%\\{launcher.file}".Fill(filler)} %*");
                    }
                }

                _settings.installed.Add(info.meta);

                WriteLine("<green>Installed info updated</>");

            }
            else {//installing libs
                try {
                    foreach (var entry in info.meta.files.Where(pkg => pkg.type == "file")) {
                        var dest = entry.destinationLocation.Fill(filler) + "\\" + entry.name;
                        var source = tempPath + "\\" + entry.rootLocation + "\\" + entry.name;
                        if (!File.Exists(dest) && File.Exists(source))
                            File.Move(source, dest);
                    }
                    _settings.installed.Add(info.meta);
                }
                finally {
                    Directory.Delete(tempPath, true);
                }
            }
            return true;
        }

        static bool UninstallPackage(string name)
        {
            foreach (var installedPackage in _settings.installed) {
                if (name == installedPackage.name) {
                    WriteLine("<cyan>Uninstalling started...</>");

                    WriteLine("<cyan>Purging...</>");

                    var filler = MakeFiller(installedPackage, null);

                    var appDir = "%root%".Fill(filler);
                    //Directory.CreateDirectory(appDir); maybe valuable
                    if (installedPackage.type == "app") {
                        try {
                            foreach (var entry in installedPackage.files.Where(pkg => pkg.type == "folder")) {
                                if (entry.destinationLocation != "%root%\\") {
                                    var filledPath = entry.destinationLocation.Fill(filler);
                                    if (Directory.Exists(filledPath))
                                        Directory.Delete(filledPath, true);
                                }
                            }

                            foreach (var entry in installedPackage.files.Where(pkg => pkg.type == "file")) {
                                var file = entry.destinationLocation.Fill(filler) + "\\" + entry.name;
                                if (File.Exists(file))
                                    File.Delete(file);
                            }
                        }
                        catch (Exception ex) {
                            WriteLine("Error while uninstalling");
                            return false;
                        }

                        foreach (var launcher in installedPackage.launchers) {
                            if (launcher.isShortcut) {
                                var shortcut = $"%user%\\desktop\\{launcher.name}.url".Fill(filler);
                                if (File.Exists(shortcut))
                                    File.Delete(shortcut);
                            }
                            else {
                                var shortcut = "%os_drive%\\Shortcuts\\".Fill(filler) + launcher.name + ".bat";
                                if (File.Exists(shortcut)) {
                                    File.Delete(shortcut);
                                }
                            }
                        }
                        Directory.Delete(appDir, true);
                    }
                    else {
                        foreach (var entry in installedPackage.files.Where(pkg => pkg.type == "file")) {
                            var file = entry.destinationLocation.Fill(filler) + "\\" + entry.name;
                            if(File.Exists(file))
                                File.Delete(entry.destinationLocation.Fill(filler) + "\\" + entry.name);
                        }
                    }
                    _settings.installed.Remove(installedPackage);

                    return true;
                }
            } //listen installed
            return false;
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
        static (string, PackageVersion) ParseCompositeName(string package)
        {
            var parts = new int[] { 0, 0, 0, 0 };
            var name = "";
            try {
                var nameSplit = package.Split('~');
                //read version
                name = nameSplit[0];
                nameSplit[1].Split('.').ForEach((part, i) =>
                {
                    parts[i] = int.Parse(part);
                });
            }
            catch (Exception ex) {
                return (name, null);
            }
            var version = new PackageVersion(parts[0], parts[1], parts[2], parts[3]);
            return (name, version);
        }
        static void SaveSettings() => File.WriteAllText(_path + "\\settings.json", JsonConvert.SerializeObject(_settings));

        static void Main(string[] args)
        {
            LoadSettings();
            LoadI10n();

            if (args.Length == 0) {
                Write(_i10n["intro"]);
            }
            else {
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

                                foreach (var arg in arguments) {
                                    if (arg.key.All(c => Char.IsDigit(c))) {
                                        var (name, version) = ParseCompositeName(arg.parameter);

                                        Create(path, name, version);
                                    }
                                }

                            }
                            break;
                        }
                    case "make": {
                            var repo = LoadRepo(arguments);

                            var pathArg = arguments.FirstOrDefault(a => a.key == "PATH");
                            var executableFile = arguments.FirstOrDefault(a => a.key == "0");

                            if (pathArg != default(Argument)) {
                                var pkg = default(PackageInfo);
                                try {
                                    var pathOfPackage = pathArg.parameter;
                                    var metaFile = File.ReadAllText(pathOfPackage + "\\metadata.json");
                                    var packageMeta = JsonConvert.DeserializeObject<PackageMeta>(metaFile);
                                    packageMeta.files.Clear();

                                    if (executableFile != default(Argument)) {
                                        //TODO add logging support
                                    }
                                    else { //for packaging
                                        File.WriteAllText(_path + "\\tempMeta.json", JsonConvert.SerializeObject(packageMeta, Formatting.Indented));
                                        if (Directory.Exists(pathOfPackage + "\\root") && packageMeta.type == "app") {
                                            var rootPath = pathOfPackage + "\\root";
                                            void addFolder(DirectoryInfo lookingDir)
                                            {
                                                var relativePath = Path.GetRelativePath(rootPath, lookingDir.FullName);
                                                if (relativePath == ".")
                                                    relativePath = "";

                                                packageMeta.files.Add(new FileMeta
                                                {
                                                    type = "folder",
                                                    name = lookingDir.Name,
                                                    destinationLocation = "%root%\\" + relativePath
                                                });

                                                foreach (var file in lookingDir.GetFiles()) {
                                                    packageMeta.files.Add(new FileMeta()
                                                    {
                                                        rootLocation = "root\\" + relativePath,
                                                        name = file.Name,
                                                        destinationLocation = "%root%\\" + relativePath
                                                    });

                                                    Write($"{_i10n["adding"]} {file.FullName}\n");
                                                }
                                                foreach (var folder in lookingDir.GetDirectories()) {
                                                    addFolder(folder);
                                                }
                                            }
                                            WriteLine(_i10n["fetching_root"]);

                                            addFolder(new DirectoryInfo(rootPath));
                                        }
                                        if (Directory.Exists(pathOfPackage + "\\libs") && packageMeta.type == "lib") {
                                            foreach (var lib in new DirectoryInfo(pathOfPackage + "\\libs").GetFiles()) {
                                                if (lib.Extension.ToLower() == ".dll") {
                                                    foreach (var dest in _locations.libs) {
                                                        packageMeta.files.Add(new FileMeta
                                                        {
                                                            rootLocation = "libs\\",
                                                            destinationLocation = dest,
                                                            name = lib.Name,
                                                            type = "file"
                                                        });
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    WriteLine(_i10n["assembling"]);
                                    pkg = AssemblyPackage(packageMeta, pathArg.parameter);
                                    File.WriteAllText(pathArg.parameter + "\\metadata.json", File.ReadAllText(_path + "\\tempMeta.json"));
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

                            }
                            else {
                                Write(_i10n["dir_not_spec"]);
                            }
                            break;
                        }
                    case "makerepo": {
                            var path = arguments.FirstOrDefault(a => a.key == "PATH");
                            if (path != default(Argument)) {
                                Directory.CreateDirectory(path.parameter);
                                File.WriteAllText(path.parameter + "\\meta.json", JsonConvert.SerializeObject(new LocalRepositoryInfo(), Formatting.Indented));
                                WriteLine("<green>Local repository was created</>");
                            }
                            else {
                                WriteLine("<red>Write path</>");
                            }
                            break;
                        }
                    case "install": {
                            var repo = LoadRepo(arguments);

                            var installedCount = 0;

                            foreach (var arg in arguments) {
                                if (arg.key.All(c => Char.IsDigit(c))) {
                                    var (name, ver) = ParseCompositeName(arg.parameter);
                                    var pkg = repo.Get(name, ver);
                                    if (pkg != null) {
                                        WriteLine($"<cyan>Installing <darkcyan>{pkg.meta.name}</></>");
                                        if (InstallPackage(pkg, repo)) {
                                            WriteLine($"<green>Success</>");
                                            SaveSettings();
                                        }
                                        else
                                            WriteLine($"<red>Not installed</>");
                                    }
                                    else {
                                        WriteLine($"<Yellow>Package <darkyellow>{name}</> not found</>");
                                    }
                                }
                            }

                            break;
                        }
                    case "uninstall": {
                            foreach (var arg in arguments) {
                                if (arg.key.All(c => Char.IsDigit(c))) {
                                    var (name, ver) = ParseCompositeName(arg.parameter);
                                    if (UninstallPackage(name)) {
                                        WriteLine($"<green>{name} deleted.</>");
                                    }
                                    else {
                                        WriteLine($"<red>{name} not installed");
                                    }
                                }
                            }
                            break;
                        }
                    case "delete": {
                            var repo = LoadRepo(arguments);
                            foreach (var arg in arguments) {
                                if (arg.key.All(c => Char.IsDigit(c))) {
                                    var (name, ver) = ParseCompositeName(arg.parameter);
                                    repo.Delete(name, ver);
                                }
                            }
                            break;
                        }
                }
                SaveSettings();
            }
        }
    }
}