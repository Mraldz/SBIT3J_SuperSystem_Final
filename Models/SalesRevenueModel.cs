using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SBIT3J_SuperSystem_Final.Models
{
    public class SalesRevenueModel
    {
        public int TransactionDetailID { get; set; }
        public int TransactionID { get; set; }

        public Nullable<decimal> TotalAmount { get; set; }
        public Nullable<decimal> DiscountAmount { get; set; }
        public Nullable<decimal>TotalSales { get; set; }
        public Nullable<System.DateTime> Date { get; set; }

        // Additional properties for date filtering
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}