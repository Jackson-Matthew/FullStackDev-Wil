document.addEventListener("DOMContentLoaded", function () {

    const searchInput =
        document.getElementById("projectSearch");

    const statusFilter =
        document.getElementById("statusFilter");

    const projectRows =
        document.querySelectorAll(".project-row");

    const noSearchResults =
        document.getElementById("noSearchResults");

    if (!searchInput || !statusFilter) {
        return;
    }


    function filterProjects() {

        const searchValue =
            searchInput.value.toLowerCase().trim();

        const statusValue =
            statusFilter.value.toLowerCase();

        let visibleProjects = 0;


        projectRows.forEach(function (row) {

            const projectName =
                row.getAttribute("data-project-name");

            const projectStatus =
                row.getAttribute("data-project-status");


            const matchesSearch =
                projectName.includes(searchValue);


            const matchesStatus =
                statusValue === "all" ||
                projectStatus === statusValue;


            if (matchesSearch && matchesStatus) {

                row.style.display = "";

                visibleProjects++;

            }
            else {

                row.style.display = "none";

            }

        });


        if (noSearchResults) {

            if (visibleProjects === 0) {

                noSearchResults.style.display = "block";

            }
            else {

                noSearchResults.style.display = "none";

            }

        }

    }


    searchInput.addEventListener(
        "input",
        filterProjects
    );


    statusFilter.addEventListener(
        "change",
        filterProjects
    );

});