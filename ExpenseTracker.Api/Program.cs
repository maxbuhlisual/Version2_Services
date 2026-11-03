using Domain.Interfaces;
using Infrastructure.Repositories;
using Application.Services;
using Infrastructure.Data;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

string connectionString = builder.Configuration.GetConnectionString("ExpenseTracker")
    ?? throw new InvalidOperationException("Не задана строка подключения ExpenseTracker.");
builder.Services.AddSingleton(new SqliteConnectionFactory(
    connectionString, builder.Environment.ContentRootPath));

builder.Services.AddSingleton<SqliteDatabaseInitializer>();

builder.Services.AddScoped<IUserRepository, SqliteUserRepository>();
builder.Services.AddScoped<ICategoryRepository, SqliteCategoryRepository>();
builder.Services.AddScoped<IExpenseRepository, SqliteExpenseRepository>();
builder.Services.AddScoped<IBudgetRepository, SqliteBudgetRepository>();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<ReportService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Services.GetRequiredService<SqliteDatabaseInitializer>().Initialize();

// Проверяем подключение до начала обработки HTTP-запросов.
using (var connection = app.Services.GetRequiredService<SqliteConnectionFactory>().CreateConnection())
{
    connection.Open();
    int result = connection.QuerySingle<int>("SELECT 1;");
    if (result != 1)
    {
        throw new InvalidOperationException("Проверка подключения SQLite не пройдена.");
    }
    app.Logger.LogInformation("SQLite: SELECT 1 = {Result}. База: {DatabasePath}",
        result, connection.DataSource);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        Exception? exception = exceptionFeature?.Error;

        if (exception is ArgumentException)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = exception.Message });
        }
        else
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = "Внутренняя ошибка сервера" });
        }
    });
});

app.MapControllers();

app.Run();