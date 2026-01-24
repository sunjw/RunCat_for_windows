namespace RunCat365
{
    internal enum SpeedSource
    {
        CPU,
        GPU,
        Memory
    }

    internal static class SpeedSourceExtension
    {
        internal static string GetString(this SpeedSource source)
        {
            return source.ToString();
        }
    }
}
