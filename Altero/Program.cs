﻿using System;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using Altero;
using Altero.Tools;
using Altero.Models;
using AlteroShared.Packaging;
using AlteroShared;
using Altero.Repositories;
using System.Net;
using static Altero.Tools.RichConsole;

namespace Altero
{
    class Program
    {
        static void Create(string path, string name, PackageVersion ver)
        {
            if (Directory.Exists(path)) {
                try {
                    var dir = $"{path}\\{name}~{ver}";

                    Directory.CreateDirectory(dir);
                    var meta = JsonConvert.SerializeObject(new PackageMeta { name = name, version = ver }, Formatting.Indented);
                    var instMeta = JsonConvert.SerializeObject(new InstallationInfo { owner = name}, Formatting.Indented);
                    File.WriteAllText(dir + "\\metadata.json", meta);
                    Console.WriteLine(instMeta);
                    File.WriteAllText(dir + "\\installation.json", instMeta);
                }
                catch (InvalidOperationException ex) {
                    WriteLocal("cannot_create");
                }
            }
            else {
                WriteLocal("dir_not_exists");
            }
        }

        static PackageInfo AssemblyPackage(PackageMeta meta, InstallationInfo instMeta, string path, string root = "\\root")
        {
            var pkg = new PackageInfo();
            pkg.meta = meta;

            var file = Path.GetTempPath() + "\\" + Path.GetRandomFileName();

            File.WriteAllText(path + "\\metadata.json", JsonConvert.SerializeObject(pkg.meta, Formatting.Indented));

            if (File.Exists(file))
                File.Delete(file);

            File.WriteAllText(path + "\\installation.json", JsonConvert.SerializeObject(instMeta, Formatting.Indented));

            ZipFile.CreateFromDirectory(path, file);
            Console.WriteLine("Packed "+file);
            pkg.packagePath = file;
            return pkg;
        }

        static Dictionary<string, string> MakeFiller(PackageMeta meta, IPackagesRepository repo)
        {
            var filler = new Dictionary<string, string>();
            filler.Add("os_drive", Path.GetPathRoot(Environment.SystemDirectory));
            filler.Add("root", Settings.Locations.apps.Fill(filler) + "\\" + meta.name);
            filler.Add("user", Path.GetPathRoot(Environment.SystemDirectory) + "//users//" + Environment.UserName);
            if(repo != null && repo.GetType() == typeof(LocalRepository))
                filler.Add("std_repo", (repo as LocalRepository).Path);
            return filler;
        }

        static bool InstallPackage(PackageInfo info, IPackagesRepository repo)
        {
            foreach (var iP in Settings.Installed) {
                if (info.meta.Equals(iP)) {
                    WriteLocalLine("already_inst");
                    return true;
                }
                else if (info.meta.name == iP.name) {
                    if (info.meta.version.CompareTo(iP.version) == 1) {
                        WriteLocalLine("updin_pkg");
                        //TODO updating
                        return true;
                    }
                    else {
                        WriteLocalLine("lat_exists");
                        return false;
                    }
                }
            } //listen installed

            WriteLocalLine("inst_started");

            WriteLocalLine("resolving");

            var resolved = new List<PackageInfo>();

            foreach(var dep in info.meta.dependencies) {
                var (name, ver) = ParseCompositeName(dep.package);
                var pkg = repo.Get(name, ver);
                if(pkg != default(PackageInfo)) {
                    WriteLocalLine("inst_dep", pkg.meta.name);
                    if(InstallPackage(pkg, repo)) {
                        WriteLocalLine("resolved");
                        resolved.Add(pkg);
                    }
                    else {
                        WriteLocalLine("cant_resolve");
                        resolved.ForEach(p => UninstallPackage(p.meta.name));
                        return false;
                    }
                }
            }

            var tempPath = Settings.Path + "\\installingTEMP";
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
            Directory.CreateDirectory(tempPath);
            WriteLocalLine("extracting");
            ZipFile.ExtractToDirectory(info.packagePath, tempPath);

            WriteLocalLine("distributing");

            var instMeta = JsonConvert.DeserializeObject<InstallationInfo>(System.IO.File.ReadAllText(tempPath + "\\installation.json"));

            var filler = MakeFiller(info.meta, repo);
            if (info.meta.type == "app") {
                var appDir = "%root%".Fill(filler);
                Directory.CreateDirectory(appDir);

                try {
                    foreach (var entry in instMeta.files.Where(pkg => pkg.type == "folder")) {
                        if (entry.destinationLocation != "%root%\\") {
                            var filledPath = entry.destinationLocation.Fill(filler);
                            Directory.CreateDirectory(filledPath);
                        }
                    }

                    foreach (var entry in instMeta.files.Where(pkg => pkg.type == "file")) {
                        File.Move(tempPath + "\\" + entry.rootLocation + "\\" + entry.name, entry.destinationLocation.Fill(filler) + "\\" + entry.name);
                    }
                }
                catch (Exception ex) {
                    return false;
                }
                finally {
                    File.Move(tempPath + "\\installation.json", appDir+"\\installation.json");
                    Directory.Delete(tempPath, true);
                }

                foreach (var launcher in instMeta.launchers) {
                    if (launcher.isShortcut) {
                        if (File.Exists(appDir + "\\" + launcher.icon) && File.Exists(appDir + "\\" + launcher.file)) {
                            File.WriteAllText("%user%\\desktop\\".Fill(filler) + launcher.name + ".url", $"[InternetShortcut]\n" +
                                $"URL=file:///{appDir + "\\" + launcher.file}\n" +
                                $"IconIndex=0\n" +
                                $"IconFile={appDir + "\\" + launcher.icon}");
                        }
                        else {
                            WriteLocalLine("Icon not found");
                        }
                    }
                    else {
                        var shortcut = "%os_drive%\\Shortcuts\\".Fill(filler) + launcher.name + ".bat";
                        File.WriteAllText(shortcut,
                                    "@echo off \n" +
                                    $@"{$"%root%\\{launcher.file}".Fill(filler)} %*");
                    }
                }

                Settings.Installed.Add(info.meta);

                WriteLocalLine("inst_inf_upd");

            }
            else {//installing libs
                try {
                    foreach (var entry in instMeta.files.Where(pkg => pkg.type == "file")) {
                        var dest = entry.destinationLocation.Fill(filler) + "\\" + entry.name;
                        var source = tempPath + "\\" + entry.rootLocation + "\\" + entry.name;
                        if (!File.Exists(dest) && File.Exists(source))
                            File.Move(source, dest);
                    }
                    Settings.Installed.Add(info.meta);
                }
                finally {
                    Directory.Delete(tempPath, true);
                }
            }
            return true;
        }

