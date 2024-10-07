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
    public class DokumentReportController : Controller
    {
        private readonly Rppp07Context _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DokumentController> logger;

        public DokumentReportController(Rppp07Context context, IWebHostEnvironment environment, ILogger<DokumentController> logger)
        {
            _context = context;
            _environment = environment;
            this.logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }


        public async Task<IActionResult> ImportDokumenti(IFormFile importFile)
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
                        string nazivDokumenta = worksheet.Cells[row, 1].Value.ToString().Trim();
                        string nazivDatoteke = worksheet.Cells[row, 2].Value.ToString().Trim();
                        string nazivProjekta = worksheet.Cells[row, 3].Value.ToString().Trim();
                        string vrstaDokumenta = worksheet.Cells[row, 4].Value.ToString().Trim();
                        string tekstDokumenta = worksheet.Cells[row, 5].Value.ToString().Trim();

                        if (!string.IsNullOrEmpty(tekstDokumenta))
                        {
                            byte[] byteArray = Encoding.UTF8.GetBytes(tekstDokumenta);

                            int idDokumenta = _context.Dokumenti.Count() + 1; 

                            string folderPath = Path.Combine("Documents");

                            if (!Directory.Exists(folderPath))
                            {
                                Directory.CreateDirectory(folderPath);
                            }

                            string fileName = $"{idDokumenta}_{nazivDatoteke}";
                            string filePath = Path.Combine(folderPath, fileName);
                            await System.IO.File.WriteAllTextAsync(filePath, tekstDokumenta);
                        }

                        Projekt projekt = await _context.Projekti
                            .FirstOrDefaultAsync(p => p.NazivProjekt == nazivProjekta);

                        if (projekt == null)
                        {
                            worksheet.Cells[row, 7].Value = "INVALID Projekt";
                            worksheet.Cells[row, 6].Value = "ERROR";
                            logger.LogError("Pogreška prilikom dodavanja novog dokumenta: Nepostojeći projekt");
                            continue;
                        }

                        VrstaDoc vrstaDoc = await _context.VrstaDocsa
                            .FirstOrDefaultAsync(v => v.NazivVrstaDoc == vrstaDokumenta);

                        if (vrstaDokumenta == null)
                        {
                            worksheet.Cells[row, 7].Value = "INVALID VrstaDokumenta";
                            worksheet.Cells[row, 6].Value = "ERROR";
                            logger.LogError("Pogreška prilikom dodavanja novog dokumenta: Nepostojeća vrsta dokumenta");
                            continue;
                        }

                        Dokument dokument = new Dokument
                        {
                            NazivDokument = nazivDokumenta,
                            NazivDatoteke = nazivDatoteke,
                            IdProjekt = projekt.IdProjekt,
                            IdVrsta = vrstaDoc.IdVrsteDoc,
                            Dokument1 = Encoding.UTF8.GetBytes(tekstDokumenta)
                        };

                        try
                        {
                            _context.Add(dokument);
                            await _context.SaveChangesAsync();

                            logger.LogInformation($"Dokument uspješno dodan. NazivDokumenta={dokument.NazivDokument}");
                            worksheet.Cells[row, 6].Value = "ADDED";
                        }
                        catch (Exception exc)
                        {
                            worksheet.Cells[row, 6].Value = "ERROR";
                            logger.LogError("Pogreška prilikom dodavanja novog dokumenta: {0}", exc.CompleteExceptionMessage());
                            ModelState.AddModelError(string.Empty, exc.Message);
                        }
                    }

                    result.Workbook.Worksheets.Add("StatusiDodavanjaDokumenata", worksheet);
                }
            }

            return File(result.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StatusiDodavanjaDokumenata.xlsx");
        }
        public async Task<IActionResult> DokumentiPdf()
        {
            try
            {
                var dokumenti = await GetDokumentiViewModelsAsync();
                var naslov = "Popis dokumenata";

                PdfReport report = CreatePdfReport(naslov);

                report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(dokumenti));

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
                        column.PropertyName(nameof(DokumentViewModel.NazivDokument));
                        column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                        column.IsVisible(true);
                        column.Order(1);
                        column.Width(3);
                        column.HeaderCell("Naziv dokumenta");
                    });

                    columns.AddColumn(column =>
                    {
                        column.PropertyName(nameof(DokumentViewModel.NazivDatoteke));
                        column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                        column.IsVisible(true);
                        column.Order(2);
                        column.Width(2);
                        column.HeaderCell("Naziv datoteke");
                    });

                    columns.AddColumn(column =>
                    {
                        column.PropertyName(nameof(DokumentViewModel.NazivVrsteDoc));
                        column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                        column.IsVisible(true);
                        column.Order(3);
                        column.Width(4);
                        column.HeaderCell("Vrsta dokumenta", horizontalAlignment: HorizontalAlignment.Center);
                    });

                    columns.AddColumn(column =>
                    {
                        column.PropertyName(nameof(DokumentViewModel.NazivProjekt));
                        column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                        column.IsVisible(true);
                        column.Order(3);
                        column.Width(4);
                        column.HeaderCell("Projekt", horizontalAlignment: HorizontalAlignment.Center);
                    });

                    columns.AddColumn(column =>
                    {
                        column.PropertyName(nameof(DokumentViewModel.TextContent));
                        column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                        column.IsVisible(true);
                        column.Order(3);
                        column.Width(4);
                        column.HeaderCell("Dokument", horizontalAlignment: HorizontalAlignment.Center);
                    });
                });

                byte[] pdf = report.GenerateAsByteArray();

                if (pdf != null)
                {
                    Response.Headers.Add("content-disposition", "inline; filename=dokumenti.pdf");
                    return File(pdf, "application/pdf");
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.CompleteExceptionMessage());
            }

            return NotFound();
        }

        public async Task<IActionResult> DokumentiExcel()
        {
            var dokumenti = await _context.Dokumenti.Include(d => d.IdProjektNavigation)
                                                    .Include(d => d.IdVrstaNavigation)
                                                    .ToListAsync();
            byte[] content;

            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis dokumenata";
                excel.Workbook.Properties.Author = "FER-ZPR";
                var worksheet = excel.Workbook.Worksheets.Add("Dokumenti");

                // Dodavanje zaglavlja
                worksheet.Cells[1, 1].Value = "Naziv dokumenta";
                worksheet.Cells[1, 2].Value = "Naziv datoteke";
                worksheet.Cells[1, 3].Value = "Projekt";
                worksheet.Cells[1, 4].Value = "Vrsta";
                worksheet.Cells[1, 5].Value = "Dokument";

                // Dodavanje podataka
                for (int i = 0; i < dokumenti.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = dokumenti[i].NazivDokument;
                    worksheet.Cells[i + 2, 2].Value = dokumenti[i].NazivDatoteke;
                    worksheet.Cells[i + 2, 3].Value = dokumenti[i].IdProjektNavigation?.NazivProjekt ?? string.Empty;
                    worksheet.Cells[i + 2, 4].Value = dokumenti[i].IdVrstaNavigation?.NazivVrstaDoc ?? string.Empty;
                    worksheet.Cells[i + 2, 5].Value = worksheet.Cells[i + 2, 5].Value = dokumenti[i].Dokument1 != null
                                                ? Encoding.UTF8.GetString(dokumenti[i].Dokument1)
                                                : "Nema prilozenog dokumenta";
                }

                worksheet.Cells[1, 1, dokumenti.Count + 1, 4].AutoFitColumns();

                content = excel.GetAsByteArray();
            }

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "dokumenti.xlsx");
        }

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
                    Author = "Filip Buhinicek",
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

        public async Task<List<DokumentViewModel>> GetDokumentiViewModelsAsync()
        {
            var dokumenti = await _context.Dokumenti
                .Include(d => d.IdProjektNavigation)
                .Include(d => d.IdVrstaNavigation)
                .ToListAsync();

            var dokumentiViewModels = dokumenti.Select(d => new DokumentViewModel
            {
                NazivDokument = d.NazivDokument,
                NazivDatoteke = d.NazivDatoteke,
                IdProjekt = d.IdProjekt,
                IdVrsta = d.IdVrsta,
                IdDoc = d.IdDoc,
                Dokument1 = d.Dokument1,
                TextContent = d.Dokument1 != null
                                 ? Encoding.UTF8.GetString(d.Dokument1)
                                 : "Nema prilozenog dokumenta",
                NazivProjekt = d.IdProjektNavigation.NazivProjekt,
                NazivVrsteDoc = d.IdVrstaNavigation.NazivVrstaDoc
            }).ToList();

            return dokumentiViewModels;
        }
    }

    
}