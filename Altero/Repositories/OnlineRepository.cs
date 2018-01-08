using System;
using System.Collections.Generic;
using System.Text;
using AlteroShared;

namespace Altero.Repositories
{
    class OnlineRepository : AlteroShared.IPackagesRepository
    {
        private string _path;

        public static OnlineRepository Load()
        {
            return new OnlineRepository();
        }

        public PackageInfo Get(string name, PackageVersion version)
        {
            throw new NotImplementedException();
        }

        public void Send(PackageInfo pkg)
        {
        
            throw new NotImplementedException();
        }

        public void Delete(string name, PackageVersion version)
        {
            throw new NotImplementedException();
        }

        private OnlineRepository()
        {
            
        }
    }
}
