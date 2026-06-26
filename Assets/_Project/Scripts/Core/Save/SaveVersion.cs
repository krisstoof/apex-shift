using System;

namespace ApexShift.Core.Save
{
    [Serializable]
    public sealed class SaveVersion
    {
        public const int CurrentMajor = 1;
        public const int CurrentMinor = 0;
        public const int CurrentPatch = 0;

        public int major = CurrentMajor;
        public int minor = CurrentMinor;
        public int patch = CurrentPatch;

        public int Major => major;
        public int Minor => minor;
        public int Patch => patch;
        public bool IsCompatible => major == CurrentMajor;

        public static SaveVersion Current => new SaveVersion(CurrentMajor, CurrentMinor, CurrentPatch);

        public SaveVersion()
        {
        }

        public SaveVersion(int major, int minor, int patch)
        {
            this.major = Math.Max(0, major);
            this.minor = Math.Max(0, minor);
            this.patch = Math.Max(0, patch);
        }

        public override string ToString()
        {
            return major + "." + minor + "." + patch;
        }
    }
}
