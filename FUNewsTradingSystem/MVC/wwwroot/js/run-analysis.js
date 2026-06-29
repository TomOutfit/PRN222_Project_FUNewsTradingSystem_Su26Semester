/**
 * run-analysis.js — AI Trading Pipeline UI Handler
 * 
 * Handles the Run Analysis button click:
 * - Disables button + shows spinner during execution
 * - POSTs form data to /Staff/RunAnalysis
 * - Shows success (green) or error (red) alert with appropriate message
 * - Prevents double-submission
 */

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var btnRunAnalysis = document.getElementById('btnRunAnalysis');
        var loadingSpinner = document.getElementById('loadingSpinner');
        var resultArea = document.getElementById('resultArea');
        var form = document.getElementById('runAnalysisForm');

        if (!btnRunAnalysis || !loadingSpinner || !resultArea || !form) {
            return;
        }

        btnRunAnalysis.addEventListener('click', function (e) {
            e.preventDefault();

            // Validate form before submission
            if (!form.checkValidity()) {
                form.classList.add('was-validated');
                return;
            }

            // Disable button and show spinner
            btnRunAnalysis.disabled = true;
            btnRunAnalysis.classList.add('disabled');
            loadingSpinner.classList.remove('d-none');
            resultArea.classList.add('d-none');
            resultArea.innerHTML = '';

            // Prepare form data
            var formData = new FormData(form);

            // Get CSRF token
            var token = form.querySelector('[name="__RequestVerificationToken"]');
            var headers = {
                'Content-Type': 'application/json'
            };
            if (token) {
                headers['RequestVerificationToken'] = token.value;
            }

            // Send POST request
            fetch('/Staff/RunAnalysis', {
                method: 'POST',
                headers: headers,
                body: JSON.stringify({
                    SelectedTagId: formData.get('SelectedTagId'),
                    SelectedCategoryId: formData.get('SelectedCategoryId')
                })
            })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(function (result) {
                // Hide spinner and re-enable button
                loadingSpinner.classList.add('d-none');
                btnRunAnalysis.disabled = false;
                btnRunAnalysis.classList.remove('disabled');

                if (result.success) {
                    // Success case - green alert with link to new report
                    resultArea.classList.remove('d-none');
                    resultArea.innerHTML = 
                        '<div class="alert alert-success alert-dismissible fade show" role="alert">' +
                            '<strong><i class="bi bi-check-circle-fill me-2"></i>Analysis report generated successfully!</strong>' +
                            '<p class="mb-0 mt-2">Click the link below to view your new report.</p>' +
                            '<a href="/News/Detail/' + result.newsArticleId + '" class="btn btn-success mt-3">' +
                                '<i class="bi bi-file-text me-1"></i>View Report <i class="bi bi-arrow-right ms-1"></i>' +
                            '</a>' +
                            '<button type="button" class="btn-close position-absolute top-0 end-0" data-bs-dismiss="alert" aria-label="Close"></button>' +
                        '</div>';
                } else {
                    // Error case - red alert with error message
                    resultArea.classList.remove('d-none');
                    var errorMsg = result.errorMessage || 'An unexpected error occurred.';
                    resultArea.innerHTML = 
                        '<div class="alert alert-danger alert-dismissible fade show" role="alert">' +
                            '<strong><i class="bi bi-exclamation-triangle-fill me-2"></i>Analysis Failed</strong>' +
                            '<p class="mb-0 mt-2">' + escapeHtml(errorMsg) + '</p>' +
                            '<button type="button" class="btn-close position-absolute top-0 end-0" data-bs-dismiss="alert" aria-label="Close"></button>' +
                        '</div>';
                }
            })
            .catch(function (error) {
                // Network error - hide spinner and re-enable button
                loadingSpinner.classList.add('d-none');
                btnRunAnalysis.disabled = false;
                btnRunAnalysis.classList.remove('disabled');

                resultArea.classList.remove('d-none');
                resultArea.innerHTML = 
                    '<div class="alert alert-danger alert-dismissible fade show" role="alert">' +
                        '<strong><i class="bi bi-wifi-off me-2"></i>Network Error</strong>' +
                        '<p class="mb-0 mt-2">Unexpected network error. Please try again.</p>' +
                        '<button type="button" class="btn-close position-absolute top-0 end-0" data-bs-dismiss="alert" aria-label="Close"></button>' +
                    '</div>';
            });
        });

        // Helper function to escape HTML
        function escapeHtml(text) {
            if (!text) return '';
            var el = document.createElement('span');
            el.textContent = String(text);
            return el.innerHTML;
        }
    });
})();
