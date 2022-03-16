using ExpenseManagerAuth.Data;
using ExpenseManagerAuth.Interfaces;
using ExpenseManagerAuth.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ExpenseManager.Models
{
    public class DataAcessLayer : IDatabaseServices
    {
        private ApplicationDbContext db;
        private IHttpContextAccessor httpContextAccessor;

        public DataAcessLayer(IHttpContextAccessor _httpContextAccessor, ApplicationDbContext _db)
        {
            db = _db;
            httpContextAccessor = _httpContextAccessor;
        }

        //To Get all Expenses
        public async Task<IEnumerable<Expense>> GetAllExpensesAsync()
        {
            try
            {
                return await db.Expenses
                    .Where(er => er.UserId == httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier))
                    .ToListAsync();
            }
            catch
            {
                throw;
            }
        }

        // To filter out the records based on the search string 
        public async Task<IEnumerable<Expense>> GetSearchResultAsync(string searchString)
        {
            try
            {
                var expenses = await GetAllExpensesAsync();
                return expenses.Where(x => (!string.IsNullOrEmpty(searchString) 
                        ? x.Name.Contains(searchString)
                        : true));
            }
            catch
            {
                throw;
            }
        }

        //To Add new Expense       
        public async Task AddExpenseAsync(Expense expense)
        {
            try
            {
                await db.Expenses.AddAsync(expense);
                await db.SaveChangesAsync();
            }
            catch
            {
                throw;
            }
        }

        //To Update a particluar expense  
        public async Task UpdateExpenseAsync(Expense expense)
        {
            try
            {
                db.Entry(expense).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
            catch
            {
                throw;
            }
        }

        //To Get the data for a particular expense  
        public async Task<Expense> GetExpenseDataAsync(int id)
        {
            try
            {
                return await db.Expenses.FindAsync(id);
            }
            catch
            {
                throw;
            }
        }

        //To Delete a particular expense  
        public async Task DeleteExpenseAsync(int id)
        {
            try
            {
                Expense exp = await db.Expenses.FindAsync(id);
                db.Expenses.Remove(exp);
                await db.SaveChangesAsync();
            }
            catch
            {
                throw;
            }
        }

        // To calculate last month expenses
        public async Task<decimal> CalculateMonthlyExpenseAsync()
        {
            try
            {
                return await db.Expenses
                    .Where(ex => ex.UserId == httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) && (ex.Date > DateTime.Now.AddMonths(-1)))
                    .SumAsync(ex => ex.Amount);
            }
            catch
            {
                throw;
            }
        }

        // To calculate last week expenses
        public async Task<decimal> CalculateWeeklyExpenseAsync()
        {
            try
            {
                return await db.Expenses
                    .Where(ex => ex.UserId == httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) && (ex.Date > DateTime.Now.AddDays(-7)))
                    .SumAsync(ex => ex.Amount);
            }
            catch
            {
                throw;
            }
        }
    }
}
