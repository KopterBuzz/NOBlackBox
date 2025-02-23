using System.Threading;

namespace NOBlackBox
{
    public abstract class ACMINotUnit(bool longLife = false) : ACMIObject(NextID(longLife))
    {
        private const long LONG_START = 1 << 33;

        private static int BASE_SHORT = 0;
        private static long BASE_LONG = LONG_START;

        private static long NextID(bool longLife)
        {
            if (longLife)
                return NextLongID();
            else
                return ((long)Interlocked.Increment(ref BASE_SHORT) - 1) | (1L << 32);
        }

        private static long NextLongID()
        {
            long wasBase;
            do
            {
                wasBase = Interlocked.Increment(ref BASE_LONG);
                if (wasBase > LONG_START)
                    break;

            } while (Interlocked.CompareExchange(ref BASE_LONG, LONG_START, wasBase) != wasBase);

            if (wasBase != 0)
                return NextLongID();
            else
                return wasBase - 1;
        }
    }
}
