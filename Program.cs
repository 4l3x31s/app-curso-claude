using app_curso_claude.Data;
using app_curso_claude.Data.Repositories;
using app_curso_claude.Data.Repositories.EfCore;
using app_curso_claude.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

string RequiredSetting(string key) =>
    builder.Configuration[key]
    ?? throw new InvalidOperationException($"Configuration value '{key}' not found.");

var connectionString = new SqlConnectionStringBuilder
{
    DataSource = $"{RequiredSetting("HostDB")},{RequiredSetting("PortDB")}",
    InitialCatalog = RequiredSetting("NameDB"),
    UserID = RequiredSetting("UserDB"),
    Password = RequiredSetting("PassDB"),
    TrustServerCertificate = builder.Configuration.GetValue<bool>("TrustServerCertificateDB")
}.ConnectionString;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// === Application services (register here) ===
builder.Services.AddScoped<IProductRepository, EfProductRepository>();
builder.Services.AddScoped<ICustomerRepository, EfCustomerRepository>();
builder.Services.AddScoped<IPurchaseRepository, EfPurchaseRepository>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<PurchaseService>();

var app = builder.Build();

// "dotnet run -- seed" fills the database with sample data and exits without starting the web server.
if (args.Length > 0 && args[0] == "seed")
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var result = await DbSeeder.SeedAsync(context);

        app.Logger.LogInformation(
            "Seed completed. Rows inserted: {Customers} customers, {Products} products, {Purchases} purchases.",
            result.Customers, result.Products, result.Purchases);
    }

    // Disposing the host flushes the console logger before the process exits.
    await app.DisposeAsync();
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
