using SBIT3J_SuperSystem_Final.Models;
using SBIT3J_SuperSystem_Final.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SBIT3J_SuperSystem_Final.Controllers
{
    //[Authorize]
    public class POSController : Controller
    {
        // GET: POS
        private DatabaseConnectionEntities objDatabaseConnectionEntities;

        public POSController()
        {
            objDatabaseConnectionEntities = new DatabaseConnectionEntities();
        }
        // GET: POS
        public ActionResult Index()
        {
            ProductRepository objProductRepository = new ProductRepository();
            DiscountRepository objDiscountRepository = new DiscountRepository();

            var objMultepleModel = new Tuple<IEnumerable<SelectListItem>, IEnumerable<SelectListItem>>
                (objProductRepository.GetAllProduct(), objDiscountRepository.GetAllDiscount());


            objDatabaseConnectionEntities = new DatabaseConnectionEntities();
            return View(objMultepleModel);
        }



        [HttpGet]
        public JsonResult getItemUnitPrice(int ProductID)
        {
            decimal price = (decimal)objDatabaseConnectionEntities.Product_Info.Single(model => model.Product_ID == ProductID).Price;
            return Json(price, JsonRequestBehavior.AllowGet);

        }

        [HttpGet]
        public JsonResult getDiscountvalue(int DiscountId)
        {
            decimal discount = (decimal)objDatabaseConnectionEntities.Discounts.Single(model => model.Discount_ID == DiscountId).Discount_Amount;
            return Json(discount, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Index(Sales_Transaction objSalesTransaction)
        {
            TransactionRepository objTransactionRepository = new TransactionRepository();
            objTransactionRepository.AddOrder(objSalesTransaction);
            return Json("Sales saved, Thanks for shopping with us!", JsonRequestBehavior.AllowGet);

        }

    }
}