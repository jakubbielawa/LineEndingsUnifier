namespace JakubBielawa.LineEndingsUnifier
{
    public static class Extensions
    {
        public static bool EndsWithAny(this string str, string[] strings)
        {
            foreach (var s in strings)
            {
                if (str.EndsWith(s))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
