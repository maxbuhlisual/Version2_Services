using Domain.Interfaces;
using Infrastructure.Repositories;
using Application.Services;
using Infrastructure.Data;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__Host-ExpenseTracker-CSRF";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.HttpOnly = true;
});
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "__Host-ExpenseTracker-Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = false;
        options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization();

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
builder.Services.AddScoped<IExpenseBudgetWriter, SqliteExpenseBudgetWriter>();
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
    app.UseSwaggerUI(options => options.UseRequestInterceptor("""
        async (request) => {
            if (!["GET", "HEAD", "OPTIONS"].includes(request.method.toUpperCase())) {
                const response = await fetch('/api/Users/csrf', { credentials: 'same-origin' });
                if (!response.ok) throw new Error('Cannot obtain CSRF token');
                const data = await response.json();
                request.headers['X-CSRF-TOKEN'] = data.token;
            }
            request.credentials = 'same-origin';
            return request;
        }
        """));
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
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
