/**
 * Staff Tag Management AJAX Script
 */

function getTagModal() {
    const modalElement = document.getElementById("tagCrudModal");
    return bootstrap.Modal.getOrCreateInstance(modalElement);
}

// Trợ giúp hiệu ứng rung lắc (shake) đàn hồi khi có lỗi nhập liệu
function shakeElement(element) {
    if (!element) return;
    element.style.animation = 'none';
    element.offsetHeight; /* kích hoạt browser reflow */
    element.style.animation = 'shakeError 0.4s cubic-bezier(0.36, 0.07, 0.19, 0.97) both';
    element.addEventListener('animationend', function() {
        element.style.animation = '';
    }, { once: true });
}

// Bật/Tắt trạng thái Loading mượt mà cho nút Lưu Tag
function toggleSaveButtonLoading(isLoading, text) {
    const btn = document.getElementById('btnSaveTag');
    if (!btn) return;

    if (isLoading) {
        btn.disabled = true;
        btn.style.transform = 'scale(0.97)';
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>' + text;
    } else {
        btn.disabled = false;
        btn.style.transform = '';
        btn.innerHTML = text;
    }
}

// =========================
// CREATE
// =========================

async function openCreateModal() {
    try {
        const response = await fetch('/Staff/Tags/CreatePartial');

        if (!response.ok) {
            if (window.ToastHelpers) {
                window.ToastHelpers.showError('Could not load create form.');
            }
            return;
        }

        const html = await response.text();
        const container = document.getElementById('modalBodyContainer');

        document.getElementById('modalTitle').textContent = 'Add Tag';
        container.innerHTML = html;

        // Reset style để thực hiện hiệu ứng lướt xuất hiện mượt mà (60fps)
        container.style.opacity = '0';
        container.style.transform = 'translateY(10px)';

        const saveBtn = document.getElementById('btnSaveTag');
        saveBtn.onclick = submitCreateForm;

        getTagModal().show();

        requestAnimationFrame(() => {
            container.style.transition = 'all 0.4s cubic-bezier(0.16, 1, 0.3, 1)';
            container.style.opacity = '1';
            container.style.transform = 'translateY(0)';
        });
    }
    catch (error) {
        console.error(error);
        if (window.ToastHelpers) {
            window.ToastHelpers.showError('Error loading create form.');
        }
    }
}

async function submitCreateForm() {
    const tagNameInput = document.getElementById('TagName');
    const tagName = tagNameInput.value.trim();
    const note = document.getElementById('Note').value.trim();

    if (!tagName) {
        tagNameInput.classList.add('is-invalid');
        tagNameInput.focus();
        shakeElement(tagNameInput);
        if (window.ToastHelpers) {
            window.ToastHelpers.showError('Tag Name is required.');
        }
        return;
    }

    const token = document.querySelector(
        '#ajax-antiforgery input[name="__RequestVerificationToken"]'
    ).value;

    const payload = {
        tagName: tagName,
        note: note
    };

    toggleSaveButtonLoading(true, 'Creating...');

    try {
        const response = await fetch('/Staff/Tags/Create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify(payload)
        });

        const result = await response.json();

        if (!response.ok) {
            toggleSaveButtonLoading(false, 'Save Tag');
            if (window.ToastHelpers) {
                window.ToastHelpers.showError(result.message || 'Create failed.');
            }
            shakeElement(document.querySelector('#tagCrudModal .modal-content'));
            return;
        }

        if (window.ToastHelpers) {
            window.ToastHelpers.showSuccess('Tag created successfully!');
        }
        getTagModal().hide();
        refreshTagTable();
    }
    catch (error) {
        console.error(error);
        toggleSaveButtonLoading(false, 'Save Tag');
        getTagModal().hide();
        if (window.ToastHelpers) {
            window.ToastHelpers.showError('An unexpected error occurred.');
        }
    }
}

// =========================
// EDIT
// =========================

async function openEditModal(id) {
    try {
        const response = await fetch(`/Staff/Tags/EditPartial/${id}`);

        if (!response.ok) {
            if (window.ToastHelpers) {
                window.ToastHelpers.showError('Could not load edit form.');
            }
            return;
        }

        const html = await response.text();
        const container = document.getElementById('modalBodyContainer');

        document.getElementById('modalTitle').textContent = 'Edit Tag';
        container.innerHTML = html;

        container.style.opacity = '0';
        container.style.transform = 'translateY(10px)';

        const saveBtn = document.getElementById('btnSaveTag');
        saveBtn.onclick = submitEditForm;

        getTagModal().show();

        requestAnimationFrame(() => {
            container.style.transition = 'all 0.4s cubic-bezier(0.16, 1, 0.3, 1)';
            container.style.opacity = '1';
            container.style.transform = 'translateY(0)';
        });
    }
    catch (error) {
        console.error(error);
        if (window.ToastHelpers) {
            window.ToastHelpers.showError('Error loading edit form.');
        }
    }
}

async function submitEditForm() {
    const tagID = document.getElementById('TagID').value;
    const tagNameInput = document.getElementById('TagName');
    const tagName = tagNameInput.value.trim();
    const note = document.getElementById('Note').value.trim();

    if (!tagName) {
        tagNameInput.classList.add('is-invalid');
        tagNameInput.focus();
        shakeElement(tagNameInput);
        if (window.ToastHelpers) {
            window.ToastHelpers.showError('Tag Name is required.');
        }
        return;
    }

    const token = document.querySelector(
        '#ajax-antiforgery input[name="__RequestVerificationToken"]'
    ).value;

    const payload = {
        tagID: parseInt(tagID),
        tagName: tagName,
        note: note
    };

    toggleSaveButtonLoading(true, 'Updating...');

    try {
        const response = await fetch('/Staff/Tags/Edit', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify(payload)
        });

        const result = await response.json();

        if (!response.ok) {
            toggleSaveButtonLoading(false, 'Save Tag');
            if (window.ToastHelpers) {
                window.ToastHelpers.showError(result.message || 'Update failed.');
            }
            shakeElement(document.querySelector('#tagCrudModal .modal-content'));
            return;
        }

        if (window.ToastHelpers) {
            window.ToastHelpers.showSuccess('Tag updated successfully!');
        }
        getTagModal().hide();
        refreshTagTable();
    }
    catch (error) {
        console.error(error);
        toggleSaveButtonLoading(false, 'Save Tag');
        if (window.ToastHelpers) {
            window.ToastHelpers.showError('An unexpected error occurred.');
        }
    }
}

// =========================
// DELETE (uses shared _ConfirmDeleteModal → executeDelete)
// =========================

function refreshTagTable() {
    if (typeof window.refreshPageContentRealtime === 'function') {
        window.refreshPageContentRealtime();
    } else {
        location.reload();
    }
}