using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebApplication.Controllers
{
    [RoutePrefix("bao-hanh")]
    public class WarrantiController : Controller
    {
        [Route]
        public ActionResult Index()
        {
            return View();
        }
    }
}