using System;
using System.Collections.Generic;
using System.Text;

namespace TGREdit
{
    public static class StringExtensions
    {
        public static IEnumerable<Tuple<int, int>> AllIndexesOf(this string str, string value)
        {
            if (String.IsNullOrEmpty(value))
                throw new ArgumentException("the string to find may not be empty", nameof(value));

            for (int index = 0; ; index += value.Length)
            {
                index = str.IndexOf(value, index, StringComparison.Ordinal);
                if (index == -1)
                    break;
                yield return new Tuple<int, int>(index, index + value.Length);
            }
        }
    }
}
