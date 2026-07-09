/**
 * ajax-crud.js — Shared AJAX CRUD helpers for modal-based operations
 * Used by all entity management pages (Admin/Accounts, Category, Tag, etc.)
 */

(function () {
    'use strict';

    /**
     * Submit a form inside a Bootstrap modal via AJAX.
     * On success: closes modal, shows success toast, optionally reloads.
     * On error: shows error toast inside the modal.
     *
     * @param {HTMLFormElement} form
     * @param {string} successMessage
     * @param {boolean} reloadOnSuccess
     */
    function submitModalForm(form, successMessage, reloadOnSuccess) {
        if (!form) return;
        form.addEventListener('submit', function (e) {
            e.preventDefault();

            var url = form.action || window.location.href;
            var isEdit = form.querySelector('[name*="Id"]')?.value;

            fetch(url, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': getAntiForgeryToken(),
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(Object.fromEntries(new FormData(form)))
            })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data.success) {
                    hideActiveModal();
                    showToast(successMessage || 'Operation successful.', false);
                    if (reloadOnSuccess !== false) {
                        setTimeout(function () {
                            if (typeof window.refreshPageContentRealtime === 'function') {
                                window.refreshPageContentRealtime();
                            } else {
                                location.reload();
                            }
                        }, 500);
                    }
                } else {
                    showInlineError(form, data.errorMessage || 'An error occurred.');
                }
            })
            .catch(function () {
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
        errorEl.className = 'alert alert-danger mt-3 mb-0 modal-error';
        errorEl.textContent = message;
        form.appendChild(errorEl);
    }

    function showToast(message, isError) {
        var container = document.getElementById('toastContainer') || createToastContainer();
        var t = document.createElement('div');
        t.className = 'toast align-items-center text-white ' + (isError ? 'bg-danger' : 'bg-success') + ' border-0';
        t.innerHTML = '<div class="d-flex"><div class="toast-body">' + escapeHtml(message) +
            '</div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>';
        container.appendChild(t);
        bootstrap.Toast.getOrCreateInstance(t, { delay: isError ? 5000 : 3000 }).show();
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

    window.AjaxCrud = {
        submitModalForm: submitModalForm,
        showToast: showToast
    };
})();
