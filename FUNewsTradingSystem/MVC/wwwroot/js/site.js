/**
 * site.js — Global script for FUNewsTradingSystem Design System
 * Orchestrates premium transitions, ripple click effects, theme management, and micro-interactions.
 */

(function () {
    'use strict';

    // ─────────────────────────────────────────
    // 2. Ghim & Blur Navbar khi cuộn trang
    // ─────────────────────────────────────────
    function initNavbarScroll() {
        const navbar = document.querySelector('.navbar');
        if (!navbar) return;

        const handleScroll = () => {
            if (window.scrollY > 15) {
                navbar.classList.add('navbar-scrolled');
            } else {
                navbar.classList.remove('navbar-scrolled');
            }
        };

        window.addEventListener('scroll', handleScroll, { passive: true });
        handleScroll(); // Chạy ngay khi tải trang đề phòng đang ở giữa trang
    }

    // ─────────────────────────────────────────
    // 3. Hiệu ứng Gợn Sóng Vật Lý (Ripple Click Effect)
    // ─────────────────────────────────────────
    function initRippleEffect() {
        document.body.addEventListener('click', function (e) {
            const button = e.target.closest('.btn-primary, .btn-success, .btn-danger, .btn-warning, .btn-ripple');
            if (!button) return;

            // Tạo phần tử gợn sóng
            const ripple = document.createElement('span');
            ripple.className = 'ripple-wave';
            button.appendChild(ripple);

            // Đo kích thước
            const rect = button.getBoundingClientRect();
            const size = Math.max(rect.width, rect.height);
            
            ripple.style.width = ripple.style.height = size + 'px';
            ripple.style.left = e.clientX - rect.left - size / 2 + 'px';
            ripple.style.top = e.clientY - rect.top - size / 2 + 'px';

            // Xóa sau khi hoàn thành hoạt ảnh
            ripple.addEventListener('animationend', function () {
                ripple.remove();
            });
        });
    }

    // ─────────────────────────────────────────
    // 4. Gán animation cho bảng khi render (CSS nth-child xử lý stagger)
    // ─────────────────────────────────────────
    function initTableAnimation() {
        document.querySelectorAll('.table').forEach(table => {
            // Chỉ gán class, CSS tự xử lý stagger delay bằng nth-child
            table.querySelectorAll('tbody tr').forEach(row => {
                row.classList.add('table-row-animated');
            });
        });
    }

    // ─────────────────────────────────────────
    // 5. Accordion FAQ bóng đổ mượt mà
    // ─────────────────────────────────────────
    function initFaqTransitions() {
        const faqItems = document.querySelectorAll('.faq-item');
        faqItems.forEach(item => {
            const header = item.querySelector('.faq-header');
            if (!header) return;

            header.addEventListener('click', () => {
                const isOpen = item.classList.contains('open');
                
                // Đóng tất cả các FAQ khác
                faqItems.forEach(other => {
                    other.classList.remove('open');
                });

                if (!isOpen) {
                    item.classList.add('open');
                }
            });
        });
    }

    // ─────────────────────────────────────────
    // Khởi chạy toàn bộ hệ thống tương tác
    // ─────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', function () {
        initNavbarScroll();
        initRippleEffect();
        initTableAnimation();
        initFaqTransitions();
    });

    // Xuất API nội bộ để có thể tái kích hoạt hoạt ảnh khi tải AJAX
    window.PremiumApp = {
        refreshAnimations: function() {
            initTableAnimation();
        }
    };

})();