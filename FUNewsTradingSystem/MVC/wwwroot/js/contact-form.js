/**
 * contact-form.js — Live validation and SignalR toast for Contact page
 */
(function () {
    'use strict';

    function init() {
        var form = document.getElementById('contactForm');
        if (!form) return;

        var nameField = document.getElementById('field-name');
        var emailField = document.getElementById('field-email');
        var messageField = document.getElementById('field-message');
        var submitBtn = document.getElementById('contactSubmit');
        var messageInput = document.getElementById('contactMessage');
        var charCount = document.getElementById('charCount');
        var maxChars = 1000;

        // ── Email validation ──
        function isValidEmail(email) {
            var trimmed = email.trim();
            var atPos = trimmed.indexOf(String.fromCharCode(64)); // avoids @ literal in Razor
            if (atPos < 1 || atPos === trimmed.length - 1) return false;
            var dotPos = trimmed.indexOf('.', atPos);
            return dotPos > atPos + 1 && dotPos < trimmed.length - 1;
        }

        function validateField(field, condition) {
            if (condition) {
                field.classList.remove('is-invalid');
                field.classList.add('is-valid');
                return true;
            } else {
                field.classList.remove('is-valid');
                field.classList.add('is-invalid');
                return false;
            }
        }

        // Name — validate on blur
        var nameInput = document.getElementById('contactName');
        if (nameInput) {
            nameInput.addEventListener('blur', function () {
                validateField(nameField, this.value.trim().length >= 2);
            });
            nameInput.addEventListener('input', function () {
                if (this.value.trim().length >= 2) {
                    nameField.classList.add('is-valid');
                    nameField.classList.remove('is-invalid');
                } else {
                    nameField.classList.remove('is-valid', 'is-invalid');
                }
            });
        }

        // Email — validate on blur
        var emailInput = document.getElementById('contactEmail');
        if (emailInput) {
            emailInput.addEventListener('blur', function () {
                validateField(emailField, isValidEmail(this.value.trim()));
            });
            emailInput.addEventListener('input', function () {
                if (isValidEmail(this.value.trim())) {
                    emailField.classList.add('is-valid');
                    emailField.classList.remove('is-invalid');
                } else {
                    emailField.classList.remove('is-valid', 'is-invalid');
                }
            });
        }

        // Message — char counter + validation
        if (messageInput && charCount) {
            messageInput.addEventListener('input', function () {
                var len = this.value.length;
                charCount.textContent = len;
                var counter = document.querySelector('.char-counter');
                if (counter) {
                    counter.classList.remove('near-limit', 'at-limit');
                    if (len >= maxChars) {
                        counter.classList.add('at-limit');
                    } else if (len >= maxChars * 0.85) {
                        counter.classList.add('near-limit');
                    }
                }
                if (len >= 20) {
                    messageField.classList.add('is-valid');
                    messageField.classList.remove('is-invalid');
                } else {
                    messageField.classList.remove('is-valid', 'is-invalid');
                }
            });
        }

        // Subject dropdown visual feedback
        var subjectInput = document.getElementById('contactSubject');
        if (subjectInput) {
            subjectInput.addEventListener('change', function () {
                var field = this.closest('.float-field');
                if (field) {
                    field.style.borderLeft = this.value ? '3px solid var(--accent)' : 'none';
                }
            });
        }

        // ── Form submission ──
        form.addEventListener('submit', function (e) {
            e.preventDefault();

            var name = nameInput ? nameInput.value.trim() : '';
            var email = emailInput ? emailInput.value.trim() : '';
            var message = messageInput ? messageInput.value.trim() : '';

            var valid = true;
            if (!validateField(nameField, name.length >= 2)) valid = false;
            if (!validateField(emailField, isValidEmail(email))) valid = false;
            if (!validateField(messageField, message.length >= 20)) valid = false;

            if (!valid) {
                submitBtn.style.animation = 'none';
                submitBtn.offsetHeight; // reflow
                submitBtn.style.animation = 'shake 0.4s ease';
                return;
            }

            submitBtn.classList.add('loading');

            setTimeout(function () {
                submitBtn.classList.remove('loading');

                if (typeof window.showCustomToast === 'function') {
                    window.showCustomToast(
                        'create',
                        'Message Received!',
                        'Support team will review your submission and respond within 24 hours.'
                    );
                } else {
                    alert('Message sent! Support will respond within 24 hours.');
                }

                form.reset();
                [nameField, emailField, messageField].forEach(function (f) {
                    f.classList.remove('is-valid', 'is-invalid');
                });
                if (charCount) charCount.textContent = '0';
            }, 1200);
        });

        // Shake animation
        if (!document.getElementById('fnts-shake-style')) {
            var style = document.createElement('style');
            style.id = 'fnts-shake-style';
            style.textContent = [
                '@keyframes shake {',
                '  0%, 100% { transform: translateX(0); }',
                '  20% { transform: translateX(-6px); }',
                '  40% { transform: translateX(6px); }',
                '  60% { transform: translateX(-4px); }',
                '  80% { transform: translateX(4px); }',
                '}'
            ].join('\n');
            document.head.appendChild(style);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

})();
