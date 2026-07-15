/**
 * toast-helpers.js — Bootstrap Toast notification helpers (60fps Spring Animations Edition)
 */

(function () {
    'use strict';

    var TOAST_CONTAINER_ID = 'toastContainer';
    var TOAST_Z_INDEX     = 9999;

    /* ─────────────────────────────────────────
       DOM helpers
       ───────────────────────────────────────── */
    function escapeHtml(text) {
        if (!text) return '';
        var el = document.createElement('span');
        el.textContent = String(text);
        return el.innerHTML;
    }

    function getOrCreateContainer() {
        var existing = document.getElementById(TOAST_CONTAINER_ID);
        if (existing) return existing;

        var c = document.createElement('div');
        c.id = TOAST_CONTAINER_ID;
        c.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        c.style.zIndex = String(TOAST_Z_INDEX);
        document.body.appendChild(c);
        return c;
    }

    /* ─────────────────────────────────────────
       Core toast builder with Elastic Spring Slide-in
       ───────────────────────────────────────── */
    function showToast(message, bgClass, delayMs) {
        var container = getOrCreateContainer();

        var toast = document.createElement('div');
        toast.className = 'toast align-items-center text-white ' + bgClass + ' border-0 shadow-lg';
        toast.setAttribute('role', 'alert');
        toast.setAttribute('aria-live', 'assertive');
        toast.setAttribute('aria-atomic', 'true');

        // Định hình style cho hiệu ứng trượt nảy mượt mà từ bên ngoài màn hình
        toast.style.transform = 'translateX(100%)';
        toast.style.opacity = '0';
        toast.style.transition = 'transform 0.45s cubic-bezier(0.34, 1.56, 0.64, 1), opacity 0.3s ease';

        toast.innerHTML =
            '<div class="d-flex">' +
                '<div class="toast-body fw-semibold d-flex align-items-center">' + 
                    (bgClass.includes('success') ? '<i class="bi bi-check-circle-fill me-2 fs-5"></i>' : '<i class="bi bi-exclamation-triangle-fill me-2 fs-5"></i>') +
                    escapeHtml(message) + 
                '</div>' +
                '<button type="button" class="btn-close btn-close-white me-2 m-auto"' +
                        ' data-bs-dismiss="toast" aria-label="Close"></button>' +
            '</div>';

        container.appendChild(toast);

        var bsToast = bootstrap.Toast.getOrCreateInstance(toast, { delay: delayMs });
        bsToast.show();

        // Kích hoạt hoạt ảnh Spring ngay trên GPU Frame tiếp theo
        requestAnimationFrame(() => {
            toast.style.transform = 'translateX(0)';
            toast.style.opacity = '1';
        });

        toast.addEventListener('hidden.bs.toast', function () {
            toast.remove();
        });
    }

    function showSuccess(message) {
        if (typeof window.showCustomToast === 'function') {
            window.showCustomToast('create', 'Thành Công', message);
        } else {
            showToast(message || 'Operation completed successfully.', 'bg-success', 3000);
        }
    }

    function showError(message) {
        if (typeof window.showCustomToast === 'function') {
            window.showCustomToast('error', 'Lỗi', message);
        } else {
            showToast(message || 'An error occurred. Please try again.', 'bg-danger', 5000);
        }
    }

    /* ─────────────────────────────────────────
       Expose
       ───────────────────────────────────────── */
    window.ToastHelpers = {
        showSuccess: showSuccess,
        showError:   showError
    };
})();