/**
 * Admin Account Management AJAX Script
 */

function getAntiforgeryToken() {
    var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

function getAccountModal() {
    var modalEl = document.getElementById('accountCrudModal');
    return bootstrap.Modal.getOrCreateInstance(modalEl);
}

function openCreateModal() {
    $('#accountModalTitle').text('Add New Account');
    $('#btnSaveAccount')
        .removeClass('btn-primary btn-success')
        .addClass('btn-primary')
        .text('Save Account')
        .attr('onclick', 'submitCreateForm()');

    $.get('/Admin/Accounts/CreatePartial', function (data) {
        $('#accountModalBodyContainer').html(data);
        if ($.validator && $.validator.unobtrusive) {
            $.validator.unobtrusive.parse($('#createAccountForm'));
        }
        getAccountModal().show();
    }).fail(function () {
        alert('Could not load create form.');
    });
}

function submitCreateForm() {
    var form = $('#createAccountForm');
    if (form.length && !form.valid()) return;

    var data = {
        AccountName: form.find('[name="AccountName"]').val(),
        AccountEmail: form.find('[name="AccountEmail"]').val(),
        AccountPassword: form.find('[name="AccountPassword"]').val(),
        AccountRole: parseInt(form.find('[name="AccountRole"]').val())
    };

    $.ajax({
        url: '/Admin/Accounts/Create',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        headers: { "RequestVerificationToken": getAntiforgeryToken() },
        success: function (res) {
            if (res.success) {
                getAccountModal().hide();
                refreshAccountTable();
            } else {
                var errHtml = res.errors ? res.errors.join('<br/>') : (res.message || 'Error creating account.');
                $('#createErrors').html(errHtml).removeClass('d-none');
            }
        },
        error: function () {
            $('#createErrors').html('An unexpected network error occurred.').removeClass('d-none');
        }
    });
}

function openEditModal(id) {
    $('#accountModalTitle').text('Edit Account');
    $('#btnSaveAccount')
        .removeClass('btn-primary btn-success')
        .addClass('btn-primary')
        .text('Update Account')
        .attr('onclick', 'submitEditForm()');

    $.get('/Admin/Accounts/EditPartial/' + id, function (data) {
        $('#accountModalBodyContainer').html(data);
        if ($.validator && $.validator.unobtrusive) {
            $.validator.unobtrusive.parse($('#editAccountForm'));
        }
        getAccountModal().show();
    }).fail(function () {
        alert('Could not load edit form.');
    });
}

function submitEditForm() {
    var form = $('#editAccountForm');
    if (form.length && !form.valid()) return;

    var data = {
        AccountId: parseInt(form.find('[name="AccountId"]').val()),
        AccountName: form.find('[name="AccountName"]').val(),
        AccountEmail: form.find('[name="AccountEmail"]').val(),
        AccountPassword: form.find('[name="AccountPassword"]').val(),
        AccountRole: parseInt(form.find('[name="AccountRole"]').val())
    };

    $.ajax({
        url: '/Admin/Accounts/Edit',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        headers: { "RequestVerificationToken": getAntiforgeryToken() },
        success: function (res) {
            if (res.success) {
                getAccountModal().hide();
                refreshAccountTable();
            } else {
                var errHtml = res.errors ? res.errors.join('<br/>') : (res.message || 'Error updating account.');
                $('#editErrors').html(errHtml).removeClass('d-none');
            }
        },
        error: function () {
            $('#editErrors').html('An unexpected network error occurred.').removeClass('d-none');
        }
    });
}

var deleteModal;
function deleteAccount(id, name) {
    $('#deleteEntityName').text(name);
    $('#deleteUrl').val('/Admin/Accounts/Delete/' + id);
    $('#deleteErrors').addClass('d-none');
    
    deleteModal = bootstrap.Modal.getOrCreateInstance(document.getElementById('confirmDeleteModal'));
    deleteModal.show();
}

function executeDelete() {
    var url = $('#deleteUrl').val();
    var token = getAntiforgeryToken();

    $.ajax({
        url: url,
        type: 'POST',
        headers: { "RequestVerificationToken": token },
        success: function (res) {
            if (res.success) {
                deleteModal.hide();
                refreshAccountTable();
            } else {
                $('#deleteErrors').text(res.message || 'Failed to delete account.').removeClass('d-none');
            }
        },
        error: function () {
            $('#deleteErrors').text('An unexpected error occurred during deletion.').removeClass('d-none');
        }
    });
}

function refreshAccountTable() {
    if (typeof window.refreshPageContentRealtime === 'function') {
        window.refreshPageContentRealtime();
    } else {
        location.reload();
    }
}
