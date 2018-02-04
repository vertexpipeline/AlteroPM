using System;
using System.Collections.Generic;
using System.Text;

namespace AlteroShared.Packaging
{
    public class PackageMeta:IEquatable<PackageMeta>, IComparable<PackageMeta>
    {
        public string name;
        public string visibleName;
        public PackageVersion version = new PackageVersion();
        public string type="app"; // lib/app/driver
        public string logoURL = "";
        public string description = "";
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
