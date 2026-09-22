namespace ApexShift.Runtime.Debugging
{
    public static class RuntimeDiagnosticsPolicy
    {
        public static bool Resolve(bool isEditor, bool isDevelopmentBuild, bool requested)
        {
            return requested && (isEditor || isDevelopmentBuild);
        }
    }
}
