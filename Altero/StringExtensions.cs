using System;
using System.Collections.Generic;
using System.Text;

namespace Altero
{
    public static class StringExtensions
    {
        public static string Fill(this string str, Dictionary<string, string> values)
        {
            var result = str;
            foreach(var value in values) {
                result = result.Replace($"%{value.Key}%", value.Value);
            }
            return result;
        }
    }
}
