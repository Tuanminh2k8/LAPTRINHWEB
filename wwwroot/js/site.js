<<<<<<< HEAD
﻿(function () {
    'use strict';

    var PolyFood = {
        csrfToken: '',
        init: function () {
            this.csrfToken = $('input[name="__RequestVerificationToken"]').first().val() || '';
            this.preloader();
            this.navbarScroll();
            this.scrollToTop();
            this.autoDismissAlerts();
            this.revealOnScroll();
            this.animateStatCounters();
        },

        preloader: function () {
            $(window).on('load', function () {
                $('.preloader').addClass('loaded');
            });
            setTimeout(function () { $('.preloader').addClass('loaded'); }, 2000);
        },

        navbarScroll: function () {
            var $nav = $('.navbar-custom');
            var ticking = false;
            $(window).on('scroll', function () {
                if (!ticking) {
                    requestAnimationFrame(function () {
                        if ($(window).scrollTop() > 20) { $nav.addClass('scrolled'); }
                        else { $nav.removeClass('scrolled'); }
                        ticking = false;
                    });
                    ticking = true;
                }
            });
        },

        scrollToTop: function () {
            var $btn = $('#btnScrollTop');
            $(window).on('scroll', function () {
                if ($(this).scrollTop() > 400) { $btn.addClass('visible'); }
                else { $btn.removeClass('visible'); }
            });
            $btn.on('click', function () {
                $('html, body').animate({ scrollTop: 0 }, 500, 'swing');
            });
        },

        autoDismissAlerts: function () {
            setTimeout(function () {
                $('.alert-dismissible').each(function () {
                    var alert = bootstrap.Alert.getOrCreateInstance($(this)[0]);
                    alert.close();
                });
            }, 5000);
        },

        revealOnScroll: function () {
            if (!('IntersectionObserver' in window)) {
                $('.reveal').addClass('revealed');
                return;
            }
            var observer = new IntersectionObserver(function (entries) {
                entries.forEach(function (entry) {
                    if (entry.isIntersecting) {
                        $(entry.target).addClass('revealed');
                        observer.unobserve(entry.target);
                    }
                });
            }, { threshold: 0.08, rootMargin: '0px 0px -40px 0px' });
            $('.reveal').each(function () { observer.observe(this); });
        },

        animateStatCounters: function () {
            if (!('IntersectionObserver' in window)) return;
            var observer = new IntersectionObserver(function (entries) {
                entries.forEach(function (entry) {
                    if (entry.isIntersecting) {
                        PolyFood.countUp(entry.target);
                        observer.unobserve(entry.target);
                    }
                });
            }, { threshold: 0.3 });
            $('.admin-card-stat h2').each(function () { observer.observe(this); });
        },

        countUp: function (el) {
            var $el = $(el);
            var text = $el.text();
            var hasDong = text.indexOf('₫') > -1;
            var cleanNum = parseInt(text.replace(/[^\d]/g, ''), 10) || 0;
            if (cleanNum === 0) { $el.text('0' + (hasDong ? ' ₫' : '')); return; }
            var duration = 1200;
            var startTime = null;
            function step(ts) {
                if (!startTime) startTime = ts;
                var progress = Math.min((ts - startTime) / duration, 1);
                var eased = 1 - Math.pow(1 - progress, 3);
                var current = Math.floor(eased * cleanNum);
                $el.text(current.toLocaleString('vi-VN') + (hasDong ? ' ₫' : ''));
                if (progress < 1) requestAnimationFrame(step);
                else $el.text(text);
            }
            requestAnimationFrame(step);
        },

        showFoodSkeleton: function ($container) {
            var html = '';
            for (var i = 0; i < 6; i++) {
                html += '<div class="col"><div class="card-food"><div class="card-img-container skeleton"></div><div class="card-body"><div class="skeleton" style="height:18px;width:70%;margin-bottom:10px"></div><div class="skeleton" style="height:12px;width:90%;margin-bottom:16px"></div><div class="skeleton" style="height:22px;width:40%"></div></div></div></div>';
            }
            $container.html(html);
        },

        showToast: function (message, type) {
            type = type || 'success';
            var icon = type === 'success' ? 'fa-circle-check' : type === 'danger' ? 'fa-circle-exclamation' : type === 'warning' ? 'fa-triangle-exclamation' : 'fa-circle-info';
            var bg = type === 'success' ? 'bg-success' : type === 'danger' ? 'bg-danger' : type === 'warning' ? 'bg-warning text-dark' : 'bg-info';
            var html = '<div class="toast align-items-center text-white border-0 toast-custom ' + bg + '" role="alert" aria-live="assertive" aria-atomic="true">' +
                '<div class="d-flex"><div class="toast-body fw-600"><i class="fa-solid ' + icon + ' me-2"></i>' + message + '</div>' +
                '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button></div></div>';
            var $container = $('#toastContainer');
            if (!$container.length) {
                $container = $('<div id="toastContainer" class="position-fixed bottom-0 end-0 p-3" style="z-index:1090"></div>').appendTo('body');
            }
            var $toast = $(html).appendTo($container);
            var toast = new bootstrap.Toast($toast[0], { delay: 4000 });
            toast.show();
            $toast.on('hidden.bs.toast', function () { $(this).remove(); });
        }
    };

    /* ─── Global addToCart with loading state ─── */
    window.addToCart = function (id, isCombo, qty, btn) {
        qty = qty || 1;
        var $btn = btn ? $(btn) : $();
        if ($btn.length && $btn.prop('disabled')) return;
        $btn.prop('disabled', true);
        var originalHtml = $btn.length ? $btn.html() : '';
        if ($btn.length) $btn.html('<span class="spinner-border spinner-border-sm me-1" role="status"></span>');
        $.ajax({
            url: '/Cart/AddToCart',
            type: 'POST',
            data: {
                id: id,
                isCombo: isCombo,
                quantity: qty,
                __RequestVerificationToken: PolyFood.csrfToken
            },
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            success: function (res) {
                if (res.success) {
                    var badge = $('#cart-badge');
                    badge.text(res.cartCount).removeClass('d-none');
                    badge.css('animation', 'none');
                    setTimeout(function () { badge.css('animation', 'bounceIn 0.3s ease-out'); }, 10);
                    PolyFood.showToast(res.message || 'Đã thêm vào giỏ hàng!', 'success');
                }
            },
            error: function () {
                PolyFood.showToast('Có lỗi xảy ra. Vui lòng thử lại.', 'danger');
            },
            complete: function () {
                $btn.prop('disabled', false);
                if ($btn.length) $btn.html(originalHtml);
            }
        });
    };

    window.showToast = PolyFood.showToast;

    window.changeQty = function (amount, min, max) {
        min = min || 1; max = max || 50;
        var $input = $('#quantityInput');
        var val = parseInt($input.val(), 10) + amount;
        if (val >= min && val <= max) { $input.val(val); }
    };

    /* ─── Image fallback ─── */
    document.addEventListener('error', function (e) {
        var target = e.target;
        if (target.tagName === 'IMG' && !target.hasAttribute('data-fallback-set')) {
            target.setAttribute('data-fallback-set', 'true');
            target.src = '/images/default_food.svg';
        }
    }, true);

    /* ─── Initialize ─── */
    $(document).ready(function () {
        PolyFood.init();
    });
})();
=======
﻿// Site-wide JavaScript

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
>>>>>>> 77e94ee6c4390ff4e8e3b6c64b60eeee3e2040ed
