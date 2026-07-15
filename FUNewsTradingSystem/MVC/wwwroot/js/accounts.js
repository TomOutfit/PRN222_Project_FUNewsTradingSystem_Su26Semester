/**
 * Admin Account Management AJAX Script — Premium High-Performance Edition
 */

function getAntiforgeryToken() {
    var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

function getAccountModal() {
    var modalEl = document.getElementById('accountCrudModal');
    return bootstrap.Modal.getOrCreateInstance(modalEl);
}

// Thêm trợ giúp rung lắc (shake) khi form bị lỗi nhập liệu
function shakeElement(element) {
    if (!element) return;
    element.style.animation = 'none';
    element.offsetHeight; /* trigger reflow */
    element.style.animation = 'shakeError 0.4s cubic-bezier(0.36, 0.07, 0.19, 0.97) both';
    element.addEventListener('animationend', function() {
        element.style.animation = '';
    }, { once: true });
}

// Bật/Tắt trạng thái Loading mượt mà cho nút Save
function toggleSaveButtonLoading(isLoading, text) {
    var btn = $('#btnSaveAccount');
    if (!btn.length) return;

    if (isLoading) {
        btn.prop('disabled', true)
           .css('transform', 'scale(0.97)')
           .html('<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>' + text);
    } else {
        btn.prop('disabled', false)
           .css('transform', '')
           .html(text);
    }
}

function openCreateModal() {
    var title = $('#accountModalTitle');
    var btn = $('#btnSaveAccount');
    
    title.text('Add New Account');
    btn.removeClass('btn-primary btn-success btn-danger')
       .addClass('btn-primary')
       .text('Save Account')
       .attr('onclick', 'submitCreateForm()');

    var container = $('#accountModalBodyContainer');
    // Hiệu ứng mờ dần khi chuẩn bị nạp nội dung
    container.css({ 'opacity': '0', 'transform': 'translateY(10px)' });

    $.get('/Admin/Accounts/CreatePartial', function (data) {
        container.html(data);
        
        if ($.validator && $.validator.unobtrusive) {
            $.validator.unobtrusive.parse($('#createAccountForm'));
        }
        
        getAccountModal().show();
        
        // Kích hoạt hiệu ứng trượt nhẹ xuất hiện mượt mà (60fps)
        requestAnimationFrame(function() {
            container.css({
                'transition': 'all 0.4s cubic-bezier(0.16, 1, 0.3, 1)',
                'opacity': '1',
                'transform': 'translateY(0)'
            });
        });
    }).fail(function () {
        if (window.ToastHelpers) {
            window.ToastHelpers.showError('Could not load create form.');
        } else {
            alert('Could not load create form.');
        }
    });
}

function submitCreateForm() {
    var form = $('#createAccountForm');
    if (form.length && !form.valid()) {
        shakeElement(form[0]);
        return;
    }

    var data = {
        AccountName: form.find('[name="AccountName"]').val(),
        AccountEmail: form.find('[name="AccountEmail"]').val(),
        AccountPassword: form.find('[name="AccountPassword"]').val(),
        AccountRole: parseInt(form.find('[name="AccountRole"]').val())
    };

    $('#createErrors').addClass('d-none').html('');
    toggleSaveButtonLoading(true, 'Saving...');

    $.ajax({
        url: '/Admin/Accounts/Create',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        headers: { "RequestVerificationToken": getAntiforgeryToken() },
        success: function (res) {
            if (res.success) {
                if (window.ToastHelpers) {
                    window.ToastHelpers.showSuccess('Account created successfully!');
                }
                getAccountModal().hide();
                refreshAccountTable();
            } else {
                toggleSaveButtonLoading(false, 'Save Account');
                var errHtml = res.errors ? res.errors.join('<br/>') : (res.message || 'Error creating account.');
                var errContainer = $('#createErrors');
                
                errContainer.html(errHtml).removeClass('d-none').addClass('animate-fade-in-up');
                shakeElement(form[0]);
            }
        },
        error: function () {
            toggleSaveButtonLoading(false, 'Save Account');
            $('#createErrors').html('An unexpected network error occurred.').removeClass('d-none');
            shakeElement(form[0]);
        }
    });
}

function openEditModal(id) {
    var title = $('#accountModalTitle');
    var btn = $('#btnSaveAccount');

    title.text('Edit Account');
    btn.removeClass('btn-primary btn-success btn-danger')
       .addClass('btn-primary')
       .text('Update Account')
       .attr('onclick', 'submitEditForm()');

    var container = $('#accountModalBodyContainer');
    container.css({ 'opacity': '0', 'transform': 'translateY(10px)' });

    $.get('/Admin/Accounts/EditPartial/' + id, function (data) {
        container.html(data);
        
        if ($.validator && $.validator.unobtrusive) {
            $.validator.unobtrusive.parse($('#editAccountForm'));
        }
        
        getAccountModal().show();

        requestAnimationFrame(function() {
            container.css({
                'transition': 'all 0.4s cubic-bezier(0.16, 1, 0.3, 1)',
                'opacity': '1',
                'transform': 'translateY(0)'
            });
        });
    }).fail(function () {
        if (window.ToastHelpers) {
            window.ToastHelpers.showError('Could not load edit form.');
        } else {
            alert('Could not load edit form.');
        }
    });
}

function submitEditForm() {
    var form = $('#editAccountForm');
    if (form.length && !form.valid()) {
        shakeElement(form[0]);
        return;
    }

    var data = {
        AccountId: parseInt(form.find('[name="AccountId"]').val()),
        AccountName: form.find('[name="AccountName"]').val(),
        AccountEmail: form.find('[name="AccountEmail"]').val(),
        AccountPassword: form.find('[name="AccountPassword"]').val(),
        AccountRole: parseInt(form.find('[name="AccountRole"]').val())
    };

    $('#editErrors').addClass('d-none').html('');
    toggleSaveButtonLoading(true, 'Updating...');

    $.ajax({
        url: '/Admin/Accounts/Edit',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        headers: { "RequestVerificationToken": getAntiforgeryToken() },
        success: function (res) {
            if (res.success) {
                if (window.ToastHelpers) {
                    window.ToastHelpers.showSuccess('Account updated successfully!');
                }
                getAccountModal().hide();
                refreshAccountTable();
            } else {
                toggleSaveButtonLoading(false, 'Update Account');
                var errHtml = res.errors ? res.errors.join('<br/>') : (res.message || 'Error updating account.');
                var errContainer = $('#editErrors');
                
                errContainer.html(errHtml).removeClass('d-none').addClass('animate-fade-in-up');
                shakeElement(form[0]);
            }
        },
        error: function () {
            toggleSaveButtonLoading(false, 'Update Account');
            $('#editErrors').html('An unexpected network error occurred.').removeClass('d-none');
            shakeElement(form[0]);
        }
    });
}

var deleteModal;
var targetRowToDelete = null; // Lưu trữ hàng đang bị yêu cầu xóa để làm hiệu ứng slide-out

function deleteAccount(id, name) {
    $('#deleteEntityName').text(name);
    $('#deleteUrl').val('/Admin/Accounts/Delete/' + id);
    $('#deleteErrors').addClass('d-none').html('');
    
    // Tìm phần tử dòng (tr) trên bảng ứng với ID tài khoản để chuẩn bị animation slide-out
    var btnClicked = $('button[onclick*="deleteAccount(' + id + '"]').first();
    targetRowToDelete = btnClicked.closest('tr');

    // Elastic pop effect cho nút xác nhận xóa
    var deleteBtn = $('.btn-confirm-delete');
    deleteBtn.prop('disabled', false).html('Delete');

    deleteModal = bootstrap.Modal.getOrCreateInstance(document.getElementById('confirmDeleteModal'));
    deleteModal.show();
}

function executeDelete() {
    var url = $('#deleteUrl').val();
    var token = getAntiforgeryToken();
    var deleteBtn = $('.btn-confirm-delete');

    // Trạng thái Loading trên nút Delete
    deleteBtn.prop('disabled', true)
             .html('<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Deleting...');

    $.ajax({
        url: url,
        type: 'POST',
        headers: { "RequestVerificationToken": token },
        success: function (res) {
            if (res.success) {
                if (window.ToastHelpers) {
                    window.ToastHelpers.showSuccess('Account deleted successfully!');
                }
                deleteModal.hide();

                // Nếu tìm thấy hàng, kích hoạt animation trượt biến mất trước khi tải lại bảng thực tế
                if (targetRowToDelete && targetRowToDelete.length) {
                    targetRowToDelete.css({
                        'transition': 'all 0.4s cubic-bezier(0.16, 1, 0.3, 1)',
                        'opacity': '0',
                        'transform': 'translateX(-30px)'
                    });
                    setTimeout(function() {
                        refreshAccountTable();
                        targetRowToDelete = null;
                    }, 400);
                } else {
                    refreshAccountTable();
                }
            } else {
                deleteBtn.prop('disabled', false).html('Delete');
                $('#deleteErrors').text(res.message || 'Failed to delete account.').removeClass('d-none').addClass('animate-fade-in-up');
                shakeElement(document.getElementById('confirmDeleteModal').querySelector('.modal-content'));
            }
        },
        error: function () {
            deleteBtn.prop('disabled', false).html('Delete');
            $('#deleteErrors').text('An unexpected error occurred during deletion.').removeClass('d-none');
            shakeElement(document.getElementById('confirmDeleteModal').querySelector('.modal-content'));
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