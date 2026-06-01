namespace codecrafters_redis.src.Common.Geo;

internal class RedisHaversine
{
    public static double calculate(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6372797.560856; 
        var dLat = toRadians(lat2 - lat1);
        var dLon = toRadians(lon2 - lon1);
        lat1 = toRadians(lat1);
        lat2 = toRadians(lat2);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Sin(dLon / 2) * Math.Sin(dLon / 2)
              * Math.Cos(lat1) * Math.Cos(lat2);

        return R * 2 * Math.Asin(Math.Sqrt(a));
    }


    public static double toRadians(double angle)
    {
        return Math.PI * angle / 180.0;
    }

}
