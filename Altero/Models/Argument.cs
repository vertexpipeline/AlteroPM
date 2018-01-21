using System;
using System.Collections.Generic;
using System.Text;

namespace Altero.Models
{
    class Argument
    {
        public string key;
        public string parameter = null;

        public bool IsPath { get; set; } = false;
        public bool IsParameter { get; set; } = false;
    }
}
