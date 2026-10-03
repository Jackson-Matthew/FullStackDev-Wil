using System.Globalization;
using BlastPro.Mvc.Models.ViewModels.Reports;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace BlastPro.Mvc.Services;

/// <summary>
/// Builds the downloadable PDF copy of a saved calculation report. The content mirrors the
/// on-screen report (_ReportBody) so both show the same saved figures.
/// </summary>
public static class ReportPdfBuilder
{
    private const double PageWidth = 842;   // A4 landscape, points
    private const double PageHeight = 595;
    private const double Margin = 34;
    private const double ContentWidth = PageWidth - Margin * 2;
    private const double BottomLimit = PageHeight - 38;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly object FontLock = new();
    private static bool _fontsReady;

    public static byte[] Build(ReportViewModel report)
    {
        EnsureFonts();
        var writer = new Writer(report);
        writer.Render();
        return writer.Save();
    }

    private static void EnsureFonts()
    {
        lock (FontLock)
        {
            if (_fontsReady) return;
            if (OperatingSystem.IsWindows())
                GlobalFontSettings.UseWindowsFontsUnderWindows = true;
            else
                GlobalFontSettings.FontResolver = new InstalledFontResolver();
            _fontsReady = true;
        }
    }

    /// <summary>
    /// Font lookup for non-Windows hosts, where PDFsharp has no built-in system fonts.
    /// Uses Liberation Sans or DejaVu Sans when installed.
    /// </summary>
    private sealed class InstalledFontResolver : IFontResolver
    {
        private static readonly string[] Directories =
        {
            "/usr/share/fonts/truetype/liberation", "/usr/share/fonts/liberation",
            "/usr/share/fonts/truetype/dejavu", "/usr/share/fonts/dejavu", "/Library/Fonts", "/System/Library/Fonts/Supplemental"
        };

        private static readonly string[] Regular = { "LiberationSans-Regular.ttf", "DejaVuSans.ttf", "Arial.ttf" };
        private static readonly string[] Bold = { "LiberationSans-Bold.ttf", "DejaVuSans-Bold.ttf", "Arial Bold.ttf" };

        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
            new(isBold ? "report-bold" : "report-regular");

        public byte[]? GetFont(string faceName)
        {
            foreach (var file in faceName == "report-bold" ? Bold : Regular)
                foreach (var directory in Directories)
                {
                    var path = Path.Combine(directory, file);
                    if (File.Exists(path)) return File.ReadAllBytes(path);
                }
            throw new InvalidOperationException(
                "No usable font was found to build the PDF. Install Liberation Sans or DejaVu Sans on the server.");
        }
    }

    private sealed class Writer
    {
        private readonly ReportViewModel _report;
        private readonly PdfDocument _doc = new();
        private PdfPage _page = null!;
        private XGraphics _gfx = null!;
        private double _y;

        private const string Family = "Arial";
        private readonly XFont _body = new(Family, 9, XFontStyleEx.Regular);
        private readonly XFont _bold = new(Family, 9, XFontStyleEx.Bold);
        private readonly XFont _small = new(Family, 7.5, XFontStyleEx.Regular);
        private readonly XFont _smallBold = new(Family, 7.5, XFontStyleEx.Bold);
        private readonly XFont _section = new(Family, 12, XFontStyleEx.Bold);
        private readonly XFont _metric = new(Family, 13, XFontStyleEx.Bold);
        private readonly XFont _table = new(Family, 7, XFontStyleEx.Regular);
        private readonly XFont _tableBold = new(Family, 7, XFontStyleEx.Bold);

        private static readonly XColor Navy = XColor.FromArgb(0x15, 0x24, 0x37);
        private static readonly XColor Ink = XColor.FromArgb(0x24, 0x34, 0x47);
        private static readonly XColor Muted = XColor.FromArgb(0x64, 0x74, 0x8B);
        private static readonly XColor Line = XColor.FromArgb(0xE3, 0xEA, 0xF0);
        private static readonly XColor Panel = XColor.FromArgb(0xF8, 0xFA, 0xFC);

        public Writer(ReportViewModel report) => _report = report;

