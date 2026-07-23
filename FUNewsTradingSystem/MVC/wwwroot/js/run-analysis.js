/**
 * run-analysis.js — AI Trading Pipeline UI Handler
 */
(function () {
    'use strict';

    // Distinct color per team for high-contrast terminal UI
    var TEAM_COLORS = {
        'Analyst Team':         '#38bdf8',  // sky blue
        'Research Team':        '#c084fc',  // purple
        'Trading Team':         '#fbbf24',  // amber
        'Risk Management':      '#fb923c',  // orange
        'Portfolio Management': '#34d399',  // emerald
    };

    // ── Agent table (matches TradingAgents actual teams) ──────────────────────
    var TA_AGENTS = [
        { id: 'market',  team: 'Analyst Team',       agent: 'Market Analyst'       },
        { id: 'social',  team: 'Analyst Team',        agent: 'Social Analyst'       },
        { id: 'news',    team: 'Analyst Team',         agent: 'News Analyst'         },
        { id: 'fund',    team: 'Analyst Team',         agent: 'Fundamentals Analyst' },
        { id: 'bull',    team: 'Research Team',        agent: 'Bull Researcher'      },
        { id: 'bear',    team: 'Research Team',        agent: 'Bear Researcher'      },
        { id: 'rmgr',    team: 'Research Team',        agent: 'Research Manager'     },
        { id: 'trader',  team: 'Trading Team',         agent: 'Trader'               },
        { id: 'risky',   team: 'Risk Management',      agent: 'Risky Analyst'        },
        { id: 'neutral', team: 'Risk Management',      agent: 'Neutral Analyst'      },
        { id: 'safe',    team: 'Risk Management',      agent: 'Safe Analyst'         },
        { id: 'pm',      team: 'Portfolio Management', agent: 'Portfolio Manager'    },
    ];

    // Map progress % → currently active agent ID (null = not started yet)
    function activeAgentId(pct) {
        if (pct >= 90) return 'pm';
        if (pct >= 88) return 'safe';
        if (pct >= 84) return 'neutral';
        if (pct >= 80) return 'risky';
        if (pct >= 76) return 'trader';
        if (pct >= 70) return 'rmgr';
        if (pct >= 65) return 'bear';
        if (pct >= 60) return 'bull';
        if (pct >= 54) return 'fund';
        if (pct >= 46) return 'news';
        if (pct >= 38) return 'social';
        if (pct >= 30) return 'market';
        return null;
    }

    // Simulated Messages & Tools entries per agent stage
    var TA_STAGE_MSGS = {
        market: [
            { type: 'Tool',      msg: function (t, d) { return "get_stock_data({'ticker': '" + t + "', 'curr_date': '" + d + "'})"; } },
            { type: 'Tool',      msg: function (t)    { return "get_indicators({'ticker': '" + t + "'})"; } },
            { type: 'Reasoning', msg: function ()     { return 'Analyzing OHLCV data and technical indicators...'; } },
        ],
        social: [
            { type: 'Tool',      msg: function (t, d) { return "get_stocktwits_data({'ticker': '" + t + "', 'curr_date': '" + d + "'})"; } },
            { type: 'Reasoning', msg: function ()     { return 'Processing social media sentiment from StockTwits...'; } },
        ],
        news: [
            { type: 'Tool',      msg: function (t, d) { return "get_stock_news_openai({'ticker': '" + t + "', 'curr_date': '" + d + "'})"; } },
            { type: 'Tool',      msg: function (_t, d) { return "get_global_news_openai({'curr_date': '" + d + "'})"; } },
            { type: 'Tool',      msg: function (t, d) { return "get_google_news({'query': '" + t + "', 'curr_date': '" + d + "'})"; } },
            { type: 'Reasoning', msg: function ()     { return 'Reviewing recent news articles and macroeconomic data...'; } },
        ],
        fund: [
            { type: 'Tool',      msg: function (t)    { return "get_fundamentals({'ticker': '" + t + "'})"; } },
            { type: 'Tool',      msg: function (t)    { return "get_balance_sheet({'ticker': '" + t + "'})"; } },
            { type: 'Reasoning', msg: function ()     { return 'Evaluating earnings, balance sheet, and competitive positioning...'; } },
        ],
        bull:    [{ type: 'Reasoning', msg: function () { return 'Bull researcher constructing long investment thesis...'; } }],
        bear:    [{ type: 'Reasoning', msg: function () { return 'Bear researcher identifying downside risks and short arguments...'; } }],
        rmgr:    [{ type: 'Reasoning', msg: function () { return 'Research manager synthesizing bull/bear debate conclusions...'; } }],
        trader:  [{ type: 'Reasoning', msg: function () { return 'Trader composing investment plan with entry and exit criteria...'; } }],
        risky:   [{ type: 'Reasoning', msg: function () { return 'Risky analyst evaluating high-upside aggressive scenario...'; } }],
        neutral: [{ type: 'Reasoning', msg: function () { return 'Neutral analyst balancing risk-reward at current price levels...'; } }],
        safe:    [{ type: 'Reasoning', msg: function () { return 'Safe analyst stress-testing downside and position sizing...'; } }],
        pm:      [{ type: 'Report',    msg: function () { return 'Portfolio manager finalizing trade recommendation and confidence score...'; } }],
    };

    document.addEventListener('DOMContentLoaded', function () {
        var btnRunAnalysis  = document.getElementById('btnRunAnalysis');
        var loadingSpinner  = document.getElementById('loadingSpinner');
        var resultArea      = document.getElementById('resultArea');
        var progressArea    = document.getElementById('progressArea');
        var progressBar     = document.getElementById('progressBar');
        var progressPercent = document.getElementById('progressPercent');
        var progressLogs    = document.getElementById('progressLogs');
        var form            = document.getElementById('runAnalysisForm');
        var taPanel         = document.getElementById('taProgressPanel');
        var taBar           = document.getElementById('taProgressBar');
        var taPct           = document.getElementById('taProgressPct');
        var taMsg           = document.getElementById('taProgressMsg');
        var taAgentTbody    = document.getElementById('taAgentTbody');
        var taMsgTbody      = document.getElementById('taMsgTbody');
        var taMsgScroll     = document.getElementById('taMsgScroll');
        var taCurrentReport = document.getElementById('taCurrentReport');
        var taReportContent = document.getElementById('taReportContent');

        if (!btnRunAnalysis || !form) return;

        var currentPipeline = 'classic';
        var currentTicker   = '';
        var currentDate     = '';
        var lastActiveAgent = null;

        // ── Build agent table rows on page load ────────────────────────────────
        if (taAgentTbody) {
            TA_AGENTS.forEach(function (a) {
                var tc = TEAM_COLORS[a.team] || '#7ec8e3';
                var tr = document.createElement('tr');
                tr.id = 'taRow-' + a.id;
                tr.innerHTML =
                    '<td style="padding:2px 6px;color:' + tc + ';font-weight:600">' + escapeHtml(a.team)  + '</td>' +
                    '<td style="padding:2px 6px;color:#dde6f0">' + escapeHtml(a.agent) + '</td>' +
                    '<td id="taStatus-' + a.id + '" style="padding:2px 6px;color:#3d5878">·· pending</td>';
                taAgentTbody.appendChild(tr);
            });
        }

        // ── Agent table updater ────────────────────────────────────────────────
        function updateAgentTable(pct) {
            if (!taAgentTbody) return;
            var active = activeAgentId(pct);

            // Bug fix: when active is null (pct < 30) all agents stay pending
            if (active === null) {
                TA_AGENTS.forEach(function (a) {
                    var cell = document.getElementById('taStatus-' + a.id);
                    if (cell) { cell.style.color = '#3d5878'; cell.textContent = '·· pending'; }
                });
                return;
            }

            // Emit Messages & Tools rows when a new agent becomes active
            if (active !== lastActiveAgent) {
                lastActiveAgent = active;
                var msgs = TA_STAGE_MSGS[active];
                if (msgs) {
                    msgs.forEach(function (m) {
                        addMsgRow(m.type, m.msg(currentTicker, currentDate));
                    });
                }
            }

            var passedActive = false;
            TA_AGENTS.forEach(function (a) {
                var cell = document.getElementById('taStatus-' + a.id);
                if (!cell) return;
                if (a.id === active) {
                    passedActive = true;
                    cell.innerHTML = '<span style="color:#00ff64;animation:ta-blink 1s infinite">↓ in_progress</span>';
                } else if (!passedActive) {
                    cell.style.color = '#00cc44';
                    cell.textContent = 'completed';
                } else {
                    cell.style.color = '#3d5878';
                    cell.textContent = '·· pending';
                }
            });
        }

        // ── Messages & Tools table ─────────────────────────────────────────────
        var TYPE_COLORS = { Tool: '#22d3ee', Reasoning: '#a78bfa', Report: '#fbbf24' };

        function addMsgRow(type, content) {
            if (!taMsgTbody) return;
            // Remove placeholder row on first real message
            var placeholder = taMsgTbody.querySelector('.ta-placeholder');
            if (placeholder) placeholder.remove();
            var now   = new Date().toLocaleTimeString('en-US', { hour12: false });
            var color = TYPE_COLORS[type] || '#94a3b8';
            var sep   = 'border-bottom:1px solid rgba(96,165,250,.07);';
            var tr = document.createElement('tr');
            tr.innerHTML =
                '<td style="padding:3px 8px;color:#7a9ab8;white-space:nowrap;vertical-align:top;' + sep + '">' + now + '</td>' +
                '<td style="padding:3px 8px;white-space:nowrap;vertical-align:top;' + sep + '"><span style="color:' + color + ';font-weight:700;">' + type + '</span></td>' +
                '<td style="padding:3px 8px;color:#dde6f0;font-size:.68rem;word-break:break-all;' + sep + '">' + escapeHtml(content) + '</td>';
            taMsgTbody.appendChild(tr);
            if (taMsgScroll) taMsgScroll.scrollTop = taMsgScroll.scrollHeight;
        }

        function resetMsgTable() {
            if (taMsgTbody) taMsgTbody.innerHTML = '<tr class="ta-placeholder"><td colspan="3" style="padding:10px 8px;color:#5a7a90;font-style:italic;">// Waiting for agents...</td></tr>';
        }

        function resetAgentTable() {
            TA_AGENTS.forEach(function (a) {
                var cell = document.getElementById('taStatus-' + a.id);
                if (cell) { cell.style.color = '#3d5878'; cell.innerHTML = '·· pending'; }
            });
        }

        // ── SignalR ────────────────────────────────────────────────────────────
        var connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/analysisProgress')
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        var connectionId = null;

        connection.on('ReceiveProgress', function (message, progress) {
            // Generic progress bar (both pipelines)
            if (progressArea && progressBar && progressPercent && progressLogs) {
                progressArea.classList.remove('d-none');
                progressBar.style.width = progress + '%';
                progressBar.setAttribute('aria-valuenow', progress);
                progressPercent.textContent = progress + '%';
                var logLine = document.createElement('div');
                logLine.className = 'animate-fade-in-up';
                logLine.style.animationDuration = '0.3s';
                logLine.innerHTML = '<span class="text-white-50">[' + new Date().toLocaleTimeString() + ']</span> ' + escapeHtml(message);
                progressLogs.appendChild(logLine);
                progressLogs.scrollTo({ top: progressLogs.scrollHeight, behavior: 'smooth' });
            }

            // TradingAgents panel
            if (currentPipeline === 'tradingagents' && taPanel) {
                taPanel.classList.remove('d-none');
                if (taBar) taBar.style.width  = progress + '%';
                if (taPct) taPct.textContent  = progress + '%';
                if (taMsg) taMsg.textContent  = message;
                updateAgentTable(progress);
            }
        });

        // ── Final result via SignalR (avoids Render 30s HTTP timeout) ──────────
        connection.on('ReceiveAnalysisResult', function (result) {
            loadingSpinner.classList.add('d-none');
            btnRunAnalysis.disabled = false;
            btnRunAnalysis.classList.remove('disabled');
            btnRunAnalysis.style.transform = '';

            if (currentPipeline === 'tradingagents' && taPanel) {
                var badge = taPanel.querySelector('.ta-running-badge');
                if (result.success) {
                    // Mark all agents completed
                    TA_AGENTS.forEach(function (a) {
                        var cell = document.getElementById('taStatus-' + a.id);
                        if (cell) { cell.style.color = '#00cc44'; cell.textContent = 'completed'; }
                    });
                    if (taBar) taBar.style.width  = '100%';
                    if (taPct) taPct.textContent  = '100%';
                    if (taMsg) taMsg.textContent  = 'Analysis complete!';
                    if (badge) { badge.textContent = 'DONE'; badge.style.background = '#003311'; }
                    // Populate Current Report section with final trade decision
                    if (taCurrentReport && taReportContent && result.richData && result.richData.finalTradeDecision) {
                        taReportContent.textContent = result.richData.finalTradeDecision;
                        taCurrentReport.classList.remove('d-none');
                    }
                    addMsgRow('Report', 'Pipeline complete — report saved to database.');
                } else {
                    if (badge) { badge.textContent = 'FAILED'; badge.style.background = '#3a0000'; badge.style.color = '#ff6464'; }
                    addMsgRow('Report', 'Pipeline failed: ' + (result.errorMessage || 'unknown error'));
                }
            }

            resultArea.classList.remove('d-none');
            if (result.success) {
                if (result.pipelineType === 'tradingagents' && result.richData) {
                    resultArea.innerHTML = buildTradingAgentsCard(result.richData, result.newsArticleId);
                } else {
                    resultArea.innerHTML = buildClassicCard(result.newsArticleId);
                }
            } else {
                var errorMsg    = result.errorMessage || 'An unexpected error occurred.';
                var lowerError  = errorMsg.toLowerCase();
                var isRateLimit = lowerError.indexOf('rate limit') >= 0 || lowerError.indexOf('429') >= 0 || lowerError.indexOf('resource_exhausted') >= 0;
                var isAuth      = lowerError.indexOf('authentication') >= 0 || lowerError.indexOf('api key') >= 0 || lowerError.indexOf('401') >= 0;
                var providerSel = (document.getElementById('SelectedProvider') || {}).value || 'openai';
                var rateLimitHint = '';
                if (isRateLimit) {
                    if (providerSel === 'openai') {
                        rateLimitHint = '<strong>Rate limit hit on OpenAI</strong> — switch to <strong>Groq</strong> (Llama, free tier) or <strong>Google Gemini</strong> and retry.';
                    } else if (providerSel === 'groq') {
                        rateLimitHint = '<strong>Rate limit hit on Groq</strong> — switch to <strong>OpenAI</strong> or <strong>Google Gemini</strong> and retry.';
                    } else {
                        rateLimitHint = '<strong>Rate limit / quota exhausted on Google Gemini</strong> — your free-tier daily quota is used up. Switch to <strong>Groq</strong> (Llama, free tier) or <strong>OpenAI</strong> and retry, or wait until quota resets tomorrow.';
                    }
                }
                var extraHint = isRateLimit
                    ? '<p class="mb-0 small text-info mt-2"><i class="bi bi-lightning-charge me-1"></i>' + rateLimitHint + '</p>'
                    : isAuth
                    ? '<p class="mb-0 small text-warning mt-2"><i class="bi bi-key me-1"></i>' +
                      'Check that the <strong>' + providerSel.toUpperCase() + '</strong> API key is set correctly in the Render dashboard env vars.</p>'
                    : '';
                resultArea.innerHTML =
                    '<div class="alert alert-danger alert-dismissible fade show p-4 shadow-sm animate-fade-in-up" role="alert"' +
                    '     style="border-left:4px solid #ef4444;border-radius:var(--radius-md);animation-duration:.5s;">' +
                        '<h6 class="fw-bold text-danger mb-2"><i class="bi bi-exclamation-triangle-fill me-2"></i>Analysis Pipeline Failed</h6>' +
                        '<p class="mb-0 small text-secondary">' + escapeHtml(errorMsg) + '</p>' +
                        extraHint +
                        '<button type="button" class="btn-close position-absolute top-0 end-0" data-bs-dismiss="alert" aria-label="Close"></button>' +
                    '</div>';
                shakeElement(resultArea);
            }
        });

        connection.start().then(function () {
            connectionId = connection.connectionId;
        }).catch(function (err) {
            console.error('SignalR failed to start: ', err);
        });

        // ── Run button ─────────────────────────────────────────────────────────
        btnRunAnalysis.addEventListener('click', function (e) {
            e.preventDefault();
            if (!form.checkValidity()) {
                form.classList.add('was-validated');
                form.style.animation = 'none';
                form.offsetHeight;
                form.style.animation = 'shakeError 0.4s ease';
                return;
            }

            var formData = new FormData(form);
            currentPipeline = formData.get('SelectedPipeline') || 'classic';
            lastActiveAgent = null;

            // Capture ticker text + yesterday's date for Messages & Tools display
            var tagSelect   = document.getElementById('SelectedTagId');
            var selectedOpt = tagSelect && tagSelect.options[tagSelect.selectedIndex];
            currentTicker   = selectedOpt ? (selectedOpt.text || '').split(' ')[0].trim() : 'TICKER';
            var yesterday   = new Date();
            yesterday.setDate(yesterday.getDate() - 1);
            currentDate = yesterday.toISOString().slice(0, 10);

            btnRunAnalysis.disabled = true;
            btnRunAnalysis.classList.add('disabled');
            btnRunAnalysis.style.transform = 'scale(0.96)';
            loadingSpinner.classList.remove('d-none');
            loadingSpinner.className = 'text-center mt-4 animate-fade-in-up';
            resultArea.classList.add('d-none');
            resultArea.innerHTML = '';

            // Reset generic progress bar
            if (progressArea && progressBar && progressPercent && progressLogs) {
                progressArea.classList.remove('d-none');
                progressArea.style.opacity = '0';
                progressArea.style.transform = 'translateY(15px)';
                progressBar.style.width = '0%';
                progressBar.setAttribute('aria-valuenow', 0);
                progressPercent.textContent = '0%';
                progressLogs.innerHTML = '<div class="text-info animate-fade-in-up">// Triggering analysis execution pipeline...</div>';
                requestAnimationFrame(function () {
                    progressArea.style.transition = 'all 0.4s cubic-bezier(0.16,1,0.3,1)';
                    progressArea.style.opacity = '1';
                    progressArea.style.transform = 'translateY(0)';
                });
            }

            // Reset TradingAgents panel
            if (taPanel) {
                taPanel.classList.add('d-none');
                if (taBar) taBar.style.width = '0%';
                if (taPct) taPct.textContent = '0%';
                if (taMsg) taMsg.textContent  = 'Starting...';
                if (taCurrentReport) taCurrentReport.classList.add('d-none');
                if (taReportContent) taReportContent.textContent = '';
                var badge = taPanel.querySelector('.ta-running-badge');
                if (badge) { badge.textContent = 'RUNNING'; badge.style.background = '#00441b'; badge.style.color = '#00ff64'; }
                resetAgentTable();
                resetMsgTable();
            }

            var token = form.querySelector('[name="__RequestVerificationToken"]');
            var headers = { 'Content-Type': 'application/json' };
            if (token) headers['RequestVerificationToken'] = token.value;

            fetch('/Staff/RunAnalysis', {
                method: 'POST',
                headers: headers,
                body: JSON.stringify({
                    SelectedTagId:      parseInt(formData.get('SelectedTagId')),
                    SelectedCategoryId: parseInt(formData.get('SelectedCategoryId')),
                    SelectedPipeline:   currentPipeline,
                    SelectedDepth:      formData.get('SelectedDepth')    || 'fast',
                    SelectedProvider:   formData.get('SelectedProvider') || 'openai',
                    ConnectionId:       connectionId
                })
            })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data.started) return; // waiting for ReceiveAnalysisResult via SignalR
                // Synchronous validation error
                loadingSpinner.classList.add('d-none');
                btnRunAnalysis.disabled = false;
                btnRunAnalysis.classList.remove('disabled');
                btnRunAnalysis.style.transform = '';
                resultArea.classList.remove('d-none');
                resultArea.innerHTML =
                    '<div class="alert alert-danger alert-dismissible fade show p-4 shadow-sm" role="alert"' +
                    '     style="border-left:4px solid #ef4444;border-radius:var(--radius-md);">' +
                        '<h6 class="fw-bold text-danger mb-2"><i class="bi bi-exclamation-triangle-fill me-2"></i>Validation Error</h6>' +
                        '<p class="mb-0 small">' + escapeHtml(data.errorMessage || 'Invalid request.') + '</p>' +
                        '<button type="button" class="btn-close position-absolute top-0 end-0" data-bs-dismiss="alert"></button>' +
                    '</div>';
            })
            .catch(function () {
                loadingSpinner.classList.add('d-none');
                btnRunAnalysis.disabled = false;
                btnRunAnalysis.classList.remove('disabled');
                btnRunAnalysis.style.transform = '';
                resultArea.classList.remove('d-none');
                resultArea.innerHTML =
                    '<div class="alert alert-danger p-4" style="border-left:4px solid #ef4444;border-radius:var(--radius-md);">' +
                        '<h6 class="fw-bold text-danger mb-2"><i class="bi bi-wifi-off me-2"></i>Network Error</h6>' +
                        '<p class="mb-0 small">Could not reach the server. Please check your connection.</p>' +
                    '</div>';
            });
        });

        // ── Helpers ────────────────────────────────────────────────────────────
        function escapeHtml(text) {
            if (!text) return '';
            var el = document.createElement('span');
            el.textContent = String(text);
            return el.innerHTML;
        }

        function renderMd(text) {
            if (!text) return '';
            // Pre-process: normalize inline tables
            // 1. Split "text: | Col | Col |" → "text:\n| Col | Col |"
            text = text.replace(/^([^|\n]+?)(\|(?:[^|\n]+\|)+)\s*$/mg, '$1\n$2');
            // 2. Split "| row1 | | row2 |" → "| row1 |\n| row2 |"
            text = text.replace(/(\|)[ \t]+(\|)/g, '$1\n$2');
            var lines    = text.split('\n');
            var html     = [];
            var inList   = false;
            var inTable  = false;
            var tableRows = [];

            function applyInline(s) {
                return s
                    .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
                    .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
                    .replace(/\*([^\n*]+?)\*/g, '<em>$1</em>');
            }
            function closeList() {
                if (inList) { html.push('</ul>'); inList = false; }
            }
            function flushTable() {
                if (!inTable || !tableRows.length) { inTable = false; return; }
                var hdr  = tableRows.shift();
                var cols = hdr.split('|').map(function(c){ return c.trim(); }).filter(Boolean);
                var thead = '<thead class="table-dark"><tr>' +
                    cols.map(function(c){ return '<th>' + applyInline(c) + '</th>'; }).join('') + '</tr></thead>';
                var tbody = '<tbody>' + tableRows.map(function(r) {
                    var cells = r.split('|').map(function(c){ return c.trim(); }).filter(Boolean);
                    return '<tr>' + cells.map(function(c){ return '<td>' + applyInline(c) + '</td>'; }).join('') + '</tr>';
                }).join('') + '</tbody>';
                html.push('<div class="table-responsive my-2"><table class="table table-sm table-bordered mb-0">' + thead + tbody + '</table></div>');
                inTable  = false;
                tableRows = [];
            }

            for (var i = 0; i < lines.length; i++) {
                var line = lines[i].trim();

                if (line.startsWith('|') && line.endsWith('|') && line.length > 1) {
                    closeList();
                    if (/^\|[-:| ]+\|$/.test(line)) continue;
                    inTable = true;
                    tableRows.push(line.slice(1, -1));
                    continue;
                }
                if (inTable) flushTable();

                if (line.startsWith('### ')) {
                    closeList();
                    html.push('<h6 class="mt-2 mb-1 fw-semibold" style="color:var(--accent);font-size:.85rem;">' + applyInline(line.slice(4)) + '</h6>');
                    continue;
                }
                if (line.startsWith('## ')) {
                    closeList();
                    html.push('<h5 class="mt-3 mb-1 fw-bold" style="color:var(--text-main);font-size:.9rem;border-bottom:1px solid var(--border);padding-bottom:.2rem;">' + applyInline(line.slice(3)) + '</h5>');
                    continue;
                }
                if (/^[-*•] /.test(line)) {
                    if (!inList) { html.push('<ul class="mb-2 ps-4" style="font-size:.8375rem;line-height:1.7;color:var(--text-secondary);">'); inList = true; }
                    html.push('<li>' + applyInline(line.replace(/^[-*•] /, '')) + '</li>');
                    continue;
                }
                closeList();

                if (!line) { html.push('<div style="height:.4rem;"></div>'); continue; }

                html.push('<p style="font-size:.8375rem;line-height:1.7;color:var(--text-secondary);margin-bottom:.2rem;">' + applyInline(line) + '</p>');
            }
            closeList();
            flushTable();
            return html.join('');
        }

        function shakeElement(el) {
            el.style.animation = 'none';
            el.offsetHeight;
            el.style.animation = 'shakeError 0.4s ease';
        }

        function buildClassicCard(articleId) {
            return (
                '<div class="alert alert-success alert-dismissible fade show p-4 shadow-sm animate-fade-in-up glow-on-appear" role="alert"' +
                '     style="border-left:4px solid #10b981;border-radius:var(--radius-md);animation-duration:.5s;">' +
                    '<h6 class="fw-bold text-success mb-2"><i class="bi bi-check-circle-fill me-2"></i>Analysis Completed Successfully!</h6>' +
                    '<p class="mb-0 small text-secondary">The multi-agent system has generated the sentiment & fundamental recommendation report.</p>' +
                    '<a href="/Report/Detail/' + articleId + '" class="btn btn-success btn-sm mt-3 px-4 rounded-pill btn-ripple">' +
                        '<i class="bi bi-file-text me-1"></i>Open Synthesized Report <i class="bi bi-arrow-right ms-1"></i>' +
                    '</a>' +
                    '<button type="button" class="btn-close position-absolute top-0 end-0" data-bs-dismiss="alert" aria-label="Close"></button>' +
                '</div>'
            );
        }

        function buildTradingAgentsCard(rich, articleId) {
            var d          = rich.decision || 'HOLD';
            var badgeColor = d === 'BUY' ? '#10b981' : d === 'SELL' ? '#ef4444' : '#f59e0b';
            var pct        = Math.min(100, Math.max(0, rich.confidenceScore || 70));

            var sections = [
                { icon: 'bi-graph-up-arrow', label: 'Sentiment Analysis',        body: rich.sentimentReport },
                { icon: 'bi-bar-chart-fill', label: 'Fundamental Analysis',      body: rich.fundamentalsReport },
                { icon: 'bi-newspaper',      label: 'News & Macro Report',       body: rich.newsReport },
                { icon: 'bi-activity',       label: 'Technical Market Analysis', body: rich.marketReport },
                { icon: 'bi-lightbulb-fill', label: 'Trader Investment Plan',    body: rich.traderInvestmentPlan },
                { icon: 'bi-check2-circle',  label: 'Final Trade Decision',      body: rich.finalTradeDecision },
            ].filter(function (s) { return s.body && s.body.trim(); });

            var accordionItems = sections.map(function (s, i) {
                var collapseId = 'taCollapse' + i;
                var isFirst    = i === 0;
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
                                renderMd(s.body) +
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
                                '<div class="progress-bar" style="width:' + pct + '%;background:' + badgeColor + ';border-radius:4px;"></div>' +
                            '</div>' +
                            '<div class="fw-bold mt-1" style="font-size:.78rem;color:' + badgeColor + ';">' + pct + '%</div>' +
                        '</div>' +
                    '</div>' +
                    '<div class="accordion accordion-flush">' + accordionItems + '</div>' +
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
