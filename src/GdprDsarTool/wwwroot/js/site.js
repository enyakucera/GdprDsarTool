// Custom JavaScript for GDPR DSAR Tool

document.addEventListener('DOMContentLoaded', function() {
    console.log('GDPR DSAR Tool initialized');
    
    // Auto-dismiss alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(function(alert) {
        setTimeout(function() {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });
});
