using System;
using System.Collections.Generic;
using System.Text;

namespace AlteroShared.Packaging
{
    public class DependencyInfo
    {
        public string package="";
        public string critery;//> < (= is default)
        public string repo = "%std_repo%";
    }
}
