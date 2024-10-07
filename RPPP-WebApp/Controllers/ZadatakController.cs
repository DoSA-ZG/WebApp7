using FluentValidation.Results;
using iText.StyledXmlParser.Jsoup.Parser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RPPP_WebApp.Extensions;
using RPPP_WebApp.Extensions.Selectors;
using RPPP_WebApp.Models;
using RPPP_WebApp.ModelsValidation;
using RPPP_WebApp.ViewModels;
using System.Drawing.Printing;
using System.Text.Json;

namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// MVC kontroler razred za zadatak
    /// </summary>
    public class ZadatakController : Controller
    {
        private readonly Rppp07Context _context;
        private readonly AppSettings appData;
        private readonly ILogger<ZadatakController> logger;

        public ZadatakController(Rppp07Context context, IOptionsSnapshot<AppSettings> options, ILogger<ZadatakController> logger)
        {
            _context = context;
            appData = options.Value;
            this.logger = logger;
        }

        public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
        {
            int page_size = appData.PageSize;
            var query = _context.Zadaci
                                .Include(z => z.IdZahNavigation)
                                .Include(z => z.IdVrstaZadNavigation)
                                .AsNoTracking();
            int count = await query.CountAsync();

            var pagingInfo = new PagingInfo
            {
                CurrentPage = page,
                Sort = sort,
                Ascending = ascending,
                ItemsPerPage = page_size,
                TotalItems = count
            };

            if (page < 1 || page > pagingInfo.TotalPages)
            {
                return RedirectToAction(nameof(Index), new { page = 1, sort, ascending });
            }

            ViewBag.PagingInfo = pagingInfo;

            query = query.ApplySort(sort, ascending);

            var zadaci = await query.Select(z => new ZadatakViewModel
            {
                IdZad = z.IdZad,
                Status = z.Status,
                Trajanje = z.Trajanje,
                OpisZahtijev = z.IdZahNavigation.OpisZahtijev,
                NazivVrstaZad = z.IdVrstaZadNavigation.NazivVrstaZad
            }).Skip((page - 1) * page_size).Take(page_size).ToListAsync();

            var model = new ZadaciViewModel
            {
                Zadaci = zadaci,
                PagingInfo = pagingInfo
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PrepareDropDownLists();
            return View();
        }

        private async Task PrepareDropDownLists()
        {
            var zahtijevi = await _context.Zahtjevi
                                        .Select(v => new { v.IdZah, v.OpisZahtijev })
                                        .ToListAsync();
            var vrsteZadataka = await _context.VrstaZadatka
                                        .Select(v => new { v.IdVrstaZad, v.NazivVrstaZad })
                                        .ToListAsync();

            ViewBag.IdZah = new SelectList(zahtijevi, "IdZah", "OpisZahtijev");
            ViewBag.IdVrstaZad = new SelectList(vrsteZadataka, "IdVrstaZad", "NazivVrstaZad");

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Zadatak zadatak)
        {
            Console.WriteLine(zadatak.Status + " " + zadatak.Trajanje);

            ZadatakValidator validator = new(_context);
            ValidationResult result = validator.Validate(zadatak);

            if (result.IsValid)
            {
                logger.LogTrace(JsonSerializer.Serialize(zadatak));
                if (ModelState.IsValid)
                {
                    try
                    {
                        var maxID = _context.Zadaci.Max(p => p.IdZad);
                        var nextId = maxID + 1;
                        var noviZad = new Zadatak
                        {
                            IdZad = nextId,
                            Status = zadatak.Status,
                            Trajanje = zadatak.Trajanje,
                            IdZah = zadatak.IdZah,
                            IdVrstaZad = zadatak.IdVrstaZad
                        };
                        _context.Add(noviZad);
                        await _context.SaveChangesAsync();

                        logger.LogInformation(new EventId(1000), $"Zadatak {noviZad.IdZad} dodan.");

                        TempData[Constants.Message] = $"Zadatak {noviZad.IdZad} dodan.";
                        TempData[Constants.ErrorOccurred] = false;
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception exc)
                    {
                        logger.LogError("Pogreška prilikom dodavanje novog projekta: {0}", exc.CompleteExceptionMessage());
                        ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                        await PrepareDropDownLists();
                        return View(zadatak);
                    }
                }
                else
                {
                    await PrepareDropDownLists();
                    return View(zadatak);
                }
            }

            foreach (ValidationFailure error in result.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            await PrepareDropDownLists();
            return View(zadatak);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int sort, int page, bool ascending)
        {
            Console.WriteLine("ALO");
            var zadatak = await _context.Zadaci.FindAsync(id);
            if (zadatak != null)
            {
                try
                {
                    _context.Remove(zadatak);
                    await _context.SaveChangesAsync();

                    TempData[Constants.Message] = $"zadatak s identifikatorom {id} uspješno obrisan";
                    logger.LogInformation(new EventId(1000), $"Zadatak s identifikatorom {id} dodan.");
                    TempData[Constants.ErrorOccurred] = false;
                }
                catch (Exception exc)
                {
                    TempData[Constants.Message] = $"Pogreška prilikom brisanja zadatka: {exc.CompleteExceptionMessage()}";
                    TempData[Constants.ErrorOccurred] = true;
                }
            }
            else
            {
                TempData[Constants.Message] = $"Ne postoji zadatak s identifikatorom: {id}";
                TempData[Constants.ErrorOccurred] = true;
            }

            return RedirectToAction(nameof(Index), new { sort, page, ascending });
        }





        [HttpGet]
        public async Task<IActionResult> Edit(int id, int sort = 1, int page = 1, bool ascending = true)
        {
            var zadatak = await _context.Zadaci
                            .AsNoTracking()
                            .Where(z => z.IdZad == id)
                            .SingleOrDefaultAsync();
            if (zadatak != null)
            {
                var pagingInfo = new PagingInfo
                {
                    CurrentPage = page,
                    Sort = sort,
                    Ascending = ascending,
                };
                ViewBag.PagingInfo = pagingInfo;

                await PrepareDropDownLists();
                return View(zadatak);
            }
            else
            {
                return NotFound($"Neispravan id zadatka: {id}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var zadatak = await _context.Zadaci
                                  .Where(z => z.IdZad == id)
                                  .Select(z => new ZadatakViewModel
                                  {
                                      Status = z.Status,
                                      Trajanje = z.Trajanje,
                                      IdZad = z.IdZad,
                                      OpisZahtijev = z.IdZahNavigation.OpisZahtijev,
                                      NazivVrstaZad = z.IdVrstaZadNavigation.NazivVrstaZad
                                  })
                                  .SingleOrDefaultAsync();
            if (zadatak != null)
            {
                return PartialView(zadatak);
            }
            else
            {
                return NotFound($"Neispravan id zadatka: {id}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Zadatak zadatak, int id, int sort = 1, int page = 1, bool ascending = true)
        {
            ZadatakValidator validator = new(_context);
            ValidationResult result = validator.Validate(zadatak);

            var pagingInfo = new PagingInfo
            {
                CurrentPage = page,
                Sort = sort,
                Ascending = ascending,
            };
            ViewBag.PagingInfo = pagingInfo;

            if (result.IsValid)
            {
                try
                {
                    var model = await _context.Zadaci
                                .Where(e => e.IdZad == id)
                                .FirstOrDefaultAsync();

                    if (model == null)
                    {
                        return NotFound($"Ne postoji zadatak s identifikatorom: {id}");
                    }

                    if (await TryUpdateModelAsync<Zadatak>(model, "", z => z.Status, z => z.Trajanje, z => z.IdZah, z => z.IdVrstaZad))
                    {
                        try
                        {
                            await _context.SaveChangesAsync();

                            TempData[Constants.Message] = "Zadatak uspješno ažuriran";
                            TempData[Constants.ErrorOccurred] = false;

                            logger.LogInformation(new EventId(1000), $"Zadatak s identifikatorom {id} ažuriran.");

                            return RedirectToAction(nameof(Index), new { sort, page, ascending });
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError(string.Empty, ex.CompleteExceptionMessage());

                            return View(model);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Zadatak nije bilo moguće ažurirati");

                        return View(model);
                    }
                }
                catch (Exception ex)
                {
                    TempData[Constants.Message] = ex.CompleteExceptionMessage();
                    TempData[Constants.ErrorOccurred] = true;

                    return View(new { id, sort, page, ascending });
                }
            }

            foreach (ValidationFailure error in result.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            await PrepareDropDownLists();
            return View(zadatak);

        }
    }
}
