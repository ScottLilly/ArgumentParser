using System.Linq;

namespace ArgumentParser
{
    internal static class ExtensionMethods
    {
        public static bool Contains(this string s, char[] chars)
        {
            foreach (var c in chars)
            {
                if (s.Contains(c))
                {
                    return true;
                }
            }

            return false;
        }
    }
}