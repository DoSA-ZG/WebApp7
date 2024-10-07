using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using PdfRpt.Core.Contracts;
using RPPP_WebApp.Models;
using PdfRpt.FluentInterface;
using PdfRpt.Core.Helper;
using PdfRpt.ColumnsItemsTemplates;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Signers;

namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// Kontroler razred za generiranje master-detail PDF dokumenta za zahtijev
    /// </summary>
    public class ZahtijevMasterDetailReportController : Controller
    {
        private readonly Rppp07Context ctx;
        private readonly IWebHostEnvironment environment;
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public ZahtijevMasterDetailReportController(Rppp07Context ctx, IWebHostEnvironment environment)
        {
            this.ctx = ctx;
            this.environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }


        public async Task<IActionResult> Zahtjevi(int id)
        {
            string naslov = "Zahtijevi";
            var stavke = await ctx.Zadaci
                                  .OrderBy(s => s.IdZah)
                                  .ThenBy(s => s.Status)
                                  .Where(s => s.IdZah == id)
                                  .Include(s => s.IdVrstaZadNavigation)
                                  .ToListAsync();

            var lolo = await ctx.Zahtjevi
                                  .OrderBy(s => s.IdZah)
                                  .ThenBy(s => s.OpisZahtijev)
                                  .ToListAsync();
            var trolo = await ctx.VrstaZahtjeva
                                  .OrderBy(s => s.IdVrstaZah)
                                  .ToListAsync();

            var holo = await ctx.Osobe
                                  .OrderBy(s => s.IdSuradnik)
                                  .ThenBy(s => s.Ime)
                                  .ToListAsync();

            PdfReport report = CreateReport(naslov);

            report.PagesFooter(footer =>
            {
                footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
            })
            .PagesHeader(async header =>
            {
                header.CacheHeader(cache: true); // It's a default setting to improve the performance.
                header.CustomHeader(new MasterDetailsHeaders(naslov, lolo, trolo, holo)
                {
                    PdfRptFont = header.PdfFont
                });
            });


            report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(stavke));

            report.MainTableColumns(columns =>
            {

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zadatak>(s => s.IdZah);
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
                    column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
                });
                columns.AddColumn(column =>
                {
                    column.PropertyName<Zadatak>(x => x.Status);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.Width(4);
                    column.HeaderCell("Status zadatka", horizontalAlignment: HorizontalAlignment.Center);
                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zadatak>(x => x.Trajanje);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Width(1);
                    column.HeaderCell("Trajanje", horizontalAlignment: HorizontalAlignment.Center);

                });

                columns.AddColumn(column =>
                {
                    column.PropertyName<Zadatak>(x => x.IdVrstaZadNavigation.NazivVrstaZad);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                    column.IsVisible(true);
                    column.Width(1);
                    column.HeaderCell("Vrsta Zad", horizontalAlignment: HorizontalAlignment.Center);

                });


            });

            byte[] pdf = report.GenerateAsByteArray();

            if (pdf != null)
            {
                Response.Headers.Add("content-disposition", "inline; filename=zahtjevi.pdf");
                return File(pdf, "application/pdf");
            }
            else
                return NotFound();
        }





        public class MasterDetailsHeaders : IPageHeader
        {
            private string naslov;
            private List<Zahtijev> lolo;
            private List<VrstaZahtjeva> trolo;
            private List<Osoba> holo;

            public MasterDetailsHeaders(string naslov, List<Zahtijev> lolo, List<VrstaZahtjeva> trolo, List<Osoba> holo)
            {
                this.naslov = naslov;
                this.lolo = lolo;
                this.trolo = trolo;
                this.holo = holo;
            }

            public IPdfFont PdfRptFont { set; get; }


            public PdfGrid RenderingGroupHeader(Document pdfDoc, PdfWriter pdfWriter, IList<CellData> newGroupInfo, IList<SummaryCellData> summaryData)
            {
               
                var idZah = newGroupInfo.GetSafeStringValueOf(nameof(Zahtijev.IdZah));
                var opisZah = "";
                var prioritetZah = "";
                var vrstaZah = "";
                var suradnik = "";

                for (int i = 0; i < lolo.Count; i++)
                {
                    if (lolo[i].IdZah.ToString().Equals(idZah)){
                        opisZah = lolo[i].OpisZahtijev;
                        prioritetZah = lolo[i].Prioritet;
                        var idVrsta = lolo[i].IdVrstaZah;
                        for(int j = 0; j < trolo.Count; j++)
                        {
                            if (trolo[j].IdVrstaZah.Equals(idVrsta)){
                                vrstaZah = trolo[j].NazivVrstaZah;
                            }
                        }
                        var suradnikid = lolo[i].IdSuradnik;
                        for(int j = 0; j < holo.Count; j++)
                        {
                            if (holo[j].IdSuradnik.Equals(suradnikid))
                            {
                                suradnik = holo[j].Ime;
                            }
                        }
                    }
                }

                var table = new PdfGrid(relativeWidths: new[] { 2f, 5f, 2f, 3f }) { WidthPercentage = 100 };

                table.AddSimpleRow(
                    (cellData, cellProperties) =>
                    {
                        cellData.Value = "Id zahtjeva:";
                        cellProperties.PdfFont = PdfRptFont;
                        cellProperties.PdfFontStyle = DocumentFontStyle.Bold;
                        cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                    },
                    (cellData, cellProperties) =>
                    {
                        cellData.TableRowData = newGroupInfo; //postavi podatke retka za ćeliju
                        var cellTemplate = new HyperlinkField(BaseColor.Black, false)
                        {
                            TextPropertyName = nameof(Zahtijev.IdZah),
                            NavigationUrlPropertyName = nameof(Zahtijev.OpisZahtijev),
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
                        cellData.Value = "Zahtjev:";
                        cellProperties.PdfFont = PdfRptFont;
                        cellProperties.PdfFontStyle = DocumentFontStyle.Bold;
                        cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                    },
                    (cellData, cellProperties) =>
                    {
                        cellData.Value = opisZah;
                        cellProperties.PdfFont = PdfRptFont;
                        cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                    });

                table.AddSimpleRow(
                    (cellData, cellProperties) =>
                    {
                        cellData.Value = "Prioritet zahtjeva:";
                        cellProperties.PdfFont = PdfRptFont;
                        cellProperties.PdfFontStyle = DocumentFontStyle.Bold;
                        cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                    },
                    (cellData, cellProperties) =>
                    {
                        cellData.Value = prioritetZah;
                        cellProperties.PdfFont = PdfRptFont;
                        cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                    },
                    (cellData, cellProperties) =>
                    {
                        cellData.Value = "Vrsta zahtjeva:";
                        cellProperties.PdfFont = PdfRptFont;
                        cellProperties.PdfFontStyle = DocumentFontStyle.Bold;
                        cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                    },
                    (cellData, cellProperties) =>
                    {
                        cellData.Value = vrstaZah;
                        cellProperties.PdfFont = PdfRptFont;
                        cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                    });
                table.AddSimpleRow(
                     (cellData, cellProperties) =>
                     {
                         cellData.Value = "Suradnik";
                         cellProperties.PdfFont = PdfRptFont;
                         cellProperties.PdfFontStyle = DocumentFontStyle.Bold;
                         cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                     },
                    (cellData, cellProperties) =>
                    {
                        cellData.Value = suradnik;
                        cellProperties.PdfFont = PdfRptFont;
                        cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                    },
                    (cellData, cellProperties) =>
                    {
                        cellData.Value = "";
                        cellProperties.PdfFont = PdfRptFont;
                        cellProperties.PdfFontStyle = DocumentFontStyle.Bold;
                        cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                    },
                    (cellData, cellProperties) =>
                    {
                        cellData.Value = "";
                        cellProperties.PdfFont = PdfRptFont;
                        cellProperties.HorizontalAlignment = HorizontalAlignment.Left;
                    });

                return table.AddBorderToTable(borderColor: BaseColor.LightGray, spacingBefore: 5f);
            }

            public PdfGrid RenderingReportHeader(Document pdfDoc, PdfWriter pdfWriter, IList<SummaryCellData> summaryData)
            {
                var table = new PdfGrid(numColumns: 1) { WidthPercentage = 100 };
                table.AddSimpleRow(
                   (cellData, cellProperties) =>
                   {
                       var xd = naslov;
                       Console.Write(xd);
                       cellData.Value = xd;
                       cellProperties.PdfFont = PdfRptFont;
                       cellProperties.PdfFontStyle = DocumentFontStyle.Bold;
                       cellProperties.HorizontalAlignment = HorizontalAlignment.Center;
                   });
                return table.AddBorderToTable();
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
                    Application = "RPPP07.MVC Core",
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
