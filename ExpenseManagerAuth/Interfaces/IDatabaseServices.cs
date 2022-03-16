using ExpenseManagerAuth.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpenseManagerAuth.Interfaces
{
    public interface IDatabaseServices
    {
        Task<IEnumerable<Expense>> GetAllExpensesAsync();
        Task<IEnumerable<Expense>> GetSearchResultAsync(string searchString);
        Task AddExpenseAsync(Expense expense);
        Task UpdateExpenseAsync(Expense expense);
        Task<Expense> GetExpenseDataAsync(int id);
        Task DeleteExpenseAsync(int id);
        Task<decimal> CalculateMonthlyExpenseAsync();
        Task<decimal> CalculateWeeklyExpenseAsync();
    }
}
