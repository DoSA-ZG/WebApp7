using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
    /// Kontroler razred za generiranje excel i pdf dokumenata
    /// </summary>
    public class ReportController : Controller
    {
        private readonly Rppp07Context ctx;
        private readonly IWebHostEnvironment environment;
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public ReportController(Rppp07Context ctx, IWebHostEnvironment environment)
        {
            this.ctx = ctx;
            this.environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        //excel report

        /// <summary>
        /// Izgrađuje excel datoteku sa zahtjevima
        /// </summary>
        /// <returns>FIle tipa Excel datoteka izgrađena sa zahtjevima iz baze podataka</returns>
        public async Task<IActionResult> ZahtjeviExcel()
        {
            var zahtjevi = await ctx.Zahtjevi
                                  .AsNoTracking()
                                  .OrderBy(d => d.OpisZahtijev)
                                  .ToListAsync();
            var vrsteZah = await ctx.VrstaZahtjeva
                                  .AsNoTracking()
                                  .OrderBy(d => d.IdVrstaZah)
                                  .ToListAsync();
            var osobe = await ctx.Osobe
                                  .AsNoTracking()
                                  .OrderBy(d => d.IdSuradnik)
                                  .ToListAsync();

            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis zahtjeva";
                excel.Workbook.Properties.Author = "Kristijan Košta";
                var worksheet = excel.Workbook.Worksheets.Add("Zahtjevi");

                //First add the headers
                worksheet.Cells[1, 1].Value = "Id zatjeva";
                worksheet.Cells[1, 2].Value = "Opis zahtjeva";
                worksheet.Cells[1, 3].Value = "Prioritet";
                worksheet.Cells[1, 4].Value = "Vrsta zahtjeva";
                worksheet.Cells[1, 5].Value = "Suradnik";

                for (int i = 0; i < zahtjevi.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = zahtjevi[i].IdZah;
                    worksheet.Cells[i + 2, 2].Value = zahtjevi[i].OpisZahtijev;
                    worksheet.Cells[i + 2, 2].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[i + 2, 3].Value = zahtjevi[i].Prioritet;
                    worksheet.Cells[i + 2, 4].Value = vrsteZah.Where(x => x.IdVrstaZah == zahtjevi[i].IdVrstaZah).First().NazivVrstaZah;
                    worksheet.Cells[i + 2, 5].Value = osobe.Where(x => x.IdSuradnik == zahtjevi[i].IdSuradnik).First().Ime;
                }

                worksheet.Cells[1, 1, zahtjevi.Count + 1, 4].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "zahtjevi.xlsx");
        }

        /// <summary>
        ///  Metoda koja generira master-detail excel tablicu za neki određeni zahtijev
        /// </summary>
        /// <param name="id">Id zahtijeva za koji generiramo master-detail excel tablicu</param>
        /// <returns>File tipa Excel tablica</returns
        public async Task<IActionResult> ZahtjeviMasterDetailExcel(int id)
        {

            var ime = "";
           
            var zahtjevi = await ctx.Zahtjevi
                                 .AsNoTracking()
                                 .OrderBy(d => d.IdZah)
                                 .Include(d => d.IdVrstaZahNavigation)
                                 .Include(d => d.IdSuradnikNavigation.IdSuradnikNavigation)
                                 .ToListAsync();
            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {

                excel.Workbook.Properties.Title = "Popis zahtjeva";
                excel.Workbook.Properties.Author = "Kristijan Košta";
                var worksheet = excel.Workbook.Worksheets.Add($"Zahtjev s id: {id}");

                worksheet.Cells[1, 2].Value = "Opis zahtjeva";
                worksheet.Cells[1, 3].Value = "Prioritet";
                worksheet.Cells[1, 4].Value = "Vrsta zahtjeva";
                worksheet.Cells[1, 5].Value = "Suradnik";
                worksheet.Cells[1, 1].Value = "ID Zahtjeva";


                var zahtjev = zahtjevi.FirstOrDefault(d => d.IdZah == id);

                ime = zahtjev.OpisZahtijev;

                worksheet.Cells[2, 1].Value = zahtjev.IdZah;
                worksheet.Cells[2, 2].Value = zahtjev.OpisZahtijev;
                worksheet.Cells[2, 3].Value = zahtjev.Prioritet;
                worksheet.Cells[2, 4].Value = zahtjev.IdVrstaZahNavigation.NazivVrstaZah;
                worksheet.Cells[2, 5].Value = zahtjev.IdSuradnikNavigation.IdSuradnikNavigation.Ime;

                worksheet.Cells[4, 1].Value = "Zadaci:";

                worksheet.Cells[5, 1].Value = "ID zadatka";
                worksheet.Cells[5, 2].Value = "Status zadatka";
                worksheet.Cells[5, 3].Value = "Trajanje zadatka";
                worksheet.Cells[5, 4].Value = "Vrsta zadatka";

                    var zadaci = await ctx.Zadaci
                                      .AsNoTracking()
                                      .Where(d => d.IdZah == id)
                                      .OrderBy(d => d.IdZad)
                                      .Include(d=> d.IdVrstaZadNavigation)
                                      .ToListAsync();

                    for (int k = 0; k < zadaci.Count; k++)
                    {
                    
                           
                        worksheet.Cells[6 + k, 1].Value = zadaci[k].IdZad;
                        worksheet.Cells[6 + k, 2].Value = zadaci[k].Status;
                        worksheet.Cells[6 + k, 3].Value = zadaci[k].Trajanje;
                        worksheet.Cells[6 + k, 4].Value = zadaci[k].IdVrstaZadNavigation.NazivVrstaZad;
                    }
                   

                

                worksheet.Cells[1, 1, zahtjevi.Count + 1, 4].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, $"{ime}.xlsx");
        }

        public async Task<IActionResult> ZadaciExcel()
        {
            var zadaci = await ctx.Zadaci
                                  .AsNoTracking()
                                  .OrderBy(d => d.IdZad)
                                  .ToListAsync();
            var zahtjevi = await ctx.Zahtjevi
                                  .AsNoTracking()
                                  .OrderBy(d => d.IdZah)
                                  .ToListAsync();
            var vrsteZadataka = await ctx.VrstaZadatka
                                  .AsNoTracking()
                                  .OrderBy(d => d.IdVrstaZad)
                                  .ToListAsync();

            byte[] content;
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis zadataka";
                excel.Workbook.Properties.Author = "Kristijan Košta";
                var worksheet = excel.Workbook.Worksheets.Add("Zadaci");

                //First add the headers
                worksheet.Cells[1, 1].Value = "Id zadatka";
                worksheet.Cells[1, 2].Value = "Status";
                worksheet.Cells[1, 3].Value = "Trajanje";
                worksheet.Cells[1, 4].Value = "Zahtijev";
                worksheet.Cells[1, 5].Value = "Vrsta Zadatka";

                for (int i = 0; i < zadaci.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = zadaci[i].IdZad;
                    worksheet.Cells[i + 2, 2].Value = zadaci[i].Status;
                    worksheet.Cells[i + 2, 2].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[i + 2, 3].Value = zadaci[i].Trajanje;
                    worksheet.Cells[i + 2, 4].Value = zahtjevi.Where(x => x.IdZah == zadaci[i].IdZah).First().OpisZahtijev;
                    worksheet.Cells[i + 2, 5].Value = vrsteZadataka.Where(x => x.IdVrstaZad == zadaci[i].IdVrstaZad).First().NazivVrstaZad;
                }

                worksheet.Cells[1, 1, zadaci.Count + 1, 4].AutoFitColumns();

                content = excel.GetAsByteArray();
            }
            return File(content, ExcelContentType, "zadaci.xlsx");
        }

        public async Task<IActionResult> Zahtjevi()
        {
            string naslov = "Popis zahtjeva";
            var zahtjevi = await ctx.Zahtjevi
                                  .AsNoTracking()
                                  .OrderBy(d => d.OpisZahtijev)
                                  .Include(d => d.IdSuradnikNavigation)
                                  .Include(d => d.IdSuradnikNavigation.IdSuradnikNavigation)
                                  .Include(d => d.IdVrstaZahNavigation)
                                  .ToListAsync();

            var zadaci = await ctx.VrstaZahtjeva
                                    .AsNoTracking()
                                    .OrderBy(d => d.IdVrstaZah)
                                    .ToListAsync();

            var osobe = await ctx.Osobe
                                    .AsNoTracking()
                                    .OrderBy(d => d.IdSuradnik)
                                    .ToListAsync();

            PdfReport report = CreateReport(naslov);

            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                    defaultHeader.Message(naslov);
                });
            });

            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(zahtjevi));

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
                    column.PropertyName<Zahtijev>(x => x.OpisZahtijev);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(2);
                    column.HeaderCell("Opis zahtijeva");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zahtijev>(x => x.Prioritet);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(3);
                    column.HeaderCell("Prioritet zahtijeva", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zahtijev>(x => x.IdSuradnikNavigation.IdSuradnikNavigation.Ime);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(3);
                    column.HeaderCell("Suradnik", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                { 
                    column.PropertyName<Zahtijev>(x => x.IdVrstaZahNavigation.NazivVrstaZah);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(3);
                    column.HeaderCell("Vrsta zahtjeva", horizontalAlignment: HorizontalAlignment.Center);
                });


            });

            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "attachment; filename=zahtijevi.pdf");
                return File(pdf, "application/pdf");
                //return File(pdf, "application/pdf", "drzave.pdf"); //Otvara save as dialog
            }
            else
            {
                return NotFound();
            }

        }

        public async Task<IActionResult> Zadaci()
        {
            string naslov = "Popis zadataka";
            var zadaci = await ctx.Zadaci
                                  .AsNoTracking()
                                  .OrderBy(d => d.IdZad)
                                  .Include(d => d.IdVrstaZadNavigation)
                                  .Include(d => d.IdZahNavigation)
                                  .ToListAsync();

            var osobe = await ctx.Osobe
                                    .AsNoTracking()
                                    .OrderBy(d => d.IdSuradnik)
                                    .ToListAsync();

            PdfReport report = CreateReport(naslov);

            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.DefaultHeader(defaultHeader =>
                {
                    defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                    defaultHeader.Message(naslov);
                });
            });

            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(zadaci));

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
                    column.PropertyName<Zadatak>(x => x.Status);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(1);
                    column.Width(2);
                    column.HeaderCell("Status");
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zadatak>(x => x.Trajanje);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(3);
                    column.HeaderCell("Trajanje", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zadatak>(x => x.IdZahNavigation.OpisZahtijev);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(3);
                    column.HeaderCell("Zahtjev", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zadatak>(x => x.IdVrstaZadNavigation.NazivVrstaZad);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Order(2);
                    column.Width(3);
                    column.HeaderCell("Vrsta zadatka", horizontalAlignment: HorizontalAlignment.Center);
                });


            });

            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=zadaci.pdf");
                return File(pdf, "application/pdf");
                //return File(pdf, "application/pdf", "drzave.pdf"); //Otvara save as dialog
            }
            else
            {
                return NotFound();
            }

        }

        private PdfReport CreateReport(string naslov)
        {
            var pdf = new PdfReport();

            pdf.DocumentPreferences(doc =>
            {
                doc.Orientation(PageOrientation.Portrait);
                doc.PageSize(PdfPageSize.A4);
                doc.DocumentMetadata(new DocumentMetadata
                {
                    Author = "Kristijan Košta",
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