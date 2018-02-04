using System;
using System.Collections.Generic;
using System.Text;
using AlteroShared;
using AlteroShared.Packaging;
using System.Net.Http;
using System.Net;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using System.Linq;

namespace Altero.Repositories
{
    class OnlineRepository : AlteroShared.IPackagesRepository
    {
        private Guid _key;
        private Uri _server;

        public static OnlineRepository Load(Uri server, Guid key)
        {
            var repo = new OnlineRepository();
            repo._key = key;
            repo._server = server;
            return repo;
        }

        public PackageInfo Get(string name, PackageVersion version)
        {
            try {
                var client = new WebClient();
                client.UploadProgressChanged += Client_UploadProgressChanged;
                var tFile = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
                try {
                    client.DownloadFileTaskAsync(_server + $"api/get?name={name}" + (version != null ? $"&version={version.ToString()}" : ""), tFile).Wait();
                }catch(Exception ex) {
                    Console.WriteLine(ex);
                    return null;
                }
                Console.WriteLine(tFile);
                AlteroShared.Packaging.PackageMeta meta = null;
                using (ZipArchive arch = ZipFile.OpenRead(tFile)) {
                    var entry = arch.Entries.First(p => p.Name == "metadata.json");
                    meta = JsonConvert.DeserializeObject<AlteroShared.Packaging.PackageMeta>(new StreamReader(entry.Open()).ReadToEnd());
                }
                var info = new PackageInfo() { meta = meta, packagePath = tFile };
                return info;
            }
            catch (WebException ex) {
                Console.WriteLine(ex);
                return null;
            }
        }

        public bool Send(PackageInfo pkg)
        {
            try {
                var client = new WebClient();
                
                client.UploadProgressChanged += Client_UploadProgressChanged;
                client.UploadFileTaskAsync(new Uri(_server + $"api/upload?key={_key}"), pkg.packagePath).Wait();
                Console.WriteLine("Sent");
            }catch(WebException ex) {
                Console.WriteLine(_server + $"api/upload?key={_key}");
                Console.WriteLine("Error while send:\n"+ex.ToString());
                return false;
            }
            return true;
        }

        private void Client_UploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            var mbSent = e.BytesSent / 1024 / 1024;
            if (mbSent%5==0)
                Console.WriteLine("["+mbSent+"/"+(e.TotalBytesToSend/1024/1024)+"]");
        }

        public void Delete(string name, PackageVersion version)
        {
            throw new NotImplementedException();
        }

        public List<PackageMeta> Search(string pattern)
        {
            throw new NotImplementedException();
        }

        private OnlineRepository()
        {
            
        }
    }
}
