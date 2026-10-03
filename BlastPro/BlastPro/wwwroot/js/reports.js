document.addEventListener("DOMContentLoaded", function () {

    // Print and Download PDF both open the print dialog; "Save as PDF" there gives the PDF copy.
    document.querySelectorAll("[data-report-print]").forEach(function (button) {
        button.addEventListener("click", function () {
            window.print();
        });
    });

});
