using NLog;
using OfficeOpenXml;
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

namespace WebApplication.Areas.Admin.Controllers
{
    public class ABigController : BaseController
    {
        Logger logger = LogManager.GetCurrentClassLogger();
        //ELMEntities db = new ELMEntities();
        public ActionResult Index(int? page, string textsearch, string chanel, string status, string start_date, string end_date, string im_start_date, string im_end_date)
        {
            var model = from a in db.Accessary_Big
                        select new A_B_Model()
                        {
                            Id = a.Id,
                            Code = a.Code,
                            CountExport = a.CountExport,
                            CountImport = a.CountImport,
                            Exist = a.CountImport - a.CountExport,
                            KeyPrice = a.KeyPrice,
                            Name = a.Name,
                            ProductName = a.ProductName,
                            Discount = a.Discount,
                            Model = a.Model
                        };
            if (!string.IsNullOrEmpty(textsearch))
            {
                model = model.Where(a => a.Name.Contains(textsearch)
                || a.Code.Contains(textsearch)
                || a.ProductName.Contains(textsearch)
                || a.KeyPrice.ToString().Contains(textsearch)
                || a.Discount.ToString().Contains(textsearch));
                ViewBag.textsearch = textsearch;
            }

            IEnumerable<A_B_Model> data = model as IEnumerable<A_B_Model>;
            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(data.OrderBy(a => a.Name).ToPagedList(pageNumber, pageSize));
        }

