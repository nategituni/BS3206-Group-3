namespace GroupProject.Model.Utilities;

public static class MathHelper
{
    public static double Distance(double x1, double y1, double x2, double y2)
    {
        return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
    }

    public static long LongHash(int a, int b)
    {
        return ((long)a << 32) | (uint)b;
    }

    public static int[] Unhash(long hash)
    {
        int a = (int)(hash >> 32);
        int b = (int)(hash & 0xFFFFFFFF);
        return new[] { a, b };
    }

    public static int PositionToAreaCoord(double value)
    {
        // 2^7 = 128, areas are 128x128
        return (int)value >> 7;
    }
}