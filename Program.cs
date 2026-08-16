using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Source.Models;
using Source.Services;

var builder = WebApplication.CreateBuilder(args);

// Mặc định định dạng tiền tệ / số theo chuẩn Việt Nam (VNĐ: 1.250.000 ₫)
var viCulture = new CultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = viCulture;
CultureInfo.DefaultThreadCurrentUICulture = viCulture;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        // SameSite=Lax: trình duyệt không gửi cookie trên request POST cross-site → chặn CSRF
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("SellerOnly", policy => policy.RequireRole("Seller"));
    options.AddPolicy("KolOnly", policy => policy.RequireRole("Kol"));
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(120);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICartSessionService, CartSessionService>();
    builder.Services.AddScoped<IPromoCodeService, PromoCodeService>();
    builder.Services.AddScoped<IPaymentService, PaymentService>();
    builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();
    builder.Services.AddScoped<IOrderTrackingService, OrderTrackingService>();

// HTTP client dùng chung cho cổng thanh toán (MoMo) — pooling qua IHttpClientFactory
builder.Services.AddHttpClient("PaymentGateway", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});

// Demo DI lifetimes
builder.Services.AddSingleton<ISingletonOperation, OperationService>();
builder.Services.AddScoped<IScopedOperation, OperationService>();
builder.Services.AddTransient<ITransientOperation, OperationService>();
builder.Services.AddScoped<OperationDemoService>();

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
// Seed data chạy nền sau khi app đã lắng nghe request (không chặn khởi động)
builder.Services.AddHostedService<Source.Services.DatabaseSeederHostedService>();
builder.Services.AddAntiforgery(options =>
{
    // Cho phép API/AJAX gửi token qua header: X-CSRF-TOKEN hoặc RequestVerificationToken
    options.HeaderName = "RequestVerificationToken";
    options.SuppressXFrameOptionsHeader = false;
});

var app = builder.Build();

// Chỉ bật HttpsRedirection khi app THỰC SỰ có endpoint HTTPS.
// Nếu chạy HTTP-only (profile "http", `dotnet run --urls http://...`), bỏ qua để tránh
// cảnh báo "Failed to determine the https port for redirect" và không redirect về port không tồn tại.
string effectiveUrls = builder.Configuration["urls"]
                       ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
                       ?? "";
bool hasHttpsUrl = effectiveUrls.Split(';', StringSplitOptions.RemoveEmptyEntries)
                       .Any(u => u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                   || !string.IsNullOrEmpty(builder.Configuration["https_port"]);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

if (hasHttpsUrl)
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
// Handle 404 errors with custom page
app.UseStatusCodePagesWithReExecute("/Home/NotFound", "?statusCode={0}");

app.MapHub<Source.Hubs.OrderTrackingHub>("/hubs/order-tracking");

// Apply pending migrations khi khởi động (best-effort; seed data chạy nền qua DatabaseSeederHostedService)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi xảy ra trong quá trình Migrate Database. Kiểm tra connection string (Server/Instance/Database).");
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();



