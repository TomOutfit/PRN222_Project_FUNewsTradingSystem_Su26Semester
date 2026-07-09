/**
 * FUNewsTradingSystem - Category Management AJAX Script
 * Xử lý toàn bộ các tác vụ CRUD Danh mục không cần tải lại trang
 */

// Lấy mã Token bảo mật chống tấn công giả mạo yêu cầu (CSRF) từ Form dùng chung
const getAntiforgeryToken = () => {
    const tokenInput = document.querySelector('#ajax-antiforgery input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
};

// I. MỞ MODAL TẠO MỚI (Tải giao diện rỗng từ Server)
function openCreateModal() {
    const modalTitle = document.getElementById('modalTitle');
    const btnSave = document.getElementById('btnSaveCategory');

    if (modalTitle) modalTitle.innerText = 'Add New Category';
    if (btnSave) {
        btnSave.className = 'btn btn-success px-4';
        // Đổi sự kiện onclick của nút lưu trên Modal sang hàm submit tạo mới
        btnSave.setAttribute('onclick', 'submitCreateForm()');
    }

    fetch('/Staff/Categories/CreatePartial')
        .then(res => {
            if (!res.ok) throw new Error('Could not load create form.');
            return res.text();
        })
        .then(html => {
            document.getElementById('modalBodyContainer').innerHTML = html;
            const crudModal = bootstrap.Modal.getOrCreateInstance(document.getElementById('categoryCrudModal'));
            crudModal.show();
        })
        .catch(err => showToastError(err.message));
}

// II. MỞ MODAL CHỈNH SỬA (Tải giao diện chứa sẵn dữ liệu cũ theo ID)
function openEditModal(id) {
    const modalTitle = document.getElementById('modalTitle');
    const btnSave = document.getElementById('btnSaveCategory');

    if (modalTitle) modalTitle.innerText = 'Edit Category Details';
    if (btnSave) {
        btnSave.className = 'btn btn-primary px-4';
        // Đổi sự kiện onclick của nút lưu trên Modal sang hàm submit cập nhật
        btnSave.setAttribute('onclick', 'submitEditForm()');
    }

    fetch(`/Staff/Categories/EditPartial/${id}`)
        .then(res => {
            if (!res.ok) throw new Error('Category records not found.');
            return res.text();
        })
        .then(html => {
            document.getElementById('modalBodyContainer').innerHTML = html;
            const crudModal = bootstrap.Modal.getOrCreateInstance(document.getElementById('categoryCrudModal'));
            crudModal.show();
        })
        .catch(err => showToastError(err.message));
}

// III. GỬI DỮ LIỆU TẠO MỚI (AJAX POST JSON)
function submitCreateForm() {
    const form = document.getElementById('createCategoryForm');
    if (!form) return;

    // Thu thập dữ liệu từ các ô nhập
    const payload = {
        CategoryName: form.querySelector('[name="CategoryName"]').value,
        CategoryDescription: form.querySelector('[name="CategoryDescription"]').value,
        ParentCategoryID: form.querySelector('[name="ParentCategoryID"]').value ? parseInt(form.querySelector('[name="ParentCategoryID"]').value) : null,
        IsActive: form.querySelector('[name="IsActive"]').checked
    };

    const categoryName =
        form.querySelector('[name="CategoryName"]').value.trim();

    if (!categoryName) {

        const input =
            form.querySelector('[name="CategoryName"]');

        input.classList.add('is-invalid');

        return;
    }

    // Hiển thị trạng thái Loading
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
                refreshCategoryTable('Category created successfully!');
            } else {
                showToastError(data.message || 'Validation failed. Please check your inputs.');
            }
        })
        .catch(err => showToastError('An error occurred: ' + err.message))
        .finally(() => toggleSaveButtonLoading(false));
}

// IV. GỬI DỮ LIỆU CHỈNH SỬA (AJAX POST JSON)
function submitEditForm() {
    const form = document.getElementById('editCategoryForm');
    if (!form) return;

    // Thu thập dữ liệu (Bao gồm thẻ ẩn chứa ID bản ghi)
    const payload = {
        CategoryID: parseInt(form.querySelector('[name="CategoryID"]').value),
        CategoryName: form.querySelector('[name="CategoryName"]').value,
        CategoryDescription: form.querySelector('[name="CategoryDescription"]').value,
        ParentCategoryID: form.querySelector('[name="ParentCategoryID"]').value ? parseInt(form.querySelector('[name="ParentCategoryID"]').value) : null,
        IsActive: form.querySelector('[name="IsActive"]').checked
    };

    const categoryName =
        form.querySelector('[name="CategoryName"]').value.trim();

    if (!categoryName) {

        const input =
            form.querySelector('[name="CategoryName"]');

        input.classList.add('is-invalid');

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
                refreshCategoryTable('Category updated successfully!');
            } else {
                showToastError(data.message || 'Failed to update category details.');
            }
        })
        .catch(err => showToastError('An error occurred: ' + err.message))
        .finally(() => toggleSaveButtonLoading(false));
}

