using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Altero
{
    class RichConsole
    {
        public static void Write(object textBoxed)
        {
            var text = textBoxed.ToString();
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
        }

        public static void WriteLine(object text) => Write(text.ToString()+"\n");

        public static string ReadLine(string desk = "")
        {
            Write(desk);
            return Console.ReadLine();
        }
    }
}