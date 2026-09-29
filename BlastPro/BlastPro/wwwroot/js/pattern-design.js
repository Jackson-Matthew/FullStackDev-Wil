document.addEventListener("DOMContentLoaded", () => {
    const get = id => document.getElementById(id);
    const form = get("designForm"), body = get("holeTableBody"), template = get("holeRowTemplate"), plan = get("benchPlan");
    if (!form || !body || !template || !plan) return;
    const rows = () => [...body.querySelectorAll(".hole-row")];
    const field = (row, key) => row.querySelector('[data-field="' + key + '"]');
    const num = id => Number.parseFloat(get(id)?.value ?? "");
    const cell = (row, key) => Number.parseFloat(field(row, key)?.value ?? "");
    const ns = "http://www.w3.org/2000/svg";
    let pending = null, pendingTiming = null;
    let autoLayoutActive = false, lastLayoutSignature = "", geometryMode = "Automatic";
    let manualVibrationEdited = false;
    let autoTimingActive = false, manualTimingEdited = false, lastTimingSignature = "";
    let manualIntervalEdited = false;
    let manualAngleEdited = false, manualHeightEdited = false;
    let planView = "holes";
    let holeWasDeleted = false;
    let selectedHoleRow = null, planZoom = 1, planCenterX = 500, planCenterY = 280;
    let planDrag = null;
    const say = message => { get("patternNotice").textContent = message; };

    function reindex() {
        rows().forEach((row, i) => {
            row.querySelectorAll("[name]").forEach(input => { input.name = input.name.replace(/Holes\[[^\]]*\]/, "Holes[" + i + "]"); });
            row.querySelector(".hole-number").textContent = i + 1;
            row.querySelector(".hole-number-input").value = i + 1;
            row.querySelector(".hole-select").setAttribute("aria-label", "Select hole " + (i + 1));
        });
        get("noHolesRow").style.display = rows().length ? "none" : "";
        selectState();
        syncEstimates();
        syncLoading();
        syncEstimates();
        autoTiming();
        update();
    }

    function selectState() {
        const selected = rows().filter(row => row.querySelector(".hole-select").checked).length;
        get("selectAllHoles").checked = !!rows().length && selected === rows().length;
        get("selectAllHoles").indeterminate = selected > 0 && selected < rows().length;
    }

    function add(position, depth, generated = false) {
        const clone = template.content.cloneNode(true);
        const row = clone.querySelector(".hole-row");
        row.dataset.generated = generated ? "true" : "false";
        field(row, "X").value = position.x.toFixed(2);
        field(row, "Y").value = position.y.toFixed(2);
        if (depth > 0) field(row, "Depth").value = depth.toFixed(2);
        field(row, "Charge").value = "0";
        field(row, "Stemming").value = "0";
        field(row, "Delay").value = "0";
        body.insertBefore(clone, get("noHolesRow"));
    }

    function proposal() {
        const length = num("BenchLengthMetres"), width = num("BenchWidthMetres");
        const spacing = num("Spacing"), burden = num("Burden");
        const rowCount = num("LayoutRows"), columnCount = num("LayoutColumns");
        if (!(length > 0 && width > 0))
            return { points: [], error: "Enter the bench length and width to start." };
        if (length > 10000 || width > 10000)
            return { points: [], error: "Bench dimensions must be 10,000 m or less." };
        if (!Number.isInteger(rowCount) || !Number.isInteger(columnCount) || rowCount < 1 || columnCount < 1)
            return { points: [], error: "Enter whole-number rows and holes per row from the site plan." };
        if (rowCount * columnCount > 500)
            return { points: [], error: "This layout exceeds 500 holes. Reduce rows or holes per row." };
        if (!(spacing > 0 && burden > 0))
            return { points: [], error: "Burden and spacing will appear after the bench and counts are entered." };
        const points = [], staggered = get("PatternType").value === "Staggered";
        for (let row = 0; row < rowCount; row++) {
            const y = burden * (row + 0.5);
            for (let column = 0; column < columnCount; column++) {
                const offset = staggered ? (row % 2 ? spacing / 4 : -spacing / 4) : 0;
                const x = spacing * (column + 0.5) + offset;
                if (x < 0 || x > length || y < 0 || y > width)
                    return { points: [], error: "The edited pitch places holes outside the bench. Adjust the counts or pitch." };
                points.push({ x, y });
            }
        }
        return { points, error: "" };
    }

    function initialGeometry() {
        const length = num("BenchLengthMetres"), width = num("BenchWidthMetres");
        const burden = num("Burden"), spacing = num("Spacing");
        if (!(length > 0 && width > 0 && burden > 0 && spacing > 0)) return;
        const rowCount = num("LayoutRows") || Math.round(width / burden);
        const columnCount = num("LayoutColumns") || Math.round(length / spacing);
        if (rowCount < 1 || columnCount < 1 || rowCount * columnCount > 500) return;
        get("LayoutRows").value = String(rowCount);
        get("LayoutColumns").value = String(columnCount);
        if (Math.abs(width / rowCount - burden) > 0.0001 || Math.abs(length / columnCount - spacing) > 0.0001)
            geometryMode = "Manual";
    }

    function syncGeometry() {
        const length = num("BenchLengthMetres"), width = num("BenchWidthMetres");
        const rowCount = num("LayoutRows"), columnCount = num("LayoutColumns");
        const ready = length > 0 && width > 0 && Number.isInteger(rowCount) && Number.isInteger(columnCount) &&
            rowCount > 0 && columnCount > 0 && rowCount * columnCount <= 500;
        const pitchFits = ready && width / rowCount >= 0.0001 && length / columnCount >= 0.0001;
        if (geometryMode === "Automatic" && ready) {
            get("Burden").value = pitchFits ? (width / rowCount).toFixed(4) : "";
            get("Spacing").value = pitchFits ? (length / columnCount).toFixed(4) : "";
        }
        get("geometryStatus").textContent = !pitchFits && ready
            ? "This bench is too small for the selected counts at the saved pitch precision. Reduce the counts."
            : geometryMode === "Manual"
            ? "Custom pitch is in use. The table and plan follow your values. Select ‘Use bench geometry’ to calculate them again."
            : (ready ? rowCount * columnCount + " positions. " : "") +
                "Geometric pitch: width ÷ rows and length ÷ holes per row. This is a layout calculation, not a site-approved drilling rule.";
    }

    function layoutSignature() {
        return ["BenchLengthMetres", "BenchWidthMetres", "LayoutRows", "LayoutColumns", "PatternType",
            "NewHoleDepth", "Burden", "Spacing"].map(id => get(id).value).join("|");
    }

    function autoPlace() {
        const design = proposal(), depth = num("NewHoleDepth");
        if (design.error || !(depth > 0)) { update(); return; }
        const signature = layoutSignature();
        if (signature === lastLayoutSignature) { update(); return; }
        if (rows().length && !autoLayoutActive) { update(); return; }
        if (rows().length === design.points.length && autoLayoutActive) {
            rows().forEach((row, index) => {
                field(row, "X").value = design.points[index].x.toFixed(2);
                field(row, "Y").value = design.points[index].y.toFixed(2);
                field(row, "Depth").value = depth.toFixed(2);
            });
            lastLayoutSignature = signature;
            syncEstimates();
            syncLoading();
            syncEstimates();
            autoTiming();
            update();
            return;
        }
        if (rows().length && rows().some(row => ["diameterOverride", "subdrillOverride", "stemmingOverride", "chargeOverride", "densityOverride",
                "productOverride"].some(key => row.dataset[key] === "true") || manualTimingEdited)) {
            autoLayoutActive = false;
            update();
            return;
        }
        rows().forEach(row => row.remove());
        design.points.forEach(point => add(point, depth, true));
        autoLayoutActive = true;
        lastLayoutSignature = signature;
        reindex();
    }

    function initialLayout() {
        const design = proposal(), depth = num("NewHoleDepth"), all = rows();
        if (design.error || !all.length || all.length !== design.points.length || !(depth > 0)) return;
        if (all.every((row, index) => Math.abs(cell(row, "X") - design.points[index].x) < 0.02 &&
            Math.abs(cell(row, "Y") - design.points[index].y) < 0.02 && Math.abs(cell(row, "Depth") - depth) < 0.02)) {
            autoLayoutActive = true;
            lastLayoutSignature = layoutSignature();
        }
    }

    function calculatedCharge(row) {
        const diameter = cell(row, "DiameterMillimetres"), depth = cell(row, "Depth");
        const stemming = cell(row, "Stemming");
        const density = cell(row, "ProductDensityGramsPerCc") > 0
            ? cell(row, "ProductDensityGramsPerCc")
            : field(row, "AeciProductCode").value ? NaN : num("LegacyLoadingDensityGramsPerCc");
        if (!(diameter > 0 && depth > 0 && stemming > 0 && stemming < depth && density > 0)) return null;
        const radiusMetres = diameter / 2000;
        return Math.PI * radiusMetres * radiusMetres * (depth - stemming) * density * 1000;
    }

    function estimatedDiameter() {
        const burden = num("Burden");
        return burden > 0 ? burden * 1000 / 30 : null;
    }

    function estimatedStemming(row) {
        const burden = num("Burden"), depth = cell(row, "Depth");
        return burden > 0 && depth > 0 && burden * 0.7 < depth ? burden * 0.7 : null;
    }

    function estimatedSubdrill(row) {
        const diameter = cell(row, "DiameterMillimetres"), depth = cell(row, "Depth");
        return diameter > 0 && depth > 0 && diameter / 100 < depth ? diameter / 100 : null;
    }

    function catalogueDensity(row) {
        const option = field(row, "AeciProductCode").selectedOptions[0];
        const low = Number.parseFloat(option?.dataset.densityMin ?? "");
        const high = Number.parseFloat(option?.dataset.densityMax ?? "");
        return low > 0 && high > 0 ? (low + high) / 2 : null;
    }

    function initialLoading() {
        const diameter = estimatedDiameter();
        const defaultProduct = get("DefaultAeciProductCode").value;
        rows().forEach(row => {
            row.dataset.diameterOverride = cell(row, "DiameterMillimetres") > 0 &&
                (!(diameter > 0) || Math.abs(cell(row, "DiameterMillimetres") - diameter) > 0.15) ? "true" : "false";
            const subdrill = estimatedSubdrill(row);
            row.dataset.subdrillOverride = cell(row, "SubdrillMetres") >= 0 &&
                (subdrill === null || Math.abs(cell(row, "SubdrillMetres") - subdrill) > 0.015) ? "true" : "false";
            const stemming = estimatedStemming(row);
            row.dataset.stemmingOverride = cell(row, "Stemming") > 0 &&
                (stemming === null || Math.abs(cell(row, "Stemming") - stemming) > 0.015) ? "true" : "false";
            const estimatedDensity = catalogueDensity(row);
            row.dataset.densityOverride = cell(row, "ProductDensityGramsPerCc") > 0 &&
                (estimatedDensity === null ||
                    Math.abs(cell(row, "ProductDensityGramsPerCc") - estimatedDensity) > 0.001) ? "true" : "false";
            const calculated = calculatedCharge(row);
            row.dataset.chargeOverride = cell(row, "Charge") > 0 &&
                (calculated === null || Math.abs(cell(row, "Charge") - calculated) > 0.15) ? "true" : "false";
            row.dataset.productOverride = field(row, "AeciProductCode").value &&
                field(row, "AeciProductCode").value !== defaultProduct ? "true" : "false";
        });
    }

    function syncLoading() {
        const diameter = estimatedDiameter();
        const defaultProduct = get("DefaultAeciProductCode").value;
        let calculated = 0, manual = 0;
        rows().forEach(row => {
            if (diameter > 0 && diameter <= 1000 && row.dataset.diameterOverride !== "true")
                field(row, "DiameterMillimetres").value = diameter.toFixed(1);
            const subdrill = estimatedSubdrill(row);
            if (subdrill !== null && row.dataset.subdrillOverride !== "true")
                field(row, "SubdrillMetres").value = subdrill.toFixed(2);
            const stemming = estimatedStemming(row);
            if (stemming !== null && row.dataset.stemmingOverride !== "true")
                field(row, "Stemming").value = stemming.toFixed(2);
            if (defaultProduct && row.dataset.productOverride !== "true")
                field(row, "AeciProductCode").value = defaultProduct;
            if (field(row, "AeciProductCode").value)
                field(row, "ExplosiveProductId").value = "";
            const density = catalogueDensity(row);
            if (density !== null && row.dataset.densityOverride !== "true")
                field(row, "ProductDensityGramsPerCc").value = density.toFixed(3);
            const charge = calculatedCharge(row);
            if (row.dataset.chargeOverride === "true") manual++;
            else if (charge !== null) {
                field(row, "Charge").value = charge.toFixed(2);
                calculated++;
            } else if (cell(row, "Charge") > 0) {
                field(row, "Charge").value = "0";
            }
            field(row, "Charge").title = row.dataset.chargeOverride === "true" ?
                "Manual charge for this hole" : charge !== null ?
                    "Planning estimate from this hole's diameter, loaded length and selected product density" :
                    "Select an AECI product or enter this hole's measured density to calculate charge";
        });
        const needed = [];
        if (!rows().some(row => cell(row, "DiameterMillimetres") > 0)) needed.push("a valid burden for diameter");
        if (!rows().some(row => cell(row, "Stemming") > 0)) needed.push("depth greater than estimated stemming");
        if (rows().some(row => !(cell(row, "ProductDensityGramsPerCc") > 0) &&
            !(field(row, "AeciProductCode").value === "" && num("LegacyLoadingDensityGramsPerCc") > 0)))
            needed.push("product density for each hole");
        get("chargeFormulaStatus").textContent = needed.length
            ? "Charge estimate needs " + needed.join(", ") + "."
            : "Estimated charge in " + calculated + " hole(s): cylindrical area × (depth − stemming) × product density. " +
                (manual ? manual + " edited charge(s) retained. " : "") + "Check actual loading, decking and in-hole density.";
    }

    function syncEstimates() {
        const burden = num("Burden");
        const interval = burden > 0 ? Math.max(1, Math.round(2 * burden * 3.28084)) : null;
        if (interval !== null && !manualIntervalEdited) get("TimingStepMs").value = String(interval);
        if (!manualAngleEdited && rows().length) get("FlyrockLaunchAngleDegrees").value = "45";
        const heights = rows().map(row => cell(row, "Depth") - cell(row, "SubdrillMetres"));
        if (!manualHeightEdited) {
            if (heights.length && heights.every(height => height > 0))
                get("FlyrockLaunchHeightMetres").value = Math.max(...heights).toFixed(2);
            else if (!heights.length)
                get("FlyrockLaunchHeightMetres").value = "";
        }
    }

    function svg(tag, attributes, parent = plan) {
        const node = document.createElementNS(ns, tag);
        Object.entries(attributes).forEach(([key, val]) => node.setAttribute(key, String(val)));
        parent.appendChild(node);
        return node;
    }

    function applyPlanViewBox() {
        const width = 1000 / planZoom, height = 560 / planZoom;
        planCenterX = Math.max(width / 2, Math.min(1000 - width / 2, planCenterX));
        planCenterY = Math.max(height / 2, Math.min(560 - height / 2, planCenterY));
        plan.setAttribute("viewBox", [planCenterX - width / 2, planCenterY - height / 2, width, height].join(" "));
        get("planZoomLabel").textContent = Math.round(planZoom * 100) + "%";
        get("planZoomOut").disabled = planZoom <= 1;
        get("planZoomIn").disabled = planZoom >= 4;
    }

    function zoomPlan(factor, clientX, clientY) {
        const next = Math.max(1, Math.min(4, planZoom * factor));
        if (next === planZoom) return;
        if (Number.isFinite(clientX) && Number.isFinite(clientY)) {
            const rect = plan.getBoundingClientRect();
            const box = plan.viewBox.baseVal;
            const x = box.x + (clientX - rect.left) / rect.width * box.width;
            const y = box.y + (clientY - rect.top) / rect.height * box.height;
            planCenterX = x + (planCenterX - x) * planZoom / next;
            planCenterY = y + (planCenterY - y) * planZoom / next;
        }
        planZoom = next;
        applyPlanViewBox();
    }

    function syncPlanEditor() {
        const editor = get("planHoleEditor");
        if (selectedHoleRow && !selectedHoleRow.isConnected) selectedHoleRow = null;
        editor.hidden = !selectedHoleRow;
        if (!selectedHoleRow) return;
        get("planHoleTitle").textContent = "Hole " + selectedHoleRow.querySelector(".hole-number").textContent;
        get("planDeleteHole").textContent = "Delete hole " + selectedHoleRow.querySelector(".hole-number").textContent;
        editor.querySelectorAll("[data-plan-field]").forEach(input => {
            if (input !== document.activeElement)
                input.value = field(selectedHoleRow, input.dataset.planField).value;
        });
    }

    function draw() {
        plan.replaceChildren();
        applyPlanViewBox();
        const length = num("BenchLengthMetres"), width = num("BenchWidthMetres");
        if (!(length > 0 && width > 0)) {
            svg("text", { x: 500, y: 280, "text-anchor": "middle", class: "plan-empty" }).textContent = "Enter bench dimensions to view the plan";
            get("planDetail").textContent = "Enter the bench length and width to draw the planning boundary.";
            syncPlanEditor();
            return;
        }
        const scale = Math.min(900 / length, 450 / width), w = length * scale, h = width * scale;
        const left = (1000 - w) / 2, top = (520 - h) / 2;
        svg("rect", { x: left, y: top, width: w, height: h, class: "plan-boundary" });
        for (let i = 1; i < 10; i++) {
            svg("line", { x1: left + w * i / 10, y1: top, x2: left + w * i / 10, y2: top + h, class: "plan-grid" });
            svg("line", { x1: left, y1: top + h * i / 10, x2: left + w, y2: top + h * i / 10, class: "plan-grid" });
        }
        svg("text", { x: 500, y: 548, "text-anchor": "middle", class: "plan-axis" }).textContent = "X / length: " + length.toFixed(2) + " m";
        svg("text", { x: 12, y: 24, class: "plan-axis" }).textContent = "Y / width: " + width.toFixed(2) + " m";
        const dot = (x, y) => ({ cx: left + x * scale, cy: top + h - y * scale });
        if (planView === "holes" && !rows().length && !holeWasDeleted)
            proposal().points.forEach(p => svg("circle", { ...dot(p.x, p.y), r: 5, class: "plan-proposed" }));
        const showTimingLabels = planView === "timing" && rows().length <= 45;
        rows().forEach((row, index) => {
            const x = cell(row, "X"), y = cell(row, "Y");
            if (!Number.isFinite(x) || !Number.isFinite(y)) return;
            const outside = x < 0 || x > length || y < 0 || y > width;
            const group = svg("g", { "data-hole-index": index, tabindex: 0, role: "button",
                class: row === selectedHoleRow ? "plan-hole is-active" : "plan-hole",
                "aria-label": "Edit hole " + row.querySelector(".hole-number").textContent });
            const circle = svg("circle", { ...dot(x, y), r: showTimingLabels ? 15 : 7,
                class: outside ? "plan-outside" : planView === "timing" ? "plan-timing-halo" : "plan-saved" }, group);
            svg("circle", { ...dot(x, y), r: 20, class: "plan-hit" }, group);
            const delay = cell(row, "Delay");
            svg("title", {}, circle).textContent = "Hole " + row.querySelector(".hole-number").textContent +
                ": X " + x.toFixed(2) + " m, Y " + y.toFixed(2) + " m, delay " +
                (Number.isFinite(delay) ? delay + " ms" : "not set");
            if (showTimingLabels && !outside)
                svg("text", { ...{ x: dot(x, y).cx, y: dot(x, y).cy + 6 }, "text-anchor": "middle",
                    class: "plan-timing-label" }, group).textContent = Number.isFinite(delay) ? String(delay) : "—";
        });
        get("planDetail").textContent = planView === "timing"
            ? rows().length > 45 ? "Timing labels are hidden above 45 holes. Hover over a hole for its delay." :
                "Numbers inside holes show delay in milliseconds. Timing is a draft sequence based on the entered interval."
            : proposal().error || (rows().length ? rows().length + " editable holes on the bench. X runs along its length; Y across its width." :
                "Complete the bench dimensions, counts and depth to place holes.");
        syncPlanEditor();
    }

    function maxCharge(windowMs) {
        if (!(windowMs > 0) || !rows().length) return null;
        const holes = rows().map(row => ({ delay: cell(row, "Delay"), charge: cell(row, "Charge") }))
            .filter(h => Number.isFinite(h.delay) && Number.isFinite(h.charge) && h.delay >= 0 && h.charge >= 0)
            .sort((a, b) => a.delay - b.delay);
        let left = 0, current = 0, maximum = 0;
        for (let right = 0; right < holes.length; right++) {
            current += holes[right].charge;
            while (holes[right].delay - holes[left].delay >= windowMs) current -= holes[left++].charge;
            maximum = Math.max(maximum, current);
        }
        return maximum;
    }

    function usbmSuggestion() {
        const type = get("ReceptorStructureType").value, frequency = num("DominantFrequencyHz");
        if (!["ResidentialPlaster", "ResidentialDrywall"].includes(type) || !(frequency > 0 && frequency <= 100)) return null;
        const plateau = type === "ResidentialPlaster" ? 0.5 : 0.75;
        const inchesPerSecond = Math.min(2, Math.min(2 * Math.PI * frequency * 0.03,
            Math.max(plateau, 2 * Math.PI * frequency * 0.008)));
        return (inchesPerSecond * 25.4).toFixed(2);
    }

    function refreshVibrationGuidance() {
        const suggestion = usbmSuggestion();
        if (suggestion !== null && !manualVibrationEdited &&
            get("VibrationThresholdMode").value === "Manual" && !(num("VibrationThreshold") > 0))
            get("VibrationThresholdMode").value = "Automatic";
        if (get("VibrationThresholdMode").value === "Automatic") {
            if (suggestion === null) {
                get("VibrationThresholdMode").value = "Manual";
                get("VibrationThreshold").value = "";
            } else {
                get("VibrationThreshold").value = suggestion;
            }
        }
        get("vibrationModeStatus").textContent = get("VibrationThresholdMode").value === "Automatic"
            ? "USBM residential suggestion; review local/site limit."
            : suggestion === null ? "Manual site limit required." : "Manual override. USBM suggestion: " + suggestion + " mm/s.";
        update();
    }

    function update() {
        draw();
        get("generatePatternBtn").hidden = autoLayoutActive && rows().length > 0 &&
            layoutSignature() === lastLayoutSignature;
        get("applyTimingBtn").hidden = autoTimingActive && rows().length > 0 &&
            timingSignature() === lastTimingSignature;
        const layoutNeeds = [];
        if (!(num("BenchLengthMetres") > 0 && num("BenchWidthMetres") > 0)) layoutNeeds.push("bench length and width");
        if (!(Number.isInteger(num("LayoutRows")) && num("LayoutRows") > 0 &&
            Number.isInteger(num("LayoutColumns")) && num("LayoutColumns") > 0)) layoutNeeds.push("row and hole counts");
        if (!(num("NewHoleDepth") > 0) && !rows().length) layoutNeeds.push("drilled depth");
        const unloaded = rows().filter(row => !(cell(row, "Charge") > 0 && cell(row, "Stemming") > 0)).length;
        const readiness = get("designReadiness");
        const siteNeeds = [];
        if (!(num("RockDensity") > 0)) siteNeeds.push("rock density");
        if (!(num("VibrationThreshold") > 0)) siteNeeds.push("vibration limit");
        if (!(num("ExclusionRadiusMetres") > 0)) siteNeeds.push("approved exclusion radius");
        const screeningNeeds = [];
        if (rows().some(row => !(cell(row, "SubdrillMetres") >= 0))) screeningNeeds.push("hole subdrill for volume");
        if (!(num("ReceptorDistanceMetres") > 0 && num("PpvSiteCoefficient") > 0 &&
            num("PpvDecayExponent") > 0)) screeningNeeds.push("distance and site K/n for PPV");
        if (!(num("FlyrockLaunchSpeedMetresPerSecond") > 0 &&
            num("FlyrockLaunchAngleDegrees") >= 0 && num("FlyrockLaunchHeightMetres") >= 0))
            screeningNeeds.push("site launch assumptions for the trajectory");
        const ready = !layoutNeeds.length && !unloaded && num("DelayWindowMilliseconds") > 0 && !siteNeeds.length;
        readiness.classList.toggle("is-ready", ready);
        readiness.textContent = layoutNeeds.length
            ? "1 · Layout — enter " + layoutNeeds.join(", ") + ". Holes will appear in the plan and table as soon as these are complete."
            : unloaded ? "2 · Loading — " + rows().length + " holes placed. " + unloaded +
                " still need positive stemming and charge. Check the product density and edit any hole in the table."
            : !(num("DelayWindowMilliseconds") > 0) ? "3 · Timing — enter the charge grouping window to calculate grouped charge and PPV."
            : !ready ? "4 · Site check — enter " + siteNeeds.join(", ") + " before calculating."
            : screeningNeeds.length ? "Ready for review · Minimum calculation inputs are populated. For the remaining live estimates, enter " +
                screeningNeeds.join(", ") + "." :
                "Ready for review · Live estimates are populated. Confirm site assumptions, each hole and the timing plan before calculation.";
        const chosen = get("DefaultAeciProductCode").selectedOptions[0];
        const densityRange = chosen?.dataset.densityMin && chosen?.dataset.densityMax
            ? chosen.dataset.densityMin + "–" + chosen.dataset.densityMax + " g/cm³" : "site value required";
        const energyRange = chosen?.dataset.energyMin && chosen?.dataset.energyMax
            ? chosen.dataset.energyMin + "–" + chosen.dataset.energyMax + " MJ/kg" : "not listed";
        get("productReferenceInfo").textContent = chosen?.value
            ? chosen.textContent.trim() + " · " + chosen.dataset.application +
                " · catalogue in-hole density " + densityRange +
                " · ideal delivered energy " + energyRange +
                (chosen.dataset.minDiameter ? " · brochure minimum diameter " +
                    chosen.dataset.minDiameter + " mm." : ".") +
                " Density varies with depth and loading; edit each hole's value as needed."
            : "Choose the AECI product that will be loaded. Product properties are taken from the supplied brochure.";
        const diameterWarnings = rows().filter(row => {
            const minimum = Number.parseFloat(field(row, "AeciProductCode").selectedOptions[0]?.dataset.minDiameter ?? "");
            return minimum > 0 && cell(row, "DiameterMillimetres") > 0 &&
                cell(row, "DiameterMillimetres") < minimum;
        });
        if (diameterWarnings.length)
            get("productReferenceInfo").textContent += " " + diameterWarnings.length +
                " hole(s) are below their selected product's brochure minimum diameter; review those holes.";
        const length = num("BenchLengthMetres"), width = num("BenchWidthMetres");
        const burden = num("Burden"), spacing = num("Spacing");
        const depths = rows().map(row => cell(row, "Depth"));
        const volume = rows().length && burden > 0 && spacing > 0 &&
            rows().every(row => cell(row, "SubdrillMetres") >= 0 && cell(row, "Depth") > cell(row, "SubdrillMetres"))
            ? rows().reduce((sum, row) => sum + burden * spacing *
                (cell(row, "Depth") - cell(row, "SubdrillMetres")), 0) : null;
        const drilling = depths.length && depths.every(depth => Number.isFinite(depth) && depth > 0)
            ? depths.reduce((sum, depth) => sum + depth, 0) : null;
        const charge = rows().reduce((sum, row) => sum + (cell(row, "Charge") || 0), 0);
        const stemming = rows().reduce((sum, row) => sum + (cell(row, "Stemming") || 0), 0);
        const delays = rows().map(row => cell(row, "Delay"));
        const delaySpan = delays.length && delays.every(delay => Number.isFinite(delay) && delay >= 0)
            ? Math.max(...delays) - Math.min(...delays) : null;
        const maximum = maxCharge(num("DelayWindowMilliseconds"));
        const distance = num("ReceptorDistanceMetres"), k = num("PpvSiteCoefficient"), n = num("PpvDecayExponent");
        const ppv = maximum > 0 && distance > 0 && k > 0 && n > 0 ? k * Math.pow(distance / Math.sqrt(maximum), -n) : null;
        const limit = num("VibrationThreshold"), density = num("RockDensity");
        const speed = num("FlyrockLaunchSpeedMetresPerSecond");
        const angle = num("FlyrockLaunchAngleDegrees");
        const launchHeight = num("FlyrockLaunchHeightMetres");
        const radians = angle * Math.PI / 180;
        const verticalSpeed = speed * Math.sin(radians);
        const flightTime = rows().length && charge > 0 && speed > 0 && angle >= 0 && angle <= 90 && launchHeight >= 0
            ? (verticalSpeed + Math.sqrt(verticalSpeed * verticalSpeed + 2 * 9.80665 * launchHeight)) / 9.80665 : null;
        const flyrock = flightTime === null ? null : speed * Math.cos(radians) * flightTime;
        const exclusion = num("ExclusionRadiusMetres");
        const tonnage = volume !== null && density > 0 ? volume * density : null;
        const metric = (label, value, detail, warning = false) => {
            const node = document.createElement("div");
            node.className = "metric" + (value === null ? " is-waiting" : "") + (warning ? " is-warning" : "");
            for (const [className, text] of [["metric-label", label], ["metric-value", value ?? "Waiting for input"], ["metric-detail", detail]]) {
                const part = document.createElement("span");
                part.className = className;
                part.textContent = text;
                node.appendChild(part);
            }
            return node;
        };
        const ppvNeeds = [];
        if (!(maximum > 0) || unloaded) ppvNeeds.push("charged holes + delay window");
        if (!(distance > 0)) ppvNeeds.push("receptor distance");
        if (!(k > 0 && n > 0)) ppvNeeds.push("site K and n");
        const flyNeeds = [];
        if (!rows().length || !(charge > 0)) flyNeeds.push("charged holes");
        if (!(speed > 0)) flyNeeds.push("launch speed");
        if (!(angle >= 0 && angle <= 90)) flyNeeds.push("angle");
        if (!(launchHeight >= 0)) flyNeeds.push("launch height");
        const items = [
            metric("Holes on bench", rows().length ? String(rows().length) : null,
                rows().length ? proposal().points.length + " geometric positions; edit holes below" : "Enter bench size, counts and depth"),
            metric("Bench area", length > 0 && width > 0 ? (length * width).toFixed(1) + " m²" : null,
                "Length × width"),
            metric("Geometric burden", burden > 0 ? burden.toFixed(2) + " m" : null,
                geometryMode === "Manual" ? "Edited row pitch" : "Width ÷ rows; editable"),
            metric("Geometric spacing", spacing > 0 ? spacing.toFixed(2) + " m" : null,
                geometryMode === "Manual" ? "Edited column pitch" : "Length ÷ holes per row; editable"),
            metric("Total drilling", drilling === null ? null : drilling.toFixed(1) + " m",
                "Sum of entered hole depths"),
            metric("Rock volume", volume === null ? null : volume.toFixed(1) + " m³",
                volume === null ? "Needs holes, pitch and per-hole subdrill" : "Sum of burden × spacing × (depth − subdrill)"),
            metric("Rock tonnage", tonnage === null ? null : tonnage.toFixed(1) + " t",
                tonnage === null ? "Needs rock volume + density" : "Volume × measured rock density"),
            metric("Charge total", rows().length && charge > 0 ? charge.toFixed(1) + " kg" : null,
                unloaded ? unloaded + " hole(s) need charge or stemming" : "Sum of hole charges"),
            metric("Delay span", delaySpan !== null && num("TimingStepMs") > 0 ? delaySpan.toFixed(0) + " ms" : null,
                "From first to last hole delay"),
            metric("Powder factor", tonnage > 0 && !unloaded ? (charge / tonnage).toFixed(3) + " kg/t" : null,
                "Total charge ÷ rock tonnage"),
            metric("Max charge in delay window", maximum !== null && !unloaded ? maximum.toFixed(1) + " kg" : null,
                maximum !== null && !unloaded ? "Largest sum of charges in the site window" : "Needs loaded holes + approved delay window"),
            metric("Predicted PPV", ppvNeeds.length ? null : ppv.toFixed(2) + " mm/s",
                ppvNeeds.length ? "Needs " + ppvNeeds.join(", ") : "K × (distance ÷ √max charge)^−n",
                ppvNeeds.length === 0 && limit > 0 && ppv > limit),
            metric("Vibration limit", limit > 0 ? limit.toFixed(2) + " mm/s" : null,
                get("VibrationThresholdMode").value === "Automatic" ? "USBM residential suggestion; editable" : "Site limit; editable"),
            metric("Idealized flyrock trajectory", flyNeeds.length ? null : flyrock.toFixed(1) + " m",
                flyNeeds.length ? "Needs site " + flyNeeds.join(", ") : "Ballistic trajectory only; not an exclusion radius"),
            metric("Site exclusion radius", exclusion > 0 ? exclusion.toFixed(1) + " m" : null,
                "Enter approved site distance; trajectory does not set this value",
                exclusion > 0 && flyrock !== null && flyrock > exclusion)
        ];
        get("liveDesignSummary").replaceChildren(...items);
        const outside = rows().filter(row => cell(row, "X") < 0 || cell(row, "X") > length || cell(row, "Y") < 0 || cell(row, "Y") > width).length;
        const warnings = [proposal().error, !(num("NewHoleDepth") > 0) && !rows().length && "Enter drilled depth to place holes in the table.",
            outside && "Check " + outside + " hole position(s) outside the planning boundary.",
            ppv !== null && limit > 0 && ppv > limit && "Predicted PPV exceeds the entered limit.",
            flyrock !== null && "Flyrock is an idealized trajectory, not a safe exclusion distance.",
            flyrock !== null && exclusion > 0 && flyrock > exclusion && "Idealized range exceeds the entered exclusion radius."].filter(Boolean);
        if (get("replacePatternPrompt").hidden)
            say(warnings.join(" ") || (rows().length && !autoLayoutActive
                ? "Layout preview updated. Your edited holes are kept until you apply this layout."
                : rows().length + " holes are in the plan and table. Edit or remove any hole as needed."));
    }

    function apply() {
        if (!pending) return;
        rows().forEach(row => row.remove());
        pending.points.forEach(point => add(point, pending.depth, true));
        autoLayoutActive = true;
        lastLayoutSignature = layoutSignature();
        autoTimingActive = false;
        manualTimingEdited = false;
        lastTimingSignature = "";
        pending = null;
        get("replacePatternPrompt").hidden = true;
        reindex();
        say("Holes placed in the plan and table with editable loading and delay estimates. Review each value against the site plan.");
    }

    function timingRowIndex(row) {
        const burden = num("Burden");
        return burden > 0 ? Math.round(cell(row, "Y") / burden - 0.5) : cell(row, "Y");
    }

    function timingColumnIndex(row) {
        const spacing = num("Spacing");
        if (!(spacing > 0)) return cell(row, "X");
        const rowIndex = timingRowIndex(row);
        const offset = get("PatternType").value === "Staggered" && Number.isInteger(rowIndex)
            ? (rowIndex % 2 ? spacing / 4 : -spacing / 4) : 0;
        return Math.round((cell(row, "X") - offset) / spacing - 0.5);
    }

    function orderedTimingRows() {
        const all = rows();
        const order = get("TimingOrder").value;
        if (order === "Sequential") return all;
        if (order === "Columns" || order === "ColumnGroups")
            return all.sort((a, b) => timingColumnIndex(a) - timingColumnIndex(b) ||
                timingRowIndex(a) - timingRowIndex(b) || cell(a, "X") - cell(b, "X"));
        if (order === "Serpentine") {
            const grouped = new Map();
            all.forEach(row => {
                const key = timingRowIndex(row);
                if (!grouped.has(key)) grouped.set(key, []);
                grouped.get(key).push(row);
            });
            return [...grouped.keys()].sort((a, b) => a - b)
                .flatMap((key, index) => grouped.get(key).sort((a, b) =>
                    index % 2 ? cell(b, "X") - cell(a, "X") : cell(a, "X") - cell(b, "X")));
        }
        return all.sort((a, b) => timingRowIndex(a) - timingRowIndex(b) ||
            cell(a, "X") - cell(b, "X"));
    }

    function timingAssignments() {
        const step = num("TimingStepMs"), order = get("TimingOrder").value;
        if (order === "Chevron" || order === "Echelon" || order === "HalfRowOverlap") {
            const grouped = new Map();
            rows().forEach(row => {
                const key = timingRowIndex(row);
                if (!grouped.has(key)) grouped.set(key, []);
                grouped.get(key).push(row);
            });
            const rowKeys = [...grouped.keys()].sort((a, b) => a - b);
            const maxColumns = Math.max(0, ...[...grouped.values()].map(group => group.length));
            const halfRowStride = Math.max(1, Math.ceil((maxColumns - 1) / 2));
            const columns = rows().map(timingColumnIndex);
            const centreColumn = (Math.min(...columns) + Math.max(...columns)) / 2;
            const rightColumn = Math.max(...columns);
            return rowKeys.flatMap((key, rowRank) =>
                grouped.get(key).sort((a, b) => cell(a, "X") - cell(b, "X"))
                    .map((row, columnRank) => {
                        const rank = order === "Chevron"
                            ? rowRank + Math.floor(Math.abs(timingColumnIndex(row) - centreColumn))
                            : order === "Echelon"
                            ? rowRank + rightColumn - timingColumnIndex(row)
                            : rowRank * halfRowStride + columnRank;
                        return { row, delay: rank * step };
                    }));
        }
        const groupAxis = order === "RowGroups" ? "Y" : order === "ColumnGroups" ? "X" : null;
        const groupIndexes = new Map();
        return orderedTimingRows().map((row, index) => {
            const group = groupAxis === "Y" ? String(timingRowIndex(row)) :
                groupAxis === "X" ? String(timingColumnIndex(row)) : null;
            if (group !== null && !groupIndexes.has(group)) groupIndexes.set(group, groupIndexes.size);
            return { row, delay: (group === null ? index : groupIndexes.get(group)) * step };
        });
    }

    function describeTiming() {
        const order = get("TimingOrder").value;
        const descriptions = {
            Sequential: "Sequential by hole number: each numbered hole follows the previous one.",
            Rows: "Sequential across rows: left to right, then move to the next row.",
            Columns: "Sequential down columns: front to back, then move to the next column.",
            Serpentine: "Serpentine: alternate left-to-right and right-to-left in successive rows.",
            Chevron: "Chevron (V): start at the centre of the front row, then advance outward and into following rows. Holes on the same V wavefront share a time.",
            Echelon: "Echelon: start at the front-right hole and advance diagonally toward the left and back. Holes on the same diagonal share a time; this assumes free faces at the front and right.",
            HalfRowOverlap: "Interleaved rows: move left to right within each row; the next row begins when the previous row is about halfway across. Simultaneous holes share a time.",
            RowGroups: "One time per row: holes sharing a Y row receive the same time; each next row adds one interval.",
            ColumnGroups: "One time per column: holes sharing an X column receive the same time; each next column adds one interval."
        };
        const staggeredChevron = order === "Chevron" && get("PatternType").value === "Staggered"
            ? " Chevron firing is less practical with staggered hole loading; review the free faces and effective burden and spacing."
            : "";
        get("timingExplanation").textContent = (descriptions[order] || descriptions.Rows) +
            staggeredChevron +
            " Grouped holes add their charges in the site delay window. Verify initiation feasibility and the approved timing plan.";
    }

    function timingSignature() {
        return [get("TimingOrder").value, get("TimingStepMs").value,
            get("Burden").value, get("Spacing").value, get("PatternType").value,
            ...rows().map(row => field(row, "X").value + "," + field(row, "Y").value)].join("|");
    }

    function initialTiming() {
        const step = num("TimingStepMs"), all = rows();
        if (!all.length || !Number.isInteger(step) || step <= 0) return;
        const assignments = timingAssignments();
        if (assignments.some(({delay}) => !Number.isSafeInteger(delay) || delay > 2147483647)) return;
        if (assignments.every(({row, delay}) => cell(row, "Delay") === delay)) {
            autoTimingActive = true;
            lastTimingSignature = timingSignature();
        }
    }

    function autoTiming() {
        const step = num("TimingStepMs"), all = rows();
        if (!all.length || !Number.isInteger(step) || step <= 0) {
            get("timingNotice").textContent = all.length
                ? "Enter a positive whole-millisecond interval to calculate per-hole delays."
                : "Place holes to see a delay sequence.";
            return;
        }
        if (all.some(row => !Number.isFinite(cell(row, "X")) || !Number.isFinite(cell(row, "Y")))) {
            get("timingNotice").textContent = "Every hole needs valid X and Y coordinates for a sequence.";
            return;
        }
        const assignments = timingAssignments();
        if (assignments.some(({delay}) => !Number.isSafeInteger(delay) || delay > 2147483647)) {
            get("timingNotice").textContent = "The interval creates a delay outside the supported range. Enter a smaller interval.";
            return;
        }
        const signature = timingSignature();
        if (signature === lastTimingSignature && autoTimingActive) {
            get("timingNotice").textContent = "Delays calculated for " + all.length +
                " holes using the entered interval. Review against the site initiation plan.";
            return;
        }
        if (!autoTimingActive && (manualTimingEdited || all.some(row => cell(row, "Delay") > 0))) {
            get("timingNotice").textContent = "Existing delays are preserved. Apply the current sequence to replace them.";
            return;
        }
        assignments.forEach(({row, delay}) => { field(row, "Delay").value = String(delay); });
        autoTimingActive = true;
        lastTimingSignature = signature;
        get("timingNotice").textContent = "Delays calculated for " + all.length +
            " holes using the entered interval. Review against the site initiation plan.";
    }

    function applyTiming() {
        if (!pendingTiming) return;
        pendingTiming.assignments.forEach(({row, delay}) => { field(row, "Delay").value = String(delay); });
        autoTimingActive = true;
        manualTimingEdited = false;
        lastTimingSignature = timingSignature();
        pendingTiming = null;
        get("timingPrompt").hidden = true;
        get("timingNotice").textContent = "Delay draft applied. Review each initiation time against the site plan.";
        update();
    }

    get("generatePatternBtn").addEventListener("click", () => {
        const design = proposal(), depth = num("NewHoleDepth");
        if (design.error) return say(design.error);
        if (!(depth > 0)) { say("Enter the drilled depth for new holes."); get("NewHoleDepth").focus(); return; }
        pending = { points: design.points, depth };
        if (rows().length) { get("replacePatternPrompt").hidden = false; say(""); }
        else apply();
    });
    get("resetGeometryBtn").addEventListener("click", () => {
        geometryMode = "Automatic";
        syncGeometry();
        autoPlace();
    });
    get("confirmPatternBtn").addEventListener("click", apply);
    get("cancelPatternBtn").addEventListener("click", () => { pending = null; get("replacePatternPrompt").hidden = true; update(); });
    get("applyTimingBtn").addEventListener("click", () => {
        const step = num("TimingStepMs");
        if (!rows().length) return void (get("timingNotice").textContent = "Place or add holes first.");
        if (!Number.isInteger(step) || step <= 0)
            return void (get("timingNotice").textContent = "Enter a positive whole-millisecond interval.");
        if (rows().some(row => !Number.isFinite(cell(row, "X")) || !Number.isFinite(cell(row, "Y"))))
            return void (get("timingNotice").textContent = "Every hole needs valid X and Y coordinates.");
        const assignments = timingAssignments();
        if (assignments.some(({delay}) => !Number.isSafeInteger(delay) || delay > 2147483647))
            return void (get("timingNotice").textContent = "The interval creates a delay outside the supported range.");
        pendingTiming = { assignments };
        if (manualTimingEdited || rows().some(row => cell(row, "Delay") > 0)) get("timingPrompt").hidden = false;
        else applyTiming();
    });
    get("confirmTimingBtn").addEventListener("click", applyTiming);
    get("cancelTimingBtn").addEventListener("click", () => {
        pendingTiming = null;
        get("timingPrompt").hidden = true;
        get("timingNotice").textContent = "Existing delays kept.";
    });
    get("addHoleBtn").addEventListener("click", () => {
        autoLayoutActive = false;
        const last = rows().at(-1);
        add(last ? { x: cell(last, "X") + (num("Spacing") || 0), y: cell(last, "Y") } : { x: 0, y: 0 }, num("NewHoleDepth"));
        reindex();
    });
    get("addTenHolesBtn").addEventListener("click", () => {
        autoLayoutActive = false;
        for (let i = 0; i < 10; i++) {
            const last = rows().at(-1);
            add(last ? { x: cell(last, "X") + (num("Spacing") || 0), y: cell(last, "Y") } : { x: 0, y: 0 }, num("NewHoleDepth"));
        }
        reindex();
    });
    get("deleteSelectedBtn").addEventListener("click", () => {
        const selected = rows().filter(row => row.querySelector(".hole-select").checked);
        if (!selected.length) return say("Select at least one hole to delete.");
        autoLayoutActive = false;
        holeWasDeleted = true;
        selected.forEach(row => row.remove());
        reindex();
        say(selected.length + " hole(s) removed from this draft.");
    });
    get("selectAllHoles").addEventListener("change", event => {
        rows().forEach(row => { row.querySelector(".hole-select").checked = event.target.checked; row.classList.toggle("is-selected", event.target.checked); });
    });
    body.addEventListener("change", event => {
        if (event.target.classList.contains("hole-select")) {
            event.target.closest(".hole-row").classList.toggle("is-selected", event.target.checked);
            selectState();
        }
    });
    get("useUsbMLimitBtn").addEventListener("click", () => {
        if (usbmSuggestion() === null) {
            say("Choose a residential wall type and enter measured frequency from 0 to 100 Hz.");
            return;
        }
        manualVibrationEdited = false;
        get("VibrationThresholdMode").value = "Automatic";
        refreshVibrationGuidance();
    });
    get("VibrationThreshold").addEventListener("input", () => {
        manualVibrationEdited = true;
        get("VibrationThresholdMode").value = "Manual";
        refreshVibrationGuidance();
    });
    get("ReceptorStructureType").addEventListener("change", refreshVibrationGuidance);
    get("DominantFrequencyHz").addEventListener("input", refreshVibrationGuidance);
    form.querySelectorAll("[data-plan-view]").forEach(button => button.addEventListener("click", () => {
        planView = button.dataset.planView;
        form.querySelectorAll("[data-plan-view]").forEach(choice => {
            const active = choice === button;
            choice.classList.toggle("is-active", active);
            choice.setAttribute("aria-pressed", String(active));
        });
        draw();
    }));
    get("planZoomIn").addEventListener("click", () => zoomPlan(1.25));
    get("planZoomOut").addEventListener("click", () => zoomPlan(0.8));
    get("planFit").addEventListener("click", () => {
        planZoom = 1; planCenterX = 500; planCenterY = 280; applyPlanViewBox();
    });
    get("planExpand").addEventListener("click", () => {
        const expanded = get("planExpand").closest(".design-plan-card").classList.toggle("is-expanded");
        get("planExpand").textContent = expanded ? "Close large view" : "Expand plan";
        get("planExpand").setAttribute("aria-expanded", String(expanded));
        document.body.classList.toggle("plan-expanded", expanded);
    });
    document.addEventListener("keydown", event => {
        if (event.key === "Escape" && get("planExpand").getAttribute("aria-expanded") === "true")
            get("planExpand").click();
    });
    const selectPlanHole = target => {
        const marker = target.closest("[data-hole-index]");
        if (!marker) return;
        selectedHoleRow = rows()[Number(marker.dataset.holeIndex)] ?? null;
        draw();
        get("planHoleEditor").scrollIntoView({ block: "nearest", behavior: "smooth" });
    };
    plan.addEventListener("click", event => selectPlanHole(event.target));
    plan.addEventListener("keydown", event => {
        if ((event.key === "Enter" || event.key === " ") && event.target.matches("[data-hole-index]")) {
            event.preventDefault();
            selectPlanHole(event.target);
        }
    });
    get("planCloseEditor").addEventListener("click", () => { selectedHoleRow = null; draw(); });
    get("planDeleteHole").addEventListener("click", () => {
        if (!selectedHoleRow) return;
        const number = selectedHoleRow.querySelector(".hole-number").textContent;
        autoLayoutActive = false;
        holeWasDeleted = true;
        selectedHoleRow.remove();
        selectedHoleRow = null;
        reindex();
        const message = "Hole " + number + " removed. " + rows().length + " hole(s) remain; totals and timing updated.";
        get("planDetail").textContent = message;
        say(message);
    });
    get("planHoleEditor").addEventListener("input", event => {
        if (!selectedHoleRow || !event.target.matches("[data-plan-field]")) return;
        const original = field(selectedHoleRow, event.target.dataset.planField);
        original.value = event.target.value;
        original.dispatchEvent(new Event("input", { bubbles: true }));
    });
    get("planHoleEditor").addEventListener("change", event => {
        if (!selectedHoleRow || !event.target.matches("select[data-plan-field]")) return;
        const original = field(selectedHoleRow, event.target.dataset.planField);
        original.value = event.target.value;
        original.dispatchEvent(new Event("change", { bubbles: true }));
    });
    plan.addEventListener("wheel", event => {
        event.preventDefault();
        zoomPlan(event.deltaY < 0 ? 1.2 : 1 / 1.2, event.clientX, event.clientY);
    }, { passive: false });
    plan.addEventListener("pointerdown", event => {
        if (event.button !== 0) return;
        if (event.target.closest("[data-hole-index]")) {
            // Select before a focused form field blurs and redraws the SVG.
            selectPlanHole(event.target);
            return;
        }
        planDrag = { x: event.clientX, y: event.clientY, cx: planCenterX, cy: planCenterY };
        plan.setPointerCapture(event.pointerId);
    });
    plan.addEventListener("pointermove", event => {
        if (!planDrag) return;
        const rect = plan.getBoundingClientRect();
        planCenterX = planDrag.cx - (event.clientX - planDrag.x) * 1000 / planZoom / rect.width;
        planCenterY = planDrag.cy - (event.clientY - planDrag.y) * 560 / planZoom / rect.height;
        applyPlanViewBox();
    });
    plan.addEventListener("pointerup", () => { planDrag = null; });
    plan.addEventListener("pointercancel", () => { planDrag = null; });
    const layoutFields = new Set(["BenchLengthMetres", "BenchWidthMetres", "LayoutRows", "LayoutColumns",
        "PatternType", "NewHoleDepth", "Burden", "Spacing"]);
    function onFormEdit(event) {
        if (event.target.closest(".hole-row") && event.target.matches("[data-field]")) {
            const row = event.target.closest(".hole-row");
            if (["X", "Y", "Depth"].includes(event.target.dataset.field)) {
                autoLayoutActive = false;
                row.dataset.generated = "false";
            }
            if (event.target.dataset.field === "Delay") {
                autoTimingActive = false;
                manualTimingEdited = true;
            } else if (event.target.dataset.field === "X" || event.target.dataset.field === "Y") {
                autoTiming();
            }
            if (event.target.dataset.field === "DiameterMillimetres") row.dataset.diameterOverride = "true";
            if (event.target.dataset.field === "SubdrillMetres") row.dataset.subdrillOverride = "true";
            if (event.target.dataset.field === "Stemming") row.dataset.stemmingOverride = "true";
            if (event.target.dataset.field === "Charge") row.dataset.chargeOverride = "true";
            if (event.target.dataset.field === "AeciProductCode") {
                row.dataset.productOverride = "true";
                row.dataset.densityOverride = "false";
                field(row, "ExplosiveProductId").value = "";
            }
            if (event.target.dataset.field === "ProductDensityGramsPerCc")
                row.dataset.densityOverride = "true";
            syncLoading();
            syncEstimates();
        }
        if (layoutFields.has(event.target.id)) {
            if (event.target.id === "Burden" || event.target.id === "Spacing") geometryMode = "Manual";
            pending = null;
            get("replacePatternPrompt").hidden = true;
            syncGeometry();
            syncEstimates();
            autoPlace();
            syncLoading();
            autoTiming();
            describeTiming();
            update();
        } else if (event.target.id === "TimingOrder" || event.target.id === "TimingStepMs") {
            if (event.target.id === "TimingStepMs") manualIntervalEdited = true;
            pendingTiming = null;
            get("timingPrompt").hidden = true;
            autoTiming();
            describeTiming();
            update();
        } else {
            if (event.target.id === "FlyrockLaunchAngleDegrees") manualAngleEdited = true;
            if (event.target.id === "FlyrockLaunchHeightMetres") manualHeightEdited = true;
            if (event.target.id === "DefaultAeciProductCode") syncLoading();
            update();
        }
    }
    form.addEventListener("input", onFormEdit);
    form.addEventListener("change", onFormEdit);
    initialGeometry();
    syncGeometry();
    initialLayout();
    refreshVibrationGuidance();
    const suggestedInterval = num("Burden") > 0 ? Math.max(1, Math.round(2 * num("Burden") * 3.28084)) : null;
    manualIntervalEdited = num("TimingStepMs") > 0 && num("TimingStepMs") !== suggestedInterval;
    manualAngleEdited = num("FlyrockLaunchAngleDegrees") >= 0 && num("FlyrockLaunchAngleDegrees") !== 45;
    const savedHeights = rows().map(row => cell(row, "Depth") - cell(row, "SubdrillMetres"));
    manualHeightEdited = num("FlyrockLaunchHeightMetres") >= 0 &&
        (!savedHeights.length || savedHeights.some(height => !(height > 0)) ||
            Math.abs(num("FlyrockLaunchHeightMetres") - Math.max(...savedHeights)) > 0.015);
    syncEstimates();
    initialLoading();
    syncLoading();
    syncEstimates();
    initialTiming();
    describeTiming();
    autoTiming();
    autoPlace();
});
