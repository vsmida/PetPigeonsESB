namespace Shared
{
    public static class NumericUtils
    {
        public static bool IsPowerOfTwo(int number)
        {
            return IsPowerOfTwo((long) number);
        }

        public static bool IsPowerOfTwo(long number)
        {
            return (number & (number - 1)) == 0;
        }
    }
}