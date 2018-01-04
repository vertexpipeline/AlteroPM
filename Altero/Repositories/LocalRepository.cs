using System;
using System.Collections.Generic;
using System.Text;

namespace Altero.Repositories
{
    class LocalRepository:AlteroShared.IPackagesRepository
    {
        private string _path;

        public static LocalRepository Load(string path)
        {
            return new LocalRepository(path);
        }

        private LocalRepository(string path)
        {
            _path = path;
        }
    }
}
