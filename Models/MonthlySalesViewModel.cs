using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SBIT3J_SuperSystem_Final.Models
{
    public class MonthlySalesViewModel
    {
        public string Month { get; set; }
        public decimal? TotalSales { get; set; }
    }

    public class SalesGraphViewModel
    {
        public List<MonthlySalesViewModel> MonthlySales { get; set; }
    }
}