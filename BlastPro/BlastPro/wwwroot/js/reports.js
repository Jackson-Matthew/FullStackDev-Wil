document.addEventListener("DOMContentLoaded", function () {

    const printBtn =
        document.getElementById("printReportBtn");

    if (!printBtn) {
        return;
    }

    printBtn.addEventListener(
        "click",
        function () {
            window.print();
        }
    );

});
