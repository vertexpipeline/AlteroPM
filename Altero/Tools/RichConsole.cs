using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.IO;

namespace Altero.Tools
{
    class RichConsole
    {
        static Dictionary<string, string> _i10n;

        public static void WriteLocal(string key, params object[] values)
        {
            string text = "";
            try {
                text = _i10n[key];
            }catch(Exception ex) {
                throw new InvalidOperationException($"key {key} not found in a loaded localization");
            }
            var placeholders = Regex.Matches(text, "%(?<n>[0-9+])");

            foreach(Match placeholder in placeholders) {
                var n = int.Parse(placeholder.Groups["n"].Value);
                if (n < values.Length) {
                    text = text.Replace(placeholder.Value, values[n].ToString());
                }
                else {
                    throw new IndexOutOfRangeException("Number of argument is out of range");
                }
            }

            var colorStack = new Stack<ConsoleColor>();
            var tokens = Regex.Matches(text, @"(\<(?<color>\w+)\>|\<\/\>|[^\<\>]+)");
            foreach (Match mt in tokens) {
                if (mt.Value == "</>") {
                    colorStack.Pop();
                }
                else if (mt.Value.StartsWith("<")) { //<cyan>
                    var targetColor = mt.Groups["color"].Value;
                    var cur = ConsoleColor.Gray;

                    foreach(ConsoleColor color in Enum.GetValues(typeof(ConsoleColor))) {
                        if(targetColor.ToLower() == color.ToString().ToLower()) {
                            cur = color;
                        }
                    }
                    
                    colorStack.Push(cur);
                }
                else {//other
                    if (colorStack.TryPeek(out ConsoleColor color)) {
                        Console.ForegroundColor = color;
                    }
                    else {
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    Console.Write(mt.Value);
                }
            }
            Console.ResetColor();
        }

        public static void WriteLocalLine(string text, params object[] values)
        {
            WriteLocal(text, values);
            Console.WriteLine();
        }

        public static string ReadLine(string desk = "")
        {
            WriteLocal(desk);
            return Console.ReadLine();
        }

        static RichConsole()
        {
            _i10n = new Dictionary<string, string>();

            try {
                var data = CSVParser.Parse(File.ReadAllText(Settings.LocalizationFile));

                var cult = System.Globalization.CultureInfo.CurrentCulture.Name;

                var columns = data.GetLength(1);

                var rows = data.GetLength(0);

                for (int i = 1; i < columns; i++) {
                    if (data[0, i] == cult) {
                        for (int j = 1; j < rows; j++) {
                            _i10n.Add(data[j, 0], data[j, i].Replace("\\n", "\n"));
                        }
                    }
                }

                var temp = new Dictionary<string, string>();

                foreach (KeyValuePair<string, string> entry in _i10n) {
                    var formed = entry.Value;
                    foreach (Match mt in Regex.Matches(entry.Value, @"%(?<name>\w+)%")) {
                        var name = mt.Groups["name"].Value;
                        if (_i10n.ContainsKey(name)) {
                            formed = formed.Replace(mt.Value, _i10n[name]);
                        }
                    }
                    temp.Add(entry.Key, formed);
                }

                _i10n = temp;
            }
            catch (Exception ex) {
                throw new Exception("Cannot load localization");
            }
        }
    }
}