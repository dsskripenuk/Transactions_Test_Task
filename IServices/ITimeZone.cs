namespace Transactions_test_task.IServices
{
    public interface ITimeZone
    {
        DateTime ConvertToUserTimeZone(DateTime utcTime, string userTimeZoneId);
        Task<string> GetTimeZoneFromCoordinatesAsync(string coordinates);
    }
}
