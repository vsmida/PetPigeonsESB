namespace Shared
{
    public static class StringUtils
    {
         public static int CreateIdFromString(string str)
         {
             return str.GetHashCode();
         }
    }
}