using System;

public class GeohashEncoder
{
    private const double MIN_LATITUDE = -85.05112878;
    private const double MAX_LATITUDE = 85.05112878;
    private const double MIN_LONGITUDE = -180;
    private const double MAX_LONGITUDE = 180;
    private const double LATITUDE_RANGE = MAX_LATITUDE - MIN_LATITUDE;
    private const double LONGITUDE_RANGE = MAX_LONGITUDE - MIN_LONGITUDE;

    public static long Encode(double latitude, double longitude)
    {
        double normalizedLatitude = Math.Pow(2, 26) * (latitude - MIN_LATITUDE) / LATITUDE_RANGE;
        double normalizedLongitude = Math.Pow(2, 26) * (longitude - MIN_LONGITUDE) / LONGITUDE_RANGE;

        int normalizedLatitudeInt = (int)normalizedLatitude;
        int normalizedLongitudeInt = (int)normalizedLongitude;

        return Interleave(normalizedLatitudeInt, normalizedLongitudeInt);
    }

    private static long Interleave(int x, int y)
    {
        long spreadX = SpreadInt32ToInt64(x);
        long spreadY = SpreadInt32ToInt64(y);
        long yShifted = spreadY << 1;
        return spreadX | yShifted;
    }

    private static long SpreadInt32ToInt64(int v)
    {
        long result = v & 0xFFFFFFFF;
        result = (result | (result << 16)) & 0x0000FFFF0000FFFF;
        result = (result | (result << 8)) & 0x00FF00FF00FF00FF;
        result = (result | (result << 4)) & 0x0F0F0F0F0F0F0F0F;
        result = (result | (result << 2)) & 0x3333333333333333;
        result = (result | (result << 1)) & 0x5555555555555555;
        return result;
    }
   
}