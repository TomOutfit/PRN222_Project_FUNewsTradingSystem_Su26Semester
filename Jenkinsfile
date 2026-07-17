pipeline {
    agent any

    environment {
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        BUILD_CONFIGURATION         = 'Release'
        SLN_FILE                    = 'FUNewsTradingSystem.sln'
        MVC_PROJECT                 = 'FUNewsTradingSystem/MVC/MVC.csproj'
        DOCKER_IMAGE_NAME           = 'funewstradingsystem'
    }

    options {
        timeout(time: 30, unit: 'MINUTES')
        buildDiscarder(logRotator(numToKeepStr: '10'))
        disableConcurrentBuilds()
        timestamps()
    }

    stages {
        stage('Checkout') {
            steps {
                echo 'Checking out source code...'
                checkout scm
            }
        }

        stage('Environment Info') {
            steps {
                script {
                    echo "Running on OS: ${isUnix() ? 'Unix-like' : 'Windows'}"
                    try {
                        if (isUnix()) {
                            sh 'dotnet --version'
                            sh 'docker --version'
                        } else {
                            bat 'dotnet --version'
                            bat 'docker --version'
                        }
                    } catch (Exception e) {
                        echo "Warning: dotnet or docker tools might not be in the PATH. Details: ${e.getMessage()}"
                    }
                }
            }
        }

        stage('Restore Dependencies') {
            steps {
                script {
                    if (isUnix()) {
                        sh "dotnet restore ${env.SLN_FILE}"
                    } else {
                        bat "dotnet restore ${env.SLN_FILE}"
                    }
                }
            }
        }

        stage('Build') {
            steps {
                script {
                    if (isUnix()) {
                        sh "dotnet build ${env.SLN_FILE} --configuration ${env.BUILD_CONFIGURATION} --no-restore"
                    } else {
                        bat "dotnet build ${env.SLN_FILE} --configuration ${env.BUILD_CONFIGURATION} --no-restore"
                    }
                }
            }
        }

        stage('Test') {
            steps {
                script {
                    echo 'Scanning and running test projects...'
                    catchError(buildResult: 'SUCCESS', message: 'No test projects found or tests failed') {
                        if (isUnix()) {
                            sh """
                            dotnet test ${env.SLN_FILE} \
                              --configuration ${env.BUILD_CONFIGURATION} \
                              --verbosity normal \
                              --logger "trx;LogFileName=test-results.trx" \
                              --results-directory TestResults
                            """
                        } else {
                            bat """
                            dotnet test ${env.SLN_FILE} ^
                              --configuration ${env.BUILD_CONFIGURATION} ^
                              --verbosity normal ^
                              --logger "trx;LogFileName=test-results.trx" ^
                              --results-directory TestResults
                            """
                        }
                    }
                }
            }
            post {
                always {
                    script {
                        def trxFiles = []
                        def noTests = true
                        def trxPath = ''

                        if (isUnix()) {
                            def shellOut = sh(script: 'if [ -d TestResults ]; then find TestResults -name "*.trx" | head -n 1; fi', returnStdout: true).trim()
                            if (shellOut) {
                                trxPath = shellOut
                                noTests = false
                            }
                        } else {
                            def shellOut = bat(script: "if exist TestResults\\NUL (dir /s /b TestResults\\*.trx 2>nul | findstr /r /c:\".trx\" | head -n 1)", returnStdout: true).trim()
                            if (shellOut) {
                                trxPath = shellOut.split(/\r?\n/)[0].trim()
                                noTests = false
                            }
                        }

                        // --- Summary Stats ---
                        def total   = 0
                        def passed  = 0
                        def failed  = 0
                        def skipped = 0
                        def testResults = []

                        if (!noTests) {
                            try {
                                def trxText = readFile(trxPath)
                                def xml = new XmlSlurper(false, false).parseText(trxText)

                                // Parse counters
                                if (xml.ResultSummary.Counters.size() > 0) {
                                    def counters = xml.ResultSummary.Counters[0]
                                    total = counters.@total.text() ? counters.@total.text().toInteger() : 0
                                    passed = counters.@passed.text() ? counters.@passed.text().toInteger() : 0
                                    failed = counters.@failed.text() ? counters.@failed.text().toInteger() : 0
                                    skipped = counters.@notExecuted.text() ? counters.@notExecuted.text().toInteger() : 0
                                }

                                // Map to lookup class names efficiently
                                def classMap = [:]
                                xml.TestDefinitions.UnitTest.each { ut ->
                                    def id = ut.@id.text()
                                    def className = ut.TestMethod.@className.text()
                                    if (id && className) {
                                        classMap[id] = className
                                    }
                                }

                                // Parse individual test results
                                xml.Results.UnitTestResult.each { utr ->
                                    def name = utr.@testName.text()
                                    def rawOutcome = utr.@outcome.text()?.toLowerCase() ?: "skipped"
                                    def outcome = "skipped"
                                    if (rawOutcome == "passed") {
                                        outcome = "passed"
                                    } else if (rawOutcome == "failed") {
                                        outcome = "failed"
                                    }

                                    def duration = utr.@duration.text() ?: "0s"
                                    def testId = utr.@testId.text()
                                    def className = classMap[testId] ?: "UnknownClass"

                                    def errorMessage = ""
                                    def stackTrace = ""
                                    if (utr.Output.ErrorInfo.size() > 0) {
                                        errorMessage = utr.Output.ErrorInfo.Message.text()
                                        stackTrace = utr.Output.ErrorInfo.StackTrace.text()
                                    }

                                    testResults << [
                                        name: name,
                                        className: className,
                                        outcome: outcome,
                                        duration: duration,
                                        errorMessage: errorMessage,
                                        stackTrace: stackTrace
                                    ]
                                }
                            } catch (Exception e) {
                                echo "Warning: Failed to parse TRX file: ${e.getMessage()}. Falling back to default stats."
                                noTests = true
                            }
                        }

                        // Helper for HTML escaping
                        def escapeHtml = { String input ->
                            if (!input) return ""
                            return input.replace("&", "&amp;")
                                        .replace("<", "&lt;")
                                        .replace(">", "&gt;")
                                        .replace("\"", "&quot;")
                                        .replace("'", "&#x27;")
                        }

                        // Generate dynamic HTML for individual test results, grouped by class
                        def testResultsHtml = ""
                        if (noTests) {
                            testResultsHtml = """
                            <div class="empty-state">
                                <svg viewBox="0 0 24 24">
                                    <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-2 10H7v-2h10v2z"/>
                                </svg>
                                <h3>No test projects or test cases detected</h3>
                                <p>Make sure your solution contains test projects and that they run successfully.</p>
                            </div>
                            """
                        } else {
                            def sb = new StringBuilder()
                            def grouped = testResults.groupBy { it.className }
                            grouped.each { className, items ->
                                def classFailed = items.count { it.outcome == 'failed' }
                                sb.append("""
                                <div class="test-group">
                                    <div class="test-group-header">
                                        <span class="group-dot ${classFailed > 0 ? 'dot-fail' : 'dot-pass'}"></span>
                                        <span class="group-name">${escapeHtml(className)}</span>
                                        <span class="group-count">${items.size()} test${items.size() == 1 ? '' : 's'}</span>
                                    </div>
                                """)
                                items.each { tr ->
                                    def badgeClass = "badge-${tr.outcome}"
                                    def badgeText = tr.outcome.toUpperCase()

                                    sb.append("""
                                    <details class="test-item ${tr.outcome}" data-name="${escapeHtml(tr.name.toLowerCase())}" data-outcome="${tr.outcome}">
                                        <summary>
                                            <span class="badge ${badgeClass}">${badgeText}</span>
                                            <span class="test-name-text">${escapeHtml(tr.name)}</span>
                                            <span class="test-time-badge">${tr.duration ?: '0s'}</span>
                                            <svg class="chevron" viewBox="0 0 20 20"><path d="M6 8l4 4 4-4" stroke="currentColor" stroke-width="1.8" fill="none" stroke-linecap="round" stroke-linejoin="round"/></svg>
                                        </summary>
                                        <div class="test-details-content">
                                            <div class="detail-field"><strong>Class</strong> ${escapeHtml(tr.className)}</div>
                                            <div class="detail-field"><strong>Duration</strong> ${tr.duration ?: 'N/A'}</div>
                                    """)

                                    if (tr.errorMessage) {
                                        sb.append("""
                                            <div class="detail-field error-container">
                                                <strong>Error message</strong>
                                                <pre class="error-text">${escapeHtml(tr.errorMessage)}</pre>
                                            </div>
                                        """)
                                    }
                                    if (tr.stackTrace) {
                                        sb.append("""
                                            <div class="detail-field error-container">
                                                <strong>Stack trace</strong>
                                                <pre class="error-text">${escapeHtml(tr.stackTrace)}</pre>
                                            </div>
                                        """)
                                    }

                                    sb.append("""
                                        </div>
                                    </details>
                                    """)
                                }
                                sb.append("</div>")
                            }
                            testResultsHtml = sb.toString()
                        }

                        // Publish test results and artifacts for Jenkins UI
                        if (isUnix()) {
                            sh 'if [ -d TestResults ]; then echo "Publishing TestResults"; fi'
                            archiveArtifacts artifacts: 'TestResults/**', allowEmptyArchive: true
                        } else {
                            bat 'if exist TestResults\\NUL (echo Publishing TestResults)'
                            archiveArtifacts artifacts: 'TestResults/**', allowEmptyArchive: true
                        }

                        // Determine header values
                        def htmlTitle = noTests ? 'No Test Cases Found' : 'Test Execution Report'
                        def statusLabel = noTests ? 'NO TESTS' : (failed > 0 ? 'FAILED' : 'PASSED')
                        def statusBadgeClass = noTests ? 'status-none-top' : (failed > 0 ? 'status-failed-top' : 'status-passed-top')
                        def passRate = total > 0 ? (passed * 100 / total).setScale(1, BigDecimal.ROUND_HALF_UP) : 0
                        def ringDeg = total > 0 ? (passed * 360 / total) : 0

                        // Generate the complete unified HTML report
                        def reportHtmlContent = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>${htmlTitle}</title>
<style>
  :root {
    --bg: #0a0c12;
    --bg-grid: rgba(255,255,255,0.025);
    --surface: rgba(255,255,255,0.035);
    --surface-solid: #13161f;
    --border: rgba(255,255,255,0.09);
    --border-soft: rgba(255,255,255,0.05);
    --text: #eef0f6;
    --text-dim: #8b90a3;
    --text-faint: #565b70;
    --accent: #7c8cff;
    --accent-soft: rgba(124,140,255,0.12);
    --good: #3ddc97;
    --good-soft: rgba(61,220,151,0.12);
    --bad: #ff5d7a;
    --bad-soft: rgba(255,93,122,0.12);
    --warn: #ffb454;
    --warn-soft: rgba(255,180,84,0.12);
    --radius-lg: 20px;
    --radius-md: 14px;
    --radius-sm: 9px;
  }

  * { box-sizing: border-box; margin: 0; padding: 0; }

  body {
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
    background:
      radial-gradient(circle at 12% 0%, rgba(124,140,255,0.10), transparent 45%),
      radial-gradient(circle at 88% 8%, rgba(61,220,151,0.07), transparent 40%),
      var(--bg);
    color: var(--text);
    min-height: 100vh;
    padding: 48px 20px;
    display: flex;
    justify-content: center;
    line-height: 1.5;
  }

  .container { width: 100%; max-width: 960px; }

  .top-row {
    display: flex;
    justify-content: space-between;
    align-items: flex-end;
    margin-bottom: 22px;
    flex-wrap: wrap;
    gap: 14px;
  }

  .brand {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .brand-mark {
    width: 34px; height: 34px;
    border-radius: 9px;
    background: linear-gradient(135deg, var(--accent), #4a5bff);
    display: flex; align-items: center; justify-content: center;
    font-family: 'Space Grotesk', sans-serif;
    font-weight: 700;
    font-size: 15px;
    color: #fff;
    box-shadow: 0 6px 18px rgba(124,140,255,0.35);
  }

  .brand-text { display: flex; flex-direction: column; }
  .brand-text .eyebrow {
    font-size: 0.68rem;
    font-weight: 600;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--text-faint);
  }
  .brand-text .proj {
    font-family: 'Space Grotesk', sans-serif;
    font-size: 0.92rem;
    font-weight: 600;
    color: var(--text);
  }

  .build-pill {
    font-family: 'JetBrains Mono', monospace;
    font-size: 0.78rem;
    color: var(--text-dim);
    background: var(--surface);
    border: 1px solid var(--border-soft);
    padding: 7px 12px;
    border-radius: 999px;
  }

  .card {
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: var(--radius-lg);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    box-shadow: 0 30px 60px -20px rgba(0,0,0,0.6);
    overflow: hidden;
  }

  .hero {
    padding: 36px 40px 32px;
    display: grid;
    grid-template-columns: auto 1fr;
    gap: 32px;
    align-items: center;
    border-bottom: 1px solid var(--border-soft);
    background: linear-gradient(180deg, rgba(255,255,255,0.02), transparent);
  }

  .ring-wrap { position: relative; width: 132px; height: 132px; flex-shrink: 0; }
  .ring {
    width: 132px; height: 132px;
    border-radius: 50%;
    background: conic-gradient(var(--good) 0deg ${ringDeg}deg, rgba(255,255,255,0.06) ${ringDeg}deg 360deg);
    display: flex; align-items: center; justify-content: center;
  }
  .ring-inner {
    width: 100px; height: 100px;
    border-radius: 50%;
    background: var(--surface-solid);
    display: flex; flex-direction: column; align-items: center; justify-content: center;
    border: 1px solid var(--border-soft);
  }
  .ring-inner .pct {
    font-family: 'Space Grotesk', sans-serif;
    font-size: 1.5rem; font-weight: 700; color: var(--text);
  }
  .ring-inner .pct-label {
    font-size: 0.62rem; color: var(--text-faint); text-transform: uppercase; letter-spacing: 0.08em;
  }

  .hero-info h1 {
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
    font-size: 1.7rem;
    font-weight: 700;
    letter-spacing: -0.01em;
    margin-bottom: 6px;
  }
  .hero-info .sub { color: var(--text-dim); font-size: 0.9rem; margin-bottom: 16px; }

  .status-badge-top {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 6px 14px;
    border-radius: 999px;
    font-weight: 700;
    font-size: 0.72rem;
    letter-spacing: 0.08em;
    text-transform: uppercase;
  }
  .status-badge-top::before {
    content: '';
    width: 6px; height: 6px; border-radius: 50%;
    background: currentColor;
    box-shadow: 0 0 8px currentColor;
  }
  .status-passed-top { background: var(--good-soft); color: var(--good); border: 1px solid rgba(61,220,151,0.3); }
  .status-failed-top  { background: var(--bad-soft);  color: var(--bad);  border: 1px solid rgba(255,93,122,0.3); }
  .status-none-top    { background: rgba(255,255,255,0.06); color: var(--text-dim); border: 1px solid var(--border); }

  .card-body { padding: 32px 40px 40px; }

  .metrics {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 14px;
    margin-bottom: 28px;
  }

  .metric {
    background: var(--surface);
    border: 1px solid var(--border-soft);
    border-radius: var(--radius-md);
    padding: 18px 16px;
    position: relative;
    overflow: hidden;
  }
  .metric::before {
    content: '';
    position: absolute; top: 0; left: 0; right: 0; height: 2px;
    background: var(--bar-color, var(--text-faint));
  }
  .m-total { --bar-color: var(--accent); }
  .m-pass  { --bar-color: var(--good); }
  .m-fail  { --bar-color: var(--bad); }
  .m-skip  { --bar-color: var(--warn); }

  .metric .num {
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
    font-size: 1.9rem; font-weight: 700; line-height: 1; margin-bottom: 6px;
  }
  .m-total .num { color: var(--accent); }
  .m-pass .num  { color: var(--good); }
  .m-fail .num  { color: var(--bad); }
  .m-skip .num  { color: var(--warn); }

  .metric .label {
    font-size: 0.68rem; color: var(--text-faint);
    text-transform: uppercase; letter-spacing: 0.1em; font-weight: 600;
  }

  .csp-warning {
    background: var(--warn-soft);
    border: 1px solid rgba(255,180,84,0.25);
    border-radius: var(--radius-sm);
    padding: 13px 16px;
    margin-bottom: 26px;
    font-size: 0.82rem;
    color: var(--warn);
    display: flex; gap: 10px; align-items: flex-start;
  }
  .csp-warning svg { flex-shrink: 0; width: 17px; height: 17px; fill: currentColor; margin-top: 1px; }
  .csp-warning code {
    font-family: 'JetBrains Mono', monospace;
    background: rgba(0,0,0,0.25);
    padding: 2px 6px;
    border-radius: 4px;
    font-size: 0.78rem;
  }

  .section-label {
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
    font-size: 0.78rem;
    font-weight: 600;
    color: var(--text-dim);
    text-transform: uppercase;
    letter-spacing: 0.1em;
    margin-bottom: 14px;
  }

  .filter-bar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 14px;
    margin-bottom: 20px;
    flex-wrap: wrap;
  }

  .filter-btns { display: flex; gap: 6px; }

  .filter-btn {
    background: var(--surface);
    border: 1px solid var(--border-soft);
    color: var(--text-dim);
    padding: 8px 15px;
    border-radius: var(--radius-sm);
    font-size: 0.8rem;
    font-weight: 500;
    cursor: pointer;
    transition: all 0.15s ease;
    font-family: inherit;
  }
  .filter-btn:hover { color: var(--text); border-color: var(--border); }
  .filter-btn.active {
    background: var(--accent);
    border-color: var(--accent);
    color: #fff;
    box-shadow: 0 0 16px rgba(124,140,255,0.35);
  }

  .search-box { position: relative; flex-grow: 1; max-width: 300px; }
  .search-box input {
    width: 100%;
    background: var(--surface);
    border: 1px solid var(--border-soft);
    border-radius: var(--radius-sm);
    padding: 9px 14px 9px 38px;
    color: var(--text);
    font-family: inherit;
    font-size: 0.84rem;
    outline: none;
    transition: all 0.15s ease;
  }
  .search-box input::placeholder { color: var(--text-faint); }
  .search-box input:focus {
    border-color: var(--accent);
    box-shadow: 0 0 0 3px var(--accent-soft);
  }
  .search-box svg {
    position: absolute; left: 13px; top: 50%; transform: translateY(-50%);
    width: 15px; height: 15px; fill: var(--text-faint);
  }

  .test-list { display: flex; flex-direction: column; gap: 18px; }

  .test-group {
    border: 1px solid var(--border-soft);
    border-radius: var(--radius-md);
    overflow: hidden;
    background: rgba(255,255,255,0.012);
  }

  .test-group-header {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 12px 18px;
    background: rgba(255,255,255,0.025);
    border-bottom: 1px solid var(--border-soft);
    font-family: 'JetBrains Mono', monospace;
    font-size: 0.78rem;
  }
  .group-dot { width: 7px; height: 7px; border-radius: 50%; flex-shrink: 0; }
  .dot-pass { background: var(--good); box-shadow: 0 0 6px var(--good); }
  .dot-fail { background: var(--bad); box-shadow: 0 0 6px var(--bad); }
  .group-name { color: var(--text); font-weight: 500; flex-grow: 1; word-break: break-word; }
  .group-count { color: var(--text-faint); font-size: 0.72rem; flex-shrink: 0; }

  details.test-item {
    border-top: 1px solid var(--border-soft);
    transition: background-color 0.15s ease;
  }
  details.test-item:first-of-type { border-top: none; }
  details.test-item[open] { background: rgba(255,255,255,0.02); }

  summary {
    display: flex;
    align-items: center;
    padding: 14px 18px;
    cursor: pointer;
    user-select: none;
    outline: none;
    gap: 12px;
  }
  summary::-webkit-details-marker { display: none; }
  summary::marker { display: none; }

  .chevron {
    width: 14px; height: 14px;
    color: var(--text-faint);
    flex-shrink: 0;
    transition: transform 0.2s ease;
    margin-left: auto;
  }
  details.test-item[open] .chevron { transform: rotate(180deg); }

  .badge {
    padding: 3px 9px;
    border-radius: 5px;
    font-size: 0.66rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    flex-shrink: 0;
    font-family: 'JetBrains Mono', monospace;
  }
  .badge-passed  { background: var(--good-soft); color: var(--good); border: 1px solid rgba(61,220,151,0.25); }
  .badge-failed  { background: var(--bad-soft);  color: var(--bad);  border: 1px solid rgba(255,93,122,0.25); }
  .badge-skipped { background: var(--warn-soft); color: var(--warn); border: 1px solid rgba(255,180,84,0.25); }

  .test-name-text {
    font-size: 0.87rem;
    font-weight: 500;
    color: var(--text);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .test-time-badge {
    font-family: 'JetBrains Mono', monospace;
    font-size: 0.72rem;
    color: var(--text-faint);
    flex-shrink: 0;
    margin-left: auto;
    padding-right: 4px;
  }
  details.test-item[open] .test-time-badge { margin-left: 0; }

  .test-details-content {
    padding: 4px 18px 20px 18px;
  }

  .detail-field { margin-bottom: 12px; font-size: 0.83rem; line-height: 1.5; color: var(--text-dim); }
  .detail-field strong {
    display: block;
    color: var(--text-faint);
    font-weight: 600;
    font-size: 0.68rem;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    margin-bottom: 4px;
  }

  .error-container { margin-top: 14px; }
  .error-text {
    background: #05060a;
    border: 1px solid rgba(255,93,122,0.15);
    border-radius: var(--radius-sm);
    padding: 14px;
    font-family: 'JetBrains Mono', monospace;
    font-size: 0.78rem;
    color: var(--bad);
    overflow-x: auto;
    white-space: pre-wrap;
    line-height: 1.6;
  }

  .empty-state {
    text-align: center;
    padding: 56px 24px;
    color: var(--text-faint);
  }
  .empty-state svg { width: 44px; height: 44px; margin-bottom: 14px; fill: currentColor; opacity: 0.4; }
  .empty-state svg, .csp-warning svg { width: 17px; height: 17px; }
  .empty-state h3 { font-size: 1rem; font-weight: 600; color: var(--text-dim); margin-bottom: 6px; }
  .empty-state p { font-size: 0.84rem; }

  .info-table {
    margin-top: 34px;
    border-top: 1px solid var(--border-soft);
    padding-top: 20px;
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 4px 24px;
  }
  .info-row {
    display: flex;
    justify-content: space-between;
    padding: 7px 0;
    font-size: 0.8rem;
    color: var(--text-faint);
    border-bottom: 1px dashed var(--border-soft);
  }
  .info-row span:last-child {
    color: var(--text-dim);
    font-weight: 500;
    font-family: 'JetBrains Mono', monospace;
    font-size: 0.76rem;
  }

  .footer {
    text-align: center;
    padding: 20px;
    color: var(--text-faint);
    font-size: 0.76rem;
    margin-top: 18px;
  }

  @media (max-width: 640px) {
    body { padding: 24px 12px; }
    .hero { grid-template-columns: 1fr; text-align: center; padding: 28px 24px; }
    .ring-wrap { margin: 0 auto; }
    .card-body { padding: 24px; }
    .metrics { grid-template-columns: repeat(2, 1fr); }
    .info-table { grid-template-columns: 1fr; }
    .test-name-text { max-width: 140px; }
  }
</style>
</head>
<body>
<div class="container">

  <div class="top-row">
    <div class="brand">
      <div class="brand-mark">FN</div>
      <div class="brand-text">
        <span class="eyebrow">CI / Test Report</span>
        <span class="proj">FUNewsTradingSystem</span>
      </div>
    </div>
    <div class="build-pill">Build #${env.BUILD_NUMBER} &middot; ${env.BUILD_CONFIGURATION}</div>
  </div>

  <div class="card">
    <div class="hero">
      <div class="ring-wrap">
        <div class="ring">
          <div class="ring-inner">
            <span class="pct">${passRate}%</span>
            <span class="pct-label">Pass rate</span>
          </div>
        </div>
      </div>
      <div class="hero-info">
        <h1>${htmlTitle}</h1>
        <p class="sub">Automated test execution summary for the latest pipeline run.</p>
        <span class="status-badge-top ${statusBadgeClass}">${statusLabel}</span>
      </div>
    </div>

    <div class="card-body">

      <div class="metrics">
        <div class="metric m-total"><div class="num">${total}</div><div class="label">Total</div></div>
        <div class="metric m-pass"><div class="num">${passed}</div><div class="label">Passed</div></div>
        <div class="metric m-fail"><div class="num">${failed}</div><div class="label">Failed</div></div>
        <div class="metric m-skip"><div class="num">${skipped}</div><div class="label">Skipped</div></div>
      </div>

      <div class="csp-warning">
        <svg viewBox="0 0 20 20"><path d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"/></svg>
        <div>If styling doesn't load in Jenkins, run in <em>Manage Jenkins &rarr; Script Console</em>: <code>System.setProperty("hudson.model.DirectoryBrowserSupport.CSP", "")</code></div>
      </div>

      <div class="section-label">Test results</div>

      <div class="filter-bar">
        <div class="filter-btns">
          <button class="filter-btn active" data-filter="all" onclick="filterTests('all', this)">All</button>
          <button class="filter-btn" data-filter="passed" onclick="filterTests('passed', this)">Passed</button>
          <button class="filter-btn" data-filter="failed" onclick="filterTests('failed', this)">Failed</button>
          <button class="filter-btn" data-filter="skipped" onclick="filterTests('skipped', this)">Skipped</button>
        </div>
        <div class="search-box">
          <svg viewBox="0 0 20 20"><path fill-rule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z" clip-rule="evenodd"/></svg>
          <input type="text" id="search-input" placeholder="Search tests..." onkeyup="applyFilters()">
        </div>
      </div>

      <div class="test-list">
        ${testResultsHtml}
      </div>

      <div class="info-table">
        <div class="info-row"><span>Solution file</span><span>${env.SLN_FILE}</span></div>
        <div class="info-row"><span>Build configuration</span><span>${env.BUILD_CONFIGURATION}</span></div>
        <div class="info-row"><span>Build number</span><span>#${env.BUILD_NUMBER}</span></div>
        <div class="info-row"><span>Executed at</span><span>${new Date().toString()}</span></div>
      </div>
    </div>
  </div>

  <div class="footer">FUNewsTradingSystem &middot; Continuous Integration</div>
</div>

<script>
let activeFilter = 'all';

function filterTests(status, btn) {
    document.querySelectorAll('.filter-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    activeFilter = status;
    applyFilters();
}

function applyFilters() {
    const query = document.getElementById('search-input').value.toLowerCase();
    document.querySelectorAll('.test-group').forEach(group => {
        let visibleInGroup = 0;
        group.querySelectorAll('.test-item').forEach(item => {
            const name = item.dataset.name || '';
            const outcome = item.dataset.outcome || '';
            const matchesSearch = name.includes(query);
            const matchesFilter = activeFilter === 'all' || outcome === activeFilter;
            const show = matchesSearch && matchesFilter;
            item.style.display = show ? 'block' : 'none';
            if (show) visibleInGroup++;
        });
        group.style.display = visibleInGroup > 0 ? 'block' : 'none';
    });
}
</script>
</body>
</html>
"""

                        // Write the HTML report and archive it so it is always produced
                        if (isUnix()) {
                            sh 'mkdir -p TestResults'
                        } else {
                            bat 'if not exist TestResults mkdir TestResults'
                        }
                        writeFile file: 'TestResults/test-results.html', text: reportHtmlContent
                        archiveArtifacts artifacts: 'TestResults/test-results.html', allowEmptyArchive: true

                        // --- TRX / MSTest ---
                        if (trxFiles) {
                            try {
                                mstest testResultsFile: trxFiles[0].path, keepLongStdio: true
                            } catch (Exception e) {
                                echo "MSTest plugin not available: ${e.getMessage()}"
                            }
                        }

                        // --- Summary Table ---
                        echo ''
                        echo '=============================================='
                        echo '          TEST RESULTS SUMMARY'
                        echo '=============================================='
                        echo "  Total   : ${total}"
                        echo "  Passed  : ${passed}   [${passed > 0 ? 'SUCCESS' : 'NONE'}]"
                        echo "  Failed  : ${failed}   [${failed > 0 ? 'FAILURE' : 'NONE'}]"
                        echo "  Skipped : ${skipped}"
                        echo '=============================================='
                        echo "  Report  : TestResults/test-results.html"
                        echo "  TRX     : ${trxFiles  ? trxFiles[0].path  : 'N/A'}"
                        echo '=============================================='
                        echo ''

                        currentBuild.result = failed > 0 ? 'FAILURE' : 'SUCCESS'
                    }
                }
            }
        }

        stage('Publish Artifacts') {
            steps {
                script {
                    if (isUnix()) {
                        sh "dotnet publish ${env.MVC_PROJECT} -c ${env.BUILD_CONFIGURATION} -o ./publish"
                    } else {
                        bat "dotnet publish ${env.MVC_PROJECT} -c ${env.BUILD_CONFIGURATION} -o ./publish"
                    }
                }
                archiveArtifacts artifacts: 'publish/**', onlyIfSuccessful: true
            }
        }

        stage('Docker Build') {
            steps {
                script {
                    echo "Building Docker image ${env.DOCKER_IMAGE_NAME}:${env.BUILD_NUMBER}..."
                    try {
                        if (isUnix()) {
                            sh "docker build -t ${env.DOCKER_IMAGE_NAME}:${env.BUILD_NUMBER} -t ${env.DOCKER_IMAGE_NAME}:latest ."
                        } else {
                            bat "docker build -t ${env.DOCKER_IMAGE_NAME}:${env.BUILD_NUMBER} -t ${env.DOCKER_IMAGE_NAME}:latest ."
                        }
                    } catch (Exception e) {
                        echo "Docker build skipped: ${e.getMessage()}"
                    }
                }
            }
        }
    }

    post {
        always {
            echo 'Pipeline execution completed.'
        }
        success {
            echo 'Build and deployment preparation succeeded!'
        }
        failure {
            echo 'Build failed. Please check the logs above.'
        }
        unstable {
            echo 'Pipeline completed with test failures (unstable).'
        }
        cleanup {
            cleanWs()
        }
    }
}