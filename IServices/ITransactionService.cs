using Microsoft.AspNetCore.Mvc;
using Transactions_test_task.Models;

namespace Transactions_test_task.IServices
{
    public interface ITransactionService
    {
        Task ImportTransactionsAsync(IFormFile file);
        Task<FileResult> ExportTransactionsAsync(DateTime fromDate, DateTime toDate, string userTimeZone);
        Task<List<Transaction>> GetTransactionsAsync(DateTime fromDate, DateTime toDate, string userTimeZone);
        Task<List<Transaction>> GetClientTransactionsAsync(DateTime fromDate, DateTime toDate);
        Task<List<Transaction>> GetJanuary2024ClientTransactionsAsync();
    }
}
