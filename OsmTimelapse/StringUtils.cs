﻿using System.Linq;
using System.Text;

public static class StringUtils
{
    public static string ToSnakeCase(this string str)
    {
        return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
    }

    public static string ToArrayString(this object[] arr)
    {
        return string.Join(", ", arr);
    }
}