        public byte[] Save()
        {
            _gfx.Dispose();
            var total = _doc.PageCount;
            for (var i = 0; i < total; i++)
            {
                using var gfx = XGraphics.FromPdfPage(_doc.Pages[i], XGraphicsPdfPageOptions.Append);
                var text = $"BlastPro · {_report.ProjectName} · Calculation report #{_report.ResultId} · Page {i + 1} of {total}";
                gfx.DrawString(text, _small, new XSolidBrush(Muted),
                    new XRect(Margin, PageHeight - 28, ContentWidth, 12), XStringFormats.TopLeft);
            }
            _doc.Info.Title = $"Calculation report - {_report.ProjectName}";
            _doc.Info.Creator = "BlastPro";
            using var stream = new MemoryStream();
            _doc.Save(stream, false);
            return stream.ToArray();
        }

        public void Render()
        {
            NewPage();
            DrawHero();
            if (_report.IsOutdated) DrawOutdatedNote();
            DrawProjectFacts();
            DrawQuantities();
            DrawScreening();
            DrawWarnings();
            DrawHolePlanAndSchedule();
            DrawFootnote();
        }

        // ---------- page handling ----------

        private void NewPage()
        {
            _gfx?.Dispose();
            _page = _doc.AddPage();
            _page.Width = XUnit.FromPoint(PageWidth);
            _page.Height = XUnit.FromPoint(PageHeight);
            _gfx = XGraphics.FromPdfPage(_page);
            _y = Margin;
        }

        private void Ensure(double height)
        {
            if (_y + height > BottomLimit) NewPage();
        }

        // ---------- drawing helpers ----------

        private static XBrush Brush(XColor color) => new XSolidBrush(color);

        private void Text(string text, XFont font, XColor color, double x, double y, double width,
            XStringFormat? format = null) =>
            _gfx.DrawString(Fit(text, font, width), font, Brush(color), new XRect(x, y, width, font.Height),
                format ?? XStringFormats.TopLeft);

        private string Fit(string text, XFont font, double width)
        {
            if (string.IsNullOrEmpty(text) || _gfx.MeasureString(text, font).Width <= width) return text;
            while (text.Length > 1 && _gfx.MeasureString(text + "…", font).Width > width)
                text = text[..^1];
            return text + "…";
        }

        private List<string> Wrap(string text, XFont font, double width)
        {
            var lines = new List<string>();
            var current = "";
            foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (current.Length > 0 && _gfx.MeasureString(candidate, font).Width > width)
                {
                    lines.Add(current);
                    current = word;
                }
                else current = candidate;
            }
            if (current.Length > 0) lines.Add(current);
            return lines.Count == 0 ? new List<string> { "" } : lines;
        }

        private void Paragraph(string text, XFont font, XColor color, double x, double width, double lineGap = 3)
        {
            foreach (var line in Wrap(text, font, width))
            {
                Ensure(font.Height + lineGap);
                Text(line, font, color, x, _y, width);
                _y += font.Height + lineGap;
            }
        }

        private void SectionTitle(string title, int? count = null)
        {
            Ensure(40);
            _y += 12;
            Text(count is null ? title : $"{title}  ({count})", _section, XColor.FromArgb(0x1A, 0x2B, 0x3D), Margin, _y, ContentWidth);
            _y += _section.Height + 3;
            _gfx.DrawLine(new XPen(Line, 1), Margin, _y, Margin + ContentWidth, _y);
            _y += 8;
        }

        private static string Number(decimal? value, string format = "0.##") =>
            value?.ToString(format, Inv) ?? "Not available";

        private static string Measure(decimal? value, string unit, string format = "0.##") =>
            value.HasValue ? value.Value.ToString(format, Inv) + " " + unit : "Not available";

        // ---------- sections ----------