// V. THAY ĐỔI NHANH TRẠNG THÁI ACTIVE BẰNG CÔNG TẮC (Revert nếu thất bại + Hiện Toast lỗi)
function toggleActive(id, checkbox) {
    // Lưu lại trạng thái trước khi bấm của checkbox đề phòng lỗi để khôi phục (Revert)
    const originalState = !checkbox.checked;

    fetch(`/Staff/Categories/ToggleActive/${id}`, {
        method: 'POST',
        headers: {
            'RequestVerificationToken': getAntiforgeryToken()
        }
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                // Đảm bảo đồng bộ chính xác giá trị IsActive mới mà Server trả về
                checkbox.checked = data.newIsActive;
            } else {
                // THẤT BẠI: Khôi phục lại trạng thái cũ của công tắc trên giao diện và báo lỗi
                checkbox.checked = originalState;
                showToastError(data.message || 'Failed to change category status.');
            }
        })
        .catch(err => {
            checkbox.checked = originalState;
            showToastError('Network error. Status change reverted.');
        });
}

// VI. XỬ LÝ XÓA DANH MỤC (Gọi confirmDelete từ Shared Modal)
function deleteCategory(id, name) {
    // 1. Cấu hình thông tin hiển thị lên Shared Confirm Delete Modal của hệ thống
    const deleteModalEl = document.getElementById('confirmDeleteModal');
    if (!deleteModalEl) return;

    // Gán dữ liệu tên danh mục vào thông điệp cảnh báo xóa
    const entityNamePlaceHolder = deleteModalEl.querySelector('.entity-name-placeholder');
    if (entityNamePlaceHolder) entityNamePlaceHolder.innerText = name;

    // 2. Định nghĩa hành động khi người dùng bấm nút "Confirm Delete" trên Modal
    const btnConfirmDelete = deleteModalEl.querySelector('.btn-confirm-delete');
    if (btnConfirmDelete) {
        btnConfirmDelete.onclick = function () {
            // Hiển thị trạng thái chờ trên nút xóa
            btnConfirmDelete.disabled = true;
            btnConfirmDelete.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Deleting...';

            // Gửi yêu cầu xóa AJAX POST kèm Token AntiForgery
            fetch(`/Staff/Categories/Delete/${id}`, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': getAntiforgeryToken()
                }
            })
                .then(res => res.json())
                .then(data => {
                    // Tắt modal xác nhận xóa
                    const modalInstance = bootstrap.Modal.getInstance(deleteModalEl);
                    if (modalInstance) modalInstance.hide();

                    if (data.success) {
                        refreshCategoryTable('Category deleted successfully!');
                    } else {
                        // Trả về lỗi vi phạm ràng buộc dữ liệu tin tức bài viết (FR-2 trong PRD)
                        showToastError(data.message || 'Cannot delete this category.');
                    }
                })
                .catch(err => showToastError('An error occurred during deletion.'))
                .finally(() => {
                    // Khôi phục lại trạng thái nút
                    btnConfirmDelete.disabled = false;
                    btnConfirmDelete.innerHTML = 'Delete';
                });
        };
    }

    // 3. Hiển thị Confirm Delete Modal lên màn hình
    const modal = bootstrap.Modal.getOrCreateInstance(deleteModalEl);
    modal.show();
}

// VII. LÀM MỚI BẢNG DỮ LIỆU SAU KHI THAY ĐỔI THÀNH CÔNG
function refreshCategoryTable(successMessage) {
    if (typeof window.refreshPageContentRealtime === 'function') {
        window.refreshPageContentRealtime();
    } else {
        location.reload();
    }
}

// ──────────────────────────────────────────────────────────────────────────
// CÁC HÀM TRỢ GIÚP GIAO DIỆN (HELPER FUNCTIONS)
// ──────────────────────────────────────────────────────────────────────────

// Tắt Modal Crud sau khi thực hiện tác vụ thành công
function closeCrudModal() {
    const modalEl = document.getElementById('categoryCrudModal');
    const modalInstance = bootstrap.Modal.getInstance(modalEl);
    if (modalInstance) modalInstance.hide();
}

// Bật tắt hiệu ứng xoay tròn Loading trên nút Lưu dữ liệu
function toggleSaveButtonLoading(isLoading) {
    const btnSave = document.getElementById('btnSaveCategory');
    const spinner = document.getElementById('saveSpinner');
    if (!btnSave || !spinner) return;

    if (isLoading) {
        btnSave.disabled = true;
        spinner.classList.remove('d-none');
    } else {
        btnSave.disabled = false;
        spinner.classList.add('d-none');
    }
}

// Hàm hiển thị thông báo Lỗi (Dùng Alert mặc định hoặc Toast tùy cấu hình dự án của bạn)
function showToastError(message) {
    // Nếu dự án có sử dụng thư viện Toast (như SweetAlert2 hoặc Toastr), bạn thay thế tại đây.
    // Dưới đây sử dụng Alert hệ thống tiêu chuẩn để luôn hoạt động độc lập ổn định:
    alert('⚠️ Cảnh báo hệ thống:\n' + message);
}

// Kiểm tra xem có thông báo thành công nào được lưu từ phiên làm việc trước không để hiển thị
document.addEventListener('DOMContentLoaded', () => {
    const pendingSuccessMsg = sessionStorage.getItem('Category_Toast_Success');
    if (pendingSuccessMsg) {
        // Bạn có thể đổi sang Toast thông báo xanh lá cây mượt mà tại đây
        console.log('Success:', pendingSuccessMsg);
        sessionStorage.removeItem('Category_Toast_Success');
    }
});