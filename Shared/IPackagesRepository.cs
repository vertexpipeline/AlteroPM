using System;
using System.Collections.Generic;
using System.Text;

namespace AlteroShared
{
    public interface IPackagesRepository
    {
        PackageInfo Get(string name, PackageVersion version);
        bool Send(PackageInfo pkg);
        void Delete(string name, PackageVersion version);
        List<Packaging.PackageMeta> Search(string pattern);
    }
}
