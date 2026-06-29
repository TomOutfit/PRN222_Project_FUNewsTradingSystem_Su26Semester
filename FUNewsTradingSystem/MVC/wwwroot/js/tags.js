let tagModal;
let deleteModal;
let deleteTagId = null;

document.addEventListener("DOMContentLoaded", function () {

    const modalElement = document.getElementById("tagCrudModal");

    if (modalElement) {
        tagModal = new bootstrap.Modal(modalElement);
    }

    const deleteElement =
        document.getElementById("deleteConfirmModal");

    if (deleteElement) {
        deleteModal = new bootstrap.Modal(deleteElement);
    }
});

// =========================
// CREATE
// =========================

async function openCreateModal() {
    try {
        const response = await fetch('/Staff/Tags/CreatePartial');

        if (!response.ok) {
            alert('Could not load create form.');
            return;
        }

        const html = await response.text();

        document.getElementById('modalTitle').textContent = 'Add Tag';
        document.getElementById('modalBodyContainer').innerHTML = html;

        const saveBtn = document.getElementById('btnSaveTag');
        saveBtn.onclick = submitCreateForm;

        tagModal.show();
    }
    catch (error) {
        console.error(error);
        alert('Error loading create form.');
    }
}

async function submitCreateForm() {

    const tagName = document.getElementById('TagName').value.trim();
    const note = document.getElementById('Note').value.trim();

    if (!tagName) {
        alert('Tag Name is required.');
        return;
    }

    const token = document.querySelector(
        '#ajax-antiforgery input[name="__RequestVerificationToken"]'
    ).value;

    const payload = {
        tagName: tagName,
        note: note
    };

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
            alert(result.message || 'Create failed.');
            return;
        }

        tagModal.hide();

        refreshTagTable();
    }
    catch (error) {
        console.error(error);
        tagModal.hide();
        alertError('Unexpected error.');
    }
}

// =========================
// EDIT
// =========================

async function openEditModal(id) {
    try {

        const response = await fetch(`/Staff/Tags/EditPartial/${id}`);

        if (!response.ok) {
            alert('Could not load edit form.');
            return;
        }

        const html = await response.text();

        document.getElementById('modalTitle').textContent = 'Edit Tag';
        document.getElementById('modalBodyContainer').innerHTML = html;

        const saveBtn = document.getElementById('btnSaveTag');
        saveBtn.onclick = submitEditForm;

        tagModal.show();
    }
    catch (error) {
        console.error(error);
        alert('Error loading edit form.');
    }
}

async function submitEditForm() {

    const tagID = document.getElementById('TagID').value;
    const tagName = document.getElementById('TagName').value.trim();
    const note = document.getElementById('Note').value.trim();

    if (!tagName) {
        alert('Tag Name is required.');
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
            alert(result.message || 'Update failed.');
            return;
        }

        tagModal.hide();

        refreshTagTable();
    }
    catch (error) {
        console.error(error);
        alert('Unexpected error.');
    }
}

// =========================
// DELETE
// =========================
function deleteTag(id, name) {

    deleteTagId = id;

    document.getElementById("deleteMessage").textContent =
        `Are you sure you want to delete tag "${name}"?`;

    document.getElementById("confirmDeleteBtn").onclick =
        confirmDeleteTag;

    deleteModal.show();
}

async function confirmDeleteTag() {

    const token = document.querySelector(
        '#ajax-antiforgery input[name="__RequestVerificationToken"]'
    ).value;

    try {

        const response = await fetch(
            `/Staff/Tags/Delete/${deleteTagId}`,
            {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token
                }
            });

        const result = await response.json();

        if (!response.ok) {
            alert(result.message || 'Delete failed.');
            return;
        }

        deleteModal.hide();

        refreshTagTable();
    }
    catch (error) {
        console.error(error);
        alert('Unexpected error.');
    }
}

// =========================
// REFRESH
// =========================

function refreshTagTable() {
    location.reload();
}

