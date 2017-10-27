namespace Oxide.Core
{
    /// <summary>
    /// Represents a version in major.minor.patch form
    /// </summary>
    public struct VersionNumber
    {
        // The major, minor and patch version numbers
        public int Major, Minor, Patch;

        /// <summary>
        /// Initializes a new instance of the Version struct with the specified values
        /// </summary>
        /// <param name="major"></param>
        /// <param name="minor"></param>
        /// <param name="patch"></param>
        public VersionNumber(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        /// <summary>
        /// Returns a human readable string representation of this version
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{Major}.{Minor}.{Patch}";

        #region Operator Overloads

        /// <summary>
        /// Compares this version for equality to another
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(VersionNumber a, VersionNumber b)
        {
            return a.Major == b.Major && a.Minor == b.Minor && a.Patch == b.Patch;
        }

        /// <summary>
        /// Compares this version for inequality to another
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(VersionNumber a, VersionNumber b)
        {
            return a.Major != b.Major || a.Minor != b.Minor || a.Patch != b.Patch;
        }

        public static bool operator >(VersionNumber a, VersionNumber b)
        {
            if (a.Major < b.Major)
                return false;
            if (a.Major > b.Major)
                return true;
            if (a.Minor < b.Minor)
                return false;
            if (a.Minor > b.Minor)
                return true;
            return a.Patch > b.Patch;
        }

        public static bool operator >=(VersionNumber a, VersionNumber b)
        {
            if (a.Major < b.Major)
                return false;
            if (a.Major > b.Major)
                return true;
            if (a.Minor < b.Minor)
                return false;
            if (a.Minor > b.Minor)
                return true;
            return a.Patch >= b.Patch;
        }

        public static bool operator <(VersionNumber a, VersionNumber b)
        {
            if (a.Major > b.Major)
                return false;
            if (a.Major < b.Major)
                return true;
            if (a.Minor > b.Minor)
                return false;
            if (a.Minor < b.Minor)
                return true;
            return a.Patch < b.Patch;
        }

        public static bool operator <=(VersionNumber a, VersionNumber b)
        {
            if (a.Major > b.Major)
                return false;
            if (a.Major < b.Major)
                return true;
            if (a.Minor > b.Minor)
                return false;
            if (a.Minor < b.Minor)
                return true;
            return a.Patch <= b.Patch;
        }

        #endregion Operator Overloads

        /// <summary>
        /// Compares this version for equality to the specified object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (!(obj is VersionNumber)) return false;
            var other = (VersionNumber)obj;
            return this == other;
        }

        /// <summary>
        /// Gets a hash code for this version
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + Major.GetHashCode();
                hash = hash * 23 + Minor.GetHashCode();
                hash = hash * 23 + Patch.GetHashCode();
                return hash;
            }
        }
    }
}
