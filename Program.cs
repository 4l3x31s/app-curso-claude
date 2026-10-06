using app_curso_claude.Data;
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

var app = builder.Build();

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
