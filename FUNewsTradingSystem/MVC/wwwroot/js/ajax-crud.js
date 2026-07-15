/**
 * ajax-crud.js — Shared AJAX CRUD helpers with premium 60fps micro-interactions
 * Used by all entity management pages (Admin/Accounts, Category, Tag, etc.)
 */

(function () {
    'use strict';

    /**
     * Submit a form inside a Bootstrap modal via AJAX with high-performance animations.
     * On success: closes modal, shows success toast, optionally reloads.
     * On error: shows error toast/inline error inside the modal.
     *
     * @param {HTMLFormElement} form
     * @param {string} successMessage
     * @param {boolean} reloadOnSuccess
     */
    function submitModalForm(form, successMessage, reloadOnSuccess) {
        if (!form) return;
        
        form.addEventListener('submit', function (e) {
            e.preventDefault();

            // 1. Tương tác vi mô: Phản hồi thu nhỏ nút Submit tức thì khi click
            var submitBtn = form.querySelector('[type="submit"]');
            var originalBtnHtml = '';
            if (submitBtn) {
                originalBtnHtml = submitBtn.innerHTML;
                submitBtn.style.transform = 'scale(0.95)';
                submitBtn.disabled = true;
                // Chuyển sang trạng thái nạp dữ liệu (Loading Spinner) mượt mà
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Processing...';
                
                setTimeout(function () {
                    submitBtn.style.transform = '';
                }, 150);
            }

            var url = form.action || window.location.href;

            // Thu thập dữ liệu an toàn
            var formData = Object.fromEntries(new FormData(form));

            fetch(url, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': getAntiForgeryToken(),
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(formData)
            })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                    if (data.success) {
                    hideActiveModal();
                    showToast(successMessage || 'Operation completed successfully.', false);
                    
                    if (reloadOnSuccess !== false) {
                        // Delay đồng bộ hoàn hảo với hoạt ảnh đóng modal
                        setTimeout(function () {
                            if (typeof window.refreshPageContentRealtime === 'function') {
                                window.refreshPageContentRealtime();
                            } else {
                                location.reload();
                            }
                        }, 400);
                    }
                } else {
                    // Khôi phục trạng thái nút bấm nếu thất bại
                    if (submitBtn) {
                        submitBtn.disabled = false;
                        submitBtn.innerHTML = originalBtnHtml;
                    }
                    showInlineError(form, data.errorMessage || 'An error occurred.');
                }
            })
            .catch(function () {
                if (submitBtn) {
                    submitBtn.disabled = false;
                    submitBtn.innerHTML = originalBtnHtml;
                }
                showInlineError(form, 'An unexpected network error occurred.');
            });
        });
    }

    function getAntiForgeryToken() {
        var el = document.querySelector('[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    function hideActiveModal() {
        var modalEl = document.querySelector('.modal.show');
        if (modalEl) {
            var modal = bootstrap.Modal.getInstance(modalEl);
            if (modal) modal.hide();
        }
    }

    function showInlineError(form, message) {
        var existing = form.querySelector('.modal-error');
        if (existing) existing.remove();
        
        var errorEl = document.createElement('div');
        // Kích hoạt animation lướt lên mượt mà của hệ thống
        errorEl.className = 'alert alert-danger mt-3 mb-0 modal-error animate-fade-in-up';
        errorEl.style.animationDuration = '0.35s';
        errorEl.textContent = message;
        form.appendChild(errorEl);

        // Hiệu ứng rung lắc (Elastic Shake) thông báo lỗi khi không thể submit
        form.style.transform = 'none';
        form.offsetHeight; /* Kích hoạt Reflow trong trình duyệt để tái tạo hoạt ảnh */
        form.style.animation = 'shakeError 0.4s cubic-bezier(0.36, 0.07, 0.19, 0.97) both';
        
        form.addEventListener('animationend', function() {
            form.style.animation = '';
        }, { once: true });
    }

    function showToast(message, isError) {
        var container = document.getElementById('toastContainer') || createToastContainer();
        var t = document.createElement('div');
        
        // Thiết lập cấu trúc Toast nảy trượt mượt mà (Slide-in Spring)
        t.className = 'toast align-items-center text-white ' + (isError ? 'bg-danger' : 'bg-success') + ' border-0';
        t.style.transform = 'translateX(100%)';
        t.style.opacity = '0';
        t.style.transition = 'transform 0.45s cubic-bezier(0.34, 1.56, 0.64, 1), opacity 0.3s ease';

        t.innerHTML = '<div class="d-flex"><div class="toast-body fw-medium">' + escapeHtml(message) +
            '</div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>';
        
        container.appendChild(t);
        
        var bsToast = bootstrap.Toast.getOrCreateInstance(t, { delay: isError ? 5000 : 3000 });
        bsToast.show();

        // Đẩy hoạt ảnh vào Frame tiếp theo của GPU để đảm bảo 60fps tuyệt đối
        requestAnimationFrame(function () {
            t.style.transform = 'translateX(0)';
            t.style.opacity = '1';
        });

        t.addEventListener('hidden.bs.toast', function () { t.remove(); });
    }

    function createToastContainer() {
        var c = document.createElement('div');
        c.id = 'toastContainer';
        c.className = 'position-fixed bottom-0 end-0 p-3';
        c.style.zIndex = '9999';
        document.body.appendChild(c);
        return c;
    }

    function escapeHtml(text) {
        var el = document.createElement('span');
        el.textContent = text;
        return el.innerHTML;
    }

    // Đăng ký Keyframes rung lắc lỗi trực tiếp nếu chưa tồn tại
    if (!document.getElementById('shake-keyframes-global')) {
        var style = document.createElement('style');
        style.id = 'shake-keyframes-global';
        style.innerHTML = `
            @keyframes shakeError {
                10%, 90% { transform: translate3d(-1px, 0, 0); }
                20%, 80% { transform: translate3d(2px, 0, 0); }
                30%, 50%, 70% { transform: translate3d(-4px, 0, 0); }
                40%, 60% { transform: translate3d(4px, 0, 0); }
            }
        `;
        document.head.appendChild(style);
    }

    window.AjaxCrud = {
        submitModalForm: submitModalForm,
        showToast: showToast
    };
})();