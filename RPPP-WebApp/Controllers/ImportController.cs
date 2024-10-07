using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using RPPP_WebApp.Models;

namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// Kontroler razred za unos podataka excel dokumentom
    /// </summary>
    public class ImportController : Controller
    {
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private readonly Rppp07Context ctx;
        private readonly ILogger<ImportController> logger;

        public ImportController(Rppp07Context ctx, ILogger<ImportController> logger)
        {
            this.ctx = ctx;
            this.logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> ImportExcel([FromForm] IFormFile file)
        {
            try
            {
                if (file != null && file.Length > 0)
                {
                    using (var stream = new MemoryStream())
                    {
                        file.CopyTo(stream);
                        using (var package = new ExcelPackage(stream))
                        {
                            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                            if (worksheet != null)
                            {

                                byte[] content;
                                using (ExcelPackage excel = new ExcelPackage())
                                {

                                    var vrsteZah =  await ctx.VrstaZahtjeva
                                        .AsNoTracking()
                                        .OrderBy(d => d.IdVrstaZah)
                                        .ToListAsync();

                                    var osobe = await ctx.Osobe
                                        .AsNoTracking()
                                        .OrderBy(d => d.IdSuradnik)
                                        .ToListAsync();

                                    excel.Workbook.Properties.Title = package.Workbook.Properties.Title;
                                    excel.Workbook.Properties.Author = package.Workbook.Properties.Author;
                                    var worksheet1 = excel.Workbook.Worksheets.Add(worksheet.Workbook.Properties.Title);

                                    worksheet1.Cells[1, 1].Value = worksheet.Cells[1,1].Value?.ToString();
                                    worksheet1.Cells[1, 2].Value = worksheet.Cells[1,2].Value?.ToString();
                                    worksheet1.Cells[1, 3].Value = worksheet.Cells[1,3].Value?.ToString();
                                    worksheet1.Cells[1, 4].Value = worksheet.Cells[1,4].Value?.ToString();
                                    worksheet1.Cells[1, 5].Value = worksheet.Cells[1, 4].Value?.ToString();
                                    worksheet1.Cells[1, 6].Value = "Status unosa";

                                    var startRow = 2;
                                    var endRow = worksheet.Dimension.End.Row;

                                    for (int row = startRow; row <= endRow; row++)
                                    {

                                        var cellValue1 = worksheet.Cells[row, 1].Value?.ToString();
                                        var cellValue2 = worksheet.Cells[row, 2].Value?.ToString();

                                        worksheet1.Cells[row,1].Value = worksheet.Cells[row, 1].Value?.ToString();
                                        worksheet1.Cells[row,2].Value = worksheet.Cells[row, 2].Value?.ToString();
                                        worksheet1.Cells[row,3].Value = worksheet.Cells[row, 3].Value?.ToString();
                                        worksheet1.Cells[row,4].Value = worksheet.Cells[row, 4].Value?.ToString();
                                        worksheet1.Cells[row,5].Value = worksheet.Cells[row, 5].Value?.ToString();

                                        
                                        var opisZah = worksheet.Cells[row, 2].Value?.ToString();
                                        var trajanje = worksheet.Cells[row, 3].Value?.ToString();
                                        var vrstaZah = worksheet.Cells[row, 4].Value?.ToString();
                                        var suradnik = worksheet.Cells[row, 5].Value?.ToString();

                                        Boolean x = false;
                                        Boolean y = false;
                                        for(int i = 0; i < vrsteZah.Count; i++)
                                        {

                                            if (vrsteZah[i].NazivVrstaZah.Equals(worksheet1.Cells[row, 4].Value.ToString()))
                                            {
                                                vrstaZah = vrsteZah[i].IdVrstaZah.ToString();
                                                x = true;
                                            }
                                        }
                                        for (int i = 0; i < osobe.Count; i++)
                                        {
                                            if (osobe[i].Ime.Equals(worksheet1.Cells[row, 5].Value.ToString()))
                                            {
                                                y = true;
                                                suradnik = osobe[i].IdSuradnik.ToString();
                                            }
                                        }
                                        var nextId = ctx.Zahtjevi.Max(p => p.IdZah) + 1;

                                        Console.WriteLine(vrstaZah);

                                        var noviZahtijev = new Zahtijev
                                        {
                                            IdZah = nextId,
                                            OpisZahtijev = opisZah,
                                            Prioritet = trajanje,
                                            IdSuradnik = int.Parse(suradnik),
                                            IdVrstaZah = int.Parse(vrstaZah)
                                        };

                                        Boolean xy = true;
                                        try
                                        {
                                            ctx.Add(noviZahtijev);
                                            await ctx.SaveChangesAsync();
                                            xy = true;
                                            logger.LogInformation(new EventId(1000), $"Zahtijev s identifikatorom {nextId} je dodan.");
                                        }
                                        catch(Exception e)
                                        {
                                            xy = false;
                                        }
                                        if (xy == true)
                                        {
                                            worksheet1.Cells[row, 6].Value = "Sucess";
                                        }
                                        else
                                        {
                                            worksheet1.Cells[row, 6].Value = "Fail";
                                        }
                                    }

                                    content = excel.GetAsByteArray();

                                    return File(content, ExcelContentType, "export.xlsx");
                                }
                            }
                        }
                    }
                }

                return View("Error");
            }
            catch (Exception ex)
            {
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ImportExcelZadaci([FromForm] IFormFile file)
        {
            try
            {
                if (file != null && file.Length > 0)
                {
                    using (var stream = new MemoryStream())
                    {
                        file.CopyTo(stream);
                        using (var package = new ExcelPackage(stream))
                        {
                            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                            if (worksheet != null)
                            {

                                byte[] content;
                                using (ExcelPackage excel = new ExcelPackage())
                                {

                                    var vrsteZad = await ctx.VrstaZadatka
                                        .AsNoTracking()
                                        .OrderBy(d => d.IdVrstaZad)
                                        .ToListAsync();

                                    var zahtjevi = await ctx.Zahtjevi
                                        .AsNoTracking()
                                        .OrderBy(d => d.IdZah)
                                        .ToListAsync();

                                    excel.Workbook.Properties.Title = package.Workbook.Properties.Title;
                                    excel.Workbook.Properties.Author = package.Workbook.Properties.Author;
                                    var worksheet1 = excel.Workbook.Worksheets.Add(worksheet.Workbook.Properties.Title);

                                    worksheet1.Cells[1, 1].Value = worksheet.Cells[1, 1].Value?.ToString();
                                    worksheet1.Cells[1, 2].Value = worksheet.Cells[1, 2].Value?.ToString();
                                    worksheet1.Cells[1, 3].Value = worksheet.Cells[1, 3].Value?.ToString();
                                    worksheet1.Cells[1, 4].Value = worksheet.Cells[1, 4].Value?.ToString();
                                    worksheet1.Cells[1, 5].Value = worksheet.Cells[1, 4].Value?.ToString();
                                    worksheet1.Cells[1, 6].Value = "Status unosa";

                                    var startRow = 2;
                                    var endRow = worksheet.Dimension.End.Row;

                                    for (int row = startRow; row <= endRow; row++)
                                    {

                                        var cellValue1 = worksheet.Cells[row, 1].Value?.ToString();
                                        var cellValue2 = worksheet.Cells[row, 2].Value?.ToString();

                                        worksheet1.Cells[row, 1].Value = worksheet.Cells[row, 1].Value?.ToString();
                                        worksheet1.Cells[row, 2].Value = worksheet.Cells[row, 2].Value?.ToString();
                                        worksheet1.Cells[row, 3].Value = worksheet.Cells[row, 3].Value?.ToString();
                                        worksheet1.Cells[row, 4].Value = worksheet.Cells[row, 4].Value?.ToString();
                                        worksheet1.Cells[row, 5].Value = worksheet.Cells[row, 5].Value?.ToString();


                                        var status = worksheet.Cells[row, 2].Value?.ToString();
                                        var trajanje = worksheet.Cells[row, 3].Value?.ToString();
                                        var zahtijev = worksheet.Cells[row, 4].Value?.ToString();
                                        var vrsta = worksheet.Cells[row, 5].Value?.ToString();

                                        Boolean x = false;
                                        Boolean y = false;
                                        for (int i = 0; i < zahtjevi.Count; i++)
                                        {

                                            if (zahtjevi[i].OpisZahtijev.Equals(worksheet1.Cells[row, 4].Value.ToString()))
                                            {
                                                zahtijev = zahtjevi[i].IdZah.ToString();
                                                x = true;
                                            }
                                        }
                                        for (int i = 0; i < vrsteZad.Count; i++)
                                        {
                                            if (vrsteZad[i].NazivVrstaZad.Equals(worksheet1.Cells[row, 5].Value.ToString()))
                                            {
                                                y = true;
                                                vrsta = vrsteZad[i].IdVrstaZad.ToString();
                                            }
                                        }
                                        var nextId = ctx.Zadaci.Max(p => p.IdZad) + 1;

                                        Console.WriteLine(trajanje);
                                        DateOnly xx = DateOnly.Parse(trajanje);

                                        var noviZadatak = new Zadatak
                                        {
                                            IdZad = nextId,
                                            Status = status,
                                            Trajanje = xx,
                                            IdZah = int.Parse(zahtijev),
                                            IdVrstaZad = int.Parse(vrsta)
                                        };

                                        Boolean xy = true;
                                        try
                                        {
                                            ctx.Add(noviZadatak);
                                            await ctx.SaveChangesAsync();
                                            xy = true;
                                            logger.LogInformation(new EventId(1000), $"Zadatak s identifikatorom {nextId} je dodan.");
                                        }
                                        catch (Exception e)
                                        {
                                            xy = false;
                                            Console.WriteLine(e.ToString());
                                        }
                                        if (xy == true)
                                        {
                                            worksheet1.Cells[row, 6].Value = "Sucess";
                                        }
                                        else
                                        {
                                            worksheet1.Cells[row, 6].Value = "Fail";
                                        }
                                    }

                                    content = excel.GetAsByteArray();

                                    return File(content, ExcelContentType, "export.xlsx");
                                }
                            }
                        }
                    }
                }

                return View("Error");
            }
            catch (Exception ex)
            {
                return View("Error");
            }
        }
    }
}
