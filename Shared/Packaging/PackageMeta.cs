using System;
using System.Collections.Generic;
using System.Text;

namespace AlteroShared.Packaging
{
    public class PackageMeta:IEquatable<PackageMeta>, IComparable<PackageMeta>
    {
        public string name;
        public PackageVersion version = new PackageVersion();
        public string type="app"; // lib/app/driver
        public List<RegistryInfo> registry = new List<RegistryInfo>();
        public List<FileMeta> files = new List<FileMeta>();
        public List<LauncherInfo> launchers = new List<LauncherInfo>();
        public List<DependencyInfo> dependencies = new List<DependencyInfo>();

        public int CompareTo(PackageMeta other)
        {
            return version.CompareTo(other.version);
        }

        public bool Equals(PackageMeta other)
        {
            return name == other.name && version.Equals(other.version);
        }
    }
}