        private void DrawHero()
        {
            const double height = 74;
            _gfx.DrawRectangle(Brush(Navy), Margin, _y, ContentWidth, height);
            Text("BLASTPRO · SAVED CALCULATION", _smallBold, XColor.FromArgb(0xFF, 0xA1, 0x8F), Margin + 20, _y + 12, 400);
            Text(_report.ProjectName, new XFont(Family, 20, XFontStyleEx.Bold), XColors.White, Margin + 20, _y + 25, ContentWidth - 190);
            Text($"Calculation report #{_report.ResultId} · {_report.ReportDateUtc.ToString("dd MMM yyyy, HH:mm", Inv)} UTC",
                _small, XColor.FromArgb(0xC0, 0xCB, 0xD7), Margin + 20, _y + 54, 500);

            var state = _report.IsOutdated ? "Outdated result" : "Current result";
            var pillBack = _report.IsOutdated ? XColor.FromArgb(0xFF, 0xF0, 0xD5) : XColor.FromArgb(0xD9, 0xF3, 0xE6);
            var pillInk = _report.IsOutdated ? XColor.FromArgb(0x8A, 0x52, 0x00) : XColor.FromArgb(0x12, 0x62, 0x3B);
            var pillWidth = _gfx.MeasureString(state, _smallBold).Width + 26;
            _gfx.DrawRoundedRectangle(Brush(pillBack), Margin + ContentWidth - pillWidth - 18, _y + 14, pillWidth, 20, 20, 20);
            Text(state, _smallBold, pillInk, Margin + ContentWidth - pillWidth - 18, _y + 20, pillWidth, XStringFormats.TopCenter);
            _y += height;
        }

        private void DrawOutdatedNote()
        {
            var note = _report.HasPatternSnapshot
                ? "The draft changed after this calculation. The plan and hole schedule below are the saved design for this result."
                : "The draft changed after this calculation. This older result has no saved hole snapshot, so its plan and hole schedule are unavailable.";
            var lines = Wrap(note, _body, ContentWidth - 28);
            var height = lines.Count * (_body.Height + 2) + 16;
            _y += 12;
            _gfx.DrawRoundedRectangle(new XPen(XColor.FromArgb(0xF0, 0xCF, 0x8A), 1), Brush(XColor.FromArgb(0xFF, 0xF9, 0xEB)),
                Margin, _y, ContentWidth, height, 8, 8);
            var ty = _y + 8;
            foreach (var line in lines)
            {
                Text(line, _body, XColor.FromArgb(0x74, 0x4E, 0x13), Margin + 14, ty, ContentWidth - 28);
                ty += _body.Height + 2;
            }
            _y += height;
        }

        private void DrawProjectFacts()
        {
            SectionTitle("Project and calculation");
            var facts = new List<(string Label, string Value)>
            {
                ("Calculated by", string.IsNullOrWhiteSpace(_report.CalculatedByName) ? "Not recorded" : _report.CalculatedByName)
            };
            if (_report.IsOutdated && !_report.HasPatternSnapshot)
                facts.Add(("Design inputs", "Not stored with this historical result"));
            else
            {
                facts.Add(("Site", string.IsNullOrWhiteSpace(_report.SiteLocation) ? "Not recorded" : _report.SiteLocation));
                facts.Add(("Blast type", string.IsNullOrWhiteSpace(_report.BlastType) ? "Not recorded" : _report.BlastType));
                facts.Add(("Rock type", string.IsNullOrWhiteSpace(_report.RockType) ? "Not recorded" : _report.RockType));
                facts.Add(("Burden", Number(_report.BurdenMetres) + " m"));
                facts.Add(("Spacing", Number(_report.SpacingMetres) + " m"));
            }

            var colWidth = ContentWidth / 3;
            for (var i = 0; i < facts.Count; i += 3)
            {
                Ensure(34);
                for (var c = 0; c < 3 && i + c < facts.Count; c++)
                {
                    var x = Margin + c * colWidth;
                    Text(facts[i + c].Label, _small, Muted, x, _y, colWidth - 12);
                    Text(facts[i + c].Value, _bold, Ink, x, _y + 11, colWidth - 12);
                }
                _y += 32;
            }
        }

