using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SBIT3J_SuperSystem_Final.Models
{
    public class ProductRevenueModel
    {
        public string Product_Code { get; set; }
        public string Product_Name { get; set; }
        public Nullable<int> Total_Quantity { get; set; }
        public Nullable<decimal> Price { get; set; }
        public Nullable<decimal> Total_Discount { get; set; }
        public Nullable<decimal> Total_Amount { get; set; }
    }
}