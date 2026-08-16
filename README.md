# FastFood Web Application

Hệ thống đặt món ăn nhanh (Fast Food) được xây dựng bằng **ASP.NET Core 11 (MVC)** với **Entity Framework Core** và **SQL Server**.

## 🚀 Tính năng chính

### 👤 Khách hàng (Customer)
- **Đăng ký / Đăng nhập** tài khoản (Cookie Authentication + BCrypt hash mật khẩu)
- **Duyệt món ăn**: Tìm kiếm theo tên, lọc theo danh mục, sắp xếp (giá, tên, mới nhất), phân trang
- **Tìm kiếm nâng cao**: Lọc theo giá (min/max), danh mục, chủ đề, mô tả (AJAX partial view)
- **Xem chi tiết món ăn / Combo** với món liên quan
- **Giỏ hàng**: Thêm/xóa/sửa số lượng món đơn lẻ & Combo (Session-based + AJAX)
- **Đặt hàng (Checkout)**: Điền thông tin giao hàng, chọn COD, xác nhận đơn hàng (Transaction)
- **Lịch sử đơn hàng**: Xem danh sách, phân trang
- **Theo dõi đơn hàng**: Xem trạng thái (Pending → Preparing → Shipping → Delivered), hủy đơn khi Pending
- **Đổi mật khẩu / Cập nhật hồ sơ**

### 🛠️ Quản trị viên (Admin - Role-based Authorization)
- **Dashboard**: Thống kê đơn hàng (tổng, chờ xử lý, đang giao, đã giao), doanh thu, số món/combo/user
- **Quản lý Người dùng**: CRUD, phân quyền (Admin/Customer), khóa/mở tài khoản
- **Quản lý Danh mục**: CRUD danh mục món ăn
- **Quản lý Món ăn (FastFood)**: CRUD, upload ảnh (validate định dạng/kích thước), phân trang
- **Quản lý Combo**: CRUD, cấu hình chi tiết combo (ComboDetail - món + số lượng)
- **Quản lý Đơn hàng**: Xem chi tiết, cập nhật trạng thái, in hóa đơn, xuất PDF/Print view

### 🛡️ Bảo mật & Kỹ thuật
- **Authentication**: Cookie-based + `HttpOnly`, `SecurePolicy`, `SlidingExpiration`
- **Authorization**: `[Authorize]`, `[Authorize(Roles = "Admin")]`, Policy-based
- **Password**: Hash BCrypt (BCrypt.Net-Next)
- **CSRF**: `[ValidateAntiForgeryToken]` trên mọi POST
- **SQL Injection prevention**: EF Core Parameterized Queries
- **Soft Delete**: `IsDeleted` flag cho Orders
- **Indexes**: Tối ưu query trên Orders (Status, IsDeleted, OrderDate, UserId)

## 🏗️ Kiến trúc & Công nghệ

| Layer | Công nghệ |
|-------|-----------|
| **Framework** | ASP.NET Core 10 (MVC) |
| **ORM** | Entity Framework Core 10 (Code-First, Migrations) |
| **Database** | SQL Server (LocalDB / Express) |
| **Auth** | ASP.NET Core Identity (Cookie) + BCrypt.Net-Next |
| **DI / Services** | Scoped (DbContext, CartSessionService), Transient (Helpers) |
| **Frontend** | Razor Views, Bootstrap 5, jQuery, jQuery Validation Unobtrusive |
| **Session** | Distributed Memory Cache (Session) |
| **File Upload** | `IFormFile` → `wwwroot/images/uploads` (validate ext/size) |
| **Logging** | `ILogger<T>` (Console, Debug) |
| **Health Check** | `/health` endpoint |

## 📁 Cấu trúc thư mục

```
LAPTRINHWEB/
├── Controllers/
│   ├── HomeController.cs        # Trang chủ, Menu, Combo, Search, Details
│   ├── AccountController.cs     # Login, Register, Logout, Profile, ChangePassword
│   ├── CartController.cs        # Cart (Session), Add/Update/Remove, Checkout
│   ├── OrdersController.cs      # Order History, Details, Tracking, Cancel
│   └── AdminController.cs       # Dashboard, Users, Categories, Foods, Combos, Orders
├── Models/
│   ├── AppDbContext.cs          # DbContext, Fluent API, Seed Data
│   ├── FastFood.cs              # Món ăn đơn lẻ
│   ├── Category.cs              # Danh mục
│   ├── Combo.cs / ComboDetail.cs # Combo & chi tiết món trong combo
│   ├── Order.cs / OrderDetail.cs # Đơn hàng & chi tiết
│   ├── User.cs                  # User (Admin/Customer), BCrypt Hash
│   ├── CartItem.cs              # Item trong giỏ hàng (Session)
│   └── OrderStatus.cs           # Enum trạng thái đơn hàng
├── ViewModels/
│   ├── LoginViewModel.cs
│   ├── RegisterViewModel.cs
│   └── ChangePasswordViewModel.cs
├── Services/
│   ├── ICartSessionService.cs / CartSessionService.cs  # Session Cart wrapper
├── Helpers/
│   ├── PasswordHelper.cs        # BCrypt Hash/Verify
│   ├── ImageUploadHelper.cs     # Validate & Save upload
│   ├── SessionExtensions.cs     # Session Get/Set JSON
│   └── UserClaimsHelper.cs      # Lấy UserId từ ClaimsPrincipal
├── Migrations/                  # EF Core Migrations
├── Database/
│   └── database.sql             # Script SQL Server đầy đủ (schema + seed data)
├── Views/                       # Razor Views (Home, Account, Cart, Orders, Admin, Shared)
├── wwwroot/
│   ├── css/site.css
│   ├── js/site.js
│   ├── images/                  # Banner, Products, Avatars
│   └── lib/                     # Bootstrap, jQuery, jQuery Validation
├── Program.cs                   # App entry, DI, Middleware, Seed Data
├── Source.csproj                # Project file (.NET 10)
└── README.md
```

## 🗄️ Cơ sở dữ liệu (Schema chính)

| Bảng | Mô tả |
|------|-------|
| `Users` | Người dùng (Admin/Customer), BCrypt PasswordHash |
| `Categories` | Danh mục món ăn (Burger, Pizza, Gà rán, Đồ uống...) |
| `FastFoods` | Món ăn đơn lẻ (Giá, Mô tả, Ảnh, CategoryId, Theme) |
| `Combos` | Combo ăn (Giá combo, Mô tả, Ảnh) |
| `ComboDetails` | Chi tiết combo (ComboId, FastFoodId, Quantity) - Composite PK |
| `Orders` | Đơn hàng (UserId, Tổng tiền, Trạng thái, Thông tin giao hàng, COD, Ship fee, Discount, SoftDelete) |
| `OrderDetails` | Chi tiết đơn hàng (OrderId, FastFoodId/ComboId, Quantity, Price) |

**Indexes**: `IX_Orders_UserId`, `IX_Orders_Status`, `IX_Orders_IsDeleted`, `IX_Orders_OrderDate`, `IX_FastFoods_CategoryId`, `IX_OrderDetails_OrderId`...

## 🔐 Tài khoản mặc định (Seed Data)

| Role | Username | Password | Email |
|------|----------|----------|-------|
| **Admin** | `admin` | `admin123` | admin@fastfood.com |
| **Customer** | `customer` | `customer123` | customer@fastfood.com |

> Mật khẩu đã được hash BCrypt trong DB/Seed (`DbInitializer.SeedAsync`)

## ⚙️ Cài đặt & Chạy dự án

### Yêu cầu
- [.NET 11 SDK (Preview)](https://dotnet.microsoft.com/download/dotnet/11.0) — **bắt buộc**, dự án target `net11.0` và `global.json` yêu cầu SDK 11 (preview).
- SQL Server (LocalDB, Express, hoặc Docker)
- Visual Studio 2022 / VS Code + C# Dev Kit

### 1. Clone & Cấu hình Connection String
```bash
git clone <repo-url>
cd LAPTRINHWEB
```

Mặc định `appsettings.json` dùng `(localdb)\MSSQLLocalDB`, còn `appsettings.Development.json` dùng `localhost\SQLEXPRESS` (để team dùng nhiều SQL Server khác nhau).
Mỗi thành viên muốn override thì tạo file `appsettings.Development.local.json` (đã gitignore, không đẩy lên git):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=my-server;Database=FastFoodDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```
Không bao giờ commit connection string chứa mật khẩu hoặc tài khoản admin thật.

### 2. Chạy Migration & Seed Data
```bash
dotnet ef database update
```
*Hoặc chạy script SQL có sẵn:*
```bash
sqlcmd -S (localdb)\mssqllocaldb -i Database/database.sql
```

Ứng dụng sẽ tự động `MigrateAsync()` khi khởi động (`Program.cs`). **Seed data chạy nền** sau khi app đã lắng nghe request (không chặn khởi động).

### 3. Chạy ứng dụng
```bash
dotnet run
```
Mở trình duyệt: `https://localhost:5001` (hoặc port hiển thị trong console)

## 📦 Các Package chính
```xml
<PackageReference Include="BCrypt.Net-Next" Version="4.2.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.10" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.10" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.10" />
```

## 🎯 Các luồng nghiệp vụ chính

### Đặt hàng (Checkout)
1. User thêm món/Combo vào giỏ (Session)
2. GET `/Cart/Checkout` → Load thông tin user làm mặc định
3. POST `/Cart/Checkout` → Validate ModelState
4. `BeginTransactionAsync()`
   - Tạo `Order` (Status = Pending)
   - Tạo `OrderDetail` cho từng item trong cart
   - `SaveChangesAsync()` → `CommitAsync()`
5. Xóa Session Cart:
   - **COD** → Redirect `/Orders/Tracking/{id}`
   - **Bank** → Redirect `/Orders/BankTransfer/{id}` (hướng dẫn chuyển khoản)
   - **VNPay / MoMo** → gọi cổng thanh toán (NGOÀI transaction) rồi redirect tới `paymentUrl`
6. Guest (không đăng nhập) theo dõi đơn bằng số điện thoại qua `/Orders/GuestTrack/{id}`

### Hủy đơn hàng (Customer)
- Chỉ cho phép khi `Status == "Pending"`
- Cập nhật `Status = "Cancelled"`, `CancelReason`, `UpdatedAt`

### Quản lý Combo (Admin)
- CRUD Combo cơ bản
- **ComboDetail**: Mỗi combo chứa nhiều `FastFood` với `Quantity`
- Hiển thị chi tiết combo kèm món ăn thành phần

### Tìm kiếm nâng cao (AJAX)
- POST `/Home/AdvancedSearch?name=&minPrice=&maxPrice=&categoryId=&theme=&description=` (nút "Tìm kiếm ngay" trên trang chủ)
- Header `X-Requested-With: XMLHttpRequest` → Trả về `PartialView("_FoodListPartial")`
- Không phải AJAX → Trả về `View("Index", results)`

## 🔒 Bảo mật đã triển khai

- **CSRF**: `[ValidateAntiForgeryToken]` trên các POST MVC; cookie auth + session đặt `SameSite=Lax` (chặn POST cross-site); `site.js` tự gắn `RequestVerificationToken` vào AJAX khi form có sẵn token.
- **Open redirect**: `DeviceController` chỉ redirect về Referer khi cùng host; `AccountController.Login` dùng `Url.IsLocalUrl`.
- **Phân quyền dữ liệu**: Seller chỉ xem/tracking được đơn có chứa món của mình (API + SignalR); dashboard chỉ dành cho Admin.
- **Thanh toán**: IPN MoMo nhận cả form + JSON, verify chữ ký **và** số tiền với `Order.TotalAmount`; không tin client ở bất kỳ bước tính tiền nào.
- **Mock Google login**: chỉ hoạt động ở môi trường `Development`.
- **Secrets**: connection string có thông tin nhạy phải để trong `appsettings.Development.local.json` (gitignored), không commit lên repo.

## 🔧 Cấu hình quan trọng (Program.cs)

```csharp
// DB Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Auth Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => { ... });

// Session (Giỏ hàng)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => { ... });

// DI Services
builder.Services.AddScoped<ICartSessionService, CartSessionService>();
builder.Services.AddHttpContextAccessor();

// Health Check
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
```

## 🧪 Health Check
```
GET /health
Response: { "status": "healthy", "timestamp": "2026-07-25T..." }
```

## 📝 Ghi chú phát triển

- **Soft Delete**: `Order.IsDeleted` = true thay vì xóa vật lý
- **Transaction**: Checkout dùng `BeginTransactionAsync` đảm bảo ACID
- **Image Upload**: Validate extension (jpg/png/webp), size < 5MB, lưu `wwwroot/images/uploads`
- **Pagination**: Tự implement (PageSize = 10-15), không dùng thư viện phân trang
- **AJAX Cart**: Trả về JSON `{ success, cartCount, cartTotal, ... }` cho UX mượt mà
- **Logging**: `ILogger<T>` ghi log các action quan trọng (Create/Update/Delete, Checkout, Cancel)

## 📄 License
MIT License - Tự do sử dụng cho mục đích học tập, nghiên cứu, thương mại.

---

**Phát triển bởi**: [Your Name] - ASP.NET Core 10 MVC Project