        private void DrawMetricGrid(IReadOnlyList<(string Label, string Value)> metrics)
        {
            const double boxHeight = 46;
            const double gap = 10;
            var boxWidth = (ContentWidth - gap * 2) / 3;
            for (var i = 0; i < metrics.Count; i += 3)
            {
                Ensure(boxHeight + gap);
                for (var c = 0; c < 3 && i + c < metrics.Count; c++)
                {
                    var x = Margin + c * (boxWidth + gap);
                    _gfx.DrawRoundedRectangle(new XPen(Line, 1), Brush(Panel), x, _y, boxWidth, boxHeight, 8, 8);
                    Text(metrics[i + c].Label, _small, Muted, x + 10, _y + 8, boxWidth - 20);
                    Text(metrics[i + c].Value, _metric, XColor.FromArgb(0x14, 0x26, 0x3B), x + 10, _y + 21, boxWidth - 20);
                }
                _y += boxHeight + gap;
            }
        }

        private void DrawQuantities()
        {
            SectionTitle("Calculated quantities");
            DrawMetricGrid(new (string, string)[]
            {
                ("Holes", _report.TotalDesignatedHoles.ToString(Inv)),
                ("Total drilling", _report.FormatNumber(_report.TotalDrillingLengthMetres, 1) + " m"),
                ("Total charge", _report.FormatNumber(_report.TotalExplosiveLoadKg, 1) + " kg"),
                ("Estimated volume", Measure(_report.EstimatedVolumeCubicMetres, "m³", "0.0")),
                ("Estimated tonnage", Measure(_report.TotalEstimatedTonnageTonnes, "t", "0.0")),
                ("Powder factor", Measure(_report.PowderFactorKgPerTonne, "kg/t", "0.###"))
            });
        }

        private void DrawScreening()
        {
            SectionTitle("Screening results");
            var metrics = new List<(string, string)>
            {
                ("Maximum charge in delay window", _report.FormatNumber(_report.MaxChargePerDelayKg, 1) + " kg"),
                ("Predicted PPV", Measure(_report.PredictedPpvMmPerSecond, "mm/s"))
            };
            if (!_report.IsOutdated || _report.HasPatternSnapshot)
                metrics.Add(("Entered vibration limit", Measure(_report.VibrationThresholdMmPerSecond, "mm/s")));
            metrics.Add(("Idealized flyrock trajectory", Measure(_report.IdealizedFlyrockRangeMetres, "m", "0.0")));
            DrawMetricGrid(metrics);
            Paragraph("The trajectory estimate is not an exclusion radius. Review site measurements, limits and the approved blast plan.",
                _small, Muted, Margin, ContentWidth);
        }

        private void DrawWarnings()
        {
            SectionTitle("Checks and warnings", _report.Warnings.Count);
            if (_report.Warnings.Count == 0)
            {
                Paragraph("No calculation warnings were recorded.", _small, Muted, Margin, ContentWidth);
                return;
            }

            foreach (var warning in _report.Warnings)
            {
                var critical = warning.Severity == "Critical";
                var lines = Wrap(warning.Message, _body, ContentWidth - 100);
                var height = Math.Max(1, lines.Count) * (_body.Height + 2) + 12;
                Ensure(height + 6);
                _gfx.DrawRoundedRectangle(
                    new XPen(critical ? XColor.FromArgb(0xF1, 0xB6, 0xB1) : XColor.FromArgb(0xF0, 0xDF, 0xBA), 1),
                    Brush(critical ? XColor.FromArgb(0xFF, 0xF2, 0xF0) : XColor.FromArgb(0xFF, 0xFA, 0xF0)),
                    Margin, _y, ContentWidth, height, 6, 6);
                var ink = critical ? XColor.FromArgb(0x84, 0x26, 0x1F) : XColor.FromArgb(0x65, 0x49, 0x1B);
                Text(warning.Severity, _bold, ink, Margin + 12, _y + 6, 70);
                var ty = _y + 6;
                foreach (var line in lines)
                {
                    Text(line, _body, ink, Margin + 88, ty, ContentWidth - 100);
                    ty += _body.Height + 2;
                }
                _y += height + 6;
            }
        }

        private void DrawHolePlanAndSchedule()
        {
            SectionTitle("Saved hole plan and schedule");
            if (!_report.HolesMatchResult || _report.Holes.Count == 0)
            {
                Paragraph("Hole details are unavailable for this saved result. Open the current Pattern Design to inspect the latest hole schedule.",
                    _small, Muted, Margin, ContentWidth);
                return;
            }

            DrawHolePlan();
            DrawHoleTable();
        }

