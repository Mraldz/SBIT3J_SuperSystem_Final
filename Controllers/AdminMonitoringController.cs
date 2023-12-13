using SBIT3J_SuperSystem_Final.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Geom;
using Rotativa;
using Rotativa.Options;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Runtime.Remoting.Contexts;

namespace SBIT3J_SuperSystem_Final.Controllers
{

    //[Authorize]
    public class AdminMonitoringController : Controller
    {
        // GET: AdminMonitoring

        DatabaseConnectionEntities dbcon = new DatabaseConnectionEntities(); //Connection String
        public ActionResult Index()
        {
            return View();
        }

        ////////////////           THIS PART IS FOR DASHBOARD                  //////////////////////////

        public ActionResult Dashboard()
        {

            using (dbcon)
            {
                var monthlySalesData = dbcon.Sales_Transaction
                    .Where(s => s.Date != null) // Exclude null dates
                    .GroupBy(s => new { Year = s.Date.Value.Year, Month = s.Date.Value.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        TotalSales = g.Sum(s => s.Total_Amount) ?? 0 // Use 0 as the default value if Total_Amount is null
                    })
                    .AsEnumerable() // Switch to LINQ to Objects
                    .Select(g => new MonthlySalesViewModel
                    {
                        Month = $"{g.Year}-{g.Month}",
                        TotalSales = g.TotalSales
                    })
                    .OrderBy(g => g.Month)
                    .ToList();

                var salesGraphData = new SalesGraphViewModel
                {
                    MonthlySales = monthlySalesData
                };

                var CritStock = dbcon.Product_Info
                   .Where(p => p.Stock_Level <= 20)
                   .Count();
                ViewBag.CritStock = CritStock;

                var totalProduct = dbcon.Product_Info.Count();
                ViewBag.TotalProducts = totalProduct;

                var currentDailySale = dbcon.Sales_Transaction
                    .Where(st => DbFunctions.TruncateTime(st.Date.Value) ==
                DbFunctions.TruncateTime(DateTime.UtcNow))
                     .Sum(st => st.Total_Amount);
                ViewBag.CurrentDailySale = currentDailySale;

               var totalStockLevel = dbcon.Product_Info.Sum(p => p.Stock_Level);
                ViewBag.TotalStockLevel = totalStockLevel;


                return View(salesGraphData);
            }

        }


        ////////////////           THIS PART IS FOR SALES REVENUE                   //////////////////////////

        public ActionResult SalesRevenue(string searchFilter, DateTime? startDate, DateTime? endDate, string filterType)
        {
            var query = dbcon.Sales_Transaction.ToList(); // Convert to List

            if (!string.IsNullOrEmpty(searchFilter) && filterType == "search")
            {
                query = query.Where(x =>
                    x.Transaction_ID.ToString().Contains(searchFilter) ||
                    x.Total_Amount.ToString().Contains(searchFilter)
                ).ToList(); // Convert to List
            }

            if (startDate != null && endDate != null && filterType == "date")
            {
                query = query.Where(x => x.Date >= startDate && x.Date <= endDate).ToList(); // Convert to List
            }

            decimal? totalAmountSum = query.Sum(x => x.Total_Amount);
            ViewBag.totalSales = totalAmountSum;

            int totalCount = query.Count();
            ViewBag.TotalCount = totalCount;

            decimal? averageDailySales = totalCount > 0 ? totalAmountSum / totalCount : 0;
            decimal roundedAverageDailySales = Math.Round(averageDailySales.Value, 2);
            ViewBag.AverageDailySales = roundedAverageDailySales;
         

            return View(query.ToList());
        }


        public ActionResult GenerateSalesTransactionList(string searchFilter, DateTime? startDate, DateTime? endDate)
        {
            var query = GetSalesTransactionFiltered(searchFilter, startDate, endDate);


            var pdfResult = new ViewAsPdf("SalesTransactionListPrintPdf", query)
            {
                FileName = "Sales Revenue .pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageOrientation = Rotativa.Options.Orientation.Landscape,
                CustomSwitches = "--no-images", // Optional: Use this switch if you want to exclude images
            };

            return pdfResult;
        }

        private List<Sales_Transaction>GetSalesTransactionFiltered(string searchFilter, DateTime? startDate, DateTime? endDate)
        {
            var query = dbcon.Sales_Transaction.ToList(); // Convert to List

            if (!string.IsNullOrEmpty(searchFilter) )
            {
                query = query.Where(x =>
                    x.Transaction_ID.ToString().Contains(searchFilter) ||
                    x.Total_Amount.ToString().Contains(searchFilter)
                ).ToList(); // Convert to List
            }

            if (startDate != null && endDate != null )
            {
                query = query.Where(x => x.Date >= startDate && x.Date <= endDate).ToList(); // Convert to List
            }

            decimal? totalAmountSum = query.Sum(x => x.Total_Amount);
            ViewBag.totalSales = totalAmountSum;

            int totalCount = query.Count();
            ViewBag.TotalCount = totalCount;

            decimal? averageDailySales = totalCount > 0 ? totalAmountSum / totalCount : 0;
            decimal roundedAverageDailySales = Math.Round(averageDailySales.Value, 2);
            ViewBag.AverageDailySales = roundedAverageDailySales;

            return query.ToList();
        }


