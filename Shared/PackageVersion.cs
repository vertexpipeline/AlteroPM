using System;
using System.Collections.Generic;
using System.Text;

namespace AlteroShared
{
    [Serializable]
    public class PackageVersion:IComparable<PackageVersion>, IEquatable<PackageVersion>
    {
        public int major, minor, state, build;

        public PackageVersion(int _major=0, int _minor=0, int _state=0, int _build = 0)
        {
            major = _major;
            minor = _minor;
            state = _state;
            build = _build;
        }

        public static PackageVersion Parse(string version)
        {
            var parts = new int[] { 0, 0, 0, 0 };
            
            version.Split('.').ForEach((part, i) =>
            {
                parts[i] = int.Parse(part);
            });
            return new PackageVersion(parts[0], parts[1], parts[2], parts[3]);
        }

        public int CompareTo(PackageVersion other)
        {
            var score = (major.CompareTo(other.major)) * 10e12 + (minor.CompareTo(other.minor)) * 10e8 + (state.CompareTo(other.state)) * 10e4 + (build.CompareTo(other.build)) * 10e0;
            return score.CompareTo(0);
        }

        public bool Equals(PackageVersion other)
        {
            return major == other.major && minor == other.minor && state == other.state && build == other.build;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{state}.{build}";
        }
    }
}