        private void DrawHolePlan()
        {
            var holes = _report.Holes;
            var hasBench = _report.BenchLengthMetres is > 0 && _report.BenchWidthMetres is > 0;
            var minX = Math.Min(0m, holes.Min(h => h.X));
            var minY = Math.Min(0m, holes.Min(h => h.Y));
            var maxX = Math.Max(_report.BenchLengthMetres ?? 0m, holes.Max(h => h.X));
            var maxY = Math.Max(_report.BenchWidthMetres ?? 0m, holes.Max(h => h.Y));
            var rangeX = (double)Math.Max(1m, maxX - minX);
            var rangeY = (double)Math.Max(1m, maxY - minY);

            // Same geometry as the on-screen plan (1000 x 560 canvas), scaled to the page.
            var scale = Math.Min(900d / rangeX, 430d / rangeY);
            var left = (1000d - rangeX * scale) / 2d;
            var top = (510d - rangeY * scale) / 2d;

            const double planWidth = 560;
            var factor = planWidth / 1000d;
            var planHeight = 560d * factor;
            Ensure(planHeight + 14);
            var originX = Margin + (ContentWidth - planWidth) / 2d;
            var originY = _y;

            double PlotX(decimal x) => originX + (left + (double)(x - minX) * scale) * factor;
            double PlotY(decimal y) => originY + (top + (double)(maxY - y) * scale) * factor;

            _gfx.DrawRoundedRectangle(new XPen(XColor.FromArgb(0xD9, 0xE2, 0xEA), 1), Brush(XColor.FromArgb(0xFB, 0xFC, 0xFE)),
                originX, originY, planWidth, planHeight, 8, 8);

            if (hasBench)
            {
                var length = _report.BenchLengthMetres!.Value;
                var width = _report.BenchWidthMetres!.Value;
                var gridPen = new XPen(XColor.FromArgb(0xD7, 0xE1, 0xEB), 0.5);
                for (var i = 1; i < 10; i++)
                {
                    _gfx.DrawLine(gridPen, PlotX(length * i / 10), PlotY(width), PlotX(length * i / 10), PlotY(0));
                    _gfx.DrawLine(gridPen, PlotX(0), PlotY(width * i / 10), PlotX(length), PlotY(width * i / 10));
                }
                _gfx.DrawRectangle(new XPen(XColor.FromArgb(0x31, 0x4E, 0x6B), 1.2),
                    Brush(XColor.FromArgb(0xF0, 0xF6, 0xFD)), PlotX(0), PlotY(width),
                    (double)length * scale * factor, (double)width * scale * factor);
                // Re-draw the grid on top of the fill.
                for (var i = 1; i < 10; i++)
                {
                    _gfx.DrawLine(gridPen, PlotX(length * i / 10), PlotY(width), PlotX(length * i / 10), PlotY(0));
                    _gfx.DrawLine(gridPen, PlotX(0), PlotY(width * i / 10), PlotX(length), PlotY(width * i / 10));
                }
            }

            var labelHoles = holes.Count <= 45;
            var radius = (labelHoles ? 13d : 8d) * factor;
            var numberFont = new XFont(Family, Math.Max(5, 13 * factor), XFontStyleEx.Bold);
            foreach (var hole in holes)
            {
                var outside = hasBench && (hole.X < 0 || hole.Y < 0 ||
                    hole.X > _report.BenchLengthMetres || hole.Y > _report.BenchWidthMetres);
                var fill = outside ? XColor.FromArgb(0xC9, 0x33, 0x33) : XColor.FromArgb(0xE7, 0x78, 0x19);
                var edge = outside ? XColor.FromArgb(0x80, 0x20, 0x20) : XColor.FromArgb(0x8A, 0x46, 0x0C);
                var cx = PlotX(hole.X);
                var cy = PlotY(hole.Y);
                _gfx.DrawEllipse(new XPen(edge, 0.8), Brush(fill), cx - radius, cy - radius, radius * 2, radius * 2);
                if (labelHoles)
                    _gfx.DrawString(hole.Number.ToString(Inv), numberFont, XBrushes.White,
                        new XRect(cx - radius, cy - numberFont.Height / 2d, radius * 2, numberFont.Height), XStringFormats.TopCenter);
            }

            var xLabel = hasBench ? "X / length: " + _report.BenchLengthMetres!.Value.ToString("0.##", Inv) + " m" : "X / length (m)";
            var yLabel = hasBench ? "Y / width: " + _report.BenchWidthMetres!.Value.ToString("0.##", Inv) + " m" : "Y / width (m)";
            Text(xLabel, _small, XColor.FromArgb(0x40, 0x55, 0x6A), originX, originY + planHeight - 14, planWidth, XStringFormats.TopCenter);
            Text(yLabel, _small, XColor.FromArgb(0x40, 0x55, 0x6A), originX + 8, originY + 6, planWidth - 16);

            _y += planHeight + 14;
        }

