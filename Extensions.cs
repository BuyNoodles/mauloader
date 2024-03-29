using Microsoft.Extensions.Options;
using System.Globalization;

public static class Extensions
{
    public static T GetConfiguration<T>(this IServiceProvider serviceProvider)
        where T : class
    {
        var o = serviceProvider.GetService<IOptions<T>>();
        if (o is null)
            throw new ArgumentNullException(nameof(T));

        return o.Value;
    }

    public static string FormatNumber(this int number)
    {
        return number.ToString("N0");
    }
}
