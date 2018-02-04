using System;
using Microsoft.Win32;
using System.IO;

namespace PostInstall
{
    class Program
    {
        static void Main(string[] args)
        {
            var rootDir = Path.GetPathRoot(Environment.SystemDirectory);
            var key = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment";
            var curPath = Registry.GetValue(key, "Path", "");
            Registry.SetValue(key, "Path", curPath + ";" + Path.Combine(rootDir, "Shortcuts;")+ Path.Combine(rootDir, "Program Files\\dotnet;"));
            curPath = Registry.GetValue(@"HKEY_CURRENT_USER\Environment", "Path", "");
            Registry.SetValue(@"HKEY_CURRENT_USER\Environment", "Path", curPath + ";" + Path.Combine(rootDir, "Shortcuts;"));

            var root = Registry.ClassesRoot.CreateSubKey("altero");
            
            root.SetValue("URL Protocol", "");
            root.SetValue("", "URL:ALtero redirect protocol");
            root.CreateSubKey("shell")
                .CreateSubKey("open")
                .CreateSubKey("command")
                .SetValue("", "\"" + rootDir+"apps\\altero\\netcoreapp2.0\\altero.exe "+"\" \"href\" \"%1\"");
        }
    }
}
