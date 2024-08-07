using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Transactions_test_task.Data;
using Transactions_test_task.IServices;
using Transactions_test_task.Services;
using TimeZone = Transactions_test_task.Services.TimeZone;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Transaction API", Version = "v1" });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ITimeZone, TimeZone>();
builder.Services.AddHttpClient<ITimeZone, TimeZone>();



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Transaction API v1"));
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