        //decimal? totalAmountSum = query.Sum(x => x.Total_Amount);
        //ViewBag.totalSales = totalAmountSum;

        //    int totalCount = query.Count();
        //ViewBag.TotalCount = totalCount;

        //    decimal? averageDailySales = totalCount > 0 ? totalAmountSum / totalCount : 0;
        //ViewBag.AverageDailySales = averageDailySales;

        ////////////////           THIS PART IS FOR PRODUCT REVENUE                   //////////////////////////
        public ActionResult ProductRevenue(string searchFilter, string filterType)
        {
            var query = (from std in dbcon.Sales_Transaction_Details
                         join pi in dbcon.Product_Info on std.Product_ID equals pi.Product_ID
                         join d in dbcon.Discounts on std.Discount_ID equals d.Discount_ID into discountGroup
                         from dg in discountGroup.DefaultIfEmpty()
                         group new { pi, std, dg } by new { pi.Product_Code, pi.Product_Name }
                               into g
                         select new ProductRevenueModel
                         {
                             Product_Code = g.Key.Product_Code,
                             Product_Name = g.Key.Product_Name,
                             Total_Quantity = g.Sum(x => x.std.Total_Quantity),
                             Price = g.Max(x => x.pi.Price),
                             Total_Discount = g.Sum(x => x.dg.Discount_Amount) ?? 0,
                             Total_Amount = g.Sum(x => (x.std.Total_Quantity * x.pi.Price) - (x.dg.Discount_Amount ?? 0))
                         }).OrderByDescending(x => x.Total_Amount).ToList();


            if (!string.IsNullOrEmpty(searchFilter) && filterType == "search")
            {
                searchFilter = searchFilter.ToLower(); // Convert to lowercase for case-insensitive search

                query = query.Where(x =>
                    x.Product_Code.ToLower().Contains(searchFilter) ||
                    x.Product_Name.ToLower().Contains(searchFilter) ||
                    x.Price.ToString().Contains(searchFilter) ||
                    x.Total_Quantity.ToString().Contains(searchFilter) ||
                    x.Total_Amount.ToString().Contains(searchFilter)
                ).ToList();
            }
            decimal? totalAmountSum = query.Sum(x => x.Total_Amount);
            ViewBag.totalSalesofProduct = totalAmountSum;

            decimal? totalDiscountAmount = query.Sum(x => x.Total_Discount);
            ViewBag.totalDiscAmount= totalDiscountAmount;

            decimal? totaItemSold= query.Sum(x => x.Total_Quantity);
            ViewBag.totalSoldItem = totaItemSold;

            return View(query);
            }
        public ActionResult GenerateProductRevenueList(string searchFilter)
        {
            var query = GetProductRevenueFiltered(searchFilter);

            var pdfResult = new ViewAsPdf("ProductRevenuePrintPdf", query)
            {
                FileName = "Product Revenue List.pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageOrientation = Rotativa.Options.Orientation.Landscape,
                CustomSwitches = "--no-images", // Optional: Use this switch if you want to exclude images
            };

            return pdfResult;
        }

        private List<ProductRevenueModel> GetProductRevenueFiltered(string searchFilter)
        {
            var query = (from std in dbcon.Sales_Transaction_Details
                         join pi in dbcon.Product_Info on std.Product_ID equals pi.Product_ID
                         join d in dbcon.Discounts on std.Discount_ID equals d.Discount_ID into discountGroup
                         from dg in discountGroup.DefaultIfEmpty()
                         group new { pi, std, dg } by new { pi.Product_Code, pi.Product_Name }
                         into g
                         select new ProductRevenueModel
                         {
                             Product_Code = g.Key.Product_Code,
                             Product_Name = g.Key.Product_Name,
                             Total_Quantity = g.Sum(x => x.std.Total_Quantity),
                             Price = g.Max(x => x.pi.Price),
                             Total_Discount = g.Sum(x => x.dg.Discount_Amount) ?? 0,
                             Total_Amount = g.Sum(x => (x.std.Total_Quantity * x.pi.Price) - (x.dg.Discount_Amount ?? 0))
                         }).OrderByDescending(x => x.Total_Amount).ToList();


            if (!string.IsNullOrEmpty(searchFilter))
            {
                searchFilter = searchFilter.ToLower(); // Convert to lowercase for case-insensitive search

                query = query.Where(x =>
                    x.Product_Code.ToLower().Contains(searchFilter) ||
                    x.Product_Name.ToLower().Contains(searchFilter) ||
                    x.Price.ToString().Contains(searchFilter) ||
                    x.Total_Quantity.ToString().Contains(searchFilter) ||
                    x.Total_Amount.ToString().Contains(searchFilter)
                ).ToList();
            }
            decimal? totalAmountSum = query.Sum(x => x.Total_Amount);
            ViewBag.totalSalesofProduct = totalAmountSum;

            decimal? totalDiscountAmount = query.Sum(x => x.Total_Discount);
            ViewBag.totalDiscAmount = totalDiscountAmount;

            decimal? totaItemSold = query.Sum(x => x.Total_Quantity);
            ViewBag.totalSoldItem = totaItemSold;

            return query.ToList();
        }

