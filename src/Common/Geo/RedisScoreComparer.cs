using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Common.Geo;

internal class RedisScoreComparer : IComparer<(double score, string value)>
{
    public static readonly RedisScoreComparer Instance = new();
    public int Compare((double score, string value) x, (double score, string value) y)
    {
        int cmp = x.score.CompareTo(y.score);
        return cmp != 0 ? cmp : string.Compare(x.value, y.value, StringComparison.Ordinal);
    }
}