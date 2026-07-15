/**
 * FUNewsTradingSystem - Category Management AJAX Script (Premium Edition)
 * Xử lý toàn bộ các tác vụ CRUD Danh mục với hiệu ứng chuyển động mượt mà 60fps
 */

// Lấy mã Token bảo mật chống tấn công giả mạo yêu cầu (CSRF) từ Form dùng chung
const getAntiforgeryToken = () => {
    const tokenInput = document.querySelector('#ajax-antiforgery input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
};

// Hàm rung lắc (shake) phần tử khi gặp lỗi nhập liệu hoặc lỗi hệ thống
function shakeElement(element) {
    if (!element) return;
    element.style.animation = 'none';
    element.offsetHeight; /* Kích hoạt Browser Reflow */
    element.style.animation = 'shakeError 0.4s cubic-bezier(0.36, 0.07, 0.19, 0.97) both';
    element.addEventListener('animationend', function() {
        element.style.animation = '';
    }, { once: true });
}

// I. MỞ MODAL TẠO MỚI (Tải giao diện rỗng từ Server với hoạt ảnh mượt)
function openCreateModal() {
    const modalTitle = document.getElementById('modalTitle');
    const btnSave = document.getElementById('btnSaveCategory');

    if (modalTitle) modalTitle.innerText = 'Add New Category';
    if (btnSave) {
        btnSave.className = 'btn btn-success px-4 btn-ripple';
        btnSave.setAttribute('onclick', 'submitCreateForm()');
    }

    const container = document.getElementById('modalBodyContainer');
    container.style.opacity = '0';
    container.style.transform = 'translateY(10px)';

    fetch('/Staff/Categories/CreatePartial')
        .then(res => {
            if (!res.ok) throw new Error('Could not load create form.');
            return res.text();
        })
        .then(html => {
            container.innerHTML = html;
            const crudModal = bootstrap.Modal.getOrCreateInstance(document.getElementById('categoryCrudModal'));
            crudModal.show();

            // Hiệu ứng trượt xuất hiện mượt mà của nội dung Modal
            requestAnimationFrame(() => {
                container.style.transition = 'all 0.4s cubic-bezier(0.16, 1, 0.3, 1)';
                container.style.opacity = '1';
                container.style.transform = 'translateY(0)';
            });
        })
        .catch(err => showToastError(err.message));
}

// II. MỞ MODAL CHỈNH SỬA (Hiệu ứng trượt mượt mà)
function openEditModal(id) {
    const modalTitle = document.getElementById('modalTitle');
    const btnSave = document.getElementById('btnSaveCategory');

    if (modalTitle) modalTitle.innerText = 'Edit Category Details';
    if (btnSave) {
        btnSave.className = 'btn btn-primary px-4 btn-ripple';
        btnSave.setAttribute('onclick', 'submitEditForm()');
    }

    const container = document.getElementById('modalBodyContainer');
    container.style.opacity = '0';
    container.style.transform = 'translateY(10px)';

    fetch(`/Staff/Categories/EditPartial/${id}`)
        .then(res => {
            if (!res.ok) throw new Error('Category records not found.');
            return res.text();
        })
        .then(html => {
            container.innerHTML = html;
            const crudModal = bootstrap.Modal.getOrCreateInstance(document.getElementById('categoryCrudModal'));
            crudModal.show();

            requestAnimationFrame(() => {
                container.style.transition = 'all 0.4s cubic-bezier(0.16, 1, 0.3, 1)';
                container.style.opacity = '1';
                container.style.transform = 'translateY(0)';
            });
        })
        .catch(err => showToastError(err.message));
}

// III. GỬI DỮ LIỆU TẠO MỚI (AJAX POST JSON kèm xử lý lỗi trực quan)
function submitCreateForm() {
    const form = document.getElementById('createCategoryForm');
    if (!form) return;

    const payload = {
        CategoryName: form.querySelector('[name="CategoryName"]').value,
        CategoryDescription: form.querySelector('[name="CategoryDescription"]').value,
        ParentCategoryID: form.querySelector('[name="ParentCategoryID"]').value ? parseInt(form.querySelector('[name="ParentCategoryID"]').value) : null,
        IsActive: form.querySelector('[name="IsActive"]').checked
    };

    const categoryInput = form.querySelector('[name="CategoryName"]');
    const categoryName = categoryInput.value.trim();

    if (!categoryName) {
        categoryInput.classList.add('is-invalid');
        categoryInput.focus();
        shakeElement(form);
        return;
    }

    toggleSaveButtonLoading(true);

    fetch('/Staff/Categories/Create', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiforgeryToken()
        },
        body: JSON.stringify(payload)
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                closeCrudModal();
                if (window.ToastHelpers) {
                    window.ToastHelpers.showSuccess('Category created successfully!');
                }
                refreshCategoryTable();
            } else {
                showToastError(data.message || 'Validation failed. Please check your inputs.');
                shakeElement(form);
            }
        })
        .catch(err => {
            showToastError('An error occurred: ' + err.message);
            shakeElement(form);
        })
        .finally(() => toggleSaveButtonLoading(false));
}