        private void DrawHoleTable()
        {
            var headers = new[] { "#", "X (m)", "Y (m)", "Depth (m)", "Diameter (mm)", "Subdrill (m)", "Product",
                "Density (g/cm³)", "Charge (kg)", "Stemming (m)", "Delay (ms)" };
            var widths = new double[] { 28, 46, 46, 50, 62, 56, 0, 70, 58, 62, 50 };
            widths[6] = ContentWidth - widths.Sum();
            const double rowHeight = 15;

            void Header()
            {
                _gfx.DrawRectangle(Brush(XColor.FromArgb(0xEE, 0xF3, 0xF7)), Margin, _y, ContentWidth, rowHeight + 2);
                var hx = Margin;
                for (var c = 0; c < headers.Length; c++)
                {
                    Text(headers[c], _tableBold, XColor.FromArgb(0x34, 0x46, 0x5A), hx + 4, _y + 5, widths[c] - 8);
                    hx += widths[c];
                }
                _y += rowHeight + 2;
            }

            Ensure(rowHeight * 3);
            Header();
            var index = 0;
            foreach (var hole in _report.Holes)
            {
                if (_y + rowHeight > BottomLimit)
                {
                    NewPage();
                    Header();
                }
                if (index++ % 2 == 1)
                    _gfx.DrawRectangle(Brush(XColor.FromArgb(0xFB, 0xFC, 0xFD)), Margin, _y, ContentWidth, rowHeight);

                var cells = new[]
                {
                    hole.Number.ToString(Inv), hole.X.ToString("0.##", Inv), hole.Y.ToString("0.##", Inv),
                    hole.Depth.ToString("0.##", Inv), Number(hole.DiameterMillimetres, "0.#"), Number(hole.SubdrillMetres),
                    hole.Explosive, Number(hole.ProductDensityGramsPerCc, "0.###"), hole.Charge.ToString("0.##", Inv),
                    hole.Stemming.ToString("0.##", Inv), hole.Delay.ToString(Inv)
                };
                var x = Margin;
                for (var c = 0; c < cells.Length; c++)
                {
                    Text(cells[c], _table, Ink, x + 4, _y + 4, widths[c] - 8);
                    x += widths[c];
                }
                _gfx.DrawLine(new XPen(XColor.FromArgb(0xE8, 0xED, 0xF2), 0.5), Margin, _y + rowHeight, Margin + ContentWidth, _y + rowHeight);
                _y += rowHeight;
            }
        }

        private void DrawFootnote()
        {
            const string note = "This report records planning calculations and warnings. A qualified blasting professional must verify " +
                "site inputs, hole loading, initiation timing, vibration and flyrock controls before operational use.";
            var lines = Wrap(note, _small, ContentWidth - 28);
            var height = (lines.Count + 1) * (_small.Height + 2) + 14;
            Ensure(height + 14);
            _y += 14;
            _gfx.DrawRectangle(Brush(Panel), Margin, _y, ContentWidth, height);
            Text("Review required.", _smallBold, XColor.FromArgb(0x29, 0x3D, 0x53), Margin + 14, _y + 8, ContentWidth - 28);
            var ty = _y + 8 + _small.Height + 2;
            foreach (var line in lines)
            {
                Text(line, _small, XColor.FromArgb(0x5A, 0x68, 0x78), Margin + 14, ty, ContentWidth - 28);
                ty += _small.Height + 2;
            }
            _y += height;
        }
    }
}
