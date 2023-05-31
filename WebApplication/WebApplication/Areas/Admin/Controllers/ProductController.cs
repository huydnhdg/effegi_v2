using Newtonsoft.Json;
using NLog;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication.Areas.Admin.Data;
using WebApplication.Models;
using Product = WebApplication.Models.Product;

namespace WebApplication.Areas.Admin.Controllers
{
    public class ProductController : BaseController
    {
        Logger logger = LogManager.GetCurrentClassLogger();
        private static List<Product> listExc = new List<Product>();
        static IEnumerable<ProductView> listexc = null;
        public ActionResult Index(int? page, string phone, string textsearch, string chanel, string status, string start_date, string end_date, string im_start_date, string im_end_date)
        {
            var model = from a in db.Products
                        orderby a.Createdate

                        join b in db.Customers on a.Active_phone equals b.Phone into temp
                        from c in temp.DefaultIfEmpty()

                        join d in db.AspNetUsers on a.Active_by equals d.UserName into temp1
                        from t1 in temp1.DefaultIfEmpty()
                        
                        select new ProductView()
                        {
                            Id = a.Id,
                            Name = a.Name,
                            Serial = a.Serial,
                            Code = a.Code,
                            Model = a.Model,
                            Trademark = a.Trademark,
                            Exportdate = a.Exportdate,
                            Status = a.Status,

                            AgentC1 = a.AgentC1,
                            Limited = a.Limited,
                            Createdate = a.Createdate,

                            Importdate = a.Importdate,
                            AgentC2 = a.AgentC2,
                            Importchanel = a.Importchanel,

                            CusName = c.Name,
                            Active_phone = a.Active_phone,
                            Address = c.Address,
                            Ward = c.Ward,
                            District = c.District,
                            Province = c.Province,

                            Active_date = a.Active_date,//ngày kích hoạt của sản phẩm có thể trước ngày khách hàng kích hoạt vì tới hạn
                            Customer_date = a.Customer_date,//ngày khách hàng kích hoạt
                            End_date = a.End_date,
                            Active_chanel = a.Active_chanel,
                            Active_by = a.Active_by,
                            AgentPhone = t1.PhoneNumber,
                            Note  = a.Note,
                            Wait_date = a.Wait_date,
                            Wait_day = a.Wait_day
                        };
            //hiển thị theo quyền
            if (User.IsInRole("Đại lý") || User.IsInRole("Nhân viên"))
            {
                model = model.Where(a => a.Active_by == User.Identity.Name || a.AgentC1 == User.Identity.Name || a.AgentC2 == User.Identity.Name);
            }
            //lọc theo thông tin
            if (!string.IsNullOrEmpty(phone))
            {
                model = model.Where(a => a.Active_phone == phone);
                ViewBag.phone = phone;
            }
            if (!string.IsNullOrEmpty(textsearch))
            {
                model = model.Where(a => a.Name.Contains(textsearch)
                || a.Serial.Contains(textsearch)
                || a.Code.Contains(textsearch)
                || a.Model.Contains(textsearch)

                || a.AgentC1.Contains(textsearch)
                || a.AgentC2.Contains(textsearch)

                || a.CusName.Contains(textsearch)
                || a.Active_phone.Contains(textsearch)
                || a.Address.Contains(textsearch)
                || a.Ward.Contains(textsearch)
                || a.District.Contains(textsearch)
                || a.Province.Contains(textsearch)

                || a.Active_by.Contains(textsearch)
                || a.AgentPhone.Contains(textsearch)
                );

                ViewBag.textsearch = textsearch;
            }
            if (!string.IsNullOrEmpty(chanel))
            {
                model = model.Where(a => a.Active_chanel.Contains(chanel));
                ViewBag.chanel = chanel;
            }
            if (!string.IsNullOrEmpty(status))
            {

                var _status = int.Parse(status);
                model = model.Where(a => a.Status == _status);
                ViewBag.status = status;
            }
            if (!string.IsNullOrEmpty(start_date))
            {
                DateTime s = DateTime.ParseExact(start_date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                model = model.Where(a => a.Active_date >= s);
                ViewBag.start_date = start_date;
            }
            if (!string.IsNullOrEmpty(end_date))
            {
                DateTime s = DateTime.ParseExact(end_date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                s = s.AddDays(1);
                model = model.Where(a => a.Active_date <= s);
                ViewBag.end_date = end_date;
            }
            if (!string.IsNullOrEmpty(im_start_date))
            {
                DateTime s = DateTime.ParseExact(im_start_date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                model = model.Where(a => a.Createdate >= s);
                ViewBag.im_start_date = im_start_date;
            }
            if (!string.IsNullOrEmpty(im_end_date))
            {
                DateTime s = DateTime.ParseExact(im_end_date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                s = s.AddDays(1);
                model = model.Where(a => a.Createdate <= s);
                ViewBag.im_end_date = im_end_date;
            }
            listexc = model as IEnumerable<ProductView>;

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            //bị connect time out nên phải take 1000
            return View(model.OrderByDescending(a => a.Active_date).Take(1000).ToPagedList(pageNumber, pageSize));
        }

        public ActionResult UploadFile()
        {
            //List<Product> list_product = new List<Product>();
            return View();
        }
        [HttpPost]
        public ActionResult UploadFile(FormCollection collection)
        {
            //List<Product> list_product = new List<Product>();
            try
            {
                HttpPostedFileBase file = Request.Files["UploadedFile"];
                if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                {
                    string fileName = file.FileName;
                    string fileContentType = file.ContentType;
                    byte[] fileBytes = new byte[file.ContentLength];
                    var data = file.InputStream.Read(fileBytes, 0, Convert.ToInt32(file.ContentLength));
                    var products = new List<UploadProduct>();

                    int count = 0;
                    using (var package = new ExcelPackage(file.InputStream))
                    {
                        var currentSheet = package.Workbook.Worksheets;
                        var workSheet = currentSheet.First();
                        var noOfCol = workSheet.Dimension.End.Column;
                        var noOfRow = workSheet.Dimension.End.Row;
                        for (int rowIterator = 2; rowIterator <= noOfRow; rowIterator++)
                        {
                            UploadProduct prodview = new UploadProduct();
                            Product product = new Product();
                            //string serial;
                            //string name;
                            //string code;
                            //string cate;
                            //string model;
                            //string trademark;
                            //string limited;
                            //string exportdate;
                            //string agent1;
                            //string note;

                            try { product.Name = workSheet.Cells[rowIterator, 1].Value.ToString(); } catch (Exception) { product.Name = ""; }

                            try { product.Code = workSheet.Cells[rowIterator, 2].Value.ToString(); } catch (Exception) { product.Code = ""; }
                            try { product.Serial = workSheet.Cells[rowIterator, 3].Value.ToString(); } catch (Exception) { product.Serial = ""; }
                            try { product.ProductCate = workSheet.Cells[rowIterator, 4].Value.ToString(); } catch (Exception) { product.ProductCate = ""; }
                            try { product.Model = workSheet.Cells[rowIterator, 5].Value.ToString(); } catch (Exception) { product.Model = ""; }
                            try { product.Trademark = workSheet.Cells[rowIterator, 6].Value.ToString(); } catch (Exception) { product.Trademark = ""; }
                            product.Limited = int.Parse(workSheet.Cells[rowIterator, 7].Value.ToString());
                            try {
                                product.Exportdate = DateTime.ParseExact(workSheet.Cells[rowIterator, 8].Value.ToString(), "dd/MM/yyyy", null);
                            }
                            catch (Exception) { product.Exportdate = null; }
                            
                            product.Wait_day = int.Parse(workSheet.Cells[rowIterator, 9].Value.ToString());
                            try { product.AgentC1 = workSheet.Cells[rowIterator, 10].Value.ToString(); } catch (Exception) { product.AgentC1 = ""; }
                            try { product.Note = workSheet.Cells[rowIterator, 11].Value.ToString(); } catch (Exception) { product.Note = ""; }
                            product.Createdate = DateTime.Now;
                            prodview.Product = product;
                            products.Add(prodview);

                            //add thong tin rows vao product
                            var cp = db.Products.Where(a => a.Code == product.Code || a.Serial == product.Serial).SingleOrDefault();
                            if (cp != null)
                            {
                                count++;
                                prodview.Error = "Serial hoặc Code đã bị trùng.";
                            
                            }
                            else
                            {

                                //check tài khoản 
                                //var check_agent = db.AspNetUsers.FirstOrDefault(a => a.UserName == agent1);
                                //if (!string.IsNullOrEmpty(agent1) && check_agent == null)
                                //{
                                //    product.Name = "tài khoản không đúng";
                                //    list_product.Add(product);
                                //    continue;
                                //}
                                //var check_product = db.Products.FirstOrDefault(a => a.Serial == serial);
                                //if (string.IsNullOrEmpty(serial) || check_product != null)
                                //{
                                //    product.Name = "serial không đúng";
                                //    list_product.Add(product);
                                //    continue;
                                //}
                                db.Products.Add(product);
                                db.SaveChanges();
                                prodview.Error = "Lưu thành công";

                            }

                        }
                    }
                    return View(products);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                logger.Error(ex.InnerException);
            }
            return View();
        }
        public ActionResult UploadExtra()
        {
            List<Product> list_product = new List<Product>();
            return View(list_product);
        }
        [HttpPost]
        public ActionResult UploadExtra(FormCollection collection)
        {
            List<Product> list_product = new List<Product>();
            HttpPostedFileBase file = Request.Files["UploadedFile"];
            if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
            {
                string fileName = file.FileName;
                string fileContentType = file.ContentType;
                byte[] fileBytes = new byte[file.ContentLength];
                var data = file.InputStream.Read(fileBytes, 0, Convert.ToInt32(file.ContentLength));
                using (var package = new ExcelPackage(file.InputStream))
                {
                    var currentSheet = package.Workbook.Worksheets;
                    var workSheet = currentSheet.First();
                    var noOfCol = workSheet.Dimension.End.Column;
                    var noOfRow = workSheet.Dimension.End.Row;
                    for (int rowIterator = 2; rowIterator <= noOfRow; rowIterator++)
                    {
                        string serial;
                        string name;
                        string code;
                        string cate;
                        string model;
                        string trademark;
                        string limited;
                        string exportdate;
                        string waitday;
                        string agent1;
                        string note;

                        try { name = workSheet.Cells[rowIterator, 1].Value.ToString(); } catch (Exception) { name = ""; }

                        try { code = workSheet.Cells[rowIterator, 2].Value.ToString(); } catch (Exception) { code = ""; }
                        try { serial = workSheet.Cells[rowIterator, 3].Value.ToString(); } catch (Exception) { serial = ""; }
                        try { cate = workSheet.Cells[rowIterator, 4].Value.ToString(); } catch (Exception) { cate = ""; }
                        try { model = workSheet.Cells[rowIterator, 5].Value.ToString(); } catch (Exception) { model = ""; }
                        try { trademark = workSheet.Cells[rowIterator, 6].Value.ToString(); } catch (Exception) { trademark = ""; }
                        try { limited = workSheet.Cells[rowIterator, 7].Value.ToString(); } catch (Exception) { limited = ""; }
                        try { exportdate = workSheet.Cells[rowIterator, 8].Value.ToString(); } catch (Exception) { exportdate = null; }
                        try { waitday = workSheet.Cells[rowIterator, 9].Value.ToString(); } catch (Exception) { waitday = ""; }
                        try { agent1 = workSheet.Cells[rowIterator, 10].Value.ToString(); } catch (Exception) { agent1 = ""; }
                        try { note = workSheet.Cells[rowIterator, 11].Value.ToString(); } catch (Exception) { note = ""; }
                        
                        DateTime? date = null;
                      
                        if (string.IsNullOrEmpty(code))
                        {
                            continue;
                        }    
                        int limit = int.Parse(limited);
                        int wait_day = int.Parse(waitday);
                        if (!string.IsNullOrEmpty(exportdate))
                        {
                            date = DateTime.ParseExact(exportdate, "dd/MM/yyyy", null);
                        }
                 

                        if (!string.IsNullOrEmpty(serial))
                        {
                            serial = serial.Trim();
                        }
                        if (!string.IsNullOrEmpty(code))
                        {
                            code = code.Trim();
                        }

                        var update_product = db.Products.FirstOrDefault(a => a.Code == code);
                        var check_agent = db.AspNetUsers.FirstOrDefault(a => a.UserName == agent1);
                        //chưa kích hoạt thì mới bổ sung thông tin được
                        if (update_product != null && update_product.Status == 0)
                        {
                            //update thong tin vao san pham
                            if (!string.IsNullOrEmpty(name))
                            {
                                update_product.Name = name;
                            }
                            if (!string.IsNullOrEmpty(cate))
                            {
                                update_product.ProductCate = cate;
                            }
                            if (!string.IsNullOrEmpty(model))
                            {
                                update_product.Model = model.ToUpper();
                            }
                            if (!string.IsNullOrEmpty(trademark))
                            {
                                update_product.Trademark = trademark;
                            }
                            if (!string.IsNullOrEmpty(limited))
                            {
                                update_product.Limited = limit;
                            }
                            if (!string.IsNullOrEmpty(exportdate))
                            {
                                update_product.Exportdate = date;
                            }
                            if (!string.IsNullOrEmpty(waitday))
                            {
                                update_product.Wait_day = wait_day;
                            }
                            if (!string.IsNullOrEmpty(agent1) && check_agent != null)
                            {
                                update_product.AgentC1 = agent1;
                            }
                            if (!string.IsNullOrEmpty(note))
                            {
                                update_product.Note = note;
                            }
                            db.Entry(update_product).State = EntityState.Modified;
                            db.SaveChanges();
                            list_product.Add(update_product);
                        }
                        else
                        {
                            //xuat ra loi
                            var product = new Product()
                            {
                                Serial = serial.ToUpper(),
                                Name = name,
                                Code = code.ToUpper(),
                                ProductCate = cate,
                                Model = model.ToUpper(),
                                Trademark = trademark,
                                Limited = limit,
                                Exportdate = date,
                                AgentC1 = agent1,
                                Createdate = DateTime.Now,
                                Createby = User.Identity.Name,
                                Note = note
                            };
                            list_product.Add(product);
                        }

                    }
                }
            }
            return View(list_product);
        }

        [HttpPost]
        public ActionResult Create()
        {
            return PartialView("~/Areas/Admin/Views/Product/_Create.cshtml", null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateConfirm([Bind(Include = "")] Product product)
        {
            if (ModelState.IsValid)
            {
                var _prod = db.Products.SingleOrDefault(a => a.Code == product.Code || a.Serial == product.Serial);
                if (_prod != null)
                {
                    SetAlert("Serial đã tồn tại trong hệ thống.", "danger");
                    return RedirectToAction("Index");
                }
                product.Code = product.Code.ToUpper();
                product.Serial = product.Code.ToUpper();
                product.Createby = User.Identity.Name;
                product.Createdate = DateTime.Now;
                db.Products.Add(product);
                db.SaveChanges();
                SetAlert("Đã lưu thông tin thành công.", "success");
                return RedirectToAction("Index");
            }
            SetAlert("Lưu thông tin không thành công. Hãy kiểm tra lại.", "danger");
            return RedirectToAction("Index");

        }
        [HttpPost]
        public ActionResult Edit(long Id)
        {
            var model = db.Products.Find(Id);
            return PartialView("~/Areas/Admin/Views/Product/_Edit.cshtml", model);
        }
        [HttpPost]
        public ActionResult EditDate(long Id)
        {
            var model = db.Products.Find(Id);
            return PartialView("~/Areas/Admin/Views/Product/_Editdate.cshtml", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditdateConfirm([Bind(Include = "")] Product product)
        {
            if (ModelState.IsValid)
            {
                var model = db.Products.Find(product.Id);
                string json = JsonConvert.SerializeObject(model);
                logger.Info(string.Format("[Edit date active] @UserName={0} @Product={1}", User.Identity.Name, json));
                var _product = db.Products.Find(product.Id);
                _product.Active_date = product.Active_date;
                _product.End_date = product.Active_date.Value.AddMonths(product.Limited);
                _product.Note = product.Note;
                db.Entry(_product).State = EntityState.Modified;
                db.SaveChanges();
                SetAlert("Đã lưu thông tin thành công.", "success");
                return RedirectToAction("Index");
            }
            SetAlert("Lưu thông tin không thành công. Hãy kiểm tra lại.", "danger");
            return RedirectToAction("Index");

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditConfirm([Bind(Include = "")] Product product)
        {
            if (ModelState.IsValid)
            {
                var model = db.Products.Find(product.Id);
                string json = JsonConvert.SerializeObject(model);
                logger.Info(string.Format("[Edit] @UserName={0} @Product={1}", User.Identity.Name, json));
                model.Code = product.Code;
                model.Name = product.Name;
                model.Serial = product.Serial;
                model.Trademark = product.Trademark;
                model.Limited = product.Limited;
                model.AgentC1 = product.AgentC1;
                model.Exportdate = product.Exportdate;
                model.Note = product.Note;
                db.Entry(model).State = EntityState.Modified;
                db.SaveChanges();
                SetAlert("Đã lưu thông tin thành công.", "success");
                return RedirectToAction("Index");
            }
            SetAlert("Lưu thông tin không thành công. Hãy kiểm tra lại.", "danger");
            return RedirectToAction("Index");

        }

        [HttpPost]
        public ActionResult Waitdate(long Id)
        {
            var model = db.Products.Find(Id);
            return PartialView("~/Areas/Admin/Views/Product/_Waitdate.cshtml", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult WaitdateConfirm([Bind(Include = "")] Product product)
        {
            if (ModelState.IsValid)
            {
                if (product.Wait_day > 0)
                {
                    var model = db.Products.Find(product.Id);
                    string json = JsonConvert.SerializeObject(model);
                    logger.Info(string.Format("[Edit date active] @UserName={0} @Product={1}", User.Identity.Name, json));

                    model.Wait_day = product.Wait_day;
                    model.Status = 3;
                    model.Wait_date = DateTime.Now;
                    db.Entry(model).State = EntityState.Modified;
                    db.SaveChanges();

                    SetAlert("Đã chuyển sang trạng thái chờ kích hoạt", "success");
                    return RedirectToAction("Index");
                }
                
            }
            SetAlert("Lưu thông tin không thành công. Hãy kiểm tra lại.", "danger");
            return RedirectToAction("Index");

        }

        public ActionResult Delete(long Id)
        {
            try
            {
                var model = db.Products.Find(Id);
                if (model.Status != 0)
                {
                    SetAlert("Sản phẩm này đã được kích hoạt.", "danger");
                    return RedirectToAction("Index");
                }
                string json = JsonConvert.SerializeObject(model);
                logger.Info(string.Format("[Delete] @UserName={0} @Product={1}", User.Identity.Name, json));
                db.Products.Remove(model);
                db.SaveChanges();
                SetAlert("Đã xóa thành công.", "success");
            }
            catch (Exception ex)
            {
                SetAlert(ex.Message, "danger");
            }

            return RedirectToAction("Index");
        }
        public ActionResult RemoveActive(long Id)
        {
            try
            {
                var model = db.Products.Find(Id);
                if (model.Status == 0)
                {
                    SetAlert("Sản phẩm này chưa được kích hoạt.", "danger");
                    return RedirectToAction("Index");
                }
                string json = JsonConvert.SerializeObject(model);
                logger.Info(string.Format("[Remove Active] @UserName={0} @Product={1}", User.Identity.Name, json));

                model.Status = 0;
                model.Active_phone = null;
                model.Active_code = null;
                model.Active_date = null;
                model.End_date = null;
                model.Active_by = null;
                model.Active_chanel = null;
                model.Buydate = null;
                model.Customer_date = null;//ngày khách hàng kích hoạt
                db.Entry(model).State = EntityState.Modified;
                db.SaveChanges();
                SetAlert("Đã hoàn kích hoạt thành công.", "success");
            }
            catch (Exception ex)
            {
                SetAlert(ex.Message, "danger");
            }

            return RedirectToAction("Index");
        }


        [HttpPost]
        public JsonResult GetCate(string text)
        {
            var cate = (from a in db.ProductCates
                        where a.Name.Contains(text)
                        select new { a.Name });
            return Json(cate, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult GetAgent(string text)
        {
            var cate = (from a in db.AspNetUsers
                        where a.UserName.Contains(text)
                        select new { a.UserName });
            return Json(cate, JsonRequestBehavior.AllowGet);
        }

        public ActionResult UploadFileActive()
        {
            var list = db.Products;
            foreach(var item in list)
            {
                var product = db.Products.FirstOrDefault(a => a.Id == item.Id);
                product.Serial = item.Serial.Trim();
                product.Code = product.Serial;
                db.Entry(product).State = EntityState.Modified;                
            }
            db.SaveChanges();

            List<Product> list_product = new List<Product>();
            return View(list_product);
        }
        [HttpPost]
        public ActionResult UploadFileActive(FormCollection collection)
        {
            List<Product> list_product = new List<Product>();
            try
            {
                HttpPostedFileBase file = Request.Files["UploadedFile"];
                if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                {
                    string fileName = file.FileName;
                    string fileContentType = file.ContentType;
                    byte[] fileBytes = new byte[file.ContentLength];
                    var data = file.InputStream.Read(fileBytes, 0, Convert.ToInt32(file.ContentLength));
                    using (var package = new ExcelPackage(file.InputStream))
                    {
                        var currentSheet = package.Workbook.Worksheets;
                        var workSheet = currentSheet.First();
                        var noOfCol = workSheet.Dimension.End.Column;
                        var noOfRow = workSheet.Dimension.End.Row;
                        for (int rowIterator = 2; rowIterator <= noOfRow; rowIterator++)
                        {
                            string name = "";
                            string cate = "";
                            string serial = "";
                            string code = "";
                            string model = "";
                            string trademark = "";
                            string limited = "";
                            string activeby = "";
                            string buydate = "";
                            string exportdate = "";
                            string activedate = "";
                            string phoneactive = "";
                            string buyadr = "";
                            string cusname = "";
                            string phone = "";
                            string address = "";
                            string ward = "";
                            string district = "";
                            string province = "";
                            string note = "";

                            try { name = workSheet.Cells[rowIterator, 1].Value.ToString(); } catch (Exception) { name = ""; }
                            try { cate = workSheet.Cells[rowIterator, 2].Value.ToString(); } catch (Exception) { cate = ""; }
                            try { serial = workSheet.Cells[rowIterator, 3].Value.ToString(); } catch (Exception) { serial = ""; }
                            try { code = workSheet.Cells[rowIterator, 4].Value.ToString(); } catch (Exception) { code = ""; }
                            try { model = workSheet.Cells[rowIterator, 5].Value.ToString(); } catch (Exception) { model = ""; }
                            try { trademark = workSheet.Cells[rowIterator, 6].Value.ToString(); } catch (Exception) { trademark = ""; }
                            try { limited = workSheet.Cells[rowIterator, 7].Value.ToString(); } catch (Exception) { limited = ""; }
                            try { activeby = workSheet.Cells[rowIterator, 8].Value.ToString(); } catch (Exception) { activeby = ""; }
                            try { buydate = workSheet.Cells[rowIterator, 9].Value.ToString(); } catch (Exception) { buydate = ""; }
                            try { exportdate = workSheet.Cells[rowIterator, 10].Value.ToString(); } catch (Exception) { exportdate = ""; }
                            try { activedate = workSheet.Cells[rowIterator, 11].Value.ToString(); } catch (Exception) { activedate = ""; }
                            try { phoneactive = workSheet.Cells[rowIterator, 12].Value.ToString(); } catch (Exception) { phoneactive = ""; }
                            try { buyadr = workSheet.Cells[rowIterator, 13].Value.ToString(); } catch (Exception) { buyadr = ""; }
                            try { cusname = workSheet.Cells[rowIterator, 14].Value.ToString(); } catch (Exception) { cusname = ""; }
                            try { phone = workSheet.Cells[rowIterator, 15].Value.ToString(); } catch (Exception) { phone = ""; }
                            try { address = workSheet.Cells[rowIterator, 16].Value.ToString(); } catch (Exception) { address = ""; }
                            try { ward = workSheet.Cells[rowIterator, 17].Value.ToString(); } catch (Exception) { ward = ""; }
                            try { district = workSheet.Cells[rowIterator, 18].Value.ToString(); } catch (Exception) { district = ""; }
                            try { province = workSheet.Cells[rowIterator, 19].Value.ToString(); } catch (Exception) { province = ""; }
                            try { note = workSheet.Cells[rowIterator, 20].Value.ToString(); } catch (Exception) { note = ""; }



                            DateTime? bdate = null;
                            if (!string.IsNullOrEmpty(buydate))
                            {
                                try
                                {
                                    bdate = DateTime.ParseExact(buydate, "dd-MM-yyyy", null);
                                }
                                catch (Exception ex)
                                {
                                    bdate = DateTime.Parse(buydate);
                                }

                            }
                            DateTime? exdate = null;
                            if (!string.IsNullOrEmpty(exportdate))
                            {
                                try
                                {
                                    exdate = DateTime.ParseExact(exportdate, "dd-MM-yyyy", null);
                                }
                                catch (Exception ex)
                                {
                                    exdate = DateTime.Parse(exportdate);
                                }

                            }
                            DateTime? adate = null;
                            if (!string.IsNullOrEmpty(activedate))
                            {
                                try
                                {
                                    adate = DateTime.ParseExact(activedate, "dd-MM-yyyy", null);
                                }
                                catch (Exception ex)
                                {
                                    adate = DateTime.Parse(activedate);
                                }

                            }
                            int limit = 0;
                            DateTime? endd = null;
                            if (!string.IsNullOrEmpty(limited))
                            {
                                limit = int.Parse(limited);
                                endd = adate.Value.AddMonths(limit);
                            }
                            if (!string.IsNullOrEmpty(phoneactive))
                            {
                                phoneactive = phoneactive.Trim();
                            }
                            if (!string.IsNullOrEmpty(serial))
                            {
                                serial = serial.Trim();
                            }
                            if (!string.IsNullOrEmpty(code))
                            {
                                code = code.Trim();
                            }
                            string convert_phone = Utils.Control.formatUserId(phoneactive, 2);
                            string check_phone = Utils.Control.getMobileOperator(convert_phone) != "UNKNOWN" ? convert_phone : "0936020386";
                            string cusphone = check_phone;
                            //add thong tin rows vao product
                            var product = new Product()
                            {
                                Name = name,
                                ProductCate = cate,
                                Serial = serial,
                                Code = serial,
                                Model = model,
                                Trademark = trademark,
                                Limited = limit,
                                Active_by = activeby,
                                Buydate = bdate,
                                Exportdate = exdate,
                                Active_date = adate,
                                End_date = endd,
                                Active_phone = cusphone,
                                BuyAdr = buyadr,
                                Createdate = DateTime.Now,
                                Status = 1,
                                Createby = User.Identity.Name,
                                Note = note

                            };
                            var add_product = new Product()
                            {
                                Name = name,
                                Serial = serial,
                                Code = serial,
                                Limited = limit,
                                Status = 1,
                                Createdate = DateTime.Now,
                                Active_date = adate,
                                End_date = endd,
                                Buydate = bdate,
                                Active_phone = phone,
                                Createby = User.Identity.Name,
                                Note = note,

                            };
                            var check_product = db.Products.FirstOrDefault(a => a.Serial == serial);
                            if(check_product == null)
                            {
                                db.Products.Add(product);
                                db.SaveChanges();
                            }
                            
                            if (!string.IsNullOrEmpty(cusphone))
                            {
                                var customer = db.Customers.FirstOrDefault(a => a.Phone == cusphone);
                                if (customer == null)
                                {
                                    var _cus = new Customer()
                                    {
                                        Phone = cusphone,
                                        Name = cusname,
                                        Address = address,
                                        Createdate = adate
                                    };
                                    db.Customers.Add(_cus);
                                    db.SaveChanges();
                                }
                            }
                            ////check trung serial code
                            //if (!string.IsNullOrEmpty(serial))
                            //{
                            //    var old_product = db.Products.Where(a => a.Serial == serial);
                            //    if (old_product.Count() == 0)
                            //    {
                            //        db.Products.Add(product);
                            //        db.SaveChanges();
                            //    }

                            //}
                            list_product.Add(product);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                logger.Error(ex.InnerException);
            }
            return View(list_product);
        }

        public void Sanpham()
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");
            Sheet.Cells["A1"].Value = "stt";
            Sheet.Cells["B1"].Value = "tên sản phẩm";
            Sheet.Cells["C1"].Value = "serial";
            Sheet.Cells["D1"].Value = "mã cào";
            Sheet.Cells["E1"].Value = "model";
            Sheet.Cells["F1"].Value = "thương hiệu";
            Sheet.Cells["G1"].Value = "thời hạn";
            Sheet.Cells["H1"].Value = "ngày tạo";
            Sheet.Cells["I1"].Value = "đại lý";
            Sheet.Cells["K1"].Value = "ngày xuất";
            Sheet.Cells["L1"].Value = "sđt kích hoạt";
            Sheet.Cells["M1"].Value = "họ tên";
            Sheet.Cells["N1"].Value = "địa chỉ";
            Sheet.Cells["O1"].Value = "ngày kích hoạt";
            Sheet.Cells["P1"].Value = "ngày hết hạn";
            Sheet.Cells["Q1"].Value = "kênh";
            Sheet.Cells["R1"].Value = "kích hoạt bởi";
            Sheet.Cells["S1"].Value = "ghi chú";

            int index = 1;
            int row = 2;
            foreach (var item in listexc)
            {

                Sheet.Cells[string.Format("A{0}", row)].Value = index++;
                Sheet.Cells[string.Format("B{0}", row)].Value = item.Name;
                Sheet.Cells[string.Format("C{0}", row)].Value = item.Serial;
                Sheet.Cells[string.Format("D{0}", row)].Value = item.Code;
                Sheet.Cells[string.Format("E{0}", row)].Value = item.Model;
                Sheet.Cells[string.Format("F{0}", row)].Value = item.Trademark;
                Sheet.Cells[string.Format("G{0}", row)].Value = item.Limited;
                Sheet.Cells[string.Format("H{0}", row)].Value = (item.Createdate != null) ? item.Createdate.Value.ToString("dd/MM/yyyy") : "";
                Sheet.Cells[string.Format("I{0}", row)].Value = item.AgentC1;
                Sheet.Cells[string.Format("K{0}", row)].Value = (item.Exportdate != null) ? item.Exportdate.Value.ToString("dd/MM/yyyy") : "";
                Sheet.Cells[string.Format("L{0}", row)].Value = item.Active_phone;
                Sheet.Cells[string.Format("M{0}", row)].Value = item.CusName;
                Sheet.Cells[string.Format("N{0}", row)].Value = item.Address;
                Sheet.Cells[string.Format("O{0}", row)].Value = (item.Active_date != null) ? item.Active_date.Value.ToString("dd/MM/yyyy") : "";
                Sheet.Cells[string.Format("P{0}", row)].Value = (item.End_date != null) ? item.End_date.Value.ToString("dd/MM/yyyy") : "";
                Sheet.Cells[string.Format("Q{0}", row)].Value = item.Active_chanel;
                Sheet.Cells[string.Format("R{0}", row)].Value = item.Active_by;
                Sheet.Cells[string.Format("S{0}", row)].Value = item.Note;
                row++;
            }


            Sheet.Cells["A:AZ"].AutoFitColumns();
            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AddHeader("content-disposition", "attachment: filename=" + "Report.xlsx");
            Response.BinaryWrite(Ep.GetAsByteArray());
            Response.End();
        }
    }
}