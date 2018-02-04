using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using AlteroSite.Models;
using AlteroShared;
using Altero.Repositories;


namespace AlteroSite.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    public class APIController : Controller
    {
        public static IPackagesRepository repository = LocalRepository.Load("c:/repository");
        public List<string> keys = System.IO.File.ReadAllLines("keys.txt").ToList();

        public async Task<bool> ValidateKey()
        {
            var key = Request.Query["key"].FirstOrDefault();
            return keys.Contains(key ?? "");  
        }

        [Route("upload")]
        [ContextStatic]
        public void Upload()
        {
            var val = ValidateKey();
            val.Wait();
            if (val.Result) {
                var fName = "wwwroot/tempfiles/" + Path.GetRandomFileName();
                try {
                    using (var file = System.IO.File.OpenWrite(fName))
                        Request.Form.Files[0].CopyTo(file);
                }
                catch (Exception ex) {
                    Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                }

                AlteroShared.Packaging.PackageMeta meta = null;
                using (ZipArchive arch = ZipFile.OpenRead(fName)) {
                    var entry = arch.Entries.First(p => p.Name == "metadata.json");
                    meta = JsonConvert.DeserializeObject<AlteroShared.Packaging.PackageMeta>(new StreamReader(entry.Open()).ReadToEnd());
                }
                repository.Send(new PackageInfo() { meta = meta, packagePath = fName });
                System.IO.File.Delete(fName);
            }
            else {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
            }
        }

        [Route("get")]
        [ContextStatic]
        public void Get()
        {
            var name = Request.Query["name"];
            var version = Request.Query["version"];
            Console.WriteLine($"{name} : {version}");
            if (name.Count != 0) {
                var pkg = repository.Get(name, version.Count != 0 ? PackageVersion.Parse(version) : null);
                if (pkg != null) {
                    Response.SendFileAsync(pkg.packagePath).Wait();
                }
                else {
                    Response.StatusCode = StatusCodes.Status404NotFound;
                }
            }
            else {
                Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
    }
}