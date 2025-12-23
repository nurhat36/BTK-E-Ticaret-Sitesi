// Initialize page
document.addEventListener('DOMContentLoaded', function () {
    // Dark mode toggle
    const darkModeToggle = document.getElementById('darkModeToggle');
    const body = document.body;

    function updateDarkModeUI() {
        if (body.classList.contains('dark-mode')) {
            darkModeToggle.innerHTML = '<i class="fas fa-sun me-2"></i> Açık Mod';
            localStorage.setItem('darkMode', 'enabled');
        } else {
            darkModeToggle.innerHTML = '<i class="fas fa-moon me-2"></i> Koyu Mod';
            localStorage.setItem('darkMode', 'disabled');
        }
    }

    // Check for saved dark mode preference
    if (localStorage.getItem('darkMode') === 'enabled') {
        body.classList.add('dark-mode');
    }

    updateDarkModeUI();

    darkModeToggle.addEventListener('click', function () {
        body.classList.toggle('dark-mode');
        updateDarkModeUI();
    });

    // Load categories
    function loadCategories() {
        $.get('/Category/GetCategoryTree', function (data) {
            $('#categoriesMenu').html(renderCategoryTree(data));
            initCategoryTree();
        }).fail(function () {
            $('#categoriesMenu').html('<li><div class="dropdown-item text-danger">Kategoriler yüklenemedi</div></li>');
        });
    }

    // Render category tree
    function renderCategoryTree(categories) {
        if (!categories || categories.length === 0) {
            return '<li><div class="dropdown-item">Kategori bulunamadı</div></li>';
        }

        let html = '<ul class="category-tree">';
        // Backend'den base URL'yi alın (View'da)
        const baseUrl = '@Url.Content("~/")';

        categories.forEach(category => {
            html += `
                <li class="category-item">
                            <a href="${baseUrl}Urunler/category/${category.slug}" class="dropdown-item d-flex justify-content-between align-items-center">
                        ${category.name}
                        ${category.subCategories && category.subCategories.length > 0 ?
                    '<span class="toggle-subcategories"><i class="fas fa-chevron-right"></i></span>' : ''}
                    </a>
                    ${category.subCategories && category.subCategories.length > 0 ?
                    `<ul class="sub-categories">
                            ${renderCategoryTree(category.subCategories)}
                        </ul>` : ''}
                </li>`;
        });
        html += '</ul>';
        return html;
    }

    // Initialize category tree interactions
    function initCategoryTree() {
        $(document).on('click', '.toggle-subcategories', function (e) {
            e.preventDefault();
            e.stopPropagation();
            const icon = $(this).find('i');
            const subMenu = $(this).closest('.category-item').find('.sub-categories').first();

            subMenu.slideToggle(200);
            icon.toggleClass('fa-chevron-right fa-chevron-down');
        });
    }

    // Load categories when dropdown is shown
    $('#categoriesDropdown').on('shown.bs.dropdown', function () {
        if ($('#categoriesMenu').html().includes('Yükleniyor')) {
            loadCategories();
        }
    });

    // Toastr configuration
    toastr.options = {
        closeButton: true,
        progressBar: true,
        positionClass: "toast-bottom-right",
        showDuration: "300",
        hideDuration: "1000",
        timeOut: "5000",
        extendedTimeOut: "1000",
        showEasing: "swing",
        hideEasing: "linear",
        showMethod: "fadeIn",
        hideMethod: "fadeOut"
    };

    // Add animations to elements

    // --- DÜZENLEME BAŞLANGICI ---
    // AŞAĞIDAKİ ANİMASYON, SAYFA YÜKLENİRKEN HEADER'DA TİTREMEYE NEDEN OLDUĞU İÇİN KALDIRILDI.
    // gsap.from(".navbar", {
    //     duration: 0.8,
    //     y: -50,
    //     opacity: 0,
    //     ease: "power2.out",
    //     delay: 0.2
    // });
    // --- DÜZENLEME SONU ---

    gsap.from(".main-container", {
        duration: 0.8,
        y: 30,
        opacity: 0,
        ease: "back.out(1.2)",
        delay: 0.4
    });
});