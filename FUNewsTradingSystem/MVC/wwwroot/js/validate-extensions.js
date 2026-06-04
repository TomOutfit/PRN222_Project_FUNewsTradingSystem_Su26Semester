/**
 * validate-extensions.js — jQuery Validate custom validators
 * Extends jquery-validation with domain-specific rules:
 *   - dateRange  : StartDate <= EndDate  (Statistics filter)
 *   - notSelf    : ParentCategoryID != CategoryID  (Category Edit)
 *   - passwordMatch : ConfirmPassword == NewPassword  (Profile Change Password)
 *
 * Usage:
 *   // Automatically wired on document ready when data-val-* attributes are present.
 *   // For explicit wiring call initValidateExtensions() after DOM is ready.
 */

(function () {
    'use strict';

    function escapeHtml(text) {
        var el = document.createElement('span');
        el.textContent = text;
        return el.innerHTML;
    }

    /* ─────────────────────────────────────────
       dateRange — blocks submit when StartDate > EndDate
       ───────────────────────────────────────── */
    function dateRangeValidator(value, element, params) {
        var form = element.form || element.closest('form');
        if (!form) return true;

        var startEl = form.querySelector('[data-val-date-range-start]') || form.querySelector('[name*="StartDate"]');
        var endEl   = form.querySelector('[data-val-date-range-end]')   || form.querySelector('[name*="EndDate"]');

        if (!startEl || !endEl) return true;

        var startVal = startEl.value.trim();
        var endVal   = endEl.value.trim();
        if (!startVal || !endVal) return true; // let required validator handle empty

        var start = new Date(startVal);
        var end   = new Date(endVal);
        if (isNaN(start.getTime()) || isNaN(end.getTime())) return true;

        return start <= end;
    }

    /* ─────────────────────────────────────────
       notSelf — blocks submit when ParentCategoryID == CategoryID
       ───────────────────────────────────────── */
    function notSelfValidator(value, element, params) {
        var form = element.form || element.closest('form');
        if (!form) return true;

        var parentSelect = form.querySelector('[data-val-not-self-parent]') ||
                           form.querySelector('[name*="ParentCategoryID"]');
        if (!parentSelect) return true;

        var parentVal = parentSelect.value;
        var currentId = element.value || form.querySelector('[name*="CategoryID"]')?.value || '';

        if (!parentVal || parentVal === '0' || parentVal === '') return true;

        return parentVal !== currentId;
    }

    /* ─────────────────────────────────────────
       passwordMatch — blocks submit when passwords differ
       ───────────────────────────────────────── */
    function passwordMatchValidator(value, element, params) {
        var form = element.form || element.closest('form');
        if (!form) return true;

        var newPwdEl = form.querySelector('[data-val-password-match-new]') ||
                       form.querySelector('[name*="NewPassword"]');
        if (!newPwdEl) return true;

        return value === newPwdEl.value;
    }

    /* ─────────────────────────────────────────
       Init — wire validators + wire forms automatically
       ───────────────────────────────────────── */
    function initValidateExtensions() {
        /* dateRange */
        $.validator.addMethod('dateRange', dateRangeValidator,
            'Start date must be before or equal to end date.');

        $('form[data-val-date-range]').each(function () {
            var $form = $(this);
            var $start = $form.find('[data-val-date-range-start]').length
                ? $form.find('[data-val-date-range-start]')
                : $form.find('[name*="StartDate"]').first();
            var $end   = $form.find('[data-val-date-range-end]').length
                ? $form.find('[data-val-date-range-end]')
                : $form.find('[name*="EndDate"]').first();

            if ($start.length && $end.length) {
                $form.validate().settings.rules['dateRange__dummy'] = true;
                $form.on('submit', function (e) {
                    if (!$form.valid()) return;
                    var ok = dateRangeValidator(null, $end[0]);
                    if (!ok) {
                        $form.validate().showErrors({
                            'dateRange__dummy': 'Start date must be before or equal to end date.'
                        });
                        e.preventDefault();
                    }
                });
            }
        });

        /* notSelf */
        $.validator.addMethod('notSelf', notSelfValidator,
            'A category cannot be its own parent.');

        $('form[data-val-not-self]').each(function () {
            var $form = $(this);
            var $parentSelect = $form.find('[data-val-not-self-parent]').first();
            var $categoryId  = $form.find('[name*="CategoryID"]').first();
            if ($parentSelect.length && $categoryId.length) {
                $parentSelect.rules('add', { notSelf: true });
            }
        });

        /* passwordMatch */
        $.validator.addMethod('passwordMatch', passwordMatchValidator,
            'Confirm password does not match new password.');

        $('form[data-val-password-match]').each(function () {
            var $form = $(this);
            var $confirm = $form.find('[data-val-password-match-confirm]').first();
            if ($confirm.length) {
                $confirm.rules('add', { passwordMatch: true });
            }
        });
    }

    /* Wire on document ready */
    $(document).ready(function () {
        initValidateExtensions();
    });

    /* Expose for explicit use */
    window.ValidateExtensions = {
        init: initValidateExtensions,
        dateRangeValidator:   dateRangeValidator,
        notSelfValidator:     notSelfValidator,
        passwordMatchValidator: passwordMatchValidator
    };
})();
