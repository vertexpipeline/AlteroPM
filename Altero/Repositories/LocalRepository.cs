﻿using System;
using System.Collections.Generic;
using System.Text;
using AlteroShared;
using Newtonsoft.Json;
using System.IO;
using System.Linq;

using static Altero.RichConsole;
using AlteroShared.Packaging;

using System.Text.RegularExpressions;

namespace Altero.Repositories
{
    class LocalRepository : AlteroShared.IPackagesRepository
    {
        private static string _path;
        public string Path => _path;
        private static LocalRepositoryInfo _meta;

        public static LocalRepository Load(string path)
        {
            WriteLocalLine("connecting_local");
            try {
                var meta = JsonConvert.DeserializeObject<LocalRepositoryInfo>(File.ReadAllText(path + "\\meta.json"));
                WriteLocalLine("success");
                return new LocalRepository(path, meta);
            }
            catch (Exception ex) {
                WriteLocalLine("mis_repo");
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

            WriteLocalLine("pkg_sent");

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
                    WriteLocalLine($"item_n_found", name);
                else
                    WriteLocalLine($"deleted_vers", deletionPending.Count, name);

                foreach (var pended in deletionPending)
                    _meta.packages.Remove(pended);
            }
            else {
                var pkg = versions.FirstOrDefault(p => p.meta.version.Equals(version));
                if (pkg != default(PackageInfo)) {
                    if (File.Exists(pkg.packagePath))
                        File.Delete(pkg.packagePath);
                    _meta.packages.Remove(pkg);
                    WriteLocalLine($"deleted", name);

                }
            }
            File.WriteAllText(_path + "\\meta.json", JsonConvert.SerializeObject(_meta));
        }

        public List<PackageMeta> Search(string searchPattern)
        {
            try {
                return _meta.packages.Where(pkg => Regex.IsMatch(pkg.meta.name, searchPattern)).Select(pkg => pkg.meta).ToList();
            }catch(Exception ex) {
                return new List<PackageMeta>();
            }
        }

        private LocalRepository(string path, LocalRepositoryInfo meta)
        {
            _path = path;
            _meta = meta;
        }
    }
}