// IV. GỬI DỮ LIỆU CHỈNH SỬA (AJAX POST JSON)
function submitEditForm() {
    const form = document.getElementById('editCategoryForm');
    if (!form) return;

    const payload = {
        CategoryID: parseInt(form.querySelector('[name="CategoryID"]').value),
        CategoryName: form.querySelector('[name="CategoryName"]').value,
        CategoryDescription: form.querySelector('[name="CategoryDescription"]').value,
        ParentCategoryID: form.querySelector('[name="ParentCategoryID"]').value ? parseInt(form.querySelector('[name="ParentCategoryID"]').value) : null,
        IsActive: form.querySelector('[name="IsActive"]').checked
    };

    const categoryInput = form.querySelector('[name="CategoryName"]');
    const categoryName = categoryInput.value.trim();

    if (!categoryName) {
        categoryInput.classList.add('is-invalid');
        categoryInput.focus();
        shakeElement(form);
        return;
    }

    toggleSaveButtonLoading(true);

    fetch('/Staff/Categories/Edit', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiforgeryToken()
        },
        body: JSON.stringify(payload)
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                closeCrudModal();
                if (window.ToastHelpers) {
                    window.ToastHelpers.showSuccess('Category updated successfully!');
                }
                refreshCategoryTable();
            } else {
                showToastError(data.message || 'Failed to update category details.');
                shakeElement(form);
            }
        })
        .catch(err => {
            showToastError('An error occurred: ' + err.message);
            shakeElement(form);
        })
        .finally(() => toggleSaveButtonLoading(false));
}

// V. THAY ĐỔI NHANH TRẠNG THÁI ACTIVE BẰNG CÔNG TẮC (Hiệu ứng Spring scale mượt mà)
function toggleActive(id, checkbox) {
    const originalState = !checkbox.checked;

    // Hiệu ứng vật lý nảy nhẹ khi nhấn vào checkbox
    checkbox.style.transform = 'scale(1.2)';
    setTimeout(() => checkbox.style.transform = '', 200);

    fetch(`/Staff/Categories/ToggleActive/${id}`, {
        method: 'POST',
        headers: {
            'RequestVerificationToken': getAntiforgeryToken()
        }
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                checkbox.checked = data.newIsActive;
                if (window.ToastHelpers) {
                    window.ToastHelpers.showSuccess('Category status updated!');
                }
            } else {
                checkbox.checked = originalState;
                showToastError(data.message || 'Failed to change category status.');
            }
        })
        .catch(err => {
            checkbox.checked = originalState;
            showToastError('Network error. Status change reverted.');
        });
}

