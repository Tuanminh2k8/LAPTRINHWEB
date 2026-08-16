(function () {
    'use strict';

    var PolyFood = {
        csrfToken: '',
        init: function () {
            this.csrfToken = $('input[name="__RequestVerificationToken"]').first().val() || '';

            // Tự gắn CSRF token vào mọi AJAX nếu form có sẵn (hỗ trợ các action [ValidateAntiForgeryToken])
            if (this.csrfToken) {
                $.ajaxSetup({
                    headers: { 'RequestVerificationToken': this.csrfToken }
                });
            }

            this.preloader();
            this.navbarScroll();
            this.scrollToTop();
            this.autoDismissAlerts();
            this.revealOnScroll();
            this.animateStatCounters();
            this.autoSlideSlider();
        },

        preloader: function () {
            // Ẩn ngay khi DOM ready (không chờ window.load vì ảnh ngoài có thể làm chậm)
            $('.preloader').addClass('loaded');
            $(window).on('load', function () {
                $('.preloader').addClass('loaded');
            });
        },

        navbarScroll: function () {
            var $nav = $('.navbar-polyfood');
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

        autoSlideSlider: function () {
            var $slider = $('#homepageSlider');
            if ($slider.length === 0) return;
            var $items = $slider.find('.slider-item');
            var totalItems = $items.length;
            var currentIndex = 0;
            var isTransitioning = false;

            // Sinh dots động (tránh phụ thuộc HTML có sẵn)
            var $dotsWrap = $slider.find('.slider-dots');
            if ($dotsWrap.length && $dotsWrap.find('.slider-dot').length === 0) {
                for (var i = 0; i < totalItems; i++) {
                    $dotsWrap.append('<span class="slider-dot"></span>');
                }
            }
            var $dots = $slider.find('.slider-dot');

            function showSlide(index) {
                if (isTransitioning) return;
                isTransitioning = true;
                index = (index + totalItems) % totalItems;
                $items.removeClass('active').eq(index).addClass('active');
                $dots.removeClass('active').eq(index).addClass('active');
                currentIndex = index;
                isTransitioning = false;
            }

            function nextSlide() {
                showSlide(currentIndex + 1);
            }

            function prevSlide() {
                showSlide(currentIndex - 1);
            }

            // Auto-slide every 5 seconds
            var slideInterval = setInterval(nextSlide, 5000);

            // Pause on hover
            $slider.on('mouseenter', function () {
                clearInterval(slideInterval);
            }).on('mouseleave', function () {
                clearInterval(slideInterval);
                slideInterval = setInterval(nextSlide, 5000);
            });

            // Nav controls (khớp class HTML: slider-control prev/next)
            $slider.find('.slider-control.prev').on('click', function () {
                prevSlide();
                clearInterval(slideInterval);
                slideInterval = setInterval(nextSlide, 5000);
            });
            $slider.find('.slider-control.next').on('click', function () {
                nextSlide();
                clearInterval(slideInterval);
                slideInterval = setInterval(nextSlide, 5000);
            });
            $slider.find('.slider-dot').on('click', function () {
                showSlide($(this).index());
                clearInterval(slideInterval);
                slideInterval = setInterval(nextSlide, 5000);
            });
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

    /* ─── Food Customize Modal (size/topping/độ cay) ─── */
    var CustomizeModal = {
        food: null,
        currentBtn: null,

        open: function (food, btn) {
            this.food = food;
            this.currentBtn = btn || null;
            $('#cm-name').text(food.name);
            $('#cm-image').attr('src', food.imageUrl || '/images/default_food.svg');
            $('#cm-desc').text(food.description || '');
            $('#cm-category').text(food.categoryName || '');
            if (food.reviewCount > 0) {
                $('#cm-rating').removeClass('d-none').addClass('d-flex');
                $('#cm-rating-value').text(food.avgRating ? food.avgRating.toFixed(1) : '0');
                $('#cm-rating-count').text('(' + food.reviewCount + ' đánh giá)');
            } else {
                $('#cm-rating').addClass('d-none').removeClass('d-flex');
            }
            this.selectedVariantId = null;
            this.renderGroups(food.modifierGroups || [], food.variants || []);
            $('#cm-qty').val(1);
            this.updateTotal();
            var modal = new bootstrap.Modal(document.getElementById('foodCustomizeModal'));
            modal.show();
        },

        renderGroups: function (groups, variants) {
            var $container = $('#cm-groups');
            $container.empty();
            $('#cm-loading').addClass('d-none');

            // ─── Phân loại (FoodVariant: Size/SKU) ───
            if (variants && variants.length) {
                var $v = $('#cm-variant-group-template').html();
                var $vg = $($v);
                variants.forEach(function (v, idx) {
                    var $t = $('#cm-variant-option').html();
                    var $o = $t
                        .replace(/__ID__/g, v.id)
                        .replace(/__NAME__/g, v.displayName || v.name)
                        .replace(/__PRICE__/g, v.price)
                        .replace(/__STOCK__/g, v.stockQuantity)
                        .replace(/__DEFAULT__/g, v.isDefault);
                    var $chip = $($o);
                    $chip.find('.cm-var-name').text(v.displayName || v.name);
                    var stockText = v.stockQuantity <= 5 ? ' · còn ' + v.stockQuantity : '';
                    $chip.find('.cm-var-stock').text(stockText);
                    $chip.find('.cm-var-price').text(Number(v.price).toLocaleString('vi-VN') + ' ₫');
                    if (v.isDefault || idx === 0) {
                        $chip.find('.cm-var-input').prop('checked', true);
                        this.selectedVariantId = v.id;
                    }
                    $vg.find('.cm-variants').append($chip);
                }.bind(this));
                $container.append($vg);
            }

            if (!groups || !groups.length) {
                if (!variants || !variants.length) {
                    $container.append('<p class="text-muted mb-0">Món ăn này không có tùy chọn.</p>');
                }
                this.updateTotal();
                return;
            }

            groups.forEach(function (group) {
                var $tmpl = group.isMultiple ? $('#cm-multi-group-template') : $('#cm-single-group-template');
                var $g = $tmpl.html();
                var $group = $($g);
                $group.find('.cm-group-title').text(group.name);
                if (group.description) {
                    $group.find('.cm-group-desc').removeClass('d-none').text(group.description);
                }
                var $opts = $group.find('.cm-options');
                if (group.isMultiple) {
                    $group.find('.cm-group-count').text('Tối đa ' + group.maxOptions + ' lựa chọn');
                } else {
                    $group.find('.cm-group-required').removeClass('d-none').text(group.minOptions > 0 ? 'Bắt buộc' : 'Chọn 1');
                }
                (group.options || []).forEach(function (opt) {
                    var $t = group.isMultiple ? $('#cm-option-multi').html() : $('#cm-option-single').html();
                    $t = $t.replace(/__GROUP__/g, group.id);
                    var $o = $t
                        .replace(/__ID__/g, opt.id)
                        .replace(/__OPTNAME__/g, opt.name)
                        .replace(/__OPTPRICE__/g, opt.price)
                        .replace(/__DEFAULT__/g, opt.isDefault);
                    var $chip = $($o);
                    $chip.find('.cm-option-name').text(opt.name);
                    if (opt.price > 0) $chip.find('.cm-opt-price-display').text('+' + Number(opt.price).toLocaleString('vi-VN') + ' ₫');
                    else $chip.find('.cm-opt-price-display').text('Miễn phí');
                    if (opt.isDefault) $chip.find('.cm-opt-input').prop('checked', true);
                    $opts.append($chip);
                });
                $container.append($group);
            });
            this.updateTotal();
        },

        getSelected: function () {
            var selected = [];
            $('#cm-groups .cm-group').each(function () {
                var isMulti = $(this).find('.cm-opt-input[type="checkbox"]').length > 0;
                if (isMulti) {
                    $(this).find('.cm-opt-input:checked').each(function () {
                        selected.push($(this).val());
                    });
                } else {
                    var $checked = $(this).find('.cm-opt-input:checked');
                    if ($checked.length) selected.push($checked.val());
                    else {
                        var $first = $(this).find('.cm-opt-input').first();
                        $first.prop('checked', true);
                        selected.push($first.val());
                    }
                }
            });
            return selected;
        },

        getSelectedVariant: function () {
            var $checked = $('#cm-variants .cm-var-input:checked');
            if ($checked.length) return Number($checked.val());
            if (this.selectedVariantId) return this.selectedVariantId;
            return null;
        },

        getVariantPrice: function () {
            var $checked = $('#cm-variants .cm-var-input:checked');
            if ($checked.length) return Number($checked.data('price') || 0);
            var base = this.food.price || 0;
            var variants = this.food.variants || [];
            if (variants.length) return Number(variants[0].price || base);
            return base;
        },

        getUnitPrice: function () {
            var base = this.getVariantPrice();
            var addon = 0;
            $('#cm-groups .cm-opt-input:checked').each(function () {
                addon += Number($(this).data('price') || 0);
            });
            return base + addon;
        },

        updateTotal: function () {
            var qty = parseInt($('#cm-qty').val(), 10) || 1;
            var unit = this.getUnitPrice();
            $('#cm-total').text(Number(unit * qty).toLocaleString('vi-VN') + ' ₫');
        },

        enforceMultiLimit: function ($group) {
            var max = 1;
            var $req = $group.closest('.cm-group').find('.cm-group-count');
            var name = $group.closest('.cm-group').find('.cm-group-title').text();
            // parse max from template container name lookup
            CustomizeModal.food.modifierGroups.forEach(function (g) {
                if (g.name === name) max = g.maxOptions;
            });
            var checked = $group.closest('.cm-group').find('.cm-opt-input:checked').length;
            if (checked > max) {
                PolyFood.showToast('Chỉ được chọn tối đa ' + max + ' lựa chọn cho "' + name + '".', 'warning');
                $group.prop('checked', false);
            }
        }
    };

    window.addToCart = function (id, isCombo, qty, btn) {
        qty = qty || 1;
        var $btn = btn ? $(btn) : $();
        if ($btn.length && $btn.prop('disabled')) return;

        if (isCombo) {
            addToCartDirect(id, isCombo, qty, btn);
            return;
        }

        // Món lẻ: kiểm tra tùy chọn (size/topping/độ cay) qua API
        $.getJSON('/api/foods/' + id)
            .done(function (food) {
                var hasVariants = food.variants && food.variants.length;
                var hasGroups = food.modifierGroups && food.modifierGroups.length;
                if (hasVariants || hasGroups) {
                    CustomizeModal.open(food, btn);
                } else {
                    addToCartDirect(id, false, qty, btn);
                }
            })
            .fail(function () {
                addToCartDirect(id, false, qty, btn);
            });
    };

    function addToCartDirect(id, isCombo, qty, btn) {
        var $btn = btn ? $(btn) : $();
        if ($btn.length && $btn.prop('disabled')) return;
        $btn.prop('disabled', true);
        var originalHtml = $btn.length ? $btn.html() : '';
        if ($btn.length) $btn.html('<span class="spinner-border spinner-border-sm me-1" role="status"></span>');
        $.ajax({
            url: '/api/cart/add',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                foodId: isCombo ? null : id,
                comboId: isCombo ? id : null,
                quantity: qty,
                optionIds: []
            }),
            success: function (res) {
                if (res.success) {
                    updateCartBadge(res.count);
                    PolyFood.showToast(res.message || 'Đã thêm vào giỏ hàng!', 'success');
                }
            },
            error: function (xhr) {
                var msg = 'Có lỗi xảy ra. Vui lòng thử lại.';
                if (xhr.responseJSON && xhr.responseJSON.message) msg = xhr.responseJSON.message;
                PolyFood.showToast(msg, 'danger');
            },
            complete: function () {
                $btn.prop('disabled', false);
                if ($btn.length) $btn.html(originalHtml);
            }
        });
    }

    function updateCartBadge(count) {
        var badge = $('#cart-badge');
        if (count > 0) {
            badge.text(count).removeClass('d-none');
            badge.css('animation', 'none');
            setTimeout(function () { badge.css('animation', 'bounceIn 0.3s ease-out'); }, 10);
        } else {
            badge.addClass('d-none');
        }
    }

    window.showToast = PolyFood.showToast;

    /* ─── Global loading overlay helpers (dùng bởi Admin/Orders, API actions) ─── */
    window.showLoading = function (message) {
        var $overlay = $('#global-loading-overlay');
        if ($overlay.length === 0) {
            $overlay = $('<div id="global-loading-overlay" class="global-loading-overlay"><div class="spinner-border text-danger" role="status"></div><div class="global-loading-text"></div></div>').appendTo('body');
        }
        $overlay.find('.global-loading-text').text(message || 'Đang xử lý...');
        $overlay.addClass('show');
    };

    window.hideLoading = function () {
        $('#global-loading-overlay').removeClass('show');
    };

    window.changeQty = function (amount, min, max) {
        min = min || 1; max = max || 50;
        var $input = $('#quantityInput');
        var val = parseInt($input.val(), 10) + amount;
        if (val >= min && val <= max) { $input.val(val); }
    };

    /* ─── Image fallback (category-aware, offline-safe) ─── */
    document.addEventListener('error', function (e) {
        var target = e.target;
        if (target && target.tagName === 'IMG' && !target.hasAttribute('data-fallback-set')) {
            target.setAttribute('data-fallback-set', 'true');
            var cat = (target.getAttribute('data-category') || '').trim().toLowerCase();
            var map = {
                'burgers': 'burger', 'pizzas': 'pizza', 'gà rán': 'chicken',
                'thức uống & tráng miệng': 'drink', 'món kèm': 'side', 'tráng miệng': 'dessert',
                'đồ ăn sáng': 'breakfast', 'salad & wrap': 'salad', 'combos': 'combo'
            };
            var fallback = map[cat] ? '/images/category-' + map[cat] + '.svg' : '/images/default_food.svg';
            target.src = fallback;
        }
    }, true);

    /* ─── Initialize ─── */
    $(document).ready(function () {
        PolyFood.init();

        // ─── Food Customize Modal events ───
        $('#cm-qty-plus').on('click', function () {
            var $q = $('#cm-qty');
            var v = parseInt($q.val(), 10) || 1;
            if (v < 50) { $q.val(v + 1); CustomizeModal.updateTotal(); }
        });
        $('#cm-qty-minus').on('click', function () {
            var $q = $('#cm-qty');
            var v = parseInt($q.val(), 10) || 1;
            if (v > 1) { $q.val(v - 1); CustomizeModal.updateTotal(); }
        });
        $('#cm-groups').on('change', '.cm-opt-input', function () {
            if ($(this).attr('type') === 'checkbox') CustomizeModal.enforceMultiLimit($(this));
            CustomizeModal.updateTotal();
        });
        $('#cm-groups').on('change', '.cm-var-input', function () {
            CustomizeModal.updateTotal();
        });
        $('#cm-add-btn').on('click', function () {
            if (!CustomizeModal.food) return;
            var $btn = $(this).prop('disabled', true);
            var original = $btn.html();
            $btn.html('<span class="spinner-border spinner-border-sm me-2"></span> Đang thêm...');
            var qty = parseInt($('#cm-qty').val(), 10) || 1;
            $.ajax({
                url: '/api/cart/add',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    foodId: CustomizeModal.food.id,
                    comboId: null,
                    quantity: qty,
                    variantId: CustomizeModal.getSelectedVariant(),
                    optionIds: CustomizeModal.getSelected().map(Number)
                }),
                success: function (res) {
                    if (res.success) {
                        updateCartBadge(res.count);
                        PolyFood.showToast(res.message || 'Đã thêm vào giỏ hàng!', 'success');
                        bootstrap.Modal.getInstance(document.getElementById('foodCustomizeModal')).hide();
                    }
                },
                error: function (xhr) {
                    var msg = 'Có lỗi xảy ra. Vui lòng thử lại.';
                    if (xhr.responseJSON && xhr.responseJSON.message) msg = xhr.responseJSON.message;
                    PolyFood.showToast(msg, 'danger');
                },
                complete: function () {
                    $btn.prop('disabled', false).html(original);
                }
            });
        });
        $('#foodCustomizeModal').on('hidden.bs.modal', function () {
            $('#cm-groups').empty();
            $('#cm-loading').removeClass('d-none');
            CustomizeModal.food = null;
        });
    });
})();
