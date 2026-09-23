document.addEventListener("DOMContentLoaded", function () {

    document.querySelectorAll(".password-toggle").forEach(function (toggle) {

        const input = document.getElementById(toggle.dataset.target);

        if (!input) {
            return;
        }

        toggle.addEventListener("click", function () {

            const isHidden = input.type === "password";

            input.type = isHidden ? "text" : "password";

            toggle.innerHTML = isHidden
                ? '<i class="bi bi-eye-slash"></i>'
                : '<i class="bi bi-eye"></i>';

            toggle.title = isHidden ? "Hide password" : "Show password";

        });

    });

});