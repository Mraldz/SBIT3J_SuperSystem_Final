using SBIT3J_SuperSystem_Final.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SBIT3J_SuperSystem_Final.Repository
{
    public class RestockRepository
    {
        private DatabaseConnectionEntities objSBIT3JEntities;

        public RestockRepository()
        {
            objSBIT3JEntities = new DatabaseConnectionEntities();
        }

        public bool AddRestock(Restock objRestock)
        {
            try
            {
                using (var transaction = objSBIT3JEntities.Database.BeginTransaction())
                {
                    try
                    {
                        Restock restock = objRestock;
                        restock.Date = DateTime.Now;
                        restock.Total_Amount = objRestock.Total_Amount;

                        objSBIT3JEntities.Restocks.Add(restock);
                        objSBIT3JEntities.SaveChanges();
                        int RestockID = restock.Restock_ID;

                        foreach (var item in objRestock.ListofRestockDetailViewModel)
                        {
                            Restock_Detail objRestock_Detail = new Restock_Detail();

                            objRestock_Detail.Restock_ID = RestockID;
                            objRestock_Detail.Product_ID = item.Product_ID;
                            objRestock_Detail.Quantity_Added = item.Quantity_Added;
                            objRestock_Detail.Total_Amount = item.Total_Amount;
                            objSBIT3JEntities.Restock_Detail.Add(objRestock_Detail);
                            objSBIT3JEntities.SaveChanges();

                            // Update Stock Level
                            UpdateStockLevel((int)item.Product_ID, (int)item.Quantity_Added);
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");

                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        private void UpdateStockLevel(int productID, int quantityAdded)
        {
            var product = objSBIT3JEntities.Product_Info.Find(productID);

            if (product != null)
            {
                // Update stock level
                product.Stock_Level += quantityAdded;
                objSBIT3JEntities.SaveChanges();
            }
        }
    }
}