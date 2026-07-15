/**
 * pipeline-spinner.js — Handles the AI Trading Pipeline loading UI with premium transition curves
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
            if (show) {
                spinner.className = 'text-center mt-4 animate-fade-in-up';
                spinner.style.animationDuration = '0.4s';
            } else {
                spinner.className = 'd-none text-center mt-4';
            }
        }
        
        if (btn) {
            btn.disabled = show;
            var btnText = document.getElementById('btnText');
            if (btnText) {
                btnText.textContent = show ? 'Analyzing...' : 'Run Analysis';
            }
            
            if (show) {
                btn.style.transform = 'scale(0.97)';
            } else {
                btn.style.transform = '';
            }
        }
    }

    /**
     * Display the pipeline result in the result area with elastic entrance animation.
     * @param {'success'|'error'} type
     * @param {string} htmlContent
     */
    function showResult(type, htmlContent) {
        var area = document.getElementById('resultArea');
        if (!area) return;
        
        if (!htmlContent) {
            area.style.opacity = '0';
            area.style.transform = 'translateY(10px)';
            return;
        }

        area.className = 'mt-4 alert ' + (type === 'success' ? 'alert-success glow-on-appear' : 'alert-danger') + ' animate-fade-in-up';
        area.style.animationDuration = '0.5s';
        area.innerHTML = htmlContent;
        
        requestAnimationFrame(function() {
            area.style.opacity = '1';
            area.style.transform = 'translateY(0)';
        });
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