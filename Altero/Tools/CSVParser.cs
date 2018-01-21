using System;
using System.Collections.Generic;
using System.Text;

namespace Altero.Tools
{
    public static class CSVParser
    {
        public static string[,] Parse(string data)
        {
            var lines = data.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            var columns = lines[0].Split(',').Length;
            var rows = lines.Length;

            var result = new string[rows, columns];
            for (int i = 0; i < rows; i++) {
                if (lines[i].Trim() != "") {
                    var row = lines[i].Split(',');
                    for (int j = 0; j < columns; j++) {
                        result[i, j] = row[j];
                    }
                }
            }
            return result;
        }
    }
}
