using Newtonsoft.Json;
using NLog;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;
using WebApplication.Models;
using WebApplication.Utils;
using Product = WebApplication.Models.Product;

namespace WebApplication.API
{
    [RoutePrefix("api/sms")]
    public class SmsProviderController : ApiController
    {
        Logger logger = LogManager.GetCurrentClassLogger();
        Entities db = new Entities();
        [Route("receive")]
        [HttpGet]

        //https://localhost:44355//api/sms/receive?command_code=EFFEGI&user_id=6566668875&service_id=xx&request_id=xx&message=EFFEGI%20EFFEGI001
        public HttpResponseMessage Recevice(string Command_Code, string User_ID, string Service_ID, string Request_ID, string Message)
        {
            string MT = saicuphap;
            int status = 0;
            logger.Info(string.Format("[MO] @Command_Code= {0} @User_ID= {1} @Service_ID= {2} @Request_ID= {3} @Message= {4}", Command_Code, User_ID, Service_ID, Request_ID, Message));
            try
            {

                //format sdt ve dau 0
                User_ID = Utils.Control.formatUserId(User_ID, 0);
                string[] words = Message.ToUpper().Split(' ');

                if (words.Length == 2)
                {
                    if (words[1] == "BH")//bao loi
                    {
                        var customer = new Customer();
                        var old_customer = db.Customers.SingleOrDefault(a => a.Phone == User_ID);
                        if (customer == null)
                        {
                            MT = bh_notvalid;

                        }
                        else
                        {
                            customer = old_customer;
                            CreateWarranti(customer);
                            MT = bh_ok;
                            ;
                        }

                    }
                    else//kich hoat
                    {
                        string code = words[1];
                        var product = db.Products.SingleOrDefault(a => a.Code == code);
                        if (product == null)
                        {
                            MT = seri_notvalid;
                        }
                        else if (product.Active_date != null)
                        {
                            MT = string.Format(kh_actived, product.Code, product.Active_date.Value.ToString("dd/MM/yyyy"), product.End_date.Value.ToString("dd/MM/yyyy"));
                        }
                        else
                        {
                            //luu thong tin kich hoat
                            CreateActive(product, User_ID, "");
                            MT = string.Format(seri_actived, product.Code, product.Active_date.Value.ToString("dd/MM/yyyy"), product.End_date.Value.ToString("dd/MM/yyyy"));
                        }
                    }
                }
                else if (words.Length == 3)
                {
                    if (words[1] == "TC" || words[1] == "TRACUU")//tra cuu bao hanh
                    {
                        string code = words[2];
                        var product = db.Products.SingleOrDefault(a => a.Code == code);
                        //var today = DateTime.Now;
                        //today = today.AddDays(1);
                        if (product == null )
                        {
                            //khong ton tai serial
                            MT = seri_notvalid;
                        }
                        else if (product.Active_date == null)
                        {
                            //code chua kich hoat
                            MT = seri_not_actived;
                        }
                        else if (product.Active_date != null)
                        {
                            MT = string.Format(tracuu_tc, product.Code, product.Active_date.Value.ToString("dd/MM/yyyy"), product.End_date.Value.ToString("dd/MM/yyyy"));
                        }
                    }
                    //else//khong dung cu phap
                    //{

                    //    //thêm trường hợp kích hoạt có tên khách hàng
                    //    string code = words[2];
                    //    string name = words[3];
                    //    var product = db.Products.SingleOrDefault(a => a.Code == code);
                    //    if (product == null)
                    //    {
                    //        MT = seri_notvalid;
                    //    }
                    //    else if (product.Active_date != null)
                    //    {
                    //        MT = string.Format(kh_actived, product.Code, product.Active_date.Value.ToString("dd/MM/yyyy"), product.End_date.Value.ToString("dd/MM/yyyy"));
                    //    }
                    //    else
                    //    {
                    //        CreateActive(product, User_ID, name);

                    //        MT = string.Format(seri_actived, product.Code, product.Active_date.Value.ToString("dd/MM/yyyy"), product.End_date.Value.ToString("dd/MM/yyyy"));
                    //    }
                    //}
                }
                else if (words.Length == 4)
                {
                    string code = words[1];
                    string sdt_khachhang = words[2];
                    string user = words[3];
                    //sdt khách hàng không đúng=> tin nhắn k đúng cú pháp
                    if (Utils.Control.getMobileOperator(sdt_khachhang) == "UNKNOWN")
                    {
                        MT = agent_phone_notvalid;
                    }
                    //mã đại lý không tồn tại
                    var agent = db.AspNetUsers.SingleOrDefault(a => a.UserName == user);
                    if (agent == null)
                    {
                        MT = agent_notvalid;
                    }
                    else
                    {
                        var product = db.Products.SingleOrDefault(a => a.Code == code);
                        if (product == null)
                        {
                            MT = seri_notvalid;
                        }
                        else if (product.Active_date != null)
                        {
                            //kich hoat roi
                            MT = string.Format(agent_code_used,agent.UserName, product.Code, product.Active_date.Value.ToString("dd/MM/yyyy"), product.End_date.Value.ToString("dd/MM/yyyy"));
                        }
                        else
                        {
                            AgentActive(product, agent.UserName, sdt_khachhang);

                            MT = string.Format(agent_actived, product.Code, agent.UserName, product.Active_date.Value.ToString("dd/MM/yyyy"), product.End_date.Value.ToString("dd/MM/yyyy"));
                        }
                    }
                }
                else//khong dung cu phap
                {
                    MT = saicuphap;
                }
            }
            catch (Exception ex)
            {
                status = 0;
                MT = saicuphap;
                logger.Error(ex.Message);
            }

            logger.Info(MT);

            var result = new Result()
            {
                status = "0",
                message = MT,
                phone = User_ID
            };
            var log_mo = new LOG_MO()
            {
                User_Id = User_ID,
                Service_Id = Service_ID,
                Request_Id = Request_ID,
                Message = Message,
                Createdate = DateTime.Now,
                Status = status,
                Response = MT
            };
            db.LOG_MO.Add(log_mo);
            db.SaveChanges();
            //var response = new HttpResponseMessage();
            //response.Content = new StringContent("0|" + MT);
            //response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

            logger.Info(string.Format("[MT] @Command_Code= {0} @User_ID= {1} @Service_ID= {2} @Request_ID= {3} @Message= {4}", Command_Code, User_ID, Service_ID, Request_ID, MT));
            var response = new HttpResponseMessage();
            response.Content = new StringContent("0|" + MT);
            //response.Content = new StringContent(JsonConvert.SerializeObject(result), Encoding.UTF8, "application/json");
            return response;
        }
        Product AgentActive(Product product, string agent, string sdt_khachhang)
        {
            var old_customer = db.Customers.SingleOrDefault(a => a.Phone == sdt_khachhang);
            if (old_customer == null)
            {
                old_customer = new Customer()
                {
                    Phone = sdt_khachhang,
                    Createdate = DateTime.Now,
                    Chanel = "SMS"
                };
                db.Customers.Add(old_customer);
                db.SaveChanges();
            }
            //dai ly kich hoat
            product.Active_by = agent;
            //khach  hang tu kich hoat
            product.Active_phone = sdt_khachhang;
            product.Status = 1;

            //nếu ngày chờ kích hoạt quá ngày wait_time thì sử lý khác
            //ngày kích hoạt là ngày chờ + số ngày wait_day
            int wait_time = product.Wait_day;
            var date = DateTime.Now.AddDays(-wait_time);
            if (product.Status == 3 && product.Wait_date < date)
            {
                product.Customer_date = DateTime.Now;
                product.Active_date = product.Wait_date.Value.AddDays(wait_time);
                product.End_date = product.Active_date.Value.AddMonths(product.Limited);
            }
            else
            {
                product.Active_date = DateTime.Now;
                product.End_date = DateTime.Now.AddMonths(product.Limited);
            }

            product.Active_chanel = "SMS";
            db.Entry(product).State = EntityState.Modified;

            //thưởng kích hoạt cho đại lý tài khoản có phải đại lý không
            if (ConfigControl.isBonusActive() == true)
            {
                //log thưởng cho đại lý theo sản phẩm
                var agent_active = new Agent_Log_Active()
                {
                    Createdate = DateTime.Now,
                    Model = product.Model,
                    Product = product.Code,
                    UserName = product.Active_by,
                    Amount = Getpricemodel(product.Model)
                };
                db.Agent_Log_Active.Add(agent_active);
                //check role
                var _agency = db.AspNetUsers.SingleOrDefault(a => a.UserName == agent);
                if (_agency != null)
                {
                    var role = _agency.AspNetRoles.FirstOrDefault().Id;
                    if (role == "Agent")
                    {
                        //topup luôn
                        logger.Info(string.Format("SMS ACTIVE TOPUP @Phone{0} @Amount{1}", _agency.PhoneNumber, agent_active.Amount));
                        string TOPUP = Utils.TOPUP.TopuptoUserId(_agency.PhoneNumber, agent_active.Amount.ToString());
                        logger.Info(TOPUP);
                        //lưu lại thông tin 
                        var _bonus = db.Agent_Bonus.SingleOrDefault(a => a.UserName == product.Active_by);
                        if (_bonus != null)
                        {
                            _bonus.Used = _bonus.Used + agent_active.Amount;
                            _bonus.Active = _bonus.Active + agent_active.Amount;
                            _bonus.Newdate = DateTime.Now;
                            db.Entry(_bonus).State = EntityState.Modified;
                        }
                        else
                        {
                            var agent_bonus = new Agent_Bonus()
                            {
                                UserName = product.Active_by,
                                Used = agent_active.Amount,
                                Active = agent_active.Amount,
                                Createdate = DateTime.Now,
                                Newdate = DateTime.Now
                            };
                            db.Agent_Bonus.Add(agent_bonus);
                        }
                    }
                    else if (role == "Staff")
                    {
                        //lưu thông tin lại
                        //cộng tiền vào tài khoản cho staff
                        var _bonus = db.Agent_Bonus.SingleOrDefault(a => a.UserName == product.Active_by);
                        if (_bonus != null)
                        {
                            _bonus.Active = _bonus.Active + agent_active.Amount;
                            _bonus.Newdate = DateTime.Now;
                            db.Entry(_bonus).State = EntityState.Modified;
                        }
                        else
                        {
                            var agent_bonus = new Agent_Bonus()
                            {
                                UserName = product.Active_by,
                                Active = agent_active.Amount,
                                Createdate = DateTime.Now,
                                Newdate = DateTime.Now
                            };
                            db.Agent_Bonus.Add(agent_bonus);
                        }
                    }
                }

            }
            //thưởng cho khách hàng
            AddBonus(product.Model, product.Code, old_customer);
            db.SaveChanges();
            return product;
        }
        Product CreateActive(Product product, string User_ID, string name)
        {
            var customer = db.Customers.SingleOrDefault(a => a.Phone == User_ID);
            if (customer == null)
            {
                customer = new Customer()
                {
                    Name = name,
                    Phone = User_ID,
                    Createdate = DateTime.Now,
                    Chanel = "SMS"
                };
                db.Customers.Add(customer);
                db.SaveChanges();
            }
            product.Active_phone = User_ID;
            product.Status = 1;

            //nếu ngày chờ kích hoạt quá ngày wait_time thì sử lý khác
            //ngày kích hoạt là ngày chờ + số ngày wait_day
            int wait_time = product.Wait_day;
            var date = DateTime.Now.AddDays(-wait_time);
            if (product.Status == 3 && product.Wait_date < date)
            {
                product.Customer_date = DateTime.Now;
                product.Active_date = product.Wait_date.Value.AddDays(wait_time);
                product.End_date = product.Active_date.Value.AddMonths(product.Limited);
            }
            else
            {
                product.Active_date = DateTime.Now;
                product.End_date = DateTime.Now.AddMonths(product.Limited);
            }

            product.Active_chanel = "SMS";
            db.Entry(product).State = EntityState.Modified;
            //them chuong trinh khuyen mai
            //AddBonus(product.Model, product.Code, customer);
            db.SaveChanges();
            return product;
        }
        void AddBonus(string Model, string ProductCode, Customer customer)
        {
            if (!string.IsNullOrEmpty(Model))
            {
                var bonus = db.B_Model_Process.Where(a => a.Model == Model && a.Status == true);
                if (bonus.Count() > 0)
                {
                    foreach (var item in bonus)
                    {
                        var process = db.B_Process.SingleOrDefault(a => a.Id == item.Process && a.Status == true);
                        if (process != null)
                        {
                            if (process.Startdate != null && process.Enddate != null)
                            {
                                //check hạn sử dụng chương trình
                                if (process.Startdate.Value.Date > DateTime.Now.Date)
                                {
                                    //chưa đến ngày được tham gia chương trình
                                    continue;
                                }
                                if (process.Enddate.Value.Date < DateTime.Now.Date)
                                {
                                    //hết ngày tham gia chương trình
                                    continue;
                                }
                            }
                            var log = new B_Log_Active()
                            {
                                Model = Model,
                                Process = process.Name,
                                ProcessId = item.Process,
                                ProductCode = ProductCode,
                                Unit = process.Unit,
                                Quantity = process.Quantity
                            };
                            db.B_Log_Active.Add(log);
                            if (process.Name == "TD")
                            {
                                customer.PointActive = customer.PointActive + process.Quantity;
                            }
                        }
                    }
                    //db.SaveChanges();
                }
            }

        }
        void CreateWarranti(Customer customer)
        {
            var warranti = new Warranti()
            {
                Status = 0,
                Createdate = DateTime.Now,
                Chanel = "SMS",
                Phone = customer.Phone,
                Address = customer.Address,
                Ward = customer.Ward,
                District = customer.District,
                Province = customer.Province

            };
            db.Warrantis.Add(warranti);
            db.SaveChanges();
            //tao ma phieu bao hanh 
            warranti.Code = Utils.Control.CreateCode(warranti.Id);
            db.Entry(warranti).State = EntityState.Modified;
            db.SaveChanges();
        }
        int Getpricemodel(string Model)
        {
            var model = db.Model_Price.SingleOrDefault(a => a.Model == Model);
            if (model != null)
            {
                return model.Price;
            }
            else
            {
                return 0;
            }
        }
        class Result
        {
            public string message { get; set; }
            public string status { get; set; }
            public string phone { get; set; }
        }

        string tracuu_tc = "San pham {0} da duoc kich hoat tu ngay  {1}  den ngay  {2} , Lien he CSKH 0966328980 de duoc ho tro. Cam on quy khach";
        string saicuphap = "Cu phap khong dung, vui long soan EFFEGI [ Ma cao ]gui 8077. Moi thong tin lien he 0966328980. Tran trong cam on.";
        string seri_notvalid = "Ma Kich Hoat san pham cua ban khong ton tai. Vui long kiem tra lai Ma Kich Hoat hoac lien he CSKH 0966328980 de duoc ho tro. Cam on Quy khach";
        string seri_actived = "San pham {0} da kich hoat thanh cong tu ngay {1} den ngay {2}. Lien he CSKH 0966328980 de duoc ho tro. Cam on Quy khach";
        string bh_ok = "Yeu cau bao hanh cua ban da duoc ghi nhan, chung toi se lien he lai trong vong 24h. Lien he CSKH 0966328980 de duoc ho tro. Cam on quy khach";
        string bh_notvalid = "So dien thoai yeu cau bao hanh cua ban khong ton tai. Lien he CSKH 0966328980 de duoc ho tro. Cam on quy khach.	";
        string kh_actived = "San pham {0} da duoc kich hoat tu ngay {1} den ngay {2}. Lien he CSKH 0966328980 de duoc ho tro. Cam on quy khach";
        string agent_actived = "San pham {0} kich hoat thanh cong tai {1} vao ngay {2}, ngay het han {3}.  Lien he CSKH 0966328980 de duoc ho tro. Tran trong cam on.";
        string agent_notvalid = "Ma Dai Ly cua ban khong ton tai. Kich hoat khong thanh cong. Lien he CSKH 0966328980 de duoc ho tro. Cam on quy khach.";
        string agent_phone_notvalid = "SDTKH khong dung (Gom 10 ki tu la SO), vui long kiem tra lai. Moi thong tin lien he 0966328980.Tran trong cam on.";
        string agent_code_used = "San pham {0} da duoc kich hoat thanh cong tai {1} tu ngay {2} den ngay {3}. Lien he CSKH 0966328980 de duoc ho tro.";
        string seri_not_actived = "Ma Kich Hoat san pham cua ban chua duoc kich hoat. Vui long kich hoat lai ma kich hoat hoac lien he CSKH 0966328980 de duoc ho tro. Cam on quy khach";


    }
}
