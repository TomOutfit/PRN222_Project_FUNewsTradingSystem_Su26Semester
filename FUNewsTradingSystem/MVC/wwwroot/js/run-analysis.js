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
        var progressArea = document.getElementById('progressArea');
        var progressBar = document.getElementById('progressBar');
        var progressPercent = document.getElementById('progressPercent');
        var progressLogs = document.getElementById('progressLogs');
        var form = document.getElementById('runAnalysisForm');

        if (!btnRunAnalysis || !loadingSpinner || !resultArea || !form) {
            return;
        }

        // Initialize SignalR Connection
        var connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/analysisProgress")
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        var connectionId = null;

        connection.on("ReceiveProgress", function (message, progress) {
            if (progressArea && progressBar && progressPercent && progressLogs) {
                progressArea.classList.remove('d-none');
                progressBar.style.width = progress + '%';
                progressBar.setAttribute('aria-valuenow', progress);
                progressPercent.textContent = progress + '%';

                var logLine = document.createElement('div');
                logLine.innerHTML = '<span class="text-white-50">[' + new Date().toLocaleTimeString() + ']</span> ' + escapeHtml(message);
                progressLogs.appendChild(logLine);
                progressLogs.scrollTop = progressLogs.scrollHeight;
            }
        });

        connection.start().then(function () {
            connectionId = connection.connectionId;
        }).catch(function (err) {
            console.error("SignalR failed to start: ", err);
        });

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

            // Reset and Show Progress bar
            if (progressArea && progressBar && progressPercent && progressLogs) {
                progressArea.classList.remove('d-none');
                progressBar.style.width = '0%';
                progressBar.setAttribute('aria-valuenow', 0);
                progressPercent.textContent = '0%';
                progressLogs.innerHTML = '<div class="text-info">// Triggering analysis execution pipeline...</div>';
            }

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
                    SelectedTagId: parseInt(formData.get('SelectedTagId')),
                    SelectedCategoryId: parseInt(formData.get('SelectedCategoryId')),
                    ConnectionId: connectionId
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
                        '<div class="alert alert-success alert-dismissible fade show p-4 shadow-sm" role="alert" style="border-left: 4px solid #10b981; border-radius: var(--radius-md);">' +
                            '<h6 class="fw-bold text-success mb-2"><i class="bi bi-check-circle-fill me-2"></i>Analysis Completed Successfully!</strong>' +
                            '<p class="mb-0 small text-secondary">The multi-agent system has generated the sentiment & fundamental recommendation report. You can view it now.</p>' +
                            '<a href="/Report/Detail/' + result.newsArticleId + '" class="btn btn-success btn-sm mt-3 px-4 rounded-pill">' +
                                '<i class="bi bi-file-text me-1"></i>Open Synthesized Report <i class="bi bi-arrow-right ms-1"></i>' +
                            '</a>' +
                            '<button type="button" class="btn-close position-absolute top-0 end-0" data-bs-dismiss="alert" aria-label="Close"></button>' +
                        '</div>';
                } else {
                    // Error case - red alert with error message
                    resultArea.classList.remove('d-none');
                    var errorMsg = result.errorMessage || 'An unexpected error occurred.';
                    resultArea.innerHTML = 
                        '<div class="alert alert-danger alert-dismissible fade show p-4 shadow-sm" role="alert" style="border-left: 4px solid #ef4444; border-radius: var(--radius-md);">' +
                            '<h6 class="fw-bold text-danger mb-2"><i class="bi bi-exclamation-triangle-fill me-2"></i>Analysis Pipeline Failed</strong>' +
                            '<p class="mb-0 small text-secondary">' + escapeHtml(errorMsg) + '</p>' +
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
                    '<div class="alert alert-danger alert-dismissible fade show p-4 shadow-sm" role="alert" style="border-left: 4px solid #ef4444; border-radius: var(--radius-md);">' +
                        '<h6 class="fw-bold text-danger mb-2"><i class="bi bi-wifi-off me-2"></i>Network Communication Error</strong>' +
                        '<p class="mb-0 small text-secondary">Unexpected error communicating with the agent server. Please check connection and try again.</p>' +
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
