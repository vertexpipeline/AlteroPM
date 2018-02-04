using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AlteroSite.Models;
using AlteroShared;
using Altero.Repositories;

namespace AlteroSite.Controllers
{
    public class HomeController : Controller
    {
        

        public IActionResult Index()
        {
            var query = Request.Query["s"].FirstOrDefault();
            var page = Request.Query["p"].FirstOrDefault();
            string pattern = "";
            if (query != null)
                pattern = query.ToString();
            int pageN = 1;
            if (page != null) {
                pageN = int.Parse(page.ToString());
            }

            ViewData["searchPattern"] = query;
            ViewData["packages"] = APIController.repository.Search(pattern).Skip(10*(pageN-1)).Take(10);

            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
