namespace Oxide.Core
{
    /// <summary>
    /// Represents a version in major.minor.patch form
    /// </summary>
    public struct VersionNumber
    {
        // The major, minor and patch version numbers
        public ushort Major, Minor, Patch;

        /// <summary>
        /// Initialises a new instance of the Version struct with the specified values
        /// </summary>
        /// <param name="major"></param>
        /// <param name="minor"></param>
        /// <param name="patch"></param>
        public VersionNumber(ushort major, ushort minor, ushort patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        /// <summary>
        /// Returns a human readable string representation of this version
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}", Major, Minor, Patch);
        }

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
            return a.Major != b.Major || a.Minor != b.Minor || a.Patch == b.Patch;
        }

        public static bool operator >(VersionNumber a, VersionNumber b)
        {
            if (a.Major < b.Major)
                return false;
            else if (a.Major > b.Major)
                return true;
            else
            {
                if (a.Minor < b.Minor)
                    return false;
                else if (a.Minor > b.Minor)
                    return true;
                else
                    return a.Patch > b.Patch;
            }
        }

        public static bool operator >=(VersionNumber a, VersionNumber b)
        {
            if (a.Major < b.Major)
                return false;
            else if (a.Major > b.Major)
                return true;
            else
            {
                if (a.Minor < b.Minor)
                    return false;
                else if (a.Minor > b.Minor)
                    return true;
                else
                    return a.Patch >= b.Patch;
            }
        }

        public static bool operator <(VersionNumber a, VersionNumber b)
        {
            if (a.Major > b.Major)
                return false;
            else if (a.Major < b.Major)
                return true;
            else
            {
                if (a.Minor > b.Minor)
                    return false;
                else if (a.Minor < b.Minor)
                    return true;
                else
                    return a.Patch < b.Patch;
            }
        }

        public static bool operator <=(VersionNumber a, VersionNumber b)
        {
            if (a.Major > b.Major)
                return false;
            else if (a.Major < b.Major)
                return true;
            else
            {
                if (a.Minor > b.Minor)
                    return false;
                else if (a.Minor < b.Minor)
                    return true;
                else
                    return a.Patch <= b.Patch;
            }
        }

        #endregion

        /// <summary>
        /// Compares this version for equality to the specified object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (!(obj is VersionNumber)) return false;
            VersionNumber other = (VersionNumber)obj;
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
                int hash = 17;
                hash = hash * 23 + Major.GetHashCode();
                hash = hash * 23 + Minor.GetHashCode();
                hash = hash * 23 + Patch.GetHashCode();
                return hash;
            }
        }
    }
}
