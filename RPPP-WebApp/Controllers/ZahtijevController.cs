using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.Rendering;
using RPPP_WebApp.Extensions.Selectors;
using RPPP_WebApp.Extensions;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;
using RPPP_WebApp;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Drawing.Printing;
using RPPP_WebApp.ModelsValidation;
using FluentValidation.Results;

/// <summary>
/// MVC kontroler razred za zahtjeve
/// </summary>
public class ZahtijevController : Controller
{
    private readonly Rppp07Context _context;
    private readonly AppSettings appData;
    private readonly ILogger<ZahtijevController> logger;

    public ZahtijevController(Rppp07Context context, IOptionsSnapshot<AppSettings> options, ILogger<ZahtijevController> logger)
    {
        _context = context;
        appData = options.Value;
        this.logger = logger;
    }

    public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
    {
        int pagesize = appData.PageSize;

        var query = _context.Zahtjevi
                            .Include(d => d.IdVrstaZahNavigation)
                            .Include(d => d.IdSuradnikNavigation)
                            .AsNoTracking();

        int count = await query.CountAsync();

        var pagingInfo = new PagingInfo
        {
            CurrentPage = page,
            Sort = sort,
            Ascending = ascending,
            ItemsPerPage = pagesize,
            TotalItems = count
        };

        if (page < 1 || page > pagingInfo.TotalPages)
        {
            return RedirectToAction(nameof(Index), new { page = 1, sort, ascending });
        }

        ViewBag.PagingInfo = pagingInfo;

        query = query.ApplySort(sort, ascending);

        var zahtijevi = await query.Select(d => new ZahtijevViewModel
        {
            OpisZahtijev = d.OpisZahtijev,
            Prioritet = d.Prioritet,
            IdZah = d.IdZah,
            NazivVrsteZahtijeva = d.IdVrstaZahNavigation.NazivVrstaZah,
            NazivSuradnika = d.IdSuradnikNavigation.IdSuradnikNavigation.Ime,
        }).Skip((page - 1) * pagesize)
          .Take(pagesize)
          .ToListAsync();

        var model = new ZahtijeviViewModel
        {
            Zahtijevi = zahtijevi,
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Zahtijev zahtijev)
    {
        ZahtijevValidator validator = new(_context);
        ValidationResult result = validator.Validate(zahtijev);

        if (result.IsValid)
        {
            logger.LogTrace(JsonSerializer.Serialize(zahtijev));

            if (ModelState.IsValid)
            {
                try
                {
                    var nextId = _context.Zahtjevi.Max(p => p.IdZah) + 1;

                    var noviZahtijev = new Zahtijev
                    {
                        IdZah = nextId,
                        OpisZahtijev = zahtijev.OpisZahtijev,
                        Prioritet = zahtijev.Prioritet,
                        IdSuradnik = zahtijev.IdSuradnik,
                        IdVrstaZah = zahtijev.IdVrstaZah
                    };
                    _context.Add(noviZahtijev);
                    await _context.SaveChangesAsync();


                    logger.LogInformation(new EventId(1000), $"Novi zahtijev s {nextId} dodan.");

                    TempData[Constants.Message] = $"Novi zahtijev dodan.";
                    TempData[Constants.ErrorOccurred] = false;

                    return RedirectToAction(nameof(Index));

                }
                catch (Exception exc)
                {
                    logger.LogError("Pogreška prilikom dodavanja novog zahtijeva: {e}", exc.CompleteExceptionMessage());
                    ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                    await PrepareDropDownLists();
                    return View(zahtijev);
                }
            }
            else
            {
                await PrepareDropDownLists();
                return View(zahtijev);
            }
        }

        foreach (ValidationFailure error in result.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
        await PrepareDropDownLists();
        return View(zahtijev);
    }
    private async Task PrepareDropDownLists()
    {
        var vrsteZahtijeva = await _context.VrstaZahtjeva
                                    .Select(v => new { v.IdVrstaZah, v.NazivVrstaZah })
                                    .ToListAsync();
        var suradnici = await _context.Partneri
                                    .Select(p => new { p.IdSuradnik, p.IdSuradnikNavigation.Ime })
                                    .ToListAsync();

        ViewBag.IdVrstaZah = new SelectList(vrsteZahtijeva, "IdVrstaZah", "NazivVrstaZah");
        ViewBag.ImeSuradnika = new SelectList(suradnici, "IdSuradnik", "Ime");
    }

    [HttpGet]
    [ActionName("Delete")]
    public async Task<IActionResult> Remove(int id, int page = 1, int sort = 1, bool ascending = true)
    {
        return await Delete(id, page, sort, ascending);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int sort, int page, bool ascending)
    {
        var zahtijev = await _context.Zahtjevi.FindAsync(id);
        if (zahtijev != null)
        {
            try
            {
                _context.Remove(zahtijev);
                await _context.SaveChangesAsync();

                logger.LogInformation(new EventId(1000), $"Zahtijev s id {id} obrisan.");
                TempData[Constants.Message] = $"Zahtijev s identifikatorom {id} uspješno obrisan";
                TempData[Constants.ErrorOccurred] = false;
            }
            catch (Exception exc)
            {
                TempData[Constants.Message] = $"Pogreška prilikom brisanja zahtijeva: {exc.CompleteExceptionMessage()}";
                TempData[Constants.ErrorOccurred] = true;
            }
        }
        else
        {
            TempData[Constants.Message] = $"Ne postoji zahtijev s identifikatorom: {id}";
            TempData[Constants.ErrorOccurred] = true;
        }

        return RedirectToAction(nameof(Index), new { sort, page, ascending });
    }


    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var zahtijev = await _context.Zahtjevi
                              .Include(p => p.IdSuradnikNavigation)
                              .Include(p => p.IdVrstaZahNavigation)
                              .Include(p => p.Zadataks)
                              .AsNoTracking()
                              .Where(p => p.IdZah == id)
                              .SingleOrDefaultAsync();

        var zadatakModeli = new List<ZadatakViewModel>();
        foreach (var zadatak in zahtijev.Zadataks)
        {
            ZadatakViewModel m = new ZadatakViewModel
            {
                IdZad = zadatak.IdZad,
                Status = zadatak.Status,
                Trajanje = zadatak.Trajanje,
                OpisZahtijev = _context.Zahtjevi.Where(i => i.IdZah == zadatak.IdZah).SingleOrDefault().OpisZahtijev,
                NazivVrstaZad = _context.VrstaZadatka.Where(i => i.IdVrstaZad == zadatak.IdVrstaZad).SingleOrDefault().NazivVrstaZad
            };

            zadatakModeli.Add(m);
        }


        ZahtijevViewModel model = new ZahtijevViewModel
        {
            IdZah = zahtijev.IdZah,
            OpisZahtijev = zahtijev.OpisZahtijev,
            Prioritet = zahtijev.Prioritet,
            NazivVrsteZahtijeva = _context.VrstaZahtjeva.Where(i => i.IdVrstaZah == zahtijev.IdVrstaZah).SingleOrDefault().NazivVrstaZah,
            NazivSuradnika = _context.Osobe.Where(i => i.IdSuradnik == zahtijev.IdSuradnik).SingleOrDefault().Ime,
            Zadataks = zadatakModeli
        };

        ViewBag.ZahtijevNextId = _context.Zahtjevi.Max(p => p.IdZah) + 1;

        if (zahtijev != null)
        {
            await PrepareDropDownLists();
            return View(model);
        }
        else
        {
            return NotFound($"Neispravan id partnera: {id}");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ZahtijevViewModel model, int page = 1, int sort = 1, bool ascending = true)
    {
        var id = model.IdZah;

        if (ModelState.IsValid)
        {
            var zahtjev = await _context.Zahtjevi
                                    .Include(p => p.IdSuradnikNavigation)
                                    .Include(p => p.IdVrstaZahNavigation)
                                    .Include(p => p.Zadataks)
                                    .Where(e => e.IdZah == model.IdZah)
                                    .FirstOrDefaultAsync();

            zahtjev.OpisZahtijev = model.OpisZahtijev;
            zahtjev.Prioritet = model.Prioritet;
            zahtjev.IdVrstaZah = _context.VrstaZahtjeva.Where(z => z.NazivVrstaZah.Equals(model.NazivVrsteZahtijeva)).SingleOrDefault().IdVrstaZah;
            zahtjev.IdSuradnik = _context.Osobe.Where(o => o.Ime.Equals(model.NazivSuradnika)).SingleOrDefault().IdSuradnik;

            if (zahtjev == null)
            {
                return NotFound("Ne postoji zahtjev s id-om: " + model.IdZah);
            }

            List<int> IdZadaci = model.Zadataks
                                    .Where(z => z.IdZad != 0)
                                    .Select(z => z.IdZad)
                                    .ToList();

            foreach (var zadatak in zahtjev.Zadataks.Where(e => !IdZadaci.Contains(e.IdZad)))
            {
                _context.Remove(zadatak);
            }

            var nextId = _context.Zadaci.Max(p => p.IdZad);
            foreach (var zadatak in model.Zadataks)
            {
                Zadatak novizad;

                if (zadatak.IdZad > 0)
                {
                    novizad = zahtjev.Zadataks.Where(z => z.IdZad == zadatak.IdZad).SingleOrDefault();
                }
                else
                {
                    nextId++;

                    novizad = new Zadatak
                    {
                        IdZad = nextId,
                    };

                    zahtjev.Zadataks.Add(novizad);
                }

                var IdVrstaZad = _context.VrstaZadatka
                                    .Where(z => z.NazivVrstaZad.Equals(zadatak.NazivVrstaZad))
                                    .SingleOrDefault()
                                    .IdVrstaZad;

                novizad.Status = zadatak.Status;
                novizad.Trajanje = zadatak.Trajanje;
                novizad.IdVrstaZad = IdVrstaZad;
                novizad.IdZah = zahtjev.IdZah;
            }

            try
            {
                await _context.SaveChangesAsync();

                logger.LogInformation($"Zahtjev sa ID '{zahtjev.IdZah}' uspjesno azuriran");

                TempData[Constants.Message] = $"Zahtjev sa ID '{zahtjev.IdZah}' uspjesno azuriran";
                TempData[Constants.ErrorOccurred] = false;
            }
            catch (Exception exc)
            {
                logger.LogInformation($"Pogreska prilikom azuriranja zahtjeva: " + exc.CompleteExceptionMessage);
                TempData[Constants.Message] = "Pogreška prilikom ažuriranja zahtjeva: " + exc.CompleteExceptionMessage();
                TempData[Constants.ErrorOccurred] = true;
            }
        }

        return RedirectToAction(nameof(Edit), new { id, page, sort, ascending });
    }

    [HttpGet]
    public async Task<IActionResult> Get(int id)
    {
        var zahtijev = await _context.Zahtjevi
                              .Where(d => d.IdZah == id)
                              .Select(d => new ZahtijevViewModel
                              {
                                  OpisZahtijev = d.OpisZahtijev,
                                  Prioritet = d.Prioritet,
                                  NazivVrsteZahtijeva = d.IdVrstaZahNavigation.NazivVrstaZah,
                                  NazivSuradnika = d.IdSuradnikNavigation.IdSuradnikNavigation.Ime,
                                  IdZah = d.IdZah
                              })
                              .SingleOrDefaultAsync();
        if (zahtijev != null)
        {
            return PartialView(zahtijev);
        }
        else
        {
            return NotFound($"Neispravan id zahtijeva: {id}");
        }
    }

    public async Task<IActionResult> Show(int id, int page = 1, int sort = 1, bool ascending = true)
    {
        var zahtijev = await _context.Zahtjevi
            .Where(z => z.IdZah == id)
            .Select(z => new ZahtijevViewModel
            {
                IdZah = z.IdZah,
                Prioritet = z.Prioritet,
                OpisZahtijev = z.OpisZahtijev,
                NazivVrsteZahtijeva = z.IdVrstaZahNavigation.NazivVrstaZah,
                NazivSuradnika = z.IdSuradnikNavigation.IdSuradnikNavigation.Ime
            })
            .FirstOrDefaultAsync();

        if (zahtijev == null)
        {
            return NotFound($"Zahtijev s identifikatorom {id} ne postoji.");
        }
        else
        {
            var zadaci = await _context.Zadaci
                .Include(p => p.IdVrstaZadNavigation)
                .Include(p => p.IdZahNavigation)
                .Where(d => d.IdZah == id)
                .OrderBy(d => d.IdZad)
                .Select(d => new ZadatakViewModel
                {
                    Status = d.Status,
                    Trajanje = d.Trajanje,
                    IdZad = d.IdZad,
                    OpisZahtijev = d.IdZahNavigation.OpisZahtijev,
                    NazivVrstaZad = d.IdVrstaZadNavigation.NazivVrstaZad,
                })
                .ToListAsync();

            zahtijev.Zadataks = zadaci;

            await previousAndNext(zahtijev.IdZah);

            ViewBag.Page = page;
            ViewBag.Sort = sort;
            ViewBag.Ascending = ascending;
            ViewBag.Position = zahtijev.IdZah;

            return View("Show", zahtijev);
        }

    }
    private async Task previousAndNext(int IdZah)
    {
        var zahtijevi = await _context.Zahtjevi
           .OrderBy(z => z.IdZah)
           .Select(z => new
           {
               IdZah = z.IdZah
           })
           .ToListAsync();

        var index = zahtijevi.FindIndex(z => z.IdZah == IdZah);

        if (index > 0)
        {
            ViewBag.Previous = zahtijevi[index - 1].IdZah;
        }
        if (index < zahtijevi.Count - 1)
        {
            ViewBag.Next = zahtijevi[index + 1].IdZah;
        }
    }
}
