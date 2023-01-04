using System.Collections.Generic;
using System.Linq;

namespace ZModLauncher.Converters;

internal static class LocalEx
{
    public static double ExtractDouble(this object val)
    {
        double d = val as double? ?? double.NaN;
        return double.IsInfinity(d) ? double.NaN : d;
    }

    public static bool AnyNan(this IEnumerable<double> vals)
    {
        return vals.Any(double.IsNaN);
    }
}