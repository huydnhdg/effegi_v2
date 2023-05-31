using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebApplication.Models;

namespace WebApplication.Areas.Admin.Data
{
    public class UploadProduct:Product
    {
        public string Error { get; set; }
        public Product Product { get; set; }
    }
}