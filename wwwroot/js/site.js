// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Show the page notification toast (rendered by Views/Shared/_PageNotification.cshtml) on page load.
document.addEventListener('DOMContentLoaded', function () {
    // Bootstrap's bundle is loaded before this script; skip quietly if it is missing.
    if (typeof bootstrap === 'undefined' || !bootstrap.Toast) {
        return;
    }

    document.querySelectorAll('.toast').forEach(function (toastElement) {
        bootstrap.Toast.getOrCreateInstance(toastElement).show();
    });
});
