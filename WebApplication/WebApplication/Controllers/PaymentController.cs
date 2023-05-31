using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication.Models;

namespace WebApplication.Controllers
{
    [RoutePrefix("doi-qua")]
    public class PaymentController : Controller
    {
        Entities db = new Entities();
        [Route]
        public ActionResult Index()
        {
            var listcate = db.B_Cate.Where(a => a.Status == true);
            ViewBag.cate = listcate.Count();
            return View(listcate);
        }
    }
}