using System;
using System.Collections.Generic;
using System.Text;
using AlteroShared;
using Newtonsoft.Json;
using System.IO;
using System.Linq;

using static Altero.RichConsole;

namespace Altero.Repositories
{
    class LocalRepository : AlteroShared.IPackagesRepository
    {
        private static string _path;
        public string Path => _path;
        private static LocalRepositoryInfo _meta;

        public static LocalRepository Load(string path)
        {
            WriteLine("<cyan>Connecting to local repo...</>");
            try {
                var meta = JsonConvert.DeserializeObject<LocalRepositoryInfo>(File.ReadAllText(path + "\\meta.json"));
                WriteLine("<green>Connected</>");
                return new LocalRepository(path, meta);
            }
            catch (Exception ex) {
                WriteLine("<red>Missing or invalid repository, connecting to online repo</>");
                return null;
            }
        }

        public PackageInfo Get(string name, PackageVersion version = null)
        {
            name = name.ToLower();
            var versions = _meta.packages.Where(pkg => pkg.meta.name.ToLower() == name);
            if (version == null) {
                if (versions.Count() != 0)
                    return versions.Max();
            }
            else {
                var pkg = versions.FirstOrDefault(p => p.meta.version.Equals(version));
                if (pkg != default(PackageInfo))
                    return pkg;
            }
            return default(PackageInfo);
        }

        public void Send(PackageInfo pkg)
        {
            var newPath = _path + "\\" + pkg.meta.name + "~" + pkg.meta.version;
            File.Move(pkg.packagePath, newPath);
            pkg.packagePath = newPath;

            WriteLine("<green>Package was sent</>");

            _meta.packages.Add(pkg);

            File.WriteAllText(_path + "\\meta.json", JsonConvert.SerializeObject(_meta));
        }

        public void Delete(string name, PackageVersion version)
        {
            var versions = _meta.packages.Where(pkg => pkg.meta.name.ToLower() == name);
            if (version == null) {
                var deletionPending = new List<PackageInfo>();
                foreach (var pkg in versions) {
                    if (File.Exists(pkg.packagePath))
                        File.Delete(pkg.packagePath);
                    deletionPending.Add(pkg);
                }

                if (deletionPending.Count == 0)
                    WriteLine($"<red>Not found item {name}</>");
                else
                    WriteLine($"<green>Deleted {deletionPending.Count} versions of {name}</>");

                foreach (var pended in deletionPending)
                    _meta.packages.Remove(pended);
            }
            else {
                var pkg = versions.FirstOrDefault(p => p.meta.version.Equals(version));
                if (pkg != default(PackageInfo)) {
                    if (File.Exists(pkg.packagePath))
                        File.Delete(pkg.packagePath);
                    _meta.packages.Remove(pkg);
                    WriteLine($"<green>{name} deleted</>");

                }
            }
            File.WriteAllText(_path + "\\meta.json", JsonConvert.SerializeObject(_meta));
        }

        private LocalRepository(string path, LocalRepositoryInfo meta)
        {
            _path = path;
            _meta = meta;
        }
    }
}