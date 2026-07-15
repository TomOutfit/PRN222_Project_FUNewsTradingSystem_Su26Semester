/**
 * validate-extensions.js — jQuery Validate custom validators (Premium Edition)
 */

(function () {
    'use strict';

    // Trợ giúp rung lắc phần tử bị lỗi nhập liệu
    function shakeElement(element) {
        if (!element) return;
        element.style.animation = 'none';
        element.offsetHeight; /* Trigger browser reflow */
        element.style.animation = 'shakeError 0.4s cubic-bezier(0.36, 0.07, 0.19, 0.97) both';
        element.addEventListener('animationend', function() {
            element.style.animation = '';
        }, { once: true });
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
        if (!startVal || !endVal) return true; 

        var start = new Date(startVal);
        var end   = new Date(endVal);
        if (isNaN(start.getTime()) || isNaN(end.getTime())) return true;

        var isValid = start <= end;
        if (!isValid) {
            shakeElement(startEl);
            shakeElement(endEl);
        }
        return isValid;
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

        var isValid = parentVal !== currentId;
        if (!isValid) {
            shakeElement(parentSelect);
        }
        return isValid;
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

        var isValid = value === newPwdEl.value;
        if (!isValid) {
            shakeElement(element);
        }
        return isValid;
    }

    /* ─────────────────────────────────────────
       Init — wire validators + wire forms automatically
       ───────────────────────────────────────── */
    function initValidateExtensions() {
        if (!$.validator) return;

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
                    if (!$form.valid()) {
                        shakeElement($form[0]);
                        return;
                    }
                    var ok = dateRangeValidator(null, $end[0]);
                    if (!ok) {
                        $form.validate().showErrors({
                            'dateRange__dummy': 'Start date must be before or equal to end date.'
                        });
                        shakeElement($form[0]);
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

        // Tùy biến thông báo lỗi của jquery validate hiển thị lướt nhẹ mượt mà
        var defaultOptions = $.validator.defaults;
        defaultOptions.highlight = function (element) {
            $(element).addClass('is-invalid').removeClass('is-valid');
            var group = $(element).closest('.form-group, .mb-3');
            if (group.length) {
                group.addClass('animate-pulse-error');
            }
        };
        defaultOptions.unhighlight = function (element) {
            $(element).removeClass('is-invalid').addClass('is-valid');
            var group = $(element).closest('.form-group, .mb-3');
            if (group.length) {
                group.removeClass('animate-pulse-error');
            }
        };
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