using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Transactions_test_task.Data;
using Transactions_test_task.IServices;
using Transactions_test_task.Models;

namespace Transactions_test_task.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITimeZone _timeZoneService;

        public TransactionService(ApplicationDbContext context, ITimeZone timeZoneService)
        {
            _context = context;
            _timeZoneService = timeZoneService;
        }

        public async Task ImportTransactionsAsync(IFormFile file)
        {
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var header = await reader.ReadLineAsync();

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    var values = line.Split(',');

                    var transactionId = values[0];
                    var name = values[1];
                    var email = values[2];

                    var amountString = values[3].Trim('$', ' ');
                    if (!decimal.TryParse(amountString, out decimal amount))
                    {
                        Console.WriteLine($"Invalid format for amount: {values[3]}");
                        continue;
                    }

                    DateTime transactionDate;
                    try
                    {
                        transactionDate = DateTime.Parse(values[4]);
                    }
                    catch (FormatException ex)
                    {
                        Console.WriteLine($"Invalid format for transaction date: {values[4]}");
                        continue;
                    }

                    string clientLocation = null;
                    string clientTimezone = null;
                    DateTime clientTimestamp;

                    string latitudeString = values[5].Trim('"').Trim();
                    string longitudeString = values[6].Trim('"').Trim();

                    if (TryParseCoordinate(latitudeString, out double latitude) &&
                        TryParseCoordinate(longitudeString, out double longitude))
                    {
                        clientLocation = $"{latitude},{longitude}";

                        try
                        {
                            clientTimezone = await _timeZoneService.GetTimeZoneFromCoordinatesAsync(clientLocation);

                            if (clientTimezone == null)
                            {
                                Console.WriteLine($"No time zone found for coordinates: {clientLocation}. Skipping transaction.");
                                continue;
                            }

                            clientTimestamp = _timeZoneService.ConvertToUserTimeZone(transactionDate, clientTimezone);
                        }
                        catch (ArgumentException ex)
                        {
                            Console.WriteLine($"Error obtaining time zone for coordinates: {clientLocation}");
                            continue;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Invalid coordinates format: Latitude: {latitudeString}, Longitude: {longitudeString}");
                        continue;
                    }

                    var transaction = new Transaction
                    {
                        TransactionId = transactionId,
                        Name = name,
                        Email = email,
                        Amount = amount,
                        TransactionDate = transactionDate,
                        ClientLocation = clientLocation,
                        ClientTimezone = clientTimezone,
                        ClientTimestamp = clientTimestamp,
                        ServerTimestamp = DateTime.UtcNow
                    };

                    var validationContext = new ValidationContext(transaction);
                    var validationResults = new List<ValidationResult>();

                    if (!Validator.TryValidateObject(transaction, validationContext, validationResults, true))
                    {
                        foreach (var validationResult in validationResults)
                        {
                            Console.WriteLine(validationResult.ErrorMessage);
                        }
                        continue;
                    }

                    var existingTransaction = await _context.Transactions
                        .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

                    if (existingTransaction == null)
                    {
                        _context.Transactions.Add(transaction);
                    }
                    else
                    {
                        existingTransaction.Name = name;
                        existingTransaction.Email = email;
                        existingTransaction.Amount = amount;
                        existingTransaction.TransactionDate = transactionDate;
                        existingTransaction.ClientLocation = clientLocation;
                        existingTransaction.ClientTimezone = clientTimezone;
                        existingTransaction.ClientTimestamp = clientTimestamp;
                        existingTransaction.ServerTimestamp = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
            }
        }

        bool TryParseCoordinate(string coordinateString, out double coordinate)
        {
            coordinate = 0;
            if (string.IsNullOrWhiteSpace(coordinateString))
            {
                return false;
            }

            return double.TryParse(coordinateString, NumberStyles.Any, CultureInfo.InvariantCulture, out coordinate);
        }

        public async Task<FileResult> ExportTransactionsAsync(DateTime fromDate, DateTime toDate, string userTimeZone)
        {
            var transactions = await GetTransactionsAsync(fromDate, toDate, userTimeZone);

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Transactions");
                worksheet.Cells.LoadFromCollection(transactions, true);
                var fileContent = package.GetAsByteArray();
                return new FileContentResult(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                {
                    FileDownloadName = "Transactions.xlsx"
                };
            }
        }

        public async Task<List<Transaction>> GetTransactionsAsync(DateTime fromDate, DateTime toDate, string userTimeZone)
        {
            if (fromDate > toDate)
            {
                throw new ArgumentException("The start date must be earlier than or equal to the end date.");
            }

            TimeZoneInfo userTimeZoneInfo;
            try
            {
                userTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(userTimeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                throw new ArgumentException("Invalid time zone ID", nameof(userTimeZone));
            }
            catch (InvalidTimeZoneException)
            {
                throw new ArgumentException("Invalid time zone data", nameof(userTimeZone));
            }

            var fromUtc = TimeZoneInfo.ConvertTimeToUtc(fromDate, userTimeZoneInfo);
            var toUtc = TimeZoneInfo.ConvertTimeToUtc(toDate, userTimeZoneInfo);

            return await _context.Transactions
                .Where(t => t.ClientTimestamp >= fromUtc && t.ClientTimestamp <= toUtc && t.ClientTimezone == userTimeZone)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetClientTransactionsAsync(DateTime fromDate, DateTime toDate)
        {
            if (fromDate > toDate)
            {
                throw new ArgumentException("The start date must be earlier than or equal to the end date.");
            }

            return await _context.Transactions
                .Where(t => t.ClientTimestamp >= fromDate && t.ClientTimestamp <= toDate)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetJanuary2024ClientTransactionsAsync()
        {
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31, 23, 59, 59);

            return await _context.Transactions
                .Where(t => t.ClientTimestamp >= startDate && t.ClientTimestamp <= endDate)
                .ToListAsync();
        }
    }
}
