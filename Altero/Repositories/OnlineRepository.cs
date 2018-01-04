using System;
using System.Collections.Generic;
using System.Text;

namespace Altero.Repositories
{
    class OnlineRepository : AlteroShared.IPackagesRepository
    {
        private string _path;

        public static OnlineRepository Load()
        {
            return new OnlineRepository();
        }

        private OnlineRepository()
        {
            
        }
    }
}
