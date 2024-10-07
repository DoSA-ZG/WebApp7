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
    public class ProjektMasterDetailReportController : Controller
    {
        private readonly Rppp07Context ctx;
        private readonly IWebHostEnvironment environment;

        private readonly ILogger<DokumentController> logger;

        public ProjektMasterDetailReportController(Rppp07Context context, IWebHostEnvironment env, ILogger<DokumentController> logger)
        {
            ctx = context;
            environment = env;
            this.logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ImportProjekti(IFormFile importFile)
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

                        string vrstaProjektaNaziv = worksheet.Cells[row, 4].Value.ToString().Trim();
                        int idVrsteProjekta = await ctx.VrstaProjekta
                            .Where(v => v.NazivVrsteProjekta == vrstaProjektaNaziv)
                            .Select(v => v.IdVrsteProjekta)
                            .FirstOrDefaultAsync();

                        if (idVrsteProjekta < 1)
                        {
                            worksheet.Cells[row, 7].Value = "INVALID VrstaProjekta";
                            worksheet.Cells[row, 6].Value = "ERROR";
                            logger.LogError("Pogreška prilikom dodavanja novog projekta: Nepostojeca vrsta projekta");
                            continue;
                        }

                        Projekt projekt = new Projekt
                        {
                            IdProjekt = ctx.Projekti.Count() + 1,
                            NazivProjekt = worksheet.Cells[row, 1].Value.ToString().Trim(),
                            Kratica = worksheet.Cells[row, 2].Value.ToString().Trim(),
                            Cilj = worksheet.Cells[row, 3].Value.ToString().Trim(),
                            IdVrsteProjekta = idVrsteProjekta
                        };

                        try
                        {
                            ctx.Add(projekt);
                            await ctx.SaveChangesAsync();

                            logger.LogInformation($"Projekt uspješno dodan. NazivProjekta={projekt.NazivProjekt}");
                            worksheet.Cells[row, 6].Value = "ADDED";
                        }
                        catch (Exception exc)
                        {
                            worksheet.Cells[row, 6].Value = "ERROR";
                            logger.LogError("Pogreška prilikom dodavanja novog projekta: {0}", exc.CompleteExceptionMessage());
                            ModelState.AddModelError(string.Empty, exc.Message);
                        }
                    }

                    result.Workbook.Worksheets.Add("StatusiDodavanjaProjekta", worksheet);
                }
            }

            return File(result.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StatusiDodavanjaProjekta.xlsx");
        }

        public async Task<IActionResult> ProjektiExcel()
        {
            var projekti = await GetProjektiViewModelsAsync();
            byte[] content;

            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Properties.Title = "Popis projekata";
                excel.Workbook.Properties.Author = "Filip Buhinicek";

                foreach (var projekt in projekti)
                {
                    var worksheet = excel.Workbook.Worksheets.Add(projekt.NazivProjekta);

                    worksheet.Cells[1, 1].Value = "Naziv projekta";
                    worksheet.Cells[2, 1].Value = projekt.NazivProjekta;
                    worksheet.Cells[1, 2].Value = "Kratica";
                    worksheet.Cells[2, 2].Value = projekt.Kratica;
                    worksheet.Cells[1, 3].Value = "Cilj projekta";
                    worksheet.Cells[2, 3].Value = projekt.Cilj;
                    worksheet.Cells[1, 4].Value = "Vrsta projekta";
                    worksheet.Cells[2, 4].Value = projekt.NazivVrsteProjekta;

                    worksheet.Cells[4, 1].Value = "Popis dokumenata";

                    worksheet.Cells[5, 1].Value = "Naziv dokumenta";
                    worksheet.Cells[5, 2].Value = "Naziv datoteke";
                    worksheet.Cells[5, 3].Value = "Vrsta dokumenta";
                    worksheet.Cells[5, 4].Value = "Dokument";

                    int rowIndex = 6;

                    foreach (var dokument in projekt.Dokumenti)
                    {
                        worksheet.Cells[rowIndex, 1].Value = dokument.NazivDokument;
                        worksheet.Cells[rowIndex, 2].Value = dokument.NazivDatoteke;
                        worksheet.Cells[rowIndex, 3].Value = dokument.NazivVrsteDoc;
                        worksheet.Cells[rowIndex, 4].Value = dokument.TextContent;

                        rowIndex++;
                    }

                    worksheet.Cells[1, 1, rowIndex, 4].AutoFitColumns();
                }

                content = excel.GetAsByteArray();
            }

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "projekti.xlsx");
        }

       public async Task<IActionResult> ProjektiPdf()
        {
        string naslov = $"Prikaz projekata sa dokumentima";

        var projekti = await ctx.Projekti                           
                                .OrderBy(p => p.IdProjekt)
                                .Include(p => p.IdVrsteProjektaNavigation)
                                .Include(p => p.Dokumenti)
                                    .ThenInclude(d => d.IdVrstaNavigation)
                                .ToListAsync();

        var flattenedData = projekti.SelectMany(projekt => projekt.Dokumenti.Select(dokument => new
        {
            ProjektId = projekt.IdProjekt,
            ProjektNaziv = projekt.NazivProjekt,
            ProjektVrsta = projekt.IdVrsteProjektaNavigation.NazivVrsteProjekta,
            ProjektCilj = projekt.Cilj,
            DokumentId = dokument.IdDoc,
            DokumentNaziv = dokument.NazivDokument,
            DokumentNazivDatoteke = dokument.NazivDatoteke,
            DokumentVrsta = dokument.IdVrstaNavigation.NazivVrstaDoc,
            Dokument = dokument.Dokument1 != null
                            ? Encoding.UTF8.GetString(dokument.Dokument1)
                            : "Nema prilozenog dokumenta",
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
            column.PropertyName("ProjektNaziv");
            column.CellsHorizontalAlignment(HorizontalAlignment.Center);
            column.IsVisible(true);
            column.Width(3);
            column.HeaderCell("Naziv Projekta", horizontalAlignment: HorizontalAlignment.Center);
            });
            columns.AddColumn(column =>
            {
            column.PropertyName("ProjektCilj");
            column.CellsHorizontalAlignment(HorizontalAlignment.Center);
            column.IsVisible(true);
            column.Width(2);
            column.HeaderCell("Cilj projekta", horizontalAlignment: HorizontalAlignment.Center);
            });
            columns.AddColumn(column =>
            {
            column.PropertyName("ProjektVrsta");
            column.CellsHorizontalAlignment(HorizontalAlignment.Center);
            column.IsVisible(true);
            column.Width(2);
            column.HeaderCell("Vrsta projekta", horizontalAlignment: HorizontalAlignment.Center);
            });
            columns.AddColumn(column =>
            {
            column.PropertyName("DokumentNaziv");
            column.CellsHorizontalAlignment(HorizontalAlignment.Center);
            column.IsVisible(true);
            column.Width(2);
            column.HeaderCell("Naziv dokumenta", horizontalAlignment: HorizontalAlignment.Center);
            });

            columns.AddColumn(column =>
            {
            column.PropertyName("DokumentNazivDatoteke");
            column.CellsHorizontalAlignment(HorizontalAlignment.Right);
            column.IsVisible(true);
            column.Width(2);
            column.HeaderCell("Naziv datoteke", horizontalAlignment: HorizontalAlignment.Center);
            });

            columns.AddColumn(column =>
            {
            column.PropertyName("DokumentVrsta");
            column.CellsHorizontalAlignment(HorizontalAlignment.Right);
            column.IsVisible(true);
            column.Width(2);
            column.HeaderCell("Vrsta dokumenta", horizontalAlignment: HorizontalAlignment.Center);
            });
            
            columns.AddColumn(column =>
            {
            column.PropertyName("Dokument");
            column.CellsHorizontalAlignment(HorizontalAlignment.Right);
            column.IsVisible(true);
            column.Width(3);
            column.HeaderCell("Dokument", horizontalAlignment: HorizontalAlignment.Center);
            });
        });

        #endregion
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

    #region Master-detail header
    public class MasterDetailsHeaders : IPageHeader
    {
        private string naslov;
        public MasterDetailsHeaders(string naslov)
        {
        this.naslov = naslov;
        }
        public IPdfFont PdfRptFont { set; get; }

        public PdfGrid RenderingGroupHeader(iTextSharp.text.Document pdfDoc, PdfWriter pdfWriter, IList<CellData> newGroupInfo, IList<SummaryCellData> summaryData)
        {
        var IdProjekt = newGroupInfo.GetSafeStringValueOf(nameof(Projekt.IdProjekt));
        var NazivProjekta = newGroupInfo.GetSafeStringValueOf(nameof(Projekt.NazivProjekt));
        var Kratica = newGroupInfo.GetSafeStringValueOf(nameof(Projekt.Kratica));
        var Cilj = (DateTime)newGroupInfo.GetValueOf(nameof(Projekt.Cilj));
        var Vrsta = (decimal)newGroupInfo.GetValueOf(nameof(Projekt.IdVrsteProjektaNavigation.NazivVrsteProjekta));

        var table = new PdfGrid(relativeWidths: new[] { 2f, 5f, 2f, 3f }) { WidthPercentage = 100 };

        table.AddSimpleRow(
            (cellData, cellProperties) =>
            {
            cellData.Value = "Id projekta: ";
            cellProperties.PdfFont = PdfRptFont;
            cellProperties.PdfFontStyle = DocumentFontStyle.Bold;
            cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
            },
            (cellData, cellProperties) =>
            {
            cellData.TableRowData = newGroupInfo;
                    var cellTemplate = new HyperlinkField(BaseColor.Black, false)
            {
                TextPropertyName = IdProjekt,
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
            cellData.Value = "Naziv projekta: ";
            cellProperties.PdfFont = PdfRptFont;
            cellProperties.PdfFontStyle = DocumentFontStyle.Bold;
            cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
            },
            (cellData, cellProperties) =>
            {
            cellData.Value = NazivProjekta;
            cellProperties.PdfFont = PdfRptFont;
            cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
            });

        table.AddSimpleRow(
            (cellData, cellProperties) =>
            {
            cellData.Value = "Kratica: ";
            cellProperties.PdfFont = PdfRptFont;
            cellProperties.PdfFontStyle = DocumentFontStyle.Bold;
            cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
            },
            (cellData, cellProperties) =>
            {
            cellData.Value = Kratica;
            cellProperties.PdfFont = PdfRptFont;
            cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
            },
            (cellData, cellProperties) =>
            {
            cellData.Value = "Cilj: ";
            cellProperties.PdfFont = PdfRptFont;
            cellProperties.PdfFontStyle = DocumentFontStyle.Bold;
            cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
            },
            (cellData, cellProperties) =>
            {
            cellData.Value = Cilj;
            cellProperties.PdfFont = PdfRptFont;
            cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
            },
            (cellData, cellProperties) =>
            {
            cellData.Value = "Vrsta: ";
            cellProperties.PdfFont = PdfRptFont;
            cellProperties.PdfFontStyle = DocumentFontStyle.Bold;
            cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
            },
            (cellData, cellProperties) =>
            {
            cellData.Value = Vrsta;
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
        private PdfReport CreateReport(string naslov)
        {
        var pdf = new PdfReport();

        pdf.DocumentPreferences(doc =>
        {
            doc.Orientation(PageOrientation.Portrait);
            doc.PageSize(PdfPageSize.A4);
            doc.DocumentMetadata(new DocumentMetadata
            {
            Author = "FER-ZPR",
            Application = "Firma.MVC Core",
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

        public async Task<List<ProjektViewModel>> GetProjektiViewModelsAsync()
        {
            var projekti = await ctx.Projekti
                .Include(p => p.IdVrsteProjektaNavigation)
                .Include(p => p.Dokumenti)
                    .ThenInclude(d => d.IdVrstaNavigation)
                .ToListAsync();

            var projektiViewModels = projekti.Select(p => new ProjektViewModel
            {
                NazivProjekta = p.NazivProjekt,
                Kratica = p.Kratica,
                Cilj = p.Cilj,
                IdProjekta = p.IdProjekt,
                NazivVrsteProjekta = p.IdVrsteProjektaNavigation?.NazivVrsteProjekta,
                Dokumenti = p.Dokumenti.Select(d => new DokumentViewModel
                {
                    IdDoc = d.IdDoc,
                    NazivDokument = d.NazivDokument,
                    NazivDatoteke = d.NazivDatoteke,
                    NazivVrsteDoc = d.IdVrstaNavigation?.NazivVrstaDoc,
                    TextContent = d.Dokument1 != null
                                 ? Encoding.UTF8.GetString(d.Dokument1)
                                 : "Nema prilozenog dokumenta",
                }).ToList()
            }).ToList();

            return projektiViewModels;
        }
        #endregion
    }
}