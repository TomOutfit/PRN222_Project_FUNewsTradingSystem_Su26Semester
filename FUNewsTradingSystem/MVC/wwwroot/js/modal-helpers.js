/**
 * modal-helpers.js — Shared Bootstrap modal utilities with Premium Micro-interactions
 */

(function () {
    'use strict';

    function escapeHtml(text) {
        if (!text) return '';
        var el = document.createElement('span');
        el.textContent = String(text);
        return el.innerHTML;
    }

    function getOrCreateToastContainer() {
        var existing = document.getElementById('toastContainer');
        if (existing) return existing;
        var c = document.createElement('div');
        c.id = 'toastContainer';
        c.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        c.style.zIndex = '9999';
        document.body.appendChild(c);
        return c;
    }

    function shakeElement(element) {
        if (!element) return;
        element.style.animation = 'none';
        element.offsetHeight; /* Trigger reflow */
        element.style.animation = 'shakeError 0.4s cubic-bezier(0.36, 0.07, 0.19, 0.97) both';
    }

    /* ─────────────────────────────────────────
       showSuccess / showError  (toast wrappers)
       ───────────────────────────────────────── */
    function showSuccess(message) {
        if (typeof window.showCustomToast === 'function') {
            window.showCustomToast('create', 'Thành Công', message);
        } else {
            var container = getOrCreateToastContainer();
            var t = document.createElement('div');
            t.className = 'toast align-items-center text-white bg-success border-0';
            t.setAttribute('role', 'alert');
            t.setAttribute('aria-live', 'assertive');
            t.setAttribute('aria-atomic', 'true');
            t.style.transform = 'translateX(100%)';
            t.style.opacity = '0';
            t.style.transition = 'transform 0.45s cubic-bezier(0.34, 1.56, 0.64, 1), opacity 0.3s ease';

            t.innerHTML =
                '<div class="d-flex">' +
                    '<div class="toast-body fw-medium">' + escapeHtml(message) + '</div>' +
                    '<button type="button" class="btn-close btn-close-white me-2 m-auto" ' +
                            'data-bs-dismiss="toast" aria-label="Close"></button>' +
                '</div>';
            container.appendChild(t);
            var toast = bootstrap.Toast.getOrCreateInstance(t, { delay: 3000 });
            toast.show();

            requestAnimationFrame(function() {
                t.style.transform = 'translateX(0)';
                t.style.opacity = '1';
            });

            t.addEventListener('hidden.bs.toast', function () { t.remove(); });
        }
    }

    function showError(message) {
        if (typeof window.showCustomToast === 'function') {
            window.showCustomToast('error', 'Lỗi', message);
        } else {
            var container = getOrCreateToastContainer();
            var t = document.createElement('div');
            t.className = 'toast align-items-center text-white bg-danger border-0';
            t.setAttribute('role', 'alert');
            t.setAttribute('aria-live', 'assertive');
            t.setAttribute('aria-atomic', 'true');
            t.style.transform = 'translateX(100%)';
            t.style.opacity = '0';
            t.style.transition = 'transform 0.45s cubic-bezier(0.34, 1.56, 0.64, 1), opacity 0.3s ease';

            t.innerHTML =
                '<div class="d-flex">' +
                    '<div class="toast-body fw-medium">' + escapeHtml(message) + '</div>' +
                    '<button type="button" class="btn-close btn-close-white me-2 m-auto" ' +
                            'data-bs-dismiss="toast" aria-label="Close"></button>' +
                '</div>';
            container.appendChild(t);
            var toast = bootstrap.Toast.getOrCreateInstance(t, { delay: 5000 });
            toast.show();

            requestAnimationFrame(function() {
                t.style.transform = 'translateX(0)';
                t.style.opacity = '1';
            });

            t.addEventListener('hidden.bs.toast', function () { t.remove(); });
        }
    }

    /* ─────────────────────────────────────────
       openModal / closeModal
       ───────────────────────────────────────── */
    function openModal(modalId) {
        var el = document.getElementById(modalId);
        if (!el) return;
        var modal = bootstrap.Modal.getOrCreateInstance(el);
        modal.show();
    }

    function closeModal(modalId) {
        var el = document.getElementById(modalId);
        if (!el) return;
        var modal = bootstrap.Modal.getInstance(el);
        if (modal) modal.hide();
    }

    /* ─────────────────────────────────────────
       submitModalForm
       ───────────────────────────────────────── */
    function submitModalForm(formId, successCallback, errorCallback) {
        var form = document.getElementById(formId);
        if (!form) return;

        form.addEventListener('submit', function (e) {
            e.preventDefault();
            clearInlineError(form);

            var submitBtn = form.querySelector('[type="submit"]');
            var originalBtnHtml = '';
            if (submitBtn) {
                originalBtnHtml = submitBtn.innerHTML;
                submitBtn.disabled = true;
                submitBtn.style.transform = 'scale(0.96)';
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Processing...';
            }

            var url  = form.action || window.location.href;
            var data = {};

            var elements = form.querySelectorAll('[name]');
            elements.forEach(function (el) {
                if (el.name) data[el.name] = el.value;
            });

            var token = form.querySelector('[name="__RequestVerificationToken"]');
            var headers = { 'Content-Type': 'application/json' };
            if (token) headers['RequestVerificationToken'] = token.value;

            fetch(url, {
                method: 'POST',
                headers: headers,
                body: JSON.stringify(data)
            })
            .then(function (r) { return r.json(); })
            .then(function (json) {
                if (json.success || (json.Success)) {
                    closeActiveModal();
                    if (successCallback) successCallback(json);
                } else {
                    if (submitBtn) {
                        submitBtn.disabled = false;
                        submitBtn.style.transform = '';
                        submitBtn.innerHTML = originalBtnHtml;
                    }
                    var msg = json.errorMessage || json.ErrorMessage || json.message || 'An error occurred.';
                    showInlineError(form, msg);
                    shakeElement(form);
                    if (errorCallback) errorCallback(json);
                }
            })
            .catch(function () {
                if (submitBtn) {
                    submitBtn.disabled = false;
                    submitBtn.style.transform = '';
                    submitBtn.innerHTML = originalBtnHtml;
                }
                showInlineError(form, 'An unexpected network error occurred. Please try again.');
                shakeElement(form);
                if (errorCallback) errorCallback(null);
            });
        });
    }

    /* ─────────────────────────────────────────
       confirmDelete
       ───────────────────────────────────────── */
    function confirmDelete(entityName, deleteUrl, csrfToken, successCallback) {
        var modal = document.getElementById('_ConfirmDeleteModal');
        if (!modal) {
            if (window.confirm('Delete "' + escapeHtml(entityName) + '"?')) {
                fireDelete(deleteUrl, csrfToken, successCallback);
            }
            return;
        }

        var nameEl = modal.querySelector('[data-confirm-entity-name]') ||
                     modal.querySelector('.confirm-entity-name') ||
                     modal.querySelector('.entity-name-label');
        if (nameEl) nameEl.textContent = entityName;

        modal.dataset.deleteUrl       = deleteUrl;
        modal.dataset.deleteCsrfToken = csrfToken || '';
        modal.dataset.deleteCallback  = successCallback ? 'true' : 'false';

        var modalInstance = bootstrap.Modal.getOrCreateInstance(modal);
        modalInstance.show();
    }

    function initConfirmDelete() {
        var modal = document.getElementById('_ConfirmDeleteModal');
        if (!modal) return;

        var confirmBtn = modal.querySelector('[data-confirm-delete-btn]') ||
                         modal.querySelector('.btn-confirm-delete');
        if (!confirmBtn) return;

        confirmBtn.addEventListener('click', function () {
            var url       = modal.dataset.deleteUrl;
            var token     = modal.dataset.deleteCsrfToken;
            var hasCb     = modal.dataset.deleteCallback === 'true';
            var callback  = hasCb ? window._pendingDeleteCallback : null;

            bootstrap.Modal.getInstance(modal).hide();
            fireDelete(url, token, callback);
            window._pendingDeleteCallback = null;
        });
    }

    function fireDelete(url, csrfToken, callback) {
        var headers = { 'Content-Type': 'application/json' };
        if (csrfToken) headers['RequestVerificationToken'] = csrfToken;

        fetch(url, { method: 'POST', headers: headers })
            .then(function (r) { return r.json(); })
            .then(function (json) {
                if (json.success || json.Success) {
                    showSuccess('Deleted successfully.');
                    if (callback) callback(json);
                } else {
                    showError(json.errorMessage || json.ErrorMessage || 'Delete failed.');
                }
            })
            .catch(function () {
                showError('An unexpected network error occurred during deletion.');
            });
    }

    /* ─────────────────────────────────────────
       Helpers
       ───────────────────────────────────────── */
    function closeActiveModal() {
        var el = document.querySelector('.modal.show');
        if (!el) return;
        var m = bootstrap.Modal.getInstance(el);
        if (m) m.hide();
    }

    function showInlineError(form, message) {
        clearInlineError(form);
        var el = document.createElement('div');
        el.className = 'alert alert-danger mt-3 mb-0 modal-inline-error animate-fade-in-up';
        el.style.animationDuration = '0.3s';
        el.setAttribute('role', 'alert');
        el.innerHTML = escapeHtml(message);
        form.appendChild(el);
    }

    function clearInlineError(form) {
        var existing = form.querySelector('.modal-inline-error');
        if (existing) existing.remove();
    }

    $(document).ready(function () {
        initConfirmDelete();
    });

    window.ModalHelpers = {
        openModal:         openModal,
        closeModal:        closeModal,
        submitModalForm:   submitModalForm,
        confirmDelete:     confirmDelete,
        showSuccess:       showSuccess,
        showError:         showError,
        escapeHtml:        escapeHtml
    };
})();