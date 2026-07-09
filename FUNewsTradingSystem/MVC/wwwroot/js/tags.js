let deleteTagId = null;

function getTagModal() {
    const modalElement = document.getElementById("tagCrudModal");
    return bootstrap.Modal.getOrCreateInstance(modalElement);
}

function getDeleteModal() {
    const deleteElement = document.getElementById("deleteConfirmModal") || document.getElementById("confirmDeleteModal");
    return bootstrap.Modal.getOrCreateInstance(deleteElement);
}

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

        getTagModal().show();
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

        getTagModal().hide();

        refreshTagTable();
    }
    catch (error) {
        console.error(error);
        getTagModal().hide();
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

        getTagModal().show();
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

        getTagModal().hide();

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

    getDeleteModal().show();
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

        getDeleteModal().hide();

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
    if (typeof window.refreshPageContentRealtime === 'function') {
        window.refreshPageContentRealtime();
    } else {
        location.reload();
    }
}

