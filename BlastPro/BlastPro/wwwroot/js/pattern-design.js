

document.addEventListener("DOMContentLoaded", function () {

    const tableBody =
        document.getElementById("holeTableBody");

    const rowTemplate =
        document.getElementById("holeRowTemplate");

    const selectAll =
        document.getElementById("selectAllHoles");

    const addHoleBtn =
        document.getElementById("addHoleBtn");

    const addTenHolesBtn =
        document.getElementById("addTenHolesBtn");

    const deleteSelectedBtn =
        document.getElementById("deleteSelectedBtn");

    const noHolesRow =
        document.getElementById("noHolesRow");

    const spacingInput =
        document.getElementById("Spacing");

    if (!tableBody || !rowTemplate) {
        return;
    }

    const DELAY_STEP = 25;


    function getRows() {
        return Array.from(tableBody.querySelectorAll(".hole-row"));
    }

    function getField(row, field) {
        return row.querySelector('[data-field="' + field + '"]');
    }

    function readNumber(row, field, fallback) {
        const input = getField(row, field);
        const value = input ? parseFloat(input.value) : NaN;
        return isNaN(value) ? fallback : value;
    }

    function setField(row, field, value) {
        const input = getField(row, field);
        if (input) {
            input.value = value;
        }
    }


    function reindexRows() {

        getRows().forEach(function (row, index) {

            row.querySelectorAll("[name]").forEach(function (field) {
                field.name = field.name.replace(/Holes\[[^\]]*\]/, "Holes[" + index + "]");
            });

            row.querySelector(".hole-number").textContent = index + 1;
            row.querySelector(".hole-number-input").value = index + 1;
            row.querySelector(".hole-select").setAttribute("aria-label", "Select hole " + (index + 1));

        });

        updateEmptyState();
        updateSelectAll();

    }

    function updateEmptyState() {
        if (noHolesRow) {
            noHolesRow.style.display = getRows().length === 0 ? "" : "none";
        }
    }


    function updateSelectAll() {

        if (!selectAll) {
            return;
        }

        const rows = getRows();
        const selectedCount = rows.filter(function (row) {
            return row.querySelector(".hole-select").checked;
        }).length;

        selectAll.checked = rows.length > 0 && selectedCount === rows.length;
        selectAll.indeterminate = selectedCount > 0 && selectedCount < rows.length;

    }

    tableBody.addEventListener("change", function (event) {

        if (event.target.classList.contains("hole-select")) {
            event.target.closest(".hole-row").classList.toggle("is-selected", event.target.checked);
            updateSelectAll();
        }

    });

    if (selectAll) {

        selectAll.addEventListener("change", function () {

            getRows().forEach(function (row) {
                row.querySelector(".hole-select").checked = selectAll.checked;
                row.classList.toggle("is-selected", selectAll.checked);
            });

        });

    }



    function addHole() {

        const rows = getRows();
        const lastRow = rows[rows.length - 1];

        const fragment = rowTemplate.content.cloneNode(true);
        const newRow = fragment.querySelector(".hole-row");

        if (lastRow) {

            const spacing =
                parseFloat(spacingInput ? spacingInput.value : "") || 3;

            setField(newRow, "X", (readNumber(lastRow, "X", 0) + spacing).toFixed(2));
            setField(newRow, "Y", readNumber(lastRow, "Y", 0).toFixed(2));
            setField(newRow, "Depth", readNumber(lastRow, "Depth", 12).toFixed(1));
            setField(newRow, "Charge", readNumber(lastRow, "Charge", 8.5).toFixed(1));
            setField(newRow, "Stemming", readNumber(lastRow, "Stemming", 3.5).toFixed(1));
            setField(newRow, "Delay", readNumber(lastRow, "Delay", 0) + DELAY_STEP);
            setField(newRow, "Explosive", getField(lastRow, "Explosive").value);

        }

        tableBody.insertBefore(fragment, noHolesRow);

    }

    if (addHoleBtn) {

        addHoleBtn.addEventListener("click", function () {
            addHole();
            reindexRows();
        });

    }

    if (addTenHolesBtn) {

        addTenHolesBtn.addEventListener("click", function () {
            for (let i = 0; i < 10; i++) {
                addHole();
            }
            reindexRows();
        });

    }


    if (deleteSelectedBtn) {

        deleteSelectedBtn.addEventListener("click", function () {

            const selectedRows = getRows().filter(function (row) {
                return row.querySelector(".hole-select").checked;
            });

            if (selectedRows.length === 0) {
                alert("Select at least one hole to delete.");
                return;
            }

            const message = selectedRows.length === 1
                ? "Delete the selected hole?"
                : "Delete the " + selectedRows.length + " selected holes?";

            if (!confirm(message)) {
                return;
            }

            selectedRows.forEach(function (row) {
                row.remove();
            });

            reindexRows();

        });

    }


    updateEmptyState();

});