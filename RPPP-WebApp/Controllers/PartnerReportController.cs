using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using PdfRpt.Core.Contracts;
using PdfRpt.FluentInterface;
using RPPP_WebApp.Extensions;
using RPPP_WebApp.Models;


namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// Stvara PDF i Excel tablice za jednostavne podatke partnera
    /// </summary>
    public class PartnerReportController : Controller
    {
        private readonly Rppp07Context ctx;
        private readonly IWebHostEnvironment environment;
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private readonly ILogger<PartnerReportController> logger;

        public PartnerReportController(Rppp07Context ctx, IWebHostEnvironment environment, ILogger<PartnerReportController> logger)
        {
            this.ctx = ctx;
            this.environment = environment;
            this.logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Stvara Excel tablicu s podatcima svih partnera
        /// </summary>
        /// <returns>Excel tablica s podatcima partnera</returns>
        public async Task<IActionResult> PartneriExcel()
        {
            var partneri = await ctx.Partneri
                                  .AsNoTracking()
                                  .Include(p => p.IdSuradnikNavigation)
                                  .Include(p => p.Zahtijevs)
                                  .OrderBy(d => d.IdSuradnik)
                                  .ToListAsync();

            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis partnera";
                excel.Workbook.Properties.Author = "Roko Skoblar";
                var worksheet = excel.Workbook.Worksheets.Add("Partneri");

                worksheet.Cells[1, 1].Value = "ID partnera";
                worksheet.Cells[1, 2].Value = "Ime";
                worksheet.Cells[1, 3].Value = "E-Mail";
                worksheet.Cells[1, 4].Value = "Broj mobitela";
                worksheet.Cells[1, 5].Value = "Broj zahtjeva";

                for (int i = 0; i < partneri.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = partneri[i].IdSuradnik;
                    worksheet.Cells[i + 2, 2].Value = partneri[i].IdSuradnikNavigation.Ime;
                    worksheet.Cells[i + 2, 3].Value = partneri[i].IdSuradnikNavigation.Email;
                    worksheet.Cells[i + 2, 4].Value = partneri[i].IdSuradnikNavigation.BrMob;
                    worksheet.Cells[i + 2, 5].Value = partneri[i].Zahtijevs.Count;
                }

                worksheet.Cells[1, 1, partneri.Count + 1, 4].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "partneri.xlsx");
        }

        public async Task<IActionResult> PartnerMasterDetailExcel(int id)
        {
            var partner = await ctx.Partneri
                                 .AsNoTracking()
                                 .Where(p => p.IdSuradnik == id)
                                 .Include(d => d.IdSuradnikNavigation)
                                 .Include(d => d.Zahtijevs)
                                 .SingleOrDefaultAsync();

            var ime = partner.IdSuradnikNavigation.Ime.Replace(' ', '_');

            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {

                excel.Workbook.Properties.Title = "Partner ID=" + id;
                excel.Workbook.Properties.Author = "Roko Skoblar";
                var worksheet = excel.Workbook.Worksheets.Add($"Partner s id: {id}");

                worksheet.Cells[1, 1].Value = "ID partnera";
                worksheet.Cells[1, 2].Value = "Ime";
                worksheet.Cells[1, 3].Value = "E-Mail";
                worksheet.Cells[1, 4].Value = "Broj mobitela";

                worksheet.Cells[2, 1].Value = partner.IdSuradnik;
                worksheet.Cells[2, 2].Value = partner.IdSuradnikNavigation.Ime;
                worksheet.Cells[2, 3].Value = partner.IdSuradnikNavigation.Email;
                worksheet.Cells[2, 4].Value = partner.IdSuradnikNavigation.BrMob;

                worksheet.Cells[4, 1].Value = "Zahtjevi:";

                worksheet.Cells[5, 1].Value = "ID zahtjeva";
                worksheet.Cells[5, 2].Value = "Opis zahtjeva";
                worksheet.Cells[5, 3].Value = "Prioritet";
                worksheet.Cells[5, 4].Value = "Vrsta zahtjeva";

                var zahtjevi = await ctx.Zahtjevi
                                        .AsNoTracking()
                                        .Where(z => z.IdSuradnik == partner.IdSuradnik)
                                        .Include(z => z.IdVrstaZahNavigation)
                                        .ToArrayAsync();

                for (int i = 0; i < zahtjevi.Length; i++)
                {
                    worksheet.Cells[6 + i, 1].Value = zahtjevi[i].IdZah;
                    worksheet.Cells[6 + i, 2].Value = zahtjevi[i].OpisZahtijev;
                    worksheet.Cells[6 + i, 3].Value = zahtjevi[i].Prioritet;
                    worksheet.Cells[6 + i, 4].Value = zahtjevi[i].IdVrstaZahNavigation.NazivVrstaZah;
                }

                worksheet.Cells[1, 1, zahtjevi.Length + 1, 4].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, $"{ime}.xlsx");
        }

        public async Task<IActionResult> ZahtijeviExcel()
        {
            var zadaci = await ctx.Zahtjevi
                                .OrderByDescending(d => d.IdZah)
                                .ToListAsync();
            byte[] content;
            using (ExcelPackage excel = zadaci.CreateExcel("Zahtjevi"))
            {
                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "zahtjevi.xlsx");
        }

        /// <summary>
        /// Stvara pdf dokument s podatcima svih partnera
        /// </summary>
        /// <returns>PDF dokument s podatcima partnera</returns>
        public async Task<IActionResult> Partneri()
        {
            try
            {
                string naslov = "Popis partnera";
                var partneri = await ctx.Partneri
                                      .AsNoTracking()
                                      .Include(p => p.IdSuradnikNavigation)
                                      .Include(p => p.Zahtijevs)
                                      .OrderBy(d => d.IdSuradnik)
                                      .ToListAsync();

                PdfReport report = CreateReport(naslov);

                report.PagesFooter(footer =>
                {
                    footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
                })
                .PagesHeader(header =>
                {
                    header.CacheHeader(cache: true);
                    header.DefaultHeader(defaultHeader =>
                    {
                        defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                        defaultHeader.Message(naslov);
                    });
                });

                report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(partneri));

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
                        column.PropertyName<Partner>(x => x.IdSuradnikNavigation.Ime);
                        column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                        column.IsVisible(true);
                        column.Order(1);
                        column.Width(2);
                        column.HeaderCell("Ime");
                    });

                    columns.AddColumn(column =>
                    {
                        column.PropertyName<Partner>(x => x.IdSuradnikNavigation.Email);
                        column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                        column.IsVisible(true);
                        column.Order(2);
                        column.Width(3);
                        column.HeaderCell("E-Mail", horizontalAlignment: HorizontalAlignment.Center);
                    });

                    columns.AddColumn(column =>
                    {
                        column.PropertyName<Partner>(x => x.IdSuradnikNavigation.BrMob);
                        column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                        column.IsVisible(true);
                        column.Order(2);
                        column.Width(3);
                        column.HeaderCell("Broj mobitela", horizontalAlignment: HorizontalAlignment.Center);
                    });
                });

                byte[] pdf = report.GenerateAsByteArray();

                if (pdf != null)
                {
                    Response.Headers.Add("content-disposition", "inline; filename=partneri.pdf");
                    return File(pdf, "application/pdf");
                }
                else
                {
                    return NotFound();
                }
            }
            catch(Exception ex)
            {
                logger.LogError(ex, ex.CompleteExceptionMessage());
            }

            return NotFound();
        }

        /// <summary>
        /// Postavlja postavke pdf dokumenta
        /// </summary>
        /// <param name="naslov">naslov dokumenta</param>
        /// <returns>postavljenju klasu pdf dokumenta</returns>
        private PdfReport CreateReport(string naslov)
        {
            var pdf = new PdfReport();

            pdf.DocumentPreferences(doc =>
            {
                doc.Orientation(PageOrientation.Portrait);
                doc.PageSize(PdfPageSize.A4);
                doc.DocumentMetadata(new DocumentMetadata
                {
                    Author = "Roko Skoblar",
                    Application = "RPPPHomework.MVC Core",
                    Title = naslov
                });
                doc.Compression(new CompressionSettings
                {
                    EnableCompression = true,
                    EnableFullCompression = true
                });

            })
            .DefaultFonts(fonts => {
                fonts.Path(Path.Combine(environment.WebRootPath, "fonts", "verdana.ttf"),
                        Path.Combine(environment.WebRootPath, "fonts", "tahoma.ttf"));
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
                 //table.NumberOfDataRowsPerPage(20);
                 table.GroupsPreferences(new GroupsPreferences
                 {
                     GroupType = GroupType.HideGroupingColumns,
                     RepeatHeaderRowPerGroup = true,
                     ShowOneGroupPerPage = true,
                     SpacingBeforeAllGroupsSummary = 5f,
                     NewGroupAvailableSpacingThreshold = 150,
                     SpacingAfterAllGroupsSummary = 5f
                 });
                 table.SpacingAfter(4f);
             });

            return pdf;

        }
    }
}