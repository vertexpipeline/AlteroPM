using System;
using System.Collections.Generic;
using System.Text;

namespace Altero.Models
{
    class SettingsModel
    {
        public List<LocationsInfo> locations;
        public string path_regex;
        public List<AlteroShared.Packaging.PackageMeta> installed = new List<AlteroShared.Packaging.PackageMeta>();
    }
}
