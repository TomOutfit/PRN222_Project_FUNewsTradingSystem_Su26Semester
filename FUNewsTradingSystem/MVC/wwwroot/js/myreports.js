/**
 * myreports.js — High-performance Row Status Toggle System
 */

async function toggleStatus(newsId) {
    const token = document.querySelector(
        '#ajax-antiforgery input[name="__RequestVerificationToken"]'
    ).value;

    try {
        const response = await fetch(
            `/Staff/MyReports/ToggleStatus/${newsId}`,
            {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token
                }
            });

        const result = await response.json();

        if (!response.ok || !result.success) {
            if (window.ToastHelpers) {
                window.ToastHelpers.showError(result.message || 'Operation failed.');
            } else {
                alert(result.message || 'Operation failed.');
            }
            return;
        }

        updateRowStatus(newsId, result.newStatus);

        if (window.ToastHelpers) {
            window.ToastHelpers.showSuccess('Status updated successfully!');
        }
    }
    catch (error) {
        console.error(error);
        if (window.ToastHelpers) {
            window.ToastHelpers.showError('Unexpected connection error.');
        } else {
            alert('Unexpected error.');
        }
    }
}

function updateRowStatus(newsId, isActive) {
    const badge = document.getElementById(`status-badge-${newsId}`);
    const row   = document.getElementById(`report-row-${newsId}`);
    const toggleCell = row ? row.querySelector('td:nth-child(6)') : null;

    if (!badge) {
        if (typeof window.refreshPageContentRealtime === 'function') {
            window.refreshPageContentRealtime();
        } else {
            location.reload();
        }
        return;
    }

    // Animate out
    badge.style.transform = 'scale(0.6) rotate(-10deg)';
    badge.style.opacity   = '0';
    badge.style.transition = 'all 0.3s cubic-bezier(0.34, 1.56, 0.64, 1)';
    if (toggleCell) {
        toggleCell.style.opacity = '0.4';
    }

    setTimeout(() => {
        if (isActive) {
            badge.className = 'rpt-status-active';
            badge.innerHTML = '<i class="bi bi-check-circle-fill"></i>Active';
        } else {
            badge.className = 'rpt-status-archived';
            badge.innerHTML = '<i class="bi bi-archive-fill"></i>Archived';
        }

        // Animate in with bounce
        badge.style.transform = 'scale(1.1)';
        badge.style.opacity   = '1';

        if (toggleCell) {
            toggleCell.style.opacity = '1';
            if (isActive) {
                toggleCell.innerHTML = `<a class="rpt-btn rpt-btn-archive" onclick="toggleStatus(${newsId})">
                    <i class="bi bi-archive"></i>Archive</a>`;
            } else {
                toggleCell.innerHTML = `<a class="rpt-btn rpt-btn-restore" onclick="toggleStatus(${newsId})">
                    <i class="bi bi-arrow-counterclockwise"></i>Restore</a>`;
            }
        }

        setTimeout(() => {
            badge.style.transform = 'scale(1)';
            badge.style.transition = '';
        }, 200);
    }, 180);
}