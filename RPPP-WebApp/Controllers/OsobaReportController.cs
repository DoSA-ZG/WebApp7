using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using PdfRpt.Core.Contracts;
using PdfRpt.FluentInterface;
using RPPP_WebApp.Extensions;
using RPPP_WebApp.Models; 
using RPPP_WebApp.ViewModels;

namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// Stvara PDF i Excel tablice za podatke osoba
    /// </summary>
    public class OsobaReportController : Controller
    {
        private readonly Rppp07Context _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<OsobaController> logger;

        public OsobaReportController(Rppp07Context context, IWebHostEnvironment environment, ILogger<OsobaController> logger)
        {
            _context = context;
            _environment = environment;
            this.logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Pokusa uvesti podatke iz Excel tablice.
        /// </summary>
        /// <param name="importFile">file iz kojeg se uvoze podatci</param>
        /// <returns>Excel tablica sa rezultatima unosa podataka</returns>
        public async Task<IActionResult> ImportOsobe(IFormFile importFile)
        {
            ExcelPackage result = new ExcelPackage();

            await using (var ms = new MemoryStream())
            {
                await importFile.CopyToAsync(ms);
                using (ExcelPackage import = new ExcelPackage(ms))
                {
                    ExcelWorksheet worksheet = import.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;

                    worksheet.Cells[1, 6].Value = "Status";

                    for (int row = 2; row <= rowCount; row++)
                    {
                        string ime = worksheet.Cells[row, 1].Value.ToString().Trim();
                        string email = worksheet.Cells[row, 2].Value.ToString().Trim();
                        string oib = worksheet.Cells[row, 3].Value.ToString().Trim();
                        string brMob = worksheet.Cells[row, 4].Value.ToString().Trim();
                        string iban = worksheet.Cells[row, 5].Value.ToString().Trim();

                        Osoba osoba = new Osoba
                        {
                            Ime = ime,
                            Email = email,
                            Oib = oib,
                            BrMob = brMob,
                            IbanOsoba = iban
                        };

                        try
                        {
                            _context.Add(osoba);
                            await _context.SaveChangesAsync();

                            logger.LogInformation($"{osoba.Ime} uspješno dodan");
                            worksheet.Cells[row, 6].Value = "ADDED";
                        }
                        catch (Exception exc)
                        {
                            worksheet.Cells[row, 6].Value = "ERROR";
                            logger.LogError("Pogreška prilikom dodavanja osobe: {0}", exc.CompleteExceptionMessage());
                            ModelState.AddModelError(string.Empty, exc.Message);
                        }
                    }

                    result.Workbook.Worksheets.Add("StatusiDodavanjaOsoba", worksheet);
                }
            }

            return File(result.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StatusiDodavanjaOsoba.xlsx");
        }

        /// <summary>
        /// Stvara pdf dokument s podatcima svih osoba
        /// </summary>
        /// <returns>PDF dokument s podatcima osoba</returns>
        public async Task<IActionResult> OsobePdf()
        {
            var osobe = await GetOsobeViewModelsAsync();
            var naslov = "Popis osoba";

            PdfReport report = CreatePdfReport(naslov);

            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(osobe));

            report.MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Order(0);
                    column.Width(1);
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(OsobaViewModel.Ime));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(3);
                    column.HeaderCell("Ime");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(OsobaViewModel.Email));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("E-Mail");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(OsobaViewModel.Oib));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(3);
                    column.Width(4);
                    column.HeaderCell("OIB", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(OsobaViewModel.BrMob));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(3);
                    column.Width(4);
                    column.HeaderCell("Broj mobitela", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(OsobaViewModel.IbanOsoba));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(3);
                    column.Width(4);
                    column.HeaderCell("IBAN", horizontalAlignment: HorizontalAlignment.Center);
                });
            });

            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=osobe.pdf");
                return File(pdf, "application/pdf");
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Stvara Excel tablicu s podatcima svih osoba
        /// </summary>
        /// <returns>Excel tablica s podatcima osoba</returns>
        public async Task<IActionResult> OsobeExcel()
        {
            var osobe = await _context.Osobe.ToListAsync();
            byte[] content;

            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis osoba";
                excel.Workbook.Properties.Author = "Roko Skoblar";
                var worksheet = excel.Workbook.Worksheets.Add("Osobe");

                worksheet.Cells[1, 1].Value = "Ime";
                worksheet.Cells[1, 2].Value = "E-Mail";
                worksheet.Cells[1, 3].Value = "OIB";
                worksheet.Cells[1, 4].Value = "Broj mobitela";
                worksheet.Cells[1, 5].Value = "IBAN";

                for (int i = 0; i < osobe.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = osobe[i].Ime;
                    worksheet.Cells[i + 2, 2].Value = osobe[i].Email;
                    worksheet.Cells[i + 2, 3].Value = osobe[i].Oib;
                    worksheet.Cells[i + 2, 4].Value = osobe[i].BrMob;
                    worksheet.Cells[i + 2, 5].Value = osobe[i].IbanOsoba;
                }

                worksheet.Cells[1, 1, osobe.Count + 1, 4].AutoFitColumns();

                content = excel.GetAsByteArray();
            }

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "osobe.xlsx");
        }

        /// <summary>
        /// Postavlja postavke pdf dokumenta
        /// </summary>
        /// <param name="naslov">naslov dokumenta</param>
        /// <returns>postavljenju klasu pdf dokumenta</returns>
        #region Private methods
        private PdfReport CreatePdfReport(string naslov)
        {
            var pdf = new PdfReport();

            pdf.DocumentPreferences(doc =>
            {
                doc.Orientation(PageOrientation.Portrait);
                doc.PageSize(PdfPageSize.A4);
                doc.DocumentMetadata(new DocumentMetadata
                {
                    Author = "Roko Skoblar",
                    Application = "RPPP-07",
                    Title = naslov
                });
                doc.Compression(new CompressionSettings
                {
                    EnableCompression = true,
                    EnableFullCompression = true
                });
            })
            .DefaultFonts(fonts => {
                fonts.Path(Path.Combine(_environment.WebRootPath, "fonts", "verdana.ttf"),
                     Path.Combine(_environment.WebRootPath, "fonts", "tahoma.ttf"));
                fonts.Size(9);
                fonts.Color(System.Drawing.Color.Black);
            })
            .MainTableTemplate(template =>
            {
                template.BasicTemplate(BasicTemplate.ProfessionalTemplate);
            })
            .MainTablePreferences(table =>
            {
                table.ColumnsWidthsType(TableColumnWidthType.Relative);
                table.SpacingAfter(4f);
            });

            return pdf;
        }
        #endregion

        public async Task<List<OsobaViewModel>> GetOsobeViewModelsAsync()
        {
            var osobe = await _context.Osobe.ToListAsync();

            var osobeViewModels = osobe.Select(d => new OsobaViewModel
            {
                Ime = d.Ime,
                Email = d.Email,
                Oib = d.Oib,
                BrMob = d.BrMob,
                IbanOsoba = d.IbanOsoba,
            }).ToList();

            return osobeViewModels;
        }
    }

    
}