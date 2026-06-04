/**
 * toast-helpers.js — Bootstrap Toast notification helpers
 *
 * Public API:
 *   ToastHelpers.showSuccess(message)  — green toast, auto-dismiss 3 s
 *   ToastHelpers.showError(message)    — red toast, auto-dismiss 5 s
 *
 * Both create a fixed-position toast container at bottom-right if one
 * does not already exist, and remove the toast element after it hides.
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
       Core toast builder
       @param {string} message
       @param {string} bgClass   — Bootstrap bg-* utility class  e.g. "bg-success"
       @param {number} delayMs
       ───────────────────────────────────────── */
    function showToast(message, bgClass, delayMs) {
        var container = getOrCreateContainer();

        var toast = document.createElement('div');
        toast.className = 'toast align-items-center text-white ' + bgClass + ' border-0';
        toast.setAttribute('role', 'alert');
        toast.setAttribute('aria-live', 'assertive');
        toast.setAttribute('aria-atomic', 'true');

        toast.innerHTML =
            '<div class="d-flex">' +
                '<div class="toast-body fw-medium">' + escapeHtml(message) + '</div>' +
                '<button type="button" class="btn-close btn-close-white me-2 m-auto"' +
                        ' data-bs-dismiss="toast" aria-label="Close"></button>' +
            '</div>';

        container.appendChild(toast);

        var bsToast = bootstrap.Toast.getOrCreateInstance(toast, { delay: delayMs });
        bsToast.show();

        toast.addEventListener('hidden.bs.toast', function () {
            toast.remove();
        });
    }

    /* ─────────────────────────────────────────
       Public API
       ───────────────────────────────────────── */
    function showSuccess(message) {
        showToast(message || 'Operation completed successfully.', 'bg-success', 3000);
    }

    function showError(message) {
        showToast(message || 'An error occurred. Please try again.', 'bg-danger', 5000);
    }

    /* ─────────────────────────────────────────
       Expose
       ───────────────────────────────────────── */
    window.ToastHelpers = {
        showSuccess: showSuccess,
        showError:   showError
    };
})();
