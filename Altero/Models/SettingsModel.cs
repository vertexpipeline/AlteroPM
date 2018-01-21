using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;


namespace Altero.Models
{
    public class SettingsModel
    {
        public List<LocationsInfo> locations;
        public string path_regex;
        public string localization_path;
        public List<AlteroShared.Packaging.PackageMeta> installed = new List<AlteroShared.Packaging.PackageMeta>();
        
    }
}
