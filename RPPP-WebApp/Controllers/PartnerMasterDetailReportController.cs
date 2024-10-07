using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using iText.Layout.Element;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using PdfRpt.ColumnsItemsTemplates;
using PdfRpt.Core.Contracts;
using PdfRpt.Core.Helper;
using PdfRpt.FluentInterface;
using RPPP_WebApp.Extensions;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;

namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// Stvara PDF i Excel tablice za slozene podatke partnera
    /// </summary>
    public class PartnerMasterDetailReportController : Controller
    {
        private readonly Rppp07Context ctx;
        private readonly IWebHostEnvironment environment;
        private readonly ILogger<PartnerController> logger;

        public PartnerMasterDetailReportController(Rppp07Context context, IWebHostEnvironment env, ILogger<PartnerController> logger)
        {
            ctx = context;
            environment = env;
            this.logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Stvara Excel tablicu sa podatcima partnera i njegovim zahtjevima
        /// </summary>
        /// <param name="id">id partnera za kojeg se stvara tablica</param>
        /// <returns>Excel tablica sa podatcima</returns>
        public async Task<IActionResult> PartnerMD(int id)
        {
            string naslov = "Partneri";
            var zahtjevi = await ctx.Zahtjevi
                                  .OrderBy(s => s.IdZah)
                                  .Where(s => s.IdSuradnik == id)
                                  .Include(s => s.IdVrstaZahNavigation)
                                  .ToListAsync();

            var partner = await ctx.Osobe.FirstOrDefaultAsync(s => s.IdSuradnik == id);

            PdfReport report = CreateReport(naslov);

            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(async header =>
            {
                header.CacheHeader(cache: true);
                header.CustomHeader(new MasterDetailsHeaders(naslov, id.ToString(), partner.Ime, partner.Email, partner.BrMob)
                {
                    PdfRptFont = header.PdfFont
                });
            });

            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(zahtjevi));

            report.MainTableColumns(columns =>
            {

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zahtijev>(s => s.IdZah);
                    column.Group(
                        (val1, val2) =>
                        {
                            return (int)val1 == (int)val2;
                        });
                });

                columns.AddColumn(column =>
                {
                    column.IsRowNumber(true);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Width(1);
                    column.HeaderCell("ID zahtjeva", horizontalAlignment: HorizontalAlignment.Right);
                });
                columns.AddColumn(column =>
                {
                    column.PropertyName<Zahtijev>(x => x.OpisZahtijev);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Width(4);
                    column.HeaderCell("Opis zahtjeva", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zahtijev>(x => x.Prioritet);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Width(1);
                    column.HeaderCell("Prioritet", horizontalAlignment: HorizontalAlignment.Center);

                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zahtijev>(x => x.IdVrstaZahNavigation.NazivVrstaZah);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Width(1);
                    column.HeaderCell("Vrsta zahtjeva", horizontalAlignment: HorizontalAlignment.Center);

                });


            });

            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=partner.pdf");
                return File(pdf, "application/pdf");
            }
            else
                return NotFound();
        }

        /// <summary>
        /// Stvara Excel tablicu sa podatcima svih partnera i njihovim zahtjevima
        /// </summary>
        /// <returns>Excel tablica sa podatcima</returns>
        public async Task<IActionResult> PartneriExcel()
        {
            var partneri = await GetPartneriViewModelsAsync();
            byte[] content;

            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis partnera";
                excel.Workbook.Properties.Author = "Roko Skoblar";

                foreach (var partner in partneri)
                {
                    var worksheet = excel.Workbook.Worksheets.Add(partner.Partner.Ime);

                    worksheet.Cells[1, 1].Value = "ID partnera";
                    worksheet.Cells[2, 1].Value = partner.IdSuradnik;
                    worksheet.Cells[1, 2].Value = "Ime";
                    worksheet.Cells[2, 2].Value = partner.Partner.Ime;
                    worksheet.Cells[1, 3].Value = "E-Mail";
                    worksheet.Cells[2, 3].Value = partner.Partner.Email;
                    worksheet.Cells[1, 4].Value = "Broj mobitela";
                    worksheet.Cells[2, 4].Value = partner.Partner.BrMob;

                    worksheet.Cells[4, 1].Value = "Popis zahtjeva";

                    worksheet.Cells[5, 1].Value = "Opis zahtjeva";
                    worksheet.Cells[5, 2].Value = "Prioritet";
                    worksheet.Cells[5, 3].Value = "Vrsta zahtjeva";

                    int rowIndex = 6;

                    foreach (var zahtjev in partner.Zahtijevi)
                    {
                        worksheet.Cells[rowIndex, 1].Value = zahtjev.OpisZahtijev;
                        worksheet.Cells[rowIndex, 2].Value = zahtjev.Prioritet;
                        worksheet.Cells[rowIndex, 3].Value = zahtjev.NazivVrsteZahtijeva;

                        rowIndex++;
                    }

                    worksheet.Cells[1, 1, rowIndex, 4].AutoFitColumns();
                }

                content = excel.GetAsByteArray();
            }

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "partneri.xlsx");
        }

        /// <summary>
        /// Stvara pdf dokument sa podatcima partnera i njegovim zahtjevima
        /// </summary>
        /// <returns>Pdf dokument sa podatcima</returns>
        public async Task<IActionResult> PartneriPdf()
        {
            try
            {
                string naslov = $"Prikaz partnera sa zahtjevima";

                var partneri = await ctx.Partneri
                                        .OrderBy(p => p.IdSuradnik)
                                        .Include(p => p.IdSuradnikNavigation)
                                        .Include(p => p.Zahtijevs)
                                            .ThenInclude(z => z.IdVrstaZahNavigation)
                                        .ToListAsync();

                var flattenedData = partneri.SelectMany(partner => partner.Zahtijevs.Select(zahtjev => new
                {
                    partner.IdSuradnik,
                    partner.IdSuradnikNavigation.Ime,
                    partner.IdSuradnikNavigation.Email,
                    partner.IdSuradnikNavigation.BrMob,
                    zahtjev.IdZah,
                    zahtjev.OpisZahtijev,
                    zahtjev.Prioritet,
                    zahtjev.IdVrstaZahNavigation.NazivVrstaZah,
                })).ToList();

                PdfReport report = CreateReport(naslov);

                #region Podnožje i zaglavlje
                report.PagesFooter(footer =>
                {
                    footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
                })
                .PagesHeader(header =>
                {
                    header.CacheHeader(cache: true);
                    header.CustomHeader(new MasterDetailsHeaders(naslov)
                    {
                        PdfRptFont = header.PdfFont
                    });
                });
                #endregion

                #region Postavljanje izvora podataka i stupaca
                report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(flattenedData));

                report.MainTableColumns(columns =>
                {
                    columns.AddColumn(column =>
                    {
                        column.PropertyName("Ime");
                        column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                        column.IsVisible(true);
                        column.Width(3);
                        column.HeaderCell("Ime", horizontalAlignment: HorizontalAlignment.Center);
                    });
                    columns.AddColumn(column =>
                    {
                        column.PropertyName("Email");
                        column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                        column.IsVisible(true);
                        column.Width(2);
                        column.HeaderCell("E-Mail", horizontalAlignment: HorizontalAlignment.Center);
                    });
                    columns.AddColumn(column =>
                    {
                        column.PropertyName("BrMob");
                        column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                        column.IsVisible(true);
                        column.Width(2);
                        column.HeaderCell("Broj mobitela", horizontalAlignment: HorizontalAlignment.Center);
                    });
                    columns.AddColumn(column =>
                    {
                        column.PropertyName("OpisZahtijev");
                        column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                        column.IsVisible(true);
                        column.Width(2);
                        column.HeaderCell("Opis zahtjeva", horizontalAlignment: HorizontalAlignment.Center);
                    });

                    columns.AddColumn(column =>
                    {
                        column.PropertyName("Prioritet");
                        column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                        column.IsVisible(true);
                        column.Width(2);
                        column.HeaderCell("Prioritet", horizontalAlignment: HorizontalAlignment.Center);
                    });

                    columns.AddColumn(column =>
                    {
                        column.PropertyName("NazivVrstaZah");
                        column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                        column.IsVisible(true);
                        column.Width(2);
                        column.HeaderCell("Vrsta zahtjeva", horizontalAlignment: HorizontalAlignment.Center);
                    });
                });

                #endregion
                byte[] pdf = report.GenerateAsByteArray();

                if (pdf != null)
                {
                    Response.Headers.Add("content-disposition", "inline; filename=zahtjevi.pdf");
                    return File(pdf, "application/pdf");
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.CompleteExceptionMessage());
            }

            return NotFound();
        }

        /// <summary>
        /// Custom header za pdf dokument
        /// </summary>
        #region Master-detail header
        public class MasterDetailsHeaders : IPageHeader
        {
            private string naslov;
            private string id;
            private string ime;
            private string email;
            private string brMob;

            public MasterDetailsHeaders(string naslov)
            {
                this.naslov = naslov;

            }
            public MasterDetailsHeaders(string naslov, string id, string ime, string email, string brMob)
            {
                this.naslov = naslov;
                this.id = id;
                this.ime = ime;
                this.email = email;
                this.brMob = brMob;

            }
            public IPdfFont PdfRptFont { set; get; }

            public PdfGrid RenderingGroupHeader(iTextSharp.text.Document pdfDoc, PdfWriter pdfWriter, IList<CellData> newGroupInfo, IList<SummaryCellData> summaryData)
            {
                if(id == null)
                {
                    id = newGroupInfo.GetSafeStringValueOf(nameof(Partner.IdSuradnik));
                    ime = newGroupInfo.GetSafeStringValueOf(nameof(Partner.IdSuradnikNavigation.Ime));
                    email = newGroupInfo.GetSafeStringValueOf(nameof(Partner.IdSuradnikNavigation.Email));
                    brMob = (string)newGroupInfo.GetValueOf(nameof(Partner.IdSuradnikNavigation.BrMob));
                }

                var table = new PdfGrid(relativeWidths: new[] { 2f, 5f, 2f, 3f }) { WidthPercentage = 100 };

                table.AddSimpleRow(
                    (cellData, cellProperties) =>
                    {
                        cellData.Value = "ID partnera: ";
                        cellProperties.PdfFont = PdfRptFont;
                        cellProperties.PdfFontStyle = DocumentFontStyle.Bold;
                        cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                    },
                    (cellData, cellProperties) =>
                    {
                        cellData.TableRowData = newGroupInfo;
                        var cellTemplate = new HyperlinkField(BaseColor.Black, false)
                        {
                            TextPropertyName = id,
                            BasicProperties = new CellBasicProperties
                            {
                                HorizontalAlignment = HorizontalAlignment.Left,
                                PdfFontStyle = DocumentFontStyle.Bold,
                                PdfFont = PdfRptFont
                            }
                        };

                        cellData.CellTemplate = cellTemplate;
                        cellProperties.PdfFont = PdfRptFont;
                        cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                    },
                    (cellData, cellProperties) =>
                    {
                        cellData.Value = "Ime: ";
                        cellProperties.PdfFont = PdfRptFont;
                        cellProperties.PdfFontStyle = DocumentFontStyle.Bold;
                        cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                    },
                    (cellData, cellProperties) =>
                    {
                        cellData.Value = ime;
                        cellProperties.PdfFont = PdfRptFont;
                        cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                    });

                table.AddSimpleRow(
                    (cellData, cellProperties) =>
                    {
                        cellData.Value = "E-Mail: ";
                        cellProperties.PdfFont = PdfRptFont;
                        cellProperties.PdfFontStyle = DocumentFontStyle.Bold;
                        cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                    },
                    (cellData, cellProperties) =>
                    {
                        cellData.Value = email;
                        cellProperties.PdfFont = PdfRptFont;
                        cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                    },
                    (cellData, cellProperties) =>
                    {
                        cellData.Value = "Broj mobitela: ";
                        cellProperties.PdfFont = PdfRptFont;
                        cellProperties.PdfFontStyle = DocumentFontStyle.Bold;
                        cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                    },
                    (cellData, cellProperties) =>
                    {
                        cellData.Value = brMob;
                        cellProperties.PdfFont = PdfRptFont;
                        cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                    });
                return table.AddBorderToTable(borderColor: BaseColor.LightGray, spacingBefore: 5f);
            }
            public PdfGrid RenderingReportHeader(iTextSharp.text.Document pdfDoc, PdfWriter pdfWriter, IList<SummaryCellData> summaryData)
            {
                var table = new PdfGrid(numColumns: 1) { WidthPercentage = 100 };
                table.AddSimpleRow(
                (cellData, cellProperties) =>
                {
                    cellData.Value = naslov;
                    cellProperties.PdfFont = PdfRptFont;
                    cellProperties.PdfFontStyle = DocumentFontStyle.Bold;
                    cellProperties.HorizontalAlignment = HorizontalAlignment.Center;
                });
                return table.AddBorderToTable();
            }
        }

        #endregion

        #region Private methods
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
                    Application = "HomeworkRPPP.MVC Core",
                    Title = naslov
                });
                doc.Compression(new CompressionSettings
                {
                    EnableCompression = true,
                    EnableFullCompression = true
                });
            })
            .DefaultFonts(fonts =>
            {
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

        /// <summary>
        /// Dohvaca podatke partnera i pretvara ih u view modele.
        /// </summary>
        /// <returns>Lista view model partnera</returns>
        public async Task<List<PartnerViewModel>> GetPartneriViewModelsAsync()
        {
            var partneri = await ctx.Partneri
                .Include(p => p.IdSuradnikNavigation)
                .Include(p => p.Zahtijevs)
                    .ThenInclude(z => z.IdVrstaZahNavigation)
                .ToListAsync();

            var partneriViewModels = partneri.Select(p => new PartnerViewModel
            {
                IdSuradnik = p.IdSuradnik,
                Partner = p.IdSuradnikNavigation,
                BrZahtijeva = p.Zahtijevs.Count,
                Zahtijevi = p.Zahtijevs.Select(d => new ZahtijevViewModel
                {
                    IdZah = d.IdZah,
                    OpisZahtijev = d.OpisZahtijev,
                    Prioritet = d.Prioritet,
                    NazivVrsteZahtijeva = d.IdVrstaZahNavigation.NazivVrstaZah,
                }).ToList()
            }).ToList();

            return partneriViewModels;
        }
        #endregion
    }
}