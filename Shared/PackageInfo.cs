using System;
using System.Collections.Generic;
using System.Text;

namespace AlteroShared
{
    public class PackageInfo:IComparable<PackageInfo>
    {
        public Packaging.PackageMeta meta;
        public string packagePath;

        public int CompareTo(PackageInfo other)
        {
            return meta.CompareTo(other.meta);
        }
    }
}
