using System;
using System.Linq;
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
    public class ProjektReportController : Controller
    {
        private readonly Rppp07Context _context;
        private readonly IWebHostEnvironment _environment;

        public ProjektReportController(Rppp07Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ProjektiPdf()
        {
            var projekti = await GetProjektiViewModelsAsync();
            var naslov = "Popis projekata";

            PdfReport report = CreatePdfReport(naslov);

            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(projekti));

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
                    column.PropertyName(nameof(ProjektViewModel.NazivProjekta));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(3);
                    column.HeaderCell("Naziv projekta");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(ProjektViewModel.Kratica));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(2);
                    column.HeaderCell("Kratica");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(ProjektViewModel.Cilj));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(3);
                    column.Width(4);
                    column.HeaderCell("Cilj projekta", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(ProjektViewModel.NazivVrsteProjekta));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(3);
                    column.Width(4);
                    column.HeaderCell("Vrsta projekta", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName(nameof(ProjektViewModel.NaziviDokumentata));
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(5);
                    column.Width(4);
                    column.HeaderCell("Dokumenti", horizontalAlignment: HorizontalAlignment.Center);
                });
            });

            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=projekti.pdf");
                return File(pdf, "application/pdf");
            }
            else
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> ProjektiExcel()
        {
            var projekti = await GetProjektiViewModelsAsync();
            byte[] content;

            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis projekata";
                excel.Workbook.Properties.Author = "FER-ZPR";
                var worksheet = excel.Workbook.Worksheets.Add("Projekti");

                worksheet.Cells[1, 1].Value = "Naziv projekta";
                worksheet.Cells[1, 2].Value = "Kratica";
                worksheet.Cells[1, 3].Value = "Cilj projekta";
                worksheet.Cells[1, 4].Value = "Vrsta projekta";
                worksheet.Cells[1, 5].Value = "Dokumenti";

                for (int i = 0; i < projekti.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = projekti[i].NazivProjekta;
                    worksheet.Cells[i + 2, 2].Value = projekti[i].Kratica;
                    worksheet.Cells[i + 2, 3].Value = projekti[i].Cilj;
                    worksheet.Cells[i + 2, 4].Value = projekti[i].NazivVrsteProjekta;
                    worksheet.Cells[i + 2, 5].Value = projekti[i].NaziviDokumentata;
                }

                worksheet.Cells[1, 1, projekti.Count + 1, 5].AutoFitColumns();

                content = excel.GetAsByteArray();
            }

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "projekti.xlsx");
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

        public async Task<List<ProjektViewModel>> GetProjektiViewModelsAsync()
        {
            var projekti = await _context.Projekti
                .Include(p => p.IdVrsteProjektaNavigation)
                .Include(p => p.Dokumenti)
                .ToListAsync();

            var projektiViewModels = projekti.Select(p => new ProjektViewModel
            {
                NazivProjekta = p.NazivProjekt,
                Kratica = p.Kratica,
                Cilj = p.Cilj,
                IdProjekta = p.IdProjekt,
                NazivVrsteProjekta = p.IdVrsteProjektaNavigation?.NazivVrsteProjekta,
                NaziviDokumentata = string.Join(", ", p.Dokumenti.Select(d => d.NazivDokument))
            }).ToList();

            return projektiViewModels;
        }
    }

    
}