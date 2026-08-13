# ROADMAP — Hướng phát triển tiếp theo (Phase 8)

Dựa trên khảo sát mã nguồn (read-only), đây là 4 hướng được ưu tiên: **(1) Đánh giá món (UI), (2) Dashboard phân tích, (4) Realtime đơn hàng (SignalR), (7) i18n & PWA**. Mỗi hướng gồm: hiện trạng, mục tiêu, và các bước triển khai cụ thể. Chưa implement (theo yêu cầu "chỉ làm roadmap").

---

## 1. Đánh giá món ăn (Reviews UI) — Ưu tiên cao, làm nhanh
**Hiện trạng:** `Controllers/Api/ReviewsApiController.cs` đã có đầy đủ `GET /api/reviews/food/{id}` và `POST /api/reviews` (yêu cầu đơn `Delivered` có món). Model `Review` có sẵn. Tuy nhiên **không có UI** — `Views/Home/FoodDetails.cshtml` không hiển thị/sửa đánh giá (chỉ `Orders/Details.cshtml` có nhắc đến).

**Mục tiêu:** Khách xem được điểm trung bình, số lượt đánh giá, danh sách nhận xét, và gửi đánh giá cho món đã mua.

**Bước triển khai:**
1. Tạo `Views/Home/_Reviews.cshtml` (partial): hiển thị `avgRating` (sao), `reviewCount`, list reviews (tên, sao, ngày, nội dung); form gửi (select sao 1-5 + textarea) chỉ hiện khi user đã đăng nhập và có đơn Delivered chứa món.
2. `FoodDetails.cshtml` gọi `GET /api/reviews/food/{id}` (JS) để render; sau khi gửi thành công thì reload danh sách.
3. (Tùy chọn) Admin: action `Admin/Reviews` để ẩn/xóa đánh giá sai phạm (dùng `ReviewsApiController` hoặc thêm `Delete` có `[Authorize(Roles="Admin")]`).

**File liên quan:** `ReviewsApiController.cs`, `FoodDetails.cshtml`, `_Reviews.cshtml` (mới), `site.js`.

---

## 2. Dashboard phân tích cho Admin — Ưu tiên cao
**Hiện trạng:** `Admin/Index.cshtml` có vài chart sơ bộ; chưa có biểu đồ doanh thu, món bán chạy, trạng thái đơn, hay điểm thưởng phát/đổi. Không có controller báo cáo.

**Mục tiêu:** Trang `Admin/Reports` tổng quan vận hành: doanh thu theo ngày/tháng, đơn theo trạng thái, top món bán chạy, điểm phát/đổi.

**Bước triển khai:**
1. Thêm `AdminController.Reports()` + các action trả JSON: doanh thu 30 ngày (`Orders` nhóm theo `OrderDate`), cơ cấu trạng thái (`GROUP BY Status`), top 10 `OrderDetails` theo `Quantity*Price`, tổng `PointTransactions` Earn/Redeem.
2. `Views/Admin/Reports.cshtml` dùng **Chart.js** (đã có trong `wwwroot/lib`? nếu chưa thì thêm CDN/local) vẽ line (doanh thu), doughnut (trạng thái), bar (top món).
3. Thêm link "Báo cáo" vào `_AdminLayout.cshtml`.

**File liên quan:** `AdminController.cs` (mới region Reports), `Reports.cshtml` (mới), `_AdminLayout.cshtml`.

---

## 4. Realtime đơn hàng (SignalR) — Ưu tiên trung bình-cao
**Hiện trạng:** Cập nhật trạng thái đơn chỉ qua HTTP (`UpdateOrderStatus`, `DeliverOrder`...). Không có kênh realtime. Khách phải F5 để xem tiến độ; bếp không có màn hình theo dõi.

**Mục tiêu:** (a) Màn hình bếp/Admin nhận cập nhật ngay khi đơn mới/thay đổi trạng thái; (b) Khách xem tiến độ đơn realtime trên `Orders/Details`.

**Bước triển khai:**
1. Thêm package `Microsoft.AspNetCore.SignalR`; tạo `Hubs/OrderHub.cs` với method `NotifyOrderUpdated(orderId, status)`.
2. Trong các action đổi trạng thái (`AdminController`, `OrdersApiController`) gọi `await _hub.Clients.All.SendAsync("OrderUpdated", payload)`.
3. Trang bếp `Kitchen/Index.cshtml` (mới, chỉ Nhân viên/Admin): lắng nghe `OrderUpdated`, hiển thị danh sách đơn đang chế biến.
4. `Orders/Details.cshtml`: lắng nghe `OrderUpdated` cho đơn hiện tại → cập nhật badge trạng thái live.
5. Đăng ký SignalR trong `Program.cs` (`app.MapHub<OrderHub>("/hubs/order")`).

**File liên quan:** `OrderHub.cs` (mới), `Program.cs`, `Kitchen/Index.cshtml` (mới), `Orders/Details.cshtml`, `AdminController.cs`/`OrdersApiController.cs` (gọi hub).

---

## 7. i18n & PWA — Ưu tiên trung bình
**Hiện trạng:** Toàn bộ UI tiếng Việt cứng (không có `.resx`, không `RequestLocalization`). Không có manifest/service worker → không cài được lên điện thoại, không offline.

### 7a. Đa ngôn ngữ (Việt/Anh)
1. Cấu hình `RequestLocalization` (vi mặc định, en) trong `Program.cs`.
2. Tạo resource `.resx` (`Resources/Views/...`, `SharedResource`) cho các chuỗi chính; dùng `IStringLocalizer` trong view/controller.
3. Thêm switcher ngôn ngữ (`_Layout`/`_AdminLayout`) lưu qua cookie.
> Lưu ý: việc tách toàn bộ chuỗi ra resource tốn công; có thể làm từng module (trang chủ → thực đơn → thanh toán).

### 7b. PWA (cài được, offline cơ bản)
1. Thêm `wwwroot/manifest.json` (name, icons, theme, start_url) + link trong `_Layout`.
2. Thêm `wwwroot/service-worker.js` cache thực đơn/ảnh tĩnh; register trong `site.js`.
3. (Tùy chọn) meta theme-color, apple-touch-icon.

**File liên quan:** `Program.cs`, `manifest.json` (mới), `service-worker.js` (mới), `site.js`, resource `.resx` (mới), layout views.

---

## Ghi chú chung
- Thứ tự đề xuất: **1 → 2 → 4 → 7** (1 nhanh gặt, 2 tăng giá trị quản trị, 4 nâng trải nghiệm, 7 nền tảng dài hạn).
- Các hướng 1,2,4 không phá cấu trúc hiện tại; 7 (i18n) ảnh hưởng diện rộng nên nên làm sau cùng hoặc song song từng phần.
- Mọi thay đổi cần `dotnet build` + test thủ công (login customer/admin) trước khi merge.
