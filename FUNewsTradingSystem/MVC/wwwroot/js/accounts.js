function openCreateModal() {
    $.get('/Admin/Accounts/CreatePartial', function (data) {
        $('#createModalContainer').html(data);
        var modal = new bootstrap.Modal(document.getElementById('createAccountModal'));
        
        // Re-parse unobtrusive validation for the newly injected HTML
        $.validator.unobtrusive.parse($('#createAccountForm'));
        
        modal.show();
    });
}

function submitCreateForm() {
    var form = $('#createAccountForm');
    if (!form.valid()) return;

    var data = {
        AccountName: $('#AccountName').val(),
        AccountEmail: $('#AccountEmail').val(),
        AccountPassword: $('#AccountPassword').val(),
        AccountRole: $('#AccountRole').val()
    };
    var token = form.find('input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: '/Admin/Accounts/Create',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        headers: { "RequestVerificationToken": token },
        success: function (res) {
            if (res.success) {
                location.reload();
            } else {
                $('#createErrors').html(res.errors.join('<br/>')).removeClass('d-none');
            }
        },
        error: function () {
            $('#createErrors').html('An unexpected error occurred.').removeClass('d-none');
        }
    });
}

function openEditModal(id) {
    $.get('/Admin/Accounts/EditPartial/' + id, function (data) {
        $('#editModalContainer').html(data);
        var modal = new bootstrap.Modal(document.getElementById('editAccountModal'));
        $.validator.unobtrusive.parse($('#editAccountForm'));
        modal.show();
    });
}

function submitEditForm() {
    var form = $('#editAccountForm');
    if (!form.valid()) return;

    var data = {
        AccountId: $('#AccountId').val(),
        AccountName: form.find('#AccountName').val(),
        AccountEmail: form.find('#AccountEmail').val(),
        AccountPassword: form.find('#AccountPassword').val(),
        AccountRole: form.find('#AccountRole').val()
    };
    var token = form.find('input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: '/Admin/Accounts/Edit',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        headers: { "RequestVerificationToken": token },
        success: function (res) {
            if (res.success) {
                location.reload();
            } else {
                $('#editErrors').html(res.errors.join('<br/>')).removeClass('d-none');
            }
        },
        error: function () {
            $('#editErrors').html('An unexpected error occurred.').removeClass('d-none');
        }
    });
}

var deleteModal;
function deleteAccount(id, name) {
    $('#deleteEntityName').text(name);
    $('#deleteUrl').val('/Admin/Accounts/Delete/' + id);
    $('#deleteErrors').addClass('d-none');
    
    // We need the antiforgery token for POST. We can grab it from any form on the page, or add one globally.
    // For now, we assume there's a token rendered in the page somewhere, but wait, Index.cshtml doesn't have a form.
    // Let's create a hidden form in _ConfirmDeleteModal or Index.cshtml, or pass it via layout.
    // Actually, we can fetch it if we create a small form in the modal. We'll update the layout soon to have a global token.
    
    deleteModal = new bootstrap.Modal(document.getElementById('confirmDeleteModal'));
    deleteModal.show();
}

function executeDelete() {
    var url = $('#deleteUrl').val();
    // Fetch token from a dummy form we can place in Index, or if none, we might fail if ValidateAntiForgeryToken is on.
    // We'll append a hidden form in Index.cshtml for the token.
    var token = $('input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: url,
        type: 'POST',
        headers: { "RequestVerificationToken": token },
        success: function (res) {
            if (res.success) {
                location.reload();
            } else {
                $('#deleteErrors').text(res.message).removeClass('d-none');
            }
        },
        error: function () {
            $('#deleteErrors').text('An unexpected error occurred.').removeClass('d-none');
        }
    });
}
