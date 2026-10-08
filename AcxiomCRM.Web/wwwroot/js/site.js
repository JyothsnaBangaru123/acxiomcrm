// AcxiomCRM Client-side scripts

document.addEventListener("DOMContentLoaded", () => {
    // Auto dismiss toasts/alerts after 6 seconds
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            if (bsAlert) bsAlert.close();
        }, 6000);
    });

    // Confirmation dialogs
    document.querySelectorAll('[data-confirm]').forEach(el => {
        el.addEventListener('click', (e) => {
            const msg = el.getAttribute('data-confirm') || 'Are you sure you want to perform this action?';
            if (!confirm(msg)) {
                e.preventDefault();
            }
        });
    });

    // Mobile sidebar toggle
    const toggleBtn = document.getElementById('sidebarToggle');
    const sidebar = document.querySelector('.app-sidebar');
    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener('click', () => {
            sidebar.classList.toggle('show');
        });
    }
});

// Quick fill test credentials for testing and evaluation
function fillCredentials(email, password) {
    const emailInput = document.getElementById('Email') || document.querySelector('input[name="Email"]');
    const passInput = document.getElementById('Password') || document.querySelector('input[name="Password"]');
    if (emailInput && passInput) {
        emailInput.value = email;
        passInput.value = password;
    }
}
