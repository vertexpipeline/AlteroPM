using System;
using System.Collections.Generic;
using System.Text;

namespace AlteroShared
{
    public class PackageVersion
    {
        private int _major, _minor, _state, _build;

        public PackageVersion(int major=0, int minor=0, int state=0, int build = 0)
        {
            _major = major;
            _minor = minor;
            _state = state;
            _build = build;
        }

        public override string ToString()
        {
            return $"{_major}.{_minor}.{_state}.{_build}";
        }
    }
}
