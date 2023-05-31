using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Newtonsoft.Json;
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
    public class UserController : BaseController
    {
        Logger logger = LogManager.GetCurrentClassLogger();
        
        static IEnumerable<UserView> listexc = null;
        public ActionResult Index(int? page, string textsearch, string chanel, string status, string start_date, string end_date, string im_start_date, string im_end_date)
        {
            var model = from a in db.AspNetUsers
                        from b in a.AspNetRoles
                        //where b.Id != "Customer"
                        select new UserView()
                        {
                            Id = a.Id,
                            UserName = a.UserName,
                            PhoneNumber = a.PhoneNumber,
                            Email = a.Email,
                            Createdate = a.Createdate,
                            Createby = a.Createby,
                            Address = a.Address + " " + a.Ward + " " + a.District + " " + a.Province,
                            Role = b.Name,
                            FullName = a.FullName
                        };
            //hiển thị theo quyền
            if (User.IsInRole("Trạm - Trưởng trạm"))
            {
                model = model.Where(a => a.Createby == User.Identity.Name || a.UserName == User.Identity.Name);
            }
            //đại lý, thợ chỉ hiển thị tài khoản cá nhân
            if (User.IsInRole("Trạm - Nhân viên kỹ thuật") || User.IsInRole("Đại lý") || User.IsInRole("Nhân viên"))
            {
                model = model.Where(a => a.UserName == User.Identity.Name);
            }
            //lọc theo thông tin 
            if (!string.IsNullOrEmpty(textsearch))
            {
                model = model.Where(a => a.UserName.Contains(textsearch)
                || a.PhoneNumber.Contains(textsearch)
                || a.Email.Contains(textsearch)
                || a.Address.Contains(textsearch)
                || a.FullName.Contains(textsearch)
                || a.Role.ToString().Contains(textsearch));
                ViewBag.textsearch = textsearch;
            }
            if (!string.IsNullOrEmpty(chanel))
            {
                model = model.Where(a => a.Role.Contains(chanel));
                ViewBag.chanel = chanel;
            }

            if (!string.IsNullOrEmpty(start_date))
            {
                DateTime s = DateTime.ParseExact(start_date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                model = model.Where(a => a.Createdate >= s);
                ViewBag.start_date = start_date;
            }
            if (!string.IsNullOrEmpty(end_date))
            {
                DateTime s = DateTime.ParseExact(end_date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                s = s.AddDays(1);
                model = model.Where(a => a.Createdate <= s);
                ViewBag.end_date = end_date;
            }
            ViewBag.role = db.AspNetRoles.ToList();
            listexc = model as IEnumerable<UserView>;
            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(listexc.OrderByDescending(a => a.Createdate).ToPagedList(pageNumber, pageSize));
        }

        ApplicationDbContext context = new ApplicationDbContext();
        [HttpPost]
        public ActionResult Create()
        {
            if (User.IsInRole("Admin - Quản trị toàn hệ thống"))
            {
                ViewBag.role = context.Roles.ToList();
            }
            else
            {
                ViewBag.role = context.Roles.Where(a => a.Id == "KTV").ToList();
            }
            return PartialView("~/Areas/Admin/Views/User/_Create.cshtml", null);
        }

        [HttpPost]
        public JsonResult GetProvince()
        {
            var province = (from a in db.Provinces
                            orderby a.Name
                            select new { a.Name });
            return Json(province, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetDistrict(string text)
        {
            var district = (from a in db.Districts
                            orderby a.Name
                            where a.Province.Name.Equals(text)
                            select new { a.Name });
            return Json(district, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult GetWard(string text)
        {
            var ward = (from a in db.Wards
                        orderby a.Name
                        where a.District.Name.Equals(text)
                        select new { a.Name });
            return Json(ward, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Edit(string Id)
        {
            ViewBag.role = context.Roles.ToList();
            var model = db.AspNetUsers.Find(Id);
            return PartialView("~/Areas/Admin/Views/User/_Edit.cshtml", model);
        }

        [HttpPost]
        public ActionResult EditConfirm([Bind(Include = "")] AspNetUser _user, string Role)
        {
            if (ModelState.IsValid)
            {
                if (Utils.Control.getMobileOperator(_user.PhoneNumber) == "UNKNOWN")
                {
                    SetAlert("Số điện thoại không đúng", "danger");
                    return RedirectToAction("Index");
                }
                var user = db.AspNetUsers.Find(_user.Id);

                //string json = JsonConvert.SerializeObject(user);
                //logger.Info(string.Format("[Edit] @UserName={0} @User={1}", User.Identity.Name, json));
                //update quyền cho tài khoản
                string r = user.AspNetRoles.FirstOrDefault().Name;
                if (!user.AspNetRoles.FirstOrDefault().Name.Equals(Role))
                {
                    ApplicationUser u = context.Users.Find(user.Id);
                    //xoa di roi add lai la duoc
                    u.Roles.Remove(u.Roles.FirstOrDefault());
                    u.Roles.Add(new IdentityUserRole() { UserId = user.Id, RoleId = Role });
                    context.SaveChanges();
                }
                user.PhoneNumber = _user.PhoneNumber;
                user.Address = _user.Address;
                user.Ward = _user.Ward;
                user.District = _user.District;
                user.Province = _user.Province;
                user.Email = _user.Email;
                user.FullName = _user.FullName;
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                SetAlert("Đã lưu thông tin thành công.", "success");
                ViewBag.role = context.Roles.ToList();
                return RedirectToAction("Index");
            }
            SetAlert("Lưu thông tin không thành công. Hãy kiểm tra lại.", "danger");
            ViewBag.role = context.Roles.ToList();
            return RedirectToAction("Index");
        }
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;


        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
        public ActionResult UploadFile()
        {
            List<AspNetUser> list_product = new List<AspNetUser>();
            return View(list_product);
        }

        [HttpPost]
        public ActionResult UploadFile(FormCollection collection)
        {
            List<AspNetUser> list_product = new List<AspNetUser>();
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
                            string user;
                            string password;
                            string phone;
                            string address;
                            string ward;
                            string district;
                            string province;
                            string email;
                            string role;
                            string fullname;

                            try { user = workSheet.Cells[rowIterator, 1].Value.ToString(); } catch (Exception) { user = ""; }
                            try { password = workSheet.Cells[rowIterator, 2].Value.ToString(); } catch (Exception) { password = ""; }

                            try { address = workSheet.Cells[rowIterator, 3].Value.ToString(); } catch (Exception) { address = ""; }
                            try { ward = workSheet.Cells[rowIterator, 4].Value.ToString(); } catch (Exception) { ward = ""; }
                            try { district = workSheet.Cells[rowIterator, 5].Value.ToString(); } catch (Exception) { district = ""; }
                            try { province = workSheet.Cells[rowIterator, 6].Value.ToString(); } catch (Exception) { province = ""; }

                            try { phone = workSheet.Cells[rowIterator, 7].Value.ToString(); } catch (Exception) { phone = ""; }
                            try { email = workSheet.Cells[rowIterator, 8].Value.ToString(); } catch (Exception) { email = ""; }
                            try { role = workSheet.Cells[rowIterator, 9].Value.ToString(); } catch (Exception) { role = ""; }
                            try { fullname = workSheet.Cells[rowIterator, 10].Value.ToString(); } catch (Exception) { fullname = ""; }


                            //add thong tin rows vao product
                            var product = new AspNetUser()
                            {
                                UserName = user,
                                Address = address + " " + ward + " " + district + " " + province,
                                PhoneNumber = phone,
                                Email = email,
                                FullName = fullname
                            };
                            //check trung serial code
                            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(phone) && !string.IsNullOrEmpty(role))
                            {
                                var drole = db.AspNetRoles.Where(a => a.Id == role);
                                if (drole.Count() == 0)
                                {
                                    product.FullName = "phân quyền k đúng";
                                }
                                else if (phone.Length != 10)
                                {
                                    product.PhoneNumber = "sđt không đúng";
                                }
                                else
                                {
                                    //tao tai khoan
                                    try
                                    {
                                        var asp = new ApplicationUser
                                        {
                                            UserName = user,
                                            PhoneNumber = phone,
                                            Email = email
                                        };

                                        var result = UserManager.Create(asp, password);
                                        if (result.Succeeded)
                                        {
                                            //add quyền cho tài khoản luôn
                                            ApplicationUser u = context.Users.Find(asp.Id);
                                            u.Roles.Add(new IdentityUserRole() { UserId = asp.Id, RoleId = role });
                                            context.SaveChanges();

                                            //bổ sung thong tin tài khoản
                                            var _user = db.AspNetUsers.Find(asp.Id);
                                            _user.Address = address;
                                            _user.Ward = ward;
                                            _user.District = district;
                                            _user.Province = province;
                                            _user.Createby = User.Identity.Name;
                                            _user.Createdate = DateTime.Now;
                                            _user.FullName = fullname;
                                            db.Entry(_user).State = EntityState.Modified;
                                            db.SaveChanges();

                                            product.Id = asp.Id;
                                        }
                                        else
                                        {
                                            product.FullName = "không tạo được tài khoản";
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        product.FullName = ex.Message;
                                        logger.Error(ex.Message);
                                        logger.Error(ex.InnerException);
                                    }
                                }
                                

                            }
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

        public ActionResult Delete(string Id)
        {
            try
            {
                var model = db.AspNetUsers.Find(Id);
                db.AspNetUsers.Remove(model);
                db.SaveChanges();
                string json = JsonConvert.SerializeObject(model);
                logger.Info(string.Format("[Delete] @UserName={0} @User={1}", User.Identity.Name, json));
                SetAlert("Xóa tài khoản thành công.", "success");
            }
            catch (Exception ex)
            {
                SetAlert(ex.Message, "danger");
            }
            ViewBag.role = db.AspNetRoles.ToList();
            return RedirectToAction("Index");
        }

        public void Export_User()
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");
            Sheet.Cells["A1"].Value = "STT";
            Sheet.Cells["B1"].Value = "Mã đại lý";
            Sheet.Cells["C1"].Value = "Tên đại lý";
            Sheet.Cells["D1"].Value = "Quyền";
            Sheet.Cells["E1"].Value = "Số điện thoại";
            Sheet.Cells["F1"].Value = "Email";
            Sheet.Cells["G1"].Value = "Địa chỉ";
            Sheet.Cells["H1"].Value = "Ngày tạo";

            int index = 1;
            int row = 2;
            foreach (var item in listexc)
            {

                Sheet.Cells[string.Format("A{0}", row)].Value = index++;
                Sheet.Cells[string.Format("B{0}", row)].Value = item.UserName;
                Sheet.Cells[string.Format("C{0}", row)].Value = item.FullName;
                Sheet.Cells[string.Format("D{0}", row)].Value = item.Role;
                Sheet.Cells[string.Format("E{0}", row)].Value = item.PhoneNumber;
                Sheet.Cells[string.Format("F{0}", row)].Value = item.Email;
                Sheet.Cells[string.Format("G{0}", row)].Value = item.Address;
                Sheet.Cells[string.Format("H{0}", row)].Value = (item.Createdate != null) ? item.Createdate.Value.ToString("dd/MM/yyyy") : "";

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