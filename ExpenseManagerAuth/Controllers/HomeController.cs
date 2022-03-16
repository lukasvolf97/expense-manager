using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ExpenseManagerAuth.Interfaces;
using ExpenseManagerAuth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace ExpenseManagerAuth.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IDatabaseServices expenseService;

        public HomeController(IDatabaseServices _expenseService)
        {
            expenseService = _expenseService;
        }
        public async Task<IActionResult> Index(string searchString)
        {
            ViewBag.MonthlyTotal = (await expenseService.CalculateMonthlyExpenseAsync()).ToString("C");
            ViewBag.WeeklyTotal = (await expenseService.CalculateWeeklyExpenseAsync()).ToString("C");

            return View(await expenseService
                .GetSearchResultAsync(searchString));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<ActionResult> AddEditExpenses(int itemId)
        {
            Expense model = new Expense();
            if (itemId > 0)
            {
                model = await expenseService.GetExpenseDataAsync(itemId);
            }
            return PartialView("_ExpenseForm", model);
        }

        [HttpPost]
        public async Task<ActionResult> Create(Expense newExpense)
        {
            if (ModelState.IsValid)
            {
                if (newExpense.Id > 0)
                {
                    await expenseService.UpdateExpenseAsync(newExpense);
                }
                else
                {
                    await expenseService.AddExpenseAsync(newExpense);
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await expenseService.DeleteExpenseAsync(id);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<JsonResult> GetCategories(string term)
        {
            var expenses = await expenseService.GetAllExpensesAsync();
            var categories = expenses
                .Select(x => x.Category)
                .Union(new List<string> { "Food", "Health", "Travel", "Shopping" })
                .Where(x => term != null ? x.ToUpper().Contains(term.ToUpper()) : true)
                .Take(4);

            return Json(categories);
        }

        [HttpGet]
        public async Task<IActionResult> Export()
        {
            var FileData = await expenseService.GetAllExpensesAsync();

            try
            {
                DataTable Dt = new DataTable();
                Dt.Columns.Add("Name", typeof(string));
                Dt.Columns.Add("Amount", typeof(decimal));
                Dt.Columns.Add("Date", typeof(string));
                Dt.Columns.Add("Category", typeof(string));

                foreach (var data in FileData)
                {
                    DataRow row = Dt.NewRow();
                    row[0] = data.Name;
                    row[1] = data.Amount;
                    row[2] = data.Date.ToString("dd.MM.yyyy");
                    row[3] = data.Category;
                    Dt.Rows.Add(row);
                }

                var memoryStream = new MemoryStream();
                using (var excelPackage = new ExcelPackage(memoryStream))
                {
                    var worksheet = excelPackage.Workbook.Worksheets.Add("My Expenses");
                    worksheet.Cells["A1"].LoadFromDataTable(Dt, true, TableStyles.None);
                    worksheet.Cells["A1:AN1"].Style.Font.Bold = true;
                    worksheet.DefaultRowHeight = 18;

                    for (var i = 1; i <= 4; i++)
                    {
                        worksheet.Column(i).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        worksheet.Column(i).AutoFit();
                    }
                    worksheet.DefaultColWidth = 20;

                    return File(excelPackage.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Expenses.xlsx");
                }
            }
            catch
            {
                throw;
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length <= 0)
            {
                return RedirectToAction("Index");
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);

                    using (var package = new ExcelPackage(stream))
                    {
                        foreach (var worksheet in package.Workbook.Worksheets)
                        {
                            var rowCount = worksheet.Dimension.Rows;

                            for (int row = 2; row <= rowCount; row++)
                            {
                                decimal amount;
                                DateTime date;
                                if (decimal.TryParse(worksheet.Cells[row, 2].Value.ToString().Trim(), out amount) &&
                                    DateTime.TryParse(worksheet.Cells[row, 3].Value.ToString().Trim(), out date))
                                {
                                    await expenseService.AddExpenseAsync(new Expense
                                    {
                                        Name = worksheet.Cells[row, 1].Value.ToString().Trim(),
                                        UserId = User.FindFirst(ClaimTypes.NameIdentifier).Value,
                                        Amount = amount,
                                        Date = date,
                                        Category = worksheet.Cells[row, 4].Value.ToString().Trim(),
                                    });
                                }
                            }
                        }
                    }
                }
                return RedirectToAction("Index");
            }
            catch
            {
                throw;
            }
        }
    }
}