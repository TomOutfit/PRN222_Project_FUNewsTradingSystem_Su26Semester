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
                    SelectedTagId:      parseInt(formData.get('SelectedTagId')),
                    SelectedCategoryId: parseInt(formData.get('SelectedCategoryId')),
                    SelectedPipeline:   formData.get('SelectedPipeline') || 'classic',
                    ConnectionId:       connectionId
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
                    if (result.pipelineType === 'tradingagents' && result.richData) {
                        resultArea.innerHTML = buildTradingAgentsCard(result.richData, result.newsArticleId);
                    } else {
                        resultArea.innerHTML = buildClassicCard(result.newsArticleId);
                    }
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

        function buildClassicCard(articleId) {
            return (
                '<div class="alert alert-success alert-dismissible fade show p-4 shadow-sm animate-fade-in-up glow-on-appear" role="alert"' +
                '     style="border-left:4px solid #10b981;border-radius:var(--radius-md);animation-duration:.5s;">' +
                    '<h6 class="fw-bold text-success mb-2"><i class="bi bi-check-circle-fill me-2"></i>Analysis Completed Successfully!</h6>' +
                    '<p class="mb-0 small text-secondary">The multi-agent system has generated the sentiment & fundamental recommendation report. You can view it now.</p>' +
                    '<a href="/Report/Detail/' + articleId + '" class="btn btn-success btn-sm mt-3 px-4 rounded-pill btn-ripple">' +
                        '<i class="bi bi-file-text me-1"></i>Open Synthesized Report <i class="bi bi-arrow-right ms-1"></i>' +
                    '</a>' +
                    '<button type="button" class="btn-close position-absolute top-0 end-0" data-bs-dismiss="alert" aria-label="Close"></button>' +
                '</div>'
            );
        }

        function buildTradingAgentsCard(rich, articleId) {
            var d = rich.decision || 'HOLD';
            var badgeColor = d === 'BUY' ? '#10b981' : d === 'SELL' ? '#ef4444' : '#f59e0b';
            var pct = Math.min(100, Math.max(0, rich.confidenceScore || 70));
            var barColor = d === 'BUY' ? '#10b981' : d === 'SELL' ? '#ef4444' : '#f59e0b';

            var sections = [
                { icon: 'bi-graph-up-arrow',  label: 'Sentiment Analysis',        body: rich.sentimentReport },
                { icon: 'bi-bar-chart-fill',  label: 'Fundamental Analysis',      body: rich.fundamentalsReport },
                { icon: 'bi-newspaper',       label: 'News & Macro Report',       body: rich.newsReport },
                { icon: 'bi-activity',        label: 'Technical Market Analysis', body: rich.marketReport },
                { icon: 'bi-lightbulb-fill',  label: 'Trader Investment Plan',    body: rich.traderInvestmentPlan },
                { icon: 'bi-check2-circle',   label: 'Final Trade Decision',      body: rich.finalTradeDecision },
            ].filter(function(s) { return s.body && s.body.trim(); });

            var accordionId = 'taAccordion';
            var accordionItems = sections.map(function(s, i) {
                var collapseId = 'taCollapse' + i;
                var isFirst = i === 0;
                return (
                    '<div class="accordion-item" style="background:var(--surface-card);border:1px solid var(--border);margin-bottom:.5rem;border-radius:10px;overflow:hidden;">' +
                        '<h2 class="accordion-header">' +
                            '<button class="accordion-button ' + (isFirst ? '' : 'collapsed') + '" type="button"' +
                            '        data-bs-toggle="collapse" data-bs-target="#' + collapseId + '"' +
                            '        style="background:var(--surface-card);color:var(--text-main);font-weight:600;font-size:.875rem;">' +
                                '<i class="bi ' + s.icon + ' me-2" style="color:var(--accent)"></i>' + escapeHtml(s.label) +
                            '</button>' +
                        '</h2>' +
                        '<div id="' + collapseId + '" class="accordion-collapse collapse ' + (isFirst ? 'show' : '') + '">' +
                            '<div class="accordion-body p-3">' +
                                '<pre style="white-space:pre-wrap;font-family:inherit;font-size:.8375rem;line-height:1.7;color:var(--text-secondary);margin:0;">' +
                                    escapeHtml(s.body) +
                                '</pre>' +
                            '</div>' +
                        '</div>' +
                    '</div>'
                );
            }).join('');

            return (
                '<div class="fintech-card p-4 animate-fade-in-up" style="border-radius:var(--radius-lg);animation-duration:.5s;">' +
                    '<div class="d-flex align-items-center gap-3 flex-wrap mb-4">' +
                        '<span class="badge fs-6 px-3 py-2 fw-extrabold" style="background:' + badgeColor + ';color:#fff;border-radius:8px;letter-spacing:.04em;">' + escapeHtml(d) + '</span>' +
                        '<div>' +
                            '<div class="fw-bold" style="font-size:1rem;">' + escapeHtml(rich.ticker || '') + ' — TradingAgents Analysis</div>' +
                            '<div class="text-muted" style="font-size:.78rem;"><i class="bi bi-cpu me-1"></i>Multi-Agents LLM Financial Trading Framework</div>' +
                        '</div>' +
                        '<div class="ms-auto text-end">' +
                            '<div class="text-muted" style="font-size:.72rem;">Confidence</div>' +
                            '<div class="progress mt-1" style="width:90px;height:7px;border-radius:4px;background:var(--border);">' +
                                '<div class="progress-bar" style="width:' + pct + '%;background:' + barColor + ';border-radius:4px;"></div>' +
                            '</div>' +
                            '<div class="fw-bold mt-1" style="font-size:.78rem;color:' + barColor + ';">' + pct + '%</div>' +
                        '</div>' +
                    '</div>' +

                    '<div class="accordion accordion-flush" id="' + accordionId + '">' +
                        accordionItems +
                    '</div>' +

                    '<div class="mt-4 pt-3" style="border-top:1px solid var(--border);">' +
                        '<a href="/Report/Detail/' + articleId + '" class="btn fintech-btn-primary px-4">' +
                            '<i class="bi bi-file-earmark-text me-2"></i>Open Full Report <i class="bi bi-arrow-right ms-1"></i>' +
                        '</a>' +
                        '<span class="ms-3 text-muted" style="font-size:.78rem;"><i class="bi bi-check-circle-fill text-success me-1"></i>Saved to reports</span>' +
                    '</div>' +
                '</div>'
            );
        }
    });
})();