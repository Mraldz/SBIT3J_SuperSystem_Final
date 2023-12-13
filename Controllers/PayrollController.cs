using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SBIT3J_SuperSystem_Final.Controllers
{
    [Authorize]
    public class PayrollController : Controller
    {
        // GET: Payroll
        public ActionResult Dashboard()
        {
            return View();
        }



        public ActionResult Payslip(int employeeId)
{
    var employee = db.EmployeeInformation.Find(employeeId);
    if (employee == null)
    {
        return HttpNotFound();
    }

    var payslipViewModel = new PayslipViewModel
    {
        EmployeeId = employee.EmployeeId,
        EmployeeName = employee.Name,
        BasicSalary = employee.BasicSalary,
        Deductions = CalculateDeductions(employee),
        Bonuses = CalculateBonuses(employee),      
        NetSalary = CalculateNetSalary(employee)    
        
    };

    return View("Payslip", payslipViewModel);
}
public ActionResult Reports()
{
    var payrollList = db.Payroll.ToList();
    return View("Reports", payrollList);
}

public ActionResult ExportData()
{
    try
    {
        var records = db.Payroll.ToList();
        var csvExport = new CsvExport();
        
        foreach (var record in records)
        {
            csvExport.AddRow();
            csvExport["EmployeeName"] = record.EmployeeName;
            // Add other properties as needed
        }

        var csvData = csvExport.Export();
        var bytes = Encoding.UTF8.GetBytes(csvData);
        var result = new FileContentResult(bytes, "text/csv")
        {
            FileDownloadName = "payroll_export.csv"
        };

        return result;
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = $"An error occurred during export: {ex.Message}";
        return RedirectToAction("Reports");
    }
}

public ActionResult ImportData(HttpPostedFileBase file)
{
    try
    {
        if (file != null && file.ContentLength > 0)
        {
            // Implement import logic here
            // For example, using CsvHelper library to import from CSV
            using (var reader = new StreamReader(file.InputStream))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<Payroll>().ToList();

                // Save the imported records to the database
                db.Payroll.AddRange(records);
                db.SaveChanges();

                TempData["SuccessMessage"] = $"{records.Count} records imported successfully.";
            }
        }
        else
        {
            TempData["ErrorMessage"] = "No file selected for import.";
        }
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = $"An error occurred during import: {ex.Message}";
    }

    return RedirectToAction("Reports");
}

    }
}
