using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RPPP_WebApp;
using RPPP_WebApp.Extensions;
using RPPP_WebApp.Extensions.Selectors;
using RPPP_WebApp.Models;
using RPPP_WebApp.ModelsValidation;
using RPPP_WebApp.ViewModels;
using System.Drawing.Printing;
using System.Text.Json;


public class KarticaController : Controller
{
    private readonly Rppp07Context _context;
    private readonly AppSettings appData;
    private readonly ILogger<KarticaController> logger;

    public KarticaController(Rppp07Context context, IOptionsSnapshot<AppSettings> options, ILogger<KarticaController> logger)
    {
        _context = context;
        appData = options.Value;
        this.logger = logger;
    }

    public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
    {
        int page_size = appData.PageSize;
        var query = _context.Kartice
                            .Include(k => k.IdProjektNavigation)
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

        var kartice = await query.Select(k => new KarticaViewModel
        {
            IdProjekt = k.IdProjektNavigation.IdProjekt,
            NazivProjekt = k.IdProjektNavigation.NazivProjekt,
            KraticaProjekt = k.IdProjektNavigation.Kratica,
            BrRacuna = k.BrRacuna,
            Stanje = k.Stanje,

        }).Skip((page - 1) * page_size)
          .Take(page_size)
          .ToListAsync();

        var model = new KarticeViewModel
        {
            Kartice = kartice,
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
    public async Task<IActionResult> Create(Kartica kartica)
    {
        KarticaValidator validator = new(_context);
        ValidationResult result = validator.Validate(kartica);

        if (result.IsValid)
        {
            logger.LogTrace(JsonSerializer.Serialize(kartica));
            if (ModelState.IsValid)
            {
                try
                {
                    var maxID = _context.Kartice.Max(k => k.IdProjekt);
                    var nextId = maxID + 1;
                    var novaKartica = new Kartica
                    {
                        IdProjekt = nextId,
                        BrRacuna = kartica.BrRacuna,
                        Stanje = kartica.Stanje,

                    };
                    _context.Add(novaKartica);
                    await _context.SaveChangesAsync();

                    logger.LogInformation(new EventId(1000), $"Kartica s brojem racuna {novaKartica.BrRacuna} dodana.");

                    TempData[Constants.Message] = $"Kartica s brojem racuna {novaKartica.BrRacuna} dodana.";
                    TempData[Constants.ErrorOccurred] = false;
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception exc)
                {
                    logger.LogError("Pogreška prilikom dodavanja nove kartice: {0}", exc.CompleteExceptionMessage());
                    ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                    await PrepareDropDownLists();
                    return View(kartica);
                }
            }
            else
            {
                await PrepareDropDownLists();
                return View(kartica);
            }
        }
        foreach (ValidationFailure error in result.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
        return View(kartica);
    }

    private async Task PrepareDropDownLists()
    {
        var idProjekta = await _context.Projekti
                                    .Select(k => new { k.IdProjekt, k.NazivProjekt })
                                    .ToListAsync();
        ViewBag.IdProjekt = new SelectList(idProjekta, "IdProjekta", "NazivProjekt");

    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int sort, int page, bool ascending)
    {
        var kartica = await _context.Kartice.FindAsync(id);
        if (kartica != null)
        {
            try
            {
                _context.Remove(kartica);
                await _context.SaveChangesAsync();

                TempData[Constants.Message] = $"kartica s brojem racuna {kartica.BrRacuna} uspješno obrisana";
                TempData[Constants.ErrorOccurred] = false;
            }
            catch (Exception exc)
            {
                TempData[Constants.Message] = $"Pogreška prilikom brisanja kartice: {exc.CompleteExceptionMessage()}";
                TempData[Constants.ErrorOccurred] = true;
            }
        }
        else
        {
            TempData[Constants.Message] = $"Ne postoji kartica s brojem: {kartica.BrRacuna}";
            TempData[Constants.ErrorOccurred] = true;
        }

        return RedirectToAction(nameof(Index), new { sort, page, ascending });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, int sort = 1, int page = 1, bool ascending = true)
    {
        var kartica = await _context.Kartice
                        .AsNoTracking()
                        .Where(k => k.BrRacuna == id)
                        .SingleOrDefaultAsync();
        if (kartica != null)
        {
            var pagingInfo = new PagingInfo
            {
                CurrentPage = page,
                Sort = sort,
                Ascending = ascending
            };
            ViewBag.PagingInfo = pagingInfo;

            return View(kartica);
        }
        else
        {
            return NotFound($"Neispravan broj racuna: {id}");
        }
    }
    [HttpGet]
    public async Task<IActionResult> Get(int id)
    {
        var kartica = await _context.Kartice
                              .Where(k => k.BrRacuna == id)
                              .Select(k => new KarticaViewModel
                              {
                                  IdProjekt = k.IdProjekt,
                                  BrRacuna = k.BrRacuna,
                                  Stanje = k.Stanje,
                              })
                              .SingleOrDefaultAsync();
        if (kartica != null)
        {
            return PartialView(kartica);
        }
        else
        {
            return NotFound($"Neispravan broj racuna: {id}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> EditPost(Kartica kartica, int id, int sort = 1, int page = 1, bool ascending = true)
    {
        KarticaValidator validator = new(_context);
        ValidationResult result = validator.Validate(kartica);

        var pagingInfo = new PagingInfo
        {
            CurrentPage = page,
            Sort = sort,
            Ascending = ascending
        };
        ViewBag.PagingInfo = pagingInfo;

        if (result.IsValid)
        {
            try
            {
                var model = await _context.Kartice
                            .Where(k => k.BrRacuna == id)
                            .FirstOrDefaultAsync();

                if (model == null)
                {
                    return NotFound($"Ne postoji projekt s brojem racuna: {id}");
                }

                if (await TryUpdateModelAsync<Kartica>(model, "", k => k.BrRacuna, k => k.Stanje))
                {
                    try
                    {
                        await _context.SaveChangesAsync();

                        TempData[Constants.Message] = "Broj racuna uspješno ažuriran";
                        TempData[Constants.ErrorOccurred] = false;

                        return RedirectToAction(nameof(Index), new { sort, page, ascending });
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError(string.Empty, ex.CompleteExceptionMessage());

                        return View(viewName: "Edit", model);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Broj racuna nije bilo moguće ažurirati");

                    return View(viewName: "Edit", model);
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
        return View(viewName: "Edit", kartica);
    }

    public async Task<IActionResult> Show(int id, int page = 1, int sort = 1, bool ascending = true)
    {
        var kartica = await _context.Kartice
            .Where(k => k.BrRacuna == id)
            .Select(k => new KarticaViewModel
            {
                IdProjekt = k.IdProjektNavigation.IdProjekt,
                NazivProjekt = k.IdProjektNavigation.NazivProjekt,
                KraticaProjekt = k.IdProjektNavigation.Kratica,
                BrRacuna = k.BrRacuna,
                Stanje = k.Stanje,
            })
            .FirstOrDefaultAsync();

        if (kartica == null)
        {
            return NotFound($"Kartica s identifikatorom {id} ne postoji.");
        }
        else
        {
            var transakcije = await _context.Transakcije
                .Include(p => p.IbanKorisnikNavigation)
                .Include(p => p.IdVrstaTransNavigation)
                .Where(d => d.BrRacuna == id)
                .OrderBy(d => d.IdTransakcija)
                .Select(d => new TransakcijaViewModel
                {
                    IdTransakcija = d.IdTransakcija,
                    Iznos = d.Iznos,
                    OpisTrans = d.OpisTrans,
                    Vrijeme = d.Vrijeme,
                    BrRacuna = d.BrRacuna,
                    IbanKorisnik = d.IbanKorisnik,
                    NazivKorisnik = d.IbanKorisnikNavigation.NazivKorisnik,
                    NazivVrstaTransakcija = d.IdVrstaTransNavigation.NazivVrstaTrans,
                })
                .ToListAsync();

            //kartica.Transakcijas = transakcije;

            await previousAndNext(kartica.BrRacuna);

            ViewBag.Page = page;
            ViewBag.Sort = sort;
            ViewBag.Ascending = ascending;
            ViewBag.Position = kartica.BrRacuna;

            return View("Show", kartica);
        }

    }
    private async Task previousAndNext(int BrRacuna)
    {
        var transakcije = await _context.Transakcije
           .OrderBy(z => z.BrRacuna)
           .Select(z => new
           {
               BrRacuna = z.BrRacuna
           })
           .ToListAsync();

        var index = transakcije.FindIndex(z => z.BrRacuna == BrRacuna);

        if (index > 0)
        {
            ViewBag.Previous = transakcije[index - 1].BrRacuna;
        }
        if (index < transakcije.Count - 1)
        {
            ViewBag.Next = transakcije[index + 1].BrRacuna;
        }
    }
}

