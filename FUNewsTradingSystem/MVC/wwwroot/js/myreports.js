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
    const button = document.querySelector(`button[onclick="toggleStatus(${newsId})"]`);

    if (!badge || !button) {
        if (typeof window.refreshPageContentRealtime === 'function') {
            window.refreshPageContentRealtime();
        } else {
            location.reload();
        }
        return;
    }

    // Hoạt ảnh nảy đàn hồi tinh tế khi hoán đổi màu sắc và chữ
    badge.style.transform = 'scale(0.3)';
    badge.style.opacity = '0';
    badge.style.transition = 'all 0.3s cubic-bezier(0.34, 1.56, 0.64, 1)';

    button.style.transform = 'scale(0.95)';
    button.disabled = true;

    setTimeout(() => {
        if (isActive) {
            badge.classList.remove('bg-secondary');
            badge.classList.add('bg-success');
            badge.textContent = 'Active';
            
            button.className = 'btn btn-sm btn-outline-warning';
            button.innerHTML = '<i class="bi bi-archive me-1"></i>Archive';
        } else {
            badge.classList.remove('bg-success');
            badge.classList.add('bg-secondary');
            badge.textContent = 'Archived';
            
            button.className = 'btn btn-sm btn-outline-success';
            button.innerHTML = '<i class="bi bi-arrow-counterclockwise me-1"></i>Restore';
        }

        // Hiện lại với hiệu ứng Spring nảy nhẹ
        badge.style.transform = 'scale(1)';
        badge.style.opacity = '1';
        
        button.style.transform = '';
        button.disabled = false;
    }, 150);
}