using Microsoft.AspNetCore.Mvc;
using Transactions_test_task.IServices;

namespace Transactions_test_task.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            await _transactionService.ImportTransactionsAsync(file);
            return Ok();
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export(DateTime fromDate, DateTime toDate, string userTimeZone)
        {
            var fileResult = await _transactionService.ExportTransactionsAsync(fromDate, toDate, userTimeZone);
            return fileResult;
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactions(DateTime fromDate, DateTime toDate, string userTimeZone)
        {
            var transactions = await _transactionService.GetTransactionsAsync(fromDate, toDate, userTimeZone);
            return Ok(transactions);
        }

        [HttpGet("client")]
        public async Task<IActionResult> GetClientTransactions(DateTime fromDate, DateTime toDate)
        {
            var transactions = await _transactionService.GetClientTransactionsAsync(fromDate, toDate);
            return Ok(transactions);
        }

        [HttpGet("january2024")]
        public async Task<IActionResult> GetJanuary2024ClientTransactions()
        {
            var transactions = await _transactionService.GetJanuary2024ClientTransactionsAsync();
            return Ok(transactions);
        }
    }
}