// VI. XỬ LÝ XÓA DANH MỤC (Hoạt ảnh trượt biến mất hàng dữ liệu khi xóa thành công)
function deleteCategory(id, name) {
    const deleteModalEl = document.getElementById('confirmDeleteModal');
    if (!deleteModalEl) return;

    const entityNamePlaceHolder = deleteModalEl.querySelector('.entity-name-placeholder');
    if (entityNamePlaceHolder) entityNamePlaceHolder.innerText = name;

    const btnConfirmDelete = deleteModalEl.querySelector('.btn-confirm-delete');
    if (btnConfirmDelete) {
        btnConfirmDelete.onclick = function () {
            btnConfirmDelete.disabled = true;
            btnConfirmDelete.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Deleting...';

            fetch(`/Staff/Categories/Delete/${id}`, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': getAntiforgeryToken()
                }
            })
                .then(res => res.json())
                .then(data => {
                    const modalInstance = bootstrap.Modal.getInstance(deleteModalEl);
                    if (modalInstance) modalInstance.hide();

                    if (data.success) {
                        // Xác định hàng tương ứng trên bảng và kích hoạt animation trượt biến mất (60fps slide-out)
                        const row = document.querySelector(`tr[data-id="${id}"]`) || 
                                    document.querySelector(`button[onclick*="deleteCategory(${id}"]`)?.closest('tr');
                        
                        if (row) {
                            row.style.transition = 'all 0.4s cubic-bezier(0.16, 1, 0.3, 1)';
                            row.style.opacity = '0';
                            row.style.transform = 'translateX(-30px)';
                            
                            setTimeout(() => {
                                if (window.ToastHelpers) {
                                    window.ToastHelpers.showSuccess('Category deleted successfully!');
                                }
                                refreshCategoryTable();
                            }, 400);
                        } else {
                            if (window.ToastHelpers) {
                                window.ToastHelpers.showSuccess('Category deleted successfully!');
                            }
                            refreshCategoryTable();
                        }
                    } else {
                        showToastError(data.message || 'Cannot delete this category.');
                        shakeElement(document.querySelector('.modal-content'));
                    }
                })
                .catch(err => {
                    showToastError('An error occurred during deletion.');
                    shakeElement(document.querySelector('.modal-content'));
                })
                .finally(() => {
                    btnConfirmDelete.disabled = false;
                    btnConfirmDelete.innerHTML = 'Delete';
                });
        };
    }

    const modal = bootstrap.Modal.getOrCreateInstance(deleteModalEl);
    modal.show();
}

// VII. LÀM MỚI BẢNG DỮ LIỆU SAU KHI THAY ĐỔI THÀNH CÔNG
function refreshCategoryTable() {
    if (typeof window.refreshPageContentRealtime === 'function') {
        window.refreshPageContentRealtime();
    } else {
        location.reload();
    }
}

// ──────────────────────────────────────────────────────────────────────────
// CÁC HÀM TRỢ GIÚP GIAO DIỆN (HELPER FUNCTIONS)
// ──────────────────────────────────────────────────────────────────────────

function closeCrudModal() {
    const modalEl = document.getElementById('categoryCrudModal');
    const modalInstance = bootstrap.Modal.getInstance(modalEl);
    if (modalInstance) modalInstance.hide();
}

function toggleSaveButtonLoading(isLoading) {
    const btnSave = document.getElementById('btnSaveCategory');
    const spinner = document.getElementById('saveSpinner');
    if (!btnSave || !spinner) return;

    if (isLoading) {
        btnSave.disabled = true;
        btnSave.style.transform = 'scale(0.97)';
        spinner.classList.remove('d-none');
    } else {
        btnSave.disabled = false;
        btnSave.style.transform = '';
        spinner.classList.add('d-none');
    }
}

// Nâng cấp hàm báo lỗi từ Alert mặc định thành Toast màu đỏ trực quan[cite: 13, 20]
function showToastError(message) {
    if (window.ToastHelpers) {
        window.ToastHelpers.showError(message);
    } else {
        alert('⚠️ System Notification:\n' + message);
    }
}

// Kiểm tra thông báo lưu sẵn
document.addEventListener('DOMContentLoaded', () => {
    const pendingSuccessMsg = sessionStorage.getItem('Category_Toast_Success');
    if (pendingSuccessMsg) {
        if (window.ToastHelpers) {
            window.ToastHelpers.showSuccess(pendingSuccessMsg);
        }
        sessionStorage.removeItem('Category_Toast_Success');
    }
});