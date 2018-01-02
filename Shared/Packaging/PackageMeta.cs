using System;
using System.Collections.Generic;
using System.Text;

namespace AlteroShared.Packaging
{
    public class PackageMeta
    {
        public string type; // lib/app/driver
        public List<RegistryInfo> registry;
        public List<FileInfo> files;
        public List<LauncherInfo> launchers;
    }
}