        static bool UninstallPackage(string name)
        {
            foreach (var installedPackage in Settings.Installed) {
                if (name == installedPackage.name) {
                    WriteLocalLine("uninstalling");

                    WriteLocalLine("purging");

                    var filler = MakeFiller(installedPackage, null);

                    var appDir = "%root%".Fill(filler);
                    var instMeta = JsonConvert.DeserializeObject<InstallationInfo>(System.IO.File.ReadAllText(appDir + "\\installation.json"));

                    //Directory.CreateDirectory(appDir); maybe valuable
                    if (installedPackage.type == "app") {
                        try {
                            foreach (var entry in instMeta.files.Where(pkg => pkg.type == "folder")) {
                                if (entry.destinationLocation != "%root%\\") {
                                    var filledPath = entry.destinationLocation.Fill(filler);
                                    if (Directory.Exists(filledPath))
                                        Directory.Delete(filledPath, true);
                                }
                            }

                            foreach (var entry in instMeta.files.Where(pkg => pkg.type == "file")) {
                                var file = entry.destinationLocation.Fill(filler) + "\\" + entry.name;
                                if (File.Exists(file))
                                    File.Delete(file);
                            }
                        }
                        catch (Exception ex) {
                            WriteLocalLine("error_w_inst");
                            return false;
                        }

                        foreach (var launcher in instMeta.launchers) {
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
                        foreach (var entry in instMeta.files.Where(pkg => pkg.type == "file")) {
                            var file = entry.destinationLocation.Fill(filler) + "\\" + entry.name;
                            if(File.Exists(file))
                                File.Delete(entry.destinationLocation.Fill(filler) + "\\" + entry.name);
                        }
                    }
                    Settings.Installed.Remove(installedPackage);

                    return true;
                }
            } //listen installed
            return false;
        }

        static IPackagesRepository LoadRepo(ArgumentsList args, Guid key)
        {
            IPackagesRepository repo = OnlineRepository.Load(new Uri(Settings.ServerURL), key);
            
            foreach (var arg in args.Parameters) {
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
            var name = "";
            PackageVersion ver;
            try {
                var nameSplit = package.Split('~');
                //read version
                name = nameSplit[0];
                if (nameSplit.Length == 2)
                    ver = PackageVersion.Parse(nameSplit[1]);
                else
                    ver = null;
            }
            catch (Exception ex) {
                return (name, null);
            }
            
            return (name, ver);
        }

        static void Main(string[] args)
        {
            if (args.Length == 0) {
                WriteLocal("intro");
            }
            else {
                var command = args[0].ToLower();
                var arguments = new ArgumentsList(args.Skip(1));

                switch (command) {
                    case "create": {
                            if (arguments.Items.Count == 0)
                                WriteLocal("mis_name");
                            else {
                                var path = "";
                                var pathArg = arguments.Pathes.FirstOrDefault();
                                if (pathArg != null)
                                    path = pathArg.parameter;

                                foreach (var arg in arguments.Items) {
                                        var (name, version) = ParseCompositeName(arg.parameter);

                                        Create(path, name, version);
                                }

                            }
                            break;
                        }
                    case "make": {
                            var key = arguments.Parameters.FirstOrDefault(p => p.key == "key");
                            Guid accessKey = Guid.Empty;
                            if (key != null)
                                if(Guid.TryParse(key.parameter, out var k)) {
                                    accessKey = k;
                                }

                            var repo = LoadRepo(arguments, accessKey);

                            var pathArg = arguments.Pathes.FirstOrDefault();
                            var executableFile = arguments.Items.FirstOrDefault();

                            if (pathArg != default(Argument)) {
                                var pkg = default(PackageInfo);
                                try {
                                    var pathOfPackage = pathArg.parameter;
                                    var metaFile = File.ReadAllText(pathOfPackage + "\\metadata.json");
                                    var instMetaFile = File.ReadAllText(pathOfPackage + "\\installation.json");
                                    var packageMeta = JsonConvert.DeserializeObject<PackageMeta>(metaFile);
                                    var instMeta = JsonConvert.DeserializeObject<InstallationInfo>(instMetaFile);
                                    instMeta.files = new List<FileMeta>();

                                    if (executableFile != default(Argument)) {
                                        //TODO add logging support
                                    }
                                    else { //for packaging
                                        File.WriteAllText(Settings.Path + "\\tempMeta.json", JsonConvert.SerializeObject(packageMeta, Formatting.Indented));
                                        if (Directory.Exists(pathOfPackage + "\\root") && packageMeta.type == "app") {
                                            var rootPath = pathOfPackage + "\\root";
                                            void addFolder(DirectoryInfo lookingDir)
                                            {
                                                var relativePath = Path.GetRelativePath(rootPath, lookingDir.FullName);
                                                if (relativePath == ".")
                                                    relativePath = "";

                                                instMeta.files.Add(new FileMeta
                                                {
                                                    type = "folder",
                                                    name = lookingDir.Name,
                                                    destinationLocation = "%root%\\" + relativePath
                                                });

                                                foreach (var file in lookingDir.GetFiles()) {
                                                    instMeta.files.Add(new FileMeta()
                                                    {
                                                        rootLocation = "root\\" + relativePath,
                                                        name = file.Name,
                                                        destinationLocation = "%root%\\" + relativePath
                                                    });

                                                    WriteLocal("adding", file.FullName);
                                                }
                                                foreach (var folder in lookingDir.GetDirectories()) {
                                                    addFolder(folder);
                                                }
                                            }
                                            WriteLocalLine("fetching_root");

                                            addFolder(new DirectoryInfo(rootPath));
                                        }
                                        if (Directory.Exists(pathOfPackage + "\\libs") && packageMeta.type == "lib") {
                                            foreach (var lib in new DirectoryInfo(pathOfPackage + "\\libs").GetFiles()) {
                                                if (lib.Extension.ToLower() == ".dll") {
                                                    foreach (var dest in Settings.Locations.libs) {
                                                        instMeta.files.Add(new FileMeta
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
                                    WriteLocalLine("assembling");
                                    pkg = AssemblyPackage(packageMeta, instMeta, pathArg.parameter);
                                    
                                    File.WriteAllText(pathArg.parameter + "\\metadata.json", File.ReadAllText(Settings.Path + "\\tempMeta.json"));
                                }
                                catch (FileNotFoundException ex) {
                                    WriteLocal("meta_not_exists");
                                }
                                catch (Exception ex) {
                                    WriteLocal("package_not_found");
                                }

                                if (pkg != default(PackageInfo)) {
                                    
                                    repo.Send(pkg);
                                }
                            }
                            else {
                                WriteLocal("dir_not_spec");
                            }
                            break;
                        }
                    case "makerepo": {
                            var path = arguments.Pathes.FirstOrDefault();
                            if (path != default(Argument)) {
                                Directory.CreateDirectory(path.parameter);
                                File.WriteAllText(path.parameter + "\\meta.json", JsonConvert.SerializeObject(new LocalRepositoryInfo(), Formatting.Indented));
                                WriteLocalLine("repo_created");
                            }
                            else {
                                WriteLocalLine("dir_not_spec");
                            }
                            break;
                        }
                    case "install": {
                            var repo = LoadRepo(arguments, Guid.Empty);

                            var installedCount = 0;

                            foreach (var package in arguments.Items) {
                                
                                    var (name, ver) = ParseCompositeName(package.parameter.Replace("/",""));
                                Console.WriteLine(ver);
                                    var pkg = repo.Get(name, ver);
                                    if (pkg != null) {
                                        WriteLocalLine("installing", pkg.meta.name);
                                        if (InstallPackage(pkg, repo)) {
                                            WriteLocalLine("success");
                                            Settings.Save();
                                        }
                                        else
                                            WriteLocalLine($"not_inst");
                                    }
                                    else {
                                        WriteLocalLine($"pkg_not_found", name);
                                    }
                                }

                            break;
                        }
                    case "uninstall": {
                            foreach(var program in arguments.Items) { 
                                    var (name, ver) = ParseCompositeName(program.parameter);
                                    if (UninstallPackage(name)) {
                                        WriteLocalLine($"deleted", name);
                                    }
                                    else {
                                        WriteLocalLine($"n_inst", name);
                                    }
                                }
                            break;
                        }
                    case "delete": {
                            var repo = LoadRepo(arguments, Guid.Empty);
                            foreach (var program in arguments.Items) {
                                var (name, ver) = ParseCompositeName(program.parameter);
                                    repo.Delete(name, ver);
                                }
                            break;
                        }
                    case "list": {
                            var pos = arguments.Items.FirstOrDefault();

                            var searchPattern = arguments.Parameters.FirstOrDefault(p => p.key == "s");
                            var pattern = ".*";
                            if (searchPattern != default(Argument)) {
                                pattern = searchPattern.parameter;
                            }

                            List<PackageMeta> searchingList = null;
                            if (pos != default(Argument)) {
                                if (pos.parameter.Trim() == "installed") {
                                    searchingList = Settings.Installed.Where(pkg => Regex.IsMatch(pkg.name, pattern)).ToList();
                                }
                                else if(pos.parameter.Trim() == "repo") {
                                    var repo = LoadRepo(arguments, Guid.Empty);
                                    searchingList = repo.Search(pattern);
                                }
                                else {
                                    WriteLocalLine("unk_place");
                                    return;
                                }
                            }
                            else {
                                WriteLocalLine("plc_not_spec");
                                return;
                            }

                            WriteLocalLine("list_res", searchingList.Count);
                            searchingList.ForEach(pkg => WriteLocalLine("list_item", pkg.name, pkg.version));

                            break;
                        }
                    case "help": {
                            WriteLocal("help_extended");
                            break;
                        }
                    case "href": {
                            var names = arguments.Pathes[0].parameter.Substring(9);
                            var newArgs = new List<string>();
                            newArgs.Add("install");
                            names.Split(' ').ForEach(a => newArgs.Add(a));
                            Main(newArgs.ToArray());
                        }break;
                }
                Settings.Save();
            }
        }
    }
}