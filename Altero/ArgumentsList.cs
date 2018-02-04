using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Generic;

using AlteroShared.Packaging;
using AlteroShared;
using Altero.Repositories;
using AlteroShared.Packaging;
using System.Security.Permissions;
using Altero.Models;
using System.Collections;
using System.Text.RegularExpressions;
using System.Linq;

namespace Altero
{
    class ArgumentsList : List<Argument>
    {
        public ArgumentsList Pathes
        {
            get {
                return new ArgumentsList(this.Where(p => p.IsPath).ToList());
            }
        }

        public ArgumentsList Items
        {
            get {
                return new ArgumentsList(this.Where(p => !p.IsPath && !p.IsParameter).ToList());
            }
        }

        public ArgumentsList Parameters
        {
            get {
                return new ArgumentsList(this.Where(p => p.IsParameter).ToList());
            }
        }

        private ArgumentsList(List<Argument> args)
        {
            AddRange(args);
        }

        public ArgumentsList(IEnumerable<string> args)
        {
            string key = "";
            int n = 0;
            var arguments = new List<Argument>();
            foreach (var arg in args) {
                var trimmedArg = arg.Trim();
                var match = Regex.Match(trimmedArg, "-(.+)=(.+)");
                if (match.Success) {
                    arguments.Add(new Argument { key = match.Groups[0].Value, parameter = match.Groups[1].Value, IsParameter = true });
                }
                else {
                    if (trimmedArg.StartsWith("-")) {
                        arguments.Add(new Argument { key = trimmedArg.Substring(1), IsParameter = true });
                    }
                    else {
                        arguments.Add(new Argument { key = n.ToString(), parameter = trimmedArg, IsPath = Regex.IsMatch(trimmedArg, Settings.PathRegex) });
                        n++;
                    }
                }
            }
            AddRange(arguments);
        }
    }
}
