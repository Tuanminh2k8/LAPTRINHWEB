# Fix: Gray Screen When Canceling Order — Cannot Interact

## Problem

When a user attempts to cancel an order, the screen goes gray (overlay/backdrop) and they cannot interact with any elements — cannot cancel the order, cannot close the modal, cannot click anything else.

## Root Cause Analysis

Two cancel flows exist in the codebase, both have issues:

### Flow 1: `Views/Orders/Index.cshtml` (Orders list page)
- Line 119: "Hủy đơn" button opens a Bootstrap modal (`#cancelModal@order.Id`)
- Lines 164–189: Modal HTML rendered in a `@foreach` loop for each pending order
- **Issue**: The Bootstrap modal backdrop grays out the screen. If the modal fails to render properly (z-index conflict, missing Bootstrap JS, or modal content not interactive), the user is stuck with a gray, unclickable screen.
- The modal has `data-bs-dismiss="modal"` on the "Giữ lại đơn hàng" button and a close (`btn-close`) button — these should work, but if Bootstrap JS is not loaded or there's a JS error, they won't.

### Flow 2: `Views/Orders/Details.cshtml` (Order detail page)
- Line 260: Cancel form uses `onsubmit="return confirm('Bạn có chắc chắn muốn hủy đơn hàng này?');"`
- **Issue**: The native `confirm()` dialog is blocking. If the user clicks "OK", the form submits synchronously and the page navigates to the `Cancel` action. During this navigation, the preloader (`#preloader` in `_Layout.cshtml`, line 41–43) may not dismiss properly, leaving the screen gray/white and unresponsive.
- The `Cancel` action (OrdersController.cs:131) does a server-side POST and then `RedirectToAction("Details")`. If the request is slow or errors occur, the user is stuck.

### Contributing Factor: No AJAX cancel flow
- Both flows use full-page form submission (POST + redirect). There is no client-side AJAX handling, no loading spinner on the cancel button, and no error recovery if the request fails.

## Plan

### Task 1: Convert Details.cshtml cancel to AJAX (prevent full-page reload)

**File**: `Views/Orders/Details.cshtml`

**Changes**:
1. Remove the `onsubmit="return confirm('...')"` from the form (line 260)
2. Add `id="cancelOrderForm"` to the form element
3. Replace the native `confirm()` with a Bootstrap modal (reuse the same modal pattern from Index.cshtml)
4. Add a `<div id="cancelLoadingOverlay">` that shows a spinner when the cancel request is in flight
5. Add JavaScript in `@section Scripts` to:
   - Intercept form submission with jQuery
   - Show a loading overlay on the cancel button
   - Submit via AJAX (`$.ajax` POST to `/Orders/Cancel/{id}`)
   - On success: hide the modal, show a toast notification, reload the page or update the UI
   - On error: hide the loading overlay, show an error toast, keep the modal open
   - On complete: re-enable the submit button

**Code sketch** (to be implemented):
```html
<!-- Replace the existing form on Details.cshtml line 260 -->
<form id="cancelOrderForm" asp-action="Cancel" asp-route-id="@Model.Id" method="post">
    <input type="hidden" name="cancelReason" value="Khách hàng tự hủy đơn" />
    <input type="hidden" name="__RequestVerificationToken" value="@Context.Request.Headers["__RequestVerificationToken"]" />
    <button type="submit" id="btnCancelSubmit" class="btn btn-danger rounded-pill px-4 py-2 w-100 w-md-auto">
        <i class="fa-solid fa-xmark me-2"></i> Hủy đơn hàng
    </button>
</form>
<div id="cancelLoadingOverlay" class="loading-overlay" style="display:none;">
    <div class="spinner-border text-danger" role="status">
        <span class="visually-hidden">Đang hủy đơn...</span>
    </div>
</div>
```

```javascript
// In @section Scripts
<script>
    $(function () {
        $('#cancelOrderForm').on('submit', function (e) {
            e.preventDefault();
            var $form = $(this);
            var $btn = $('#btnCancelSubmit');
            var $overlay = $('#cancelLoadingOverlay');
            var originalHtml = $btn.html();
            $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1" role="status"></span> Đang xử lý...');
            $overlay.show();
            $.ajax({
                url: $form.attr('action'),
                type: 'POST',
                data: $form.serialize(),
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                success: function (res) {
                    if (res.success || res.redirect) {
                        window.location.href = res.redirect || '@Url.Action("Details", new { id = Model.Id })';
                    } else {
                        PolyFood.showToast(res.message || 'Có lỗi xảy ra.', 'danger');
                    }
                },
                error: function (xhr) {
                    var msg = xhr.responseJSON && xhr.responseJSON.message ? xhr.responseJSON.message : 'Có lỗi xảy ra. Vui lòng thử lại.';
                    PolyFood.showToast(msg, 'danger');
                },
                complete: function () {
                    $btn.prop('disabled', false).html(originalHtml);
                    $overlay.hide();
                }
            });
        });
    });
</script>
```

**Also modify `OrdersController.cs`** to return JSON for AJAX requests:
- Check `Request.Headers["X-Requested-With"] == "XMLHttpRequest"`
- If AJAX: return `Json(new { success = true, redirect = Url.Action("Details", new { id }) })`
- If not AJAX: keep existing redirect behavior (backward compatibility)

### Task 2: Fix Bootstrap modal in Index.cshtml (ensure modal is always interactive)

**File**: `Views/Orders/Index.cshtml`

**Changes**:
1. Ensure the modal backdrop does not trap the user — add `data-bs-backdrop="static"` removal (it already uses default `true` which is fine)
2. Add `data-bs-keyboard="true"` to allow Escape key to close
3. Add explicit `aria-label` to the close button for accessibility
4. Ensure the modal footer buttons are always visible and clickable
5. Add a `loading` state to the "Xác nhận hủy" button in the modal to prevent double-submission

**Code sketch** (for the modal in Index.cshtml, lines 166–189):
```html
<div class="modal fade" id="cancelModal@order.Id" tabindex="-1" aria-labelledby="cancelModalLabel@order.Id" aria-hidden="true" data-bs-keyboard="true">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-0 shadow-lg rounded-4">
            <form asp-action="Cancel" asp-route-id="@order.Id" method="post" class="ajax-cancel-form" data-order-id="@order.Id">
                <div class="modal-header bg-danger text-white rounded-top-4 border-0">
                    <h5 class="modal-title fw-600" id="cancelModalLabel@order.Id">
                        <i class="fa-solid fa-triangle-exclamation me-2"></i>Xác nhận hủy đơn #@order.Id
                    </h5>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Đóng"></button>
                </div>
                <div class="modal-body p-4">
                    <p class="text-muted mb-3">Bạn có chắc chắn muốn hủy đơn hàng <strong>#@order.Id</strong> không? Hành động này không thể hoàn tác.</p>
                    <div class="mb-3">
                        <label for="cancelReason@order.Id" class="form-label fw-500 small">Lý do hủy (bắt buộc)</label>
                        <textarea class="form-control rounded-3" id="cancelReason@order.Id" name="cancelReason" rows="3" required placeholder="Nhập lý do hủy đơn hàng..."></textarea>
                        <div class="invalid-feedback">Vui lòng nhập lý do hủy đơn.</div>
                    </div>
                </div>
                <div class="modal-footer border-0 bg-light rounded-bottom-4 p-3">
                    <button type="button" class="btn btn-outline-secondary rounded-pill px-4" data-bs-dismiss="modal">Giữ lại đơn hàng</button>
                    <button type="submit" class="btn btn-danger rounded-pill px-4" id="btnCancelConfirm@order.Id">
                        <i class="fa-solid fa-check me-1"></i> Xác nhận hủy
                    </button>
                </div>
            </form>
        </div>
    </div>
</div>
```

**Add JavaScript** in `@section Scripts` for Index.cshtml:
```javascript
<script>
    $(function () {
        // Handle AJAX cancel from Index page modals
        $(document).on('submit', '.ajax-cancel-form', function (e) {
            e.preventDefault();
            var $form = $(this);
            var $submitBtn = $form.find('button[type="submit"]');
            var originalHtml = $submitBtn.html();
            $submitBtn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1" role="status"></span> Đang xử lý...');
            $.ajax({
                url: $form.attr('action'),
                type: 'POST',
                data: $form.serialize(),
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                success: function (res) {
                    if (res.success || res.redirect) {
                        var modalId = '#cancelModal' + $form.data('order-id');
                        var modal = bootstrap.Modal.getInstance(document.querySelector(modalId));
                        if (modal) modal.hide();
                        PolyFood.showToast('Hủy đơn hàng thành công!', 'success');
                        setTimeout(function () { window.location.reload(); }, 1000);
                    } else {
                        PolyFood.showToast(res.message || 'Có lỗi xảy ra.', 'danger');
                    }
                },
                error: function (xhr) {
                    var msg = xhr.responseJSON && xhr.responseJSON.message ? xhr.responseJSON.message : 'Có lỗi xảy ra. Vui lòng thử lại.';
                    PolyFood.showToast(msg, 'danger');
                },
                complete: function () {
                    $submitBtn.prop('disabled', false).html(originalHtml);
                }
            });
        });
    });
</script>
```

### Task 3: Update OrdersController.Cancel to support AJAX responses

**File**: `Controllers/OrdersController.cs`

**Changes** to the `Cancel` action (line 129–170):
1. At the start of the action, check if the request is AJAX
2. For validation errors (empty cancelReason, non-Pending status): return JSON `{ success = false, message = "..." }` instead of redirect
3. For success: return JSON `{ success = true, redirect = Url.Action("Details", new { id }) }` instead of redirect
4. For exceptions: return JSON `{ success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." }` with appropriate HTTP status code

**Code sketch**:
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Cancel(int id, string cancelReason)
{
    var userId = UserClaimsHelper.GetUserId(User);
    if (!userId.HasValue)
    {
        return RedirectToAction("Login", "Account");
    }

    var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

    if (string.IsNullOrWhiteSpace(cancelReason))
    {
        if (isAjax) return Json(new { success = false, message = "Vui lòng nhập lý do hủy đơn hàng." });
        TempData["ErrorMessage"] = "Vui lòng nhập lý do hủy đơn hàng.";
        return RedirectToAction("Details", new { id });
    }

    var order = await _context.Orders
        .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId.Value);

    if (order == null)
    {
        if (isAjax) return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
        return NotFound();
    }

    if (order.Status != OrderStatus.Pending)
    {
        if (isAjax) return Json(new { success = false, message = "Chỉ có thể hủy đơn hàng khi đang ở trạng thái Chờ xác nhận." });
        TempData["ErrorMessage"] = "Chỉ có thể hủy đơn hàng khi đang ở trạng thái Chờ xác nhận.";
        return RedirectToAction("Details", new { id });
    }

    try
    {
        order.Status = OrderStatus.Cancelled;
        order.CancelReason = cancelReason;
        order.UpdatedAt = DateTime.Now;
        _context.Update(order);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Customer cancelled order #{OrderId}. Reason: {Reason}", id, cancelReason);

        if (isAjax) return Json(new { success = true, redirect = Url.Action("Details", new { id }) });

        TempData["SuccessMessage"] = "Hủy đơn hàng thành công!";
        return RedirectToAction("Details", new { id });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error cancelling order #{OrderId}", id);
        if (isAjax) return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
        TempData["ErrorMessage"] = "Có lỗi xảy ra. Vui lòng thử lại.";
        return RedirectToAction("Details", new { id });
    }
}
```

### Task 4: Ensure preloader dismisses properly on page load

**File**: `Views/Shared/_Layout.cshtml`

**Changes**:
1. The preloader (lines 41–43) already has `.loaded` class handling in `site.js` (line 17–21)
2. No changes needed here — the preloader should dismiss on window load
3. However, if the user navigates via AJAX (not full page load), the preloader won't re-trigger. This is fine since AJAX navigation doesn't reload the page.

### Task 5: Add CSS for cancel loading overlay

**File**: `wwwroot/css/site.css`

**Add** (near the existing `.loading-overlay` styles at line 908):
```css
#cancelLoadingOverlay {
    position: fixed;
    inset: 0;
    z-index: 1050;
    background: rgba(255, 255, 255, 0.7);
    display: flex;
    align-items: center;
    justify-content: center;
}
```

### Task 6: Validation and testing

**Test scenarios**:
1. **Index page → Click "Hủy đơn" → Modal opens → Click "Xác nhận hủy" with empty reason → Validation error shown, modal stays open**
2. **Index page → Click "Hủy đơn" → Modal opens → Enter reason → Click "Xác nhận hủy" → Success toast appears → Page reloads → Order status shows "Đã hủy"**
3. **Index page → Click "Hủy đơn" → Modal opens → Click "Giữ lại đơn hàng" → Modal closes, no action taken**
4. **Index page → Click "Hủy đơn" → Click close button (X) → Modal closes**
5. **Index page → Click "Hủy đơn" → Press Escape → Modal closes**
6. **Details page → Click "Hủy đơn hàng" → Loading spinner appears → Success → Page redirects to Details with success message**
7. **Details page → Click "Hủy đơn hàng" → Network error → Error toast shown, button re-enabled**
8. **Details page → Click "Hủy đơn hàng" → Order already processed (not Pending) → Error toast shown**
9. **Mobile: Repeat all scenarios above on mobile viewport**

**Regression tests**:
1. Normal form submission (no JS) still works — the form should fall back to full-page POST
2. Anti-forgery token validation works for both AJAX and non-AJAX requests
3. Admin users can still cancel orders from the admin panel (if applicable)

## Files to modify

| File | Change |
|------|--------|
| `Views/Orders/Details.cshtml` | Convert cancel form to AJAX, add loading overlay |
| `Views/Orders/Index.cshtml` | Add AJAX handling to modal cancel form, improve modal accessibility |
| `Controllers/OrdersController.cs` | Add AJAX JSON responses to `Cancel` action |
| `wwwroot/css/site.css` | Add `#cancelLoadingOverlay` styles |
| `wwwroot/js/site.js` | No changes needed (reuse existing `PolyFood.showToast`) |

## Open questions / Decisions needed

1. **Should the Details.cshtml cancel also use a Bootstrap modal instead of `confirm()`?** — Recommended yes, for consistency and better UX. The `confirm()` dialog is browser-dependent and doesn't allow custom styling.
2. **Should the cancel reason be pre-filled or always require user input?** — Currently Details.cshtml uses a hardcoded reason "Khách hàng tự hủy đơn". Index.cshtml asks for a reason. The plan keeps both behaviors as-is.
3. **Should the page auto-reload after successful AJAX cancel?** — The plan uses `setTimeout(() => window.location.reload(), 1000)` to give the user time to see the success toast. This can be changed to a redirect instead.