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

            alert(result.message || 'Operation failed.');

            return;
        }

        updateRowStatus(newsId, result.newStatus);

    }
    catch (error) {

        console.error(error);

        alert('Unexpected error.');
    }
}

function updateRowStatus(newsId, isActive) {

    const badge =
        document.getElementById(`status-badge-${newsId}`);

    const button =
        document.querySelector(
            `button[onclick="toggleStatus(${newsId})"]`
        );

    if (!badge || !button) {
        location.reload();
        return;
    }

    if (isActive) {

        badge.classList.remove('bg-secondary');
        badge.classList.add('bg-success');

        badge.textContent = 'Active';

        button.textContent = 'Archive';
    }
    else {

        badge.classList.remove('bg-success');
        badge.classList.add('bg-secondary');

        badge.textContent = 'Inactive';

        button.textContent = 'Restore';
    }
}