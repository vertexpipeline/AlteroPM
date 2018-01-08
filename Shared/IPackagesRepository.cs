using System;
using System.Collections.Generic;
using System.Text;

namespace AlteroShared
{
    interface IPackagesRepository
    {
        PackageInfo Get(string name, PackageVersion version);
        void Send(PackageInfo pkg);
        void Delete(string name, PackageVersion version);
    }
}