        public ActionResult Profit()
        {
            return View();
        }

        public ActionResult Losses()
        {
            return View();
        }

        public ActionResult CashierLogs()
        {

            return View(dbcon.AuditTrails.ToList());
        }


        ////////////////           THIS PART IS FOR OVER ALL ACTIVITES               //////////////////////////
        public ActionResult OverallActivities(string searchFilter, DateTime? startDate, DateTime? endDate, string filterType)
        {
            var query = from ea in dbcon.EmployeeAccounts
                        join ei in dbcon.EmployeeInformations on ea.Employee_ID equals ei.Employee_ID
                        join at in dbcon.AuditTrails on ea.Account_ID equals at.Account_ID
                        select new OverallActivitiesModel
                        {
                            Employee_ID = ei.Employee_ID,
                            Name = ei.First_Name,
                            Username = ea.Username,
                            Role = ea.Role,
                            Activity = at.Activity,
                            Date = at.Date
                        };

            if (!string.IsNullOrEmpty(searchFilter) && filterType == "search")
            {
                query = query.Where(x =>
                    x.Employee_ID.ToString().Contains(searchFilter) ||
                    x.Name.ToLower().Contains(searchFilter) ||
                    x.Username.ToLower().Contains(searchFilter) ||
                    x.Role.ToLower().Contains(searchFilter)
                );
            }

            if (startDate != null && endDate != null && filterType == "date")
            {
                query = query.Where(x => x.Date >= startDate && x.Date <= endDate);
            }

            var auditTrailLogs = query.ToList();

            return View(auditTrailLogs);
        }

        public ActionResult GeneratePdf(string searchFilter, DateTime? startDate, DateTime? endDate)
        {
            var query = GetFilteredData(searchFilter, startDate, endDate);

            var pdfResult = new ViewAsPdf("PrintPdf", query)
            {
                FileName = "OverallActivities.pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageOrientation = Rotativa.Options.Orientation.Landscape,
                CustomSwitches = "--no-images", // Optional: Use this switch if you want to exclude images
            };

            return pdfResult;
        }

        private List<OverallActivitiesModel> GetFilteredData(string searchFilter, DateTime? startDate, DateTime? endDate)
        {
            var query = from ea in dbcon.EmployeeAccounts
                        join ei in dbcon.EmployeeInformations on ea.Employee_ID equals ei.Employee_ID
                        join at in dbcon.AuditTrails on ea.Account_ID equals at.Account_ID
                        select new OverallActivitiesModel
                        {
                            Employee_ID = ei.Employee_ID,
                            Name = ei.First_Name,
                            Username = ea.Username,
                            Role = ea.Role,
                            Activity = at.Activity,
                            Date = at.Date
                        };

            // Apply filters
            if (!string.IsNullOrEmpty(searchFilter))
            {
                query = query.Where(x =>
                    x.Employee_ID.ToString().Contains(searchFilter) ||
                    x.Name.ToLower().Contains(searchFilter) ||
                    x.Username.ToLower().Contains(searchFilter) ||
                    x.Role.ToLower().Contains(searchFilter)
                );
            }

            if (startDate != null && endDate != null)
            {
                query = query.Where(x => x.Date >= startDate && x.Date <= endDate);
            }

            return query.ToList();
        }

        ////////////////           THIS PART IS FOR INVENTORY                  //////////////////////////
        public ActionResult Inventory(string searchFilter)
        {
            var query = dbcon.Product_Info.AsQueryable();

            if (!string.IsNullOrEmpty(searchFilter))
            {
                // Apply search filter
                query = query.Where(p =>
                    p.Product_Code.Contains(searchFilter) ||
                    p.Product_Name.Contains(searchFilter) ||
                    p.Category.Contains(searchFilter) ||
                    p.Price.ToString().Contains(searchFilter)
                );
            }
            return View(query.ToList());
        }

        private List<Product_Info> GetProductsFiltered(string searchFilter)
        {
            var query = dbcon.Product_Info.AsQueryable();

            if (!string.IsNullOrEmpty(searchFilter))
            {
                // Apply search filter
                query = query.Where(p =>
                    p.Product_Code.Contains(searchFilter) ||
                    p.Product_Name.Contains(searchFilter) ||
                    p.Category.Contains(searchFilter) ||
                    p.Price.ToString().Contains(searchFilter)
                );
            }
            return query.ToList();
        }

        public ActionResult GenerateInventoryList(string searchFilter)
        {
            var query = GetProductsFiltered(searchFilter);

            var pdfResult = new ViewAsPdf("InventoryListPrintPdf", query)
            {
                FileName = "InventoryList.pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageOrientation = Rotativa.Options.Orientation.Landscape,
                CustomSwitches = "--no-images", // Optional: Use this switch if you want to exclude images
            };

            return pdfResult;
        }
    }
}