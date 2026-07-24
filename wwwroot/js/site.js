// Site-wide JavaScript

// Loading Spinner
function showLoading() {
    $('#loadingOverlay').addClass('active');
}

function hideLoading() {
    $('#loadingOverlay').removeClass('active');
}

// Intercept all AJAX requests to show/hide loading spinner
$(document).ajaxStart(function () {
    showLoading();
}).ajaxStop(function () {
    hideLoading();
}).ajaxError(function () {
    hideLoading();
});

// AJAX Add to Cart
function addToCart(id, isCombo) {
    $.ajax({
        url: '/Cart/AddToCart',
        type: 'POST',
        data: { id: id, isCombo: isCombo, quantity: 1 },
        success: function (response) {
            if (response.success) {
                var badge = $('#cart-badge');
                badge.text(response.cartCount);
                badge.removeClass('d-none');
                var toastEl = document.getElementById('cartToast');
                if (toastEl) {
                    $(toastEl).find('.toast-body').html('<i class="fa-solid fa-circle-check me-2"></i> ' + response.message);
                    var toast = new bootstrap.Toast(toastEl);
                    toast.show();
                }
            }
        },
        error: function () {
            alert('Có lỗi xảy ra khi thêm vào giỏ hàng.');
        }
    });
}

// Cart Quantity Update
function updateQuantity(id, isCombo, newQty) {
    $.ajax({
        url: '/Cart/UpdateQuantity',
        type: 'POST',
        data: { id: id, isCombo: isCombo, quantity: newQty },
        success: function (res) {
            if (res.success) {
                if (res.isEmpty) {
                    location.reload();
                    return;
                }
                if (newQty <= 0) {
                    $('#cart-row-' + id + '-' + isCombo).fadeOut(300, function () { $(this).remove(); });
                } else {
                    $('#qty-input-' + id + '-' + isCombo).val(newQty);
                    $('#item-total-' + id + '-' + isCombo).text(res.itemTotal.toLocaleString('vi-VN') + ' ₫');
                }
                $('#cart-total-amount').text(res.cartTotal.toLocaleString('vi-VN') + ' ₫');
                $('#cart-total-count').text(res.cartCount + ' món');
                var badge = $('#cart-badge');
                badge.text(res.cartCount);
                if (res.cartCount > 0) badge.removeClass('d-none');
                else badge.addClass('d-none');
            }
        }
    });
}

// Cart Remove Item
function removeItem(id, isCombo) {
    if (!confirm('Bạn có chắc muốn xóa món này khỏi giỏ hàng?')) return;
    updateQuantity(id, isCombo, 0);
}

// Advanced Search
function performAdvancedSearch() {
    var formData = $('#advSearchForm').serialize();
    $.ajax({
        url: '/Home/AdvancedSearch',
        type: 'GET',
        data: formData,
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        success: function (data) {
            $('#food-list-container').html(data);
            $('html, body').animate({
                scrollTop: $("#foods-section").offset().top - 100
            }, 500);
        },
        error: function () {
            alert('Lỗi khi thực hiện tìm kiếm nâng cao.');
        }
    });
}

// Sort change handler
function handleSortChange() {
    var sortVal = $('#sortOrder').val();
    var url = '/Home/Index';
    var searchName = $('input[name="searchName"]').val() || '';
    var categoryId = $('#advSearchForm select[name="categoryId"]').val() || $('input[name="categoryId"]').val() || '';
    var params = { searchName: searchName };
    if (categoryId) params.categoryId = categoryId;
    if (sortVal) params.sortOrder = sortVal;
    window.location.href = url + '?' + $.param(params);
}

// Quantity change handler
function changeQty(amount, max) {
    var input = $('#quantityInput');
    var val = parseInt(input.val()) + amount;
    if (val >= 1 && val <= max) {
        input.val(val);
    }
}
