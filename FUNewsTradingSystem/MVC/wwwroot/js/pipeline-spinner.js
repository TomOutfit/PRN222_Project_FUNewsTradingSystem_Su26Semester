/**
 * pipeline-spinner.js — Handles the AI Trading Pipeline loading UI
 * Controls spinner visibility and button state during async analysis.
 */

(function () {
    'use strict';

    /**
     * Show or hide the loading spinner and disable/enable the Run Analysis button.
     * @param {boolean} show
     */
    function setLoading(show) {
        var spinner = document.getElementById('loadingSpinner');
        var btn = document.getElementById('btnRunAnalysis');
        if (spinner) {
            spinner.className = show ? 'text-center mt-4' : 'd-none text-center mt-4';
        }
        if (btn) {
            btn.disabled = show;
            var btnText = document.getElementById('btnText');
            if (btnText) {
                btnText.textContent = show ? 'Analyzing...' : 'Run Analysis';
            }
        }
    }

    /**
     * Display the pipeline result in the result area.
     * @param {'success'|'error'} type
     * @param {string} htmlContent
     */
    function showResult(type, htmlContent) {
        var area = document.getElementById('resultArea');
        if (!area) return;
        area.className = 'mt-4 alert ' + (type === 'success' ? 'alert-success' : 'alert-danger');
        area.innerHTML = htmlContent;
    }

    /**
     * Submit the analysis form asynchronously.
     * @param {string} actionUrl
     * @param {FormData} formData
     */
    function submitAnalysis(actionUrl, formData) {
        setLoading(true);
        showResult('', '');

        fetch(actionUrl, {
            method: 'POST',
            body: formData
        })
        .then(function (r) { return r.json(); })
        .then(function (data) {
            setLoading(false);
            if (data.success) {
                var link = '<a href="/Report/Details/' + data.newsArticleId + '" class="alert-link">View Report &rarr;</a>';
                showResult('success', 'Analysis report generated successfully. ' + link);
            } else {
                showResult('error', data.errorMessage || 'Pipeline failed. Please try again.');
            }
        })
        .catch(function () {
            setLoading(false);
            showResult('error', 'Unexpected network error. Please try again.');
        });
    }

    window.PipelineSpinner = {
        setLoading: setLoading,
        showResult: showResult,
        submitAnalysis: submitAnalysis
    };
})();
