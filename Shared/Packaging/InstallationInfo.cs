using System;
using System.Collections.Generic;
using System.Text;

namespace AlteroShared.Packaging
{
    public class InstallationInfo
    {
        public string owner = "";
        public List<RegistryInfo> registry = new List<RegistryInfo>();
        public List<FileMeta> files = new List<FileMeta>();
        public List<LauncherInfo> launchers = new List<LauncherInfo>();
    }
}
