using System;
using System.Collections.Generic;
using System.Text;

namespace NibbleEditor
{
    /// <summary>Version Utilities</summary>
    public static class Version
    {

        internal const string VERSION_STRING = "1.0.91.0";

        /// <summary>Shorthand for AssemblyVersion.Major</summary>
        public static int Major => AssemblyVersion.Major;
        /// <summary>Shorthand for AssemblyVersion.Minor</summary>
        public static int Minor => AssemblyVersion.Minor;
        /// <summary>Shorthand for AssemblyVersion.Build</summary>
        public static int Release => AssemblyVersion.Build;
        /// <summary>Shorthand for AssemblyVersion.Revision</summary>
        public static int Prerelease => AssemblyVersion.Revision;

        /// <summary>The libMBIN assembly version.</summary>
        public static System.Version AssemblyVersion => new System.Version(VERSION_STRING);

        /// <summary>
        ///     Returns a human-readable suffix indicating the <see cref="Prerelease"/> version.
        /// </summary>
        /// <returns>
        ///     If the current assembly version is a prerelease (<see cref="Release"/> is 0 or <see cref="Prerelease"/> is not 0) then "-pre{Prerelease}" is returned.
        ///     Otherwise returns an emptry string.
        /// </returns>
        public static string GetSuffix() => (Release == 0 || Prerelease != 0) ? $"-pre{Prerelease}" : "";

        /// <summary>
        ///     Returns the assembly version in a human-readable string format.
        ///     Eg. "1.1.0" (Release) or "1.1.0-pre1" (Pre-Release)
        /// </summary>
        /// <returns>"{<see cref="Major"/>}.{<see cref="Minor"/>}.{<see cref="Release"/>}{<see cref="GetSuffix">Suffix</see>}"</returns>
        public static string GetString() => AssemblyVersion.ToString(3) + GetSuffix();

    }
}