        [HttpPost]
        public ActionResult Create()
        {
            return PartialView("~/Areas/Admin/Views/ABig/_Create.cshtml", null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateConfirm([Bind(Include = "")] Accessary_Big accessary)
        {
            if (ModelState.IsValid)
            {
                db.Accessary_Big.Add(accessary);
                db.SaveChanges();
                SetAlert("Đã lưu thông tin thành công", "success");
                return RedirectToAction("Index");
            }
            SetAlert("Liên hệ ngày quản trị hệ thống", "danger");
            return RedirectToAction("Index");

        }
        [HttpPost]
        public ActionResult Edit(long Id)
        {
            var model = db.Accessary_Big.Find(Id);
            return PartialView("~/Areas/Admin/Views/ABig/_Edit.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditConfirm([Bind(Include = "")] Accessary_Big accessary)
        {
            if (ModelState.IsValid)
            {
                db.Entry(accessary).State = EntityState.Modified;
                db.SaveChanges();
                SetAlert("Đã lưu thông tin thành công", "success");
                return RedirectToAction("Index");
            }
            SetAlert("Liên hệ ngày quản trị hệ thống", "danger");
            return RedirectToAction("Index");

        }

        public ActionResult UploadFile()
        {
            List<Accessary_Import> list_product = new List<Accessary_Import>();
            return View(list_product);
        }

        [HttpPost]
        public ActionResult UploadFile(FormCollection collection)
        {
            List<Accessary_Import> list_product = new List<Accessary_Import>();
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
                            string cimport;
                            string code;
                            string name;
                            string cate;
                            string quantity;
                            string price;
                            string note;
                            string model;

                            try { cimport = workSheet.Cells[rowIterator, 1].Value.ToString(); } catch (Exception) { cimport = ""; }
                            try { code = workSheet.Cells[rowIterator, 2].Value.ToString(); } catch (Exception) { code = ""; }
                            try { name = workSheet.Cells[rowIterator, 3].Value.ToString(); } catch (Exception) { name = ""; }
                            try { cate = workSheet.Cells[rowIterator, 4].Value.ToString(); } catch (Exception) { cate = ""; }
                            try { model = workSheet.Cells[rowIterator, 5].Value.ToString(); } catch (Exception) { model = ""; }
                            try { quantity = workSheet.Cells[rowIterator, 6].Value.ToString(); } catch (Exception) { quantity = ""; }
                            try { price = workSheet.Cells[rowIterator, 7].Value.ToString(); } catch (Exception) { price = ""; }
                            try { note = workSheet.Cells[rowIterator, 8].Value.ToString(); } catch (Exception) { note = ""; }


                            var import = new Accessary_Import()
                            {
                                CodeImport = cimport,
                                Code = code,
                                Name = name,
                                ProductName = cate,
                                Quantity = int.Parse(quantity),
                                Price = int.Parse(price),
                                Amount = int.Parse(quantity) * int.Parse(price),
                                Createdate = DateTime.Now,
                                Note = note,
                                Model = model
                            };
                            db.Accessary_Import.Add(import);

                            var item = db.Accessary_Big.SingleOrDefault(a => a.Code == import.Code);
                            if (item != null)
                            {
                                //update số lượng tổng nhập
                                item.CountImport = item.CountImport + import.Quantity;
                                db.Entry(item).State = EntityState.Modified;
                                db.SaveChanges();
                            }
                            else
                            {
                                //tạo mới linh kiện,
                                var a_big = new Accessary_Big()
                                {
                                    Code = code,
                                    Name = name,
                                    ProductName = cate,
                                    KeyPrice = import.Price,
                                    CountImport = import.Quantity,
                                    Model = import.Model
                                };
                                db.Accessary_Big.Add(a_big);
                                db.SaveChanges();
                            }
                            //add list item vao view
                            list_product.Add(import);
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

        public ActionResult ExportUploadFile()
        {
            List<ExportViewModel> list_product = new List<ExportViewModel>();
            return View(list_product);
        }

        [HttpPost]
        public ActionResult ExportUploadFile(FormCollection collection)
        {
            List<Accessary_Export> list_order = new List<Accessary_Export>();
            List<ExportViewModel> list_product = new List<ExportViewModel>();
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
                            string code = "";
                            string station = "";
                            string item_code = "";
                            string quantity = "";
                            string orderdate = "";
                            string note = "";

                            try { code = workSheet.Cells[rowIterator, 1].Value.ToString(); } catch (Exception) { }
                            try { station = workSheet.Cells[rowIterator, 2].Value.ToString(); } catch (Exception) { }
                            try { item_code = workSheet.Cells[rowIterator, 3].Value.ToString(); } catch (Exception) { }
                            try { quantity = workSheet.Cells[rowIterator, 4].Value.ToString(); } catch (Exception) { }
                            try { orderdate = workSheet.Cells[rowIterator, 5].Value.ToString(); } catch (Exception) { }
                            try { note = workSheet.Cells[rowIterator, 6].Value.ToString(); } catch (Exception) { }

                            //check mã phiếu mới thì cho tạo cũ thì bỏ qua
                            var product_view = new ExportViewModel()
                            {
                                Code = code,
                                Station = station,
                                Item_Code = item_code,
                                Quantity = quantity,
                                Orderdate = orderdate,
                                Note = note
                            };
                            try
                            {
                                long ID = 0;
                                DateTime? date = null;
                                if (!string.IsNullOrEmpty(orderdate))
                                {
                                    date = DateTime.ParseExact(orderdate, "dd/MM/yyyy", null);
                                }
                                var check_code_export = db.Accessary_Export.FirstOrDefault(a => a.Code == code);
                                if (check_code_export == null)
                                {
                                    //mã phiếu mới
                                    var export = new Accessary_Export()
                                    {
                                        Code = code,
                                        Createdate = DateTime.Now,
                                        Id_Key = station,
                                        Status = true
                                    };
                                    db.Accessary_Export.Add(export);
                                    db.SaveChanges();
                                    ID = export.Id;
                                    list_order.Add(export);
                                }
                                else if (check_code_export.Status == true)
                                {
                                    //mã phiếu chưa đóng
                                    ID = check_code_export.Id;
                                }
                                else
                                {
                                    product_view.Error = "Mã đã bị trùng";
                                    //add list item vao view
                                    list_product.Add(product_view);
                                    continue;
                                }
                                var check_station = db.AspNetUsers.FirstOrDefault(a => a.UserName == station);
                                if (check_station == null)
                                {
                                    product_view.Error = "Không tồn tại trung tâm bảo hành";
                                    //add list item vao view
                                    list_product.Add(product_view);
                                    continue;
                                }
                                var check_code_item = db.Accessary_Big.FirstOrDefault(a => a.Code == item_code);
                                int _qty = int.Parse(quantity);
                                int _price = check_code_item.KeyPrice;
                                int _amount = _price * _qty;
                                if (check_code_item != null && (check_code_item.CountImport - check_code_item.CountExport) >= _qty)
                                {
                                    //còn linh kiện ở tổng
                                    //trừ ở tổng
                                    check_code_item.CountExport = check_code_item.CountExport + _qty;
                                    db.Entry(check_code_item).State = EntityState.Modified;
                                    //cộng ở trạm
                                    string Id = Guid.NewGuid().ToString();
                                    var check_code_station = db.Accessary_Key.FirstOrDefault(a => a.Code == item_code && a.Id_Key == station);
                                    if (check_code_station == null)
                                    {
                                        //trạm chưa có linh kiện này
                                        var import = new Accessary_Key()
                                        {
                                            Id = Id,
                                            Id_Key = station,
                                            CountImport = _qty,
                                            Code = item_code,
                                            Name = check_code_item.Name,
                                            ProductName = check_code_item.ProductName,
                                            Price = 0,
                                            KeyPrice = _price,
                                            Discount = 0
                                        };
                                        db.Accessary_Key.Add(import);
                                        db.SaveChanges();
                                    }
                                    else
                                    {
                                        Id = check_code_station.Id;
                                        check_code_station.CountImport = check_code_station.CountImport + _qty;
                                        db.Entry(check_code_station).State = EntityState.Modified;
                                    }
                                    //thêm item vào phiếu
                                    var item_export = new Acessary_Export_Item()
                                    {
                                        Id_Export = ID,
                                        Code = item_code,
                                        Price = _price,
                                        Quantity = _qty,
                                        Amount = _amount,
                                        Name = check_code_item.Name,
                                        Model = check_code_item.Model,
                                        Orderdate = date,
                                        Note = note
                                    };
                                    db.Acessary_Export_Item.Add(item_export);
                                    //ghi log cho phiếu
                                    var log_akey = new Acessary_Log_Key()
                                    {
                                        Accessary = item_code,
                                        Code = code,
                                        Count = _qty,
                                        Createdate = DateTime.Now,
                                        Id_Akey = Id,
                                        Type = 1
                                    };
                                    db.Acessary_Log_Key.Add(log_akey);
                                    db.SaveChanges();
                                    //đóng phiếu 
                                    list_product.Add(product_view);

                                }
                                else
                                {
                                    product_view.Error = "Linh kiện không còn đủ ở trung tâm";
                                    //add list item vao view
                                    list_product.Add(product_view);
                                    continue;
                                }
                            }
                            catch (Exception ex)
                            {
                                product_view.Error = ex.Message;
                                list_product.Add(product_view);
                            }



                        }

                        if (list_order.Count() > 0)
                        {
                            foreach (var item in list_order)
                            {
                                var order = db.Accessary_Export.Find(item.Id);
                                var item_order = db.Acessary_Export_Item.FirstOrDefault(a => a.Id_Export == item.Id);
                                if (order != null && item_order != null)
                                {
                                    order.Status = false;
                                    db.Entry(order).State = EntityState.Modified;
                                }
                            }
                            db.SaveChanges();
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
        public ActionResult Export()
        {
            var model = new A_Export_Model()
            {
                ListItem = new List<Acessary_Export_Item>()
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult Export([Bind(Include = "")] A_Export_Model a_Export_Model)
        {
            int ERROR = 0;
            long ID = 0;
            string msg = "";
            try
            {
                //tạo phiếu xuất hàng
                var export = new Accessary_Export()
                {
                    Code = a_Export_Model.Code.ToUpper(),
                    Id_Key = a_Export_Model.Id_Key,
                    Createdate = DateTime.Now,
                    Note = a_Export_Model.Note
                };
                db.Accessary_Export.Add(export);
                db.SaveChanges();
                //lưu items cho phiếu
                if (a_Export_Model.ListItem.Count > 0)
                {
                    foreach (var item in a_Export_Model.ListItem)
                    {
                        if (!string.IsNullOrEmpty(item.Code))
                        {
                            var big_acc = db.Accessary_Big.SingleOrDefault(a => a.Code == item.Code);
                            if (big_acc != null && (big_acc.CountImport - big_acc.CountExport > 0))
                            {
                                var itemmodel = new Acessary_Export_Item()
                                {
                                    Id_Export = export.Id,
                                    Code = item.Code,
                                    Price = item.Price,
                                    Quantity = item.Quantity,
                                    Amount = item.Amount,
                                    Name = item.Name,
                                    Model = item.Model,
                                    Orderdate = item.Orderdate,
                                    Note = item.Note
                                };
                                db.Acessary_Export_Item.Add(itemmodel);
                                //cộng linh kiện cho trạm
                                string idkey = "";
                                var _old = db.Accessary_Key.Where(a => a.Id_Key == a_Export_Model.Id_Key).SingleOrDefault(a => a.Code == item.Code);
                                if (_old != null)
                                {
                                    big_acc.CountExport = big_acc.CountExport + item.Quantity;
                                    db.Entry(big_acc).State = EntityState.Modified;
                                    //da co linh kien nay update thoi
                                    _old.CountImport = _old.CountImport + item.Quantity;
                                    db.Entry(_old).State = EntityState.Modified;

                                    idkey = _old.Id;
                                }
                                else
                                {
                                    big_acc.CountExport = big_acc.CountExport + item.Quantity;
                                    db.Entry(big_acc).State = EntityState.Modified;
                                    //chua co linh kien nay add new
                                    var a_key = new Accessary_Key()
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        Name = big_acc.Name,
                                        Code = big_acc.Code,
                                        ProductName = big_acc.ProductName,
                                        KeyPrice = big_acc.KeyPrice,
                                        CountImport = item.Quantity,
                                        Id_Key = a_Export_Model.Id_Key
                                    };
                                    db.Accessary_Key.Add(a_key);
                                    idkey = a_key.Id;
                                }

                                var log_akey = new Acessary_Log_Key()
                                {
                                    Accessary = itemmodel.Code,
                                    Code = export.Code,
                                    Count = itemmodel.Quantity,
                                    Createdate = DateTime.Now,
                                    Id_Akey = idkey,
                                    Type = 1//log nhap
                                };
                                db.Acessary_Log_Key.Add(log_akey);
                            }
                            else
                            {
                                ERROR++;
                                msg = "Mã linh kiện này không còn trong kho.";
                                break;
                            }
                        }
                        else
                        {
                            ERROR++;
                            msg = "Không xác định được mã linh kiện.";
                            break;
                        }
                    }
                    if (ERROR == 0)
                    {
                        db.SaveChanges();
                        SetAlert("Đã lưu thông tin thành công.", "success");
                        return RedirectToAction("Index", "AExport");
                    }
                }
                else
                {
                    msg = "Không có linh kiện nào được chọn trong phiếu xuất.";
                }
            }
            catch (Exception ex)
            {
                //xoa phieu khong luu linh kien thanh cong
                if (ID > 0)
                {
                    var model = db.Accessary_Export.Find(ID);
                    db.Accessary_Export.Remove(model);
                    db.SaveChanges();
                }
                logger.Error(ex.Message);
                logger.Error(ex.InnerException);
                SetAlert(ex.Message, "danger");
                return View(a_Export_Model);
            }
            SetAlert(msg, "danger");
            return View(a_Export_Model);
        }

        [HttpPost]
        public ActionResult GetPriceAccess(string name)
        {
            var product = db.Accessary_Big.Where(a => a.Code == name || a.Name == name).SingleOrDefault();
            return Json(product, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetAccessary(string text)
        {
            var cate = (from a in db.Accessary_Big
                        where a.Code.Contains(text) || a.Name.Contains(text)
                        select new { a.Code, a.Name, a.Model });
            return Json(cate, JsonRequestBehavior.AllowGet);
        }
    }
}