using System;
using System.Collections.Generic;
using System.Text;

namespace AlteroShared.Packaging
{
    public class PackageMeta
    {
        public string name;
        public PackageVersion version = new PackageVersion();
        public string type="app"; // lib/app/driver
        public List<RegistryInfo> registry = new List<RegistryInfo>();
        public List<FileMeta> files = new List<FileMeta>();
        public List<LauncherInfo> launchers = new List<LauncherInfo>();
    }
}
