/**
 * run-analysis.js — AI Trading Pipeline UI Handler (Premium Animations Hub Edition)
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
                
                // Cập nhật thanh tiến trình mượt mà thông qua Spring curve định nghĩa trong site.css
                progressBar.style.width = progress + '%';
                progressBar.setAttribute('aria-valuenow', progress);
                progressPercent.textContent = progress + '%';

                // Hiệu ứng dịch chuyển nhẹ của logs khi nạp dữ liệu mới
                var logLine = document.createElement('div');
                logLine.className = 'animate-fade-in-up';
                logLine.style.animationDuration = '0.3s';
                logLine.innerHTML = '<span class="text-white-50">[' + new Date().toLocaleTimeString() + ']</span> ' + escapeHtml(message);
                
                progressLogs.appendChild(logLine);
                
                // Cuộn mượt mà (smooth scroll) thay vì nhảy dòng lập tức
                progressLogs.scrollTo({
                    top: progressLogs.scrollHeight,
                    behavior: 'smooth'
                });
            }
        });

        connection.start().then(function () {
            connectionId = connection.connectionId;
        }).catch(function (err) {
            console.error("SignalR failed to start: ", err);
        });

        btnRunAnalysis.addEventListener('click', function (e) {
            e.preventDefault();

            if (!form.checkValidity()) {
                form.classList.add('was-validated');
                // Rung lắc form khi nhập liệu sai điều kiện
                form.style.animation = 'none';
                form.offsetHeight; /* Trigger reflow */
                form.style.animation = 'shakeError 0.4s ease';
                return;
            }

            // Thu nhỏ nút phân tích (micro-interaction)
            btnRunAnalysis.disabled = true;
            btnRunAnalysis.classList.add('disabled');
            btnRunAnalysis.style.transform = 'scale(0.96)';

            loadingSpinner.classList.remove('d-none');
            loadingSpinner.className = 'text-center mt-4 animate-fade-in-up';
            
            resultArea.classList.add('d-none');
            resultArea.innerHTML = '';

            // Reset và nạp hoạt ảnh Progress bar
            if (progressArea && progressBar && progressPercent && progressLogs) {
                progressArea.classList.remove('d-none');
                progressArea.style.opacity = '0';
                progressArea.style.transform = 'translateY(15px)';
                
                progressBar.style.width = '0%';
                progressBar.setAttribute('aria-valuenow', 0);
                progressPercent.textContent = '0%';
                progressLogs.innerHTML = '<div class="text-info animate-fade-in-up">// Triggering analysis execution pipeline...</div>';

                requestAnimationFrame(function() {
                    progressArea.style.transition = 'all 0.4s cubic-bezier(0.16, 1, 0.3, 1)';
                    progressArea.style.opacity = '1';
                    progressArea.style.transform = 'translateY(0)';
                });
            }

            var formData = new FormData(form);
            var token = form.querySelector('[name="__RequestVerificationToken"]');
            var headers = { 'Content-Type': 'application/json' }; 
            
            if (token) {
                headers['RequestVerificationToken'] = token.value;
            }

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
                loadingSpinner.classList.add('d-none');
                btnRunAnalysis.disabled = false;
                btnRunAnalysis.classList.remove('disabled');
                btnRunAnalysis.style.transform = '';

                resultArea.classList.remove('d-none');
                
                if (result.success) {
                    resultArea.innerHTML =  
                        '<div class="alert alert-success alert-dismissible fade show p-4 shadow-sm animate-fade-in-up glow-on-appear" role="alert" style="border-left: 4px solid #10b981; border-radius: var(--radius-md); animation-duration: 0.5s;">' +
                            '<h6 class="fw-bold text-success mb-2"><i class="bi bi-check-circle-fill me-2"></i>Analysis Completed Successfully!</h6>' +
                            '<p class="mb-0 small text-secondary">The multi-agent system has generated the sentiment & fundamental recommendation report. You can view it now.</p>' +
                            '<a href="/Report/Detail/' + result.newsArticleId + '" class="btn btn-success btn-sm mt-3 px-4 rounded-pill btn-ripple">' +
                                '<i class="bi bi-file-text me-1"></i>Open Synthesized Report <i class="bi bi-arrow-right ms-1"></i>' +
                            '</a>' +
                            '<button type="button" class="btn-close position-absolute top-0 end-0" data-bs-dismiss="alert" aria-label="Close"></button>' +
                        '</div>';
                } else {
                    var errorMsg = result.errorMessage || 'An unexpected error occurred.';
                    resultArea.innerHTML =  
                        '<div class="alert alert-danger alert-dismissible fade show p-4 shadow-sm animate-fade-in-up" role="alert" style="border-left: 4px solid #ef4444; border-radius: var(--radius-md); animation-duration: 0.5s;">' +
                            '<h6 class="fw-bold text-danger mb-2"><i class="bi bi-exclamation-triangle-fill me-2"></i>Analysis Pipeline Failed</h6>' +
                            '<p class="mb-0 small text-secondary">' + escapeHtml(errorMsg) + '</p>' +
                            '<button type="button" class="btn-close position-absolute top-0 end-0" data-bs-dismiss="alert" aria-label="Close"></button>' +
                        '</div>';
                    shakeElement(resultArea);
                }
            })
            .catch(function (error) {
                loadingSpinner.classList.add('d-none');
                btnRunAnalysis.disabled = false;
                btnRunAnalysis.classList.remove('disabled');
                btnRunAnalysis.style.transform = '';

                resultArea.classList.remove('d-none');
                resultArea.innerHTML =  
                    '<div class="alert alert-danger alert-dismissible fade show p-4 shadow-sm animate-fade-in-up" role="alert" style="border-left: 4px solid #ef4444; border-radius: var(--radius-md); animation-duration: 0.5s;">' +
                        '<h6 class="fw-bold text-danger mb-2"><i class="bi bi-wifi-off me-2"></i>Network Communication Error</h6>' +
                        '<p class="mb-0 small text-secondary">Unexpected error communicating with the agent server. Please check connection and try again.</p>' +
                        '<button type="button" class="btn-close position-absolute top-0 end-0" data-bs-dismiss="alert" aria-label="Close"></button>' +
                    '</div>';
                shakeElement(resultArea);
            });
        });

        function escapeHtml(text) {
            if (!text) return '';
            var el = document.createElement('span');
            el.textContent = String(text);
            return el.innerHTML;
        }
    });
})();