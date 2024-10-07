using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
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
    /// Klasa za upravljanje linkova sa stranice Osobe
    /// </summary>
    public class OsobaController : Controller
    {
        private readonly Rppp07Context _context;
        private readonly AppSettings appData;
        private readonly ILogger<OsobaController> logger;

        public OsobaController(Rppp07Context context, IOptionsSnapshot<AppSettings> options, ILogger<OsobaController> logger)
        {
            _context = context;
            appData = options.Value;
            this.logger = logger;
        }

        /// <summary>
        /// Prikaz pocetne stranice Osobe.
        /// </summary>
        /// <param name="page">index tablice na kojoj ce se stranica otvoriti</param>
        /// <param name="sort">argument po kojem se tablica sortira</param>
        /// <param name="ascending">argument oznacava uzlazno ili silazno sortiranje</param>
        /// <returns>View glavne stranice</returns>
        public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
        {
            int page_size = appData.PageSize;
            var query = _context.Osobe
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

            var osobe = await query.Select(o => new OsobaViewModel
            {
                Ime = o.Ime,
                Email = o.Email,
                Oib = o.Oib,
                BrMob = o.BrMob,
                IbanOsoba = o.IbanOsoba,
                IdSuradnik = o.IdSuradnik

            }).Skip((page - 1) * page_size).Take(page_size).ToListAsync();

            var model = new OsobeViewModel
            {
                Osobe = osobe,
                PagingInfo = pagingInfo
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Pokusa dodati osobu u bazu. Ako su podatci tocni, osoba se sprema, ako ne salje se odgovarajuca poruka korisniku.
        /// </summary>
        /// <param name="osoba">Entitet osobe koja se dodaje u bazu</param>
        /// <returns>View glavne stranice</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Osoba osoba)
        {
            OsobaValidator validator = new(_context);
            ValidationResult result = validator.Validate(osoba);

            if (result.IsValid)
            {
                logger.LogTrace(JsonSerializer.Serialize(osoba));
                if (ModelState.IsValid)
                {
                    try
                    {
                        var maxID = _context.Osobe.Max(p => p.IdSuradnik);
                        var nextId = maxID + 1;
                        var novaOsoba = new Osoba
                        {
                            IdSuradnik = nextId,
                            Ime = osoba.Ime,
                            Email = osoba.Email,
                            Oib = osoba.Oib,
                            BrMob = osoba.BrMob,
                            IbanOsoba = osoba.IbanOsoba

                        };
                        _context.Add(novaOsoba);
                        await _context.SaveChangesAsync();

                        logger.LogInformation(new EventId(1000), $"Osoba {novaOsoba.Ime} dodana.");

                        TempData[Constants.Message] = $"Osoba {novaOsoba.Ime} dodana.";
                        TempData[Constants.ErrorOccurred] = false;
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception exc)
                    {
                        logger.LogError("Pogreška prilikom dodavanja nove osobe: {0}", exc.CompleteExceptionMessage());
                        ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                        return View(osoba);
                    }
                }
                else
                {
                    return View(osoba);
                }
            }

            foreach (ValidationFailure error in result.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return View(osoba);
        }

        /// <summary>
        /// Brise osobu iz baze.
        /// </summary>
        /// <param name="id">identifikator osobe koja se brise</param>
        /// <param name="page">index tablice na kojoj ce se stranica otvoriti</param>
        /// <param name="sort">argument po kojem se tablica sortira</param>
        /// <param name="ascending">argument oznacava uzlazno ili silazno sortiranje</param>
        /// <returns>View glavne stranice</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int sort, int page, bool ascending)
        {
            var osoba = await _context.Osobe.FindAsync(id);
            if (osoba != null)
            {
                try
                {
                    _context.Remove(osoba);
                    await _context.SaveChangesAsync();

                    TempData[Constants.Message] = $"Osoba s identifikatorom {id} uspješno obrisana";
                    TempData[Constants.ErrorOccurred] = false;
                }
                catch (Exception exc)
                {
                    TempData[Constants.Message] = $"Pogreška prilikom brisanja osobe: {exc.CompleteExceptionMessage()}";
                    TempData[Constants.ErrorOccurred] = true;
                }
            }
            else
            {
                TempData[Constants.Message] = $"Ne postoji osoba s identifikatorom: {id}";
                TempData[Constants.ErrorOccurred] = true;
            }

            return RedirectToAction(nameof(Index), new { sort, page, ascending });
        }

        /// <summary>
        /// Dohvaca podatke osobe koja se azurira
        /// </summary>
        /// <param name="id">identifikator osobe koja se azurira</param>
        /// <param name="page">index tablice na kojoj ce se stranica otvoriti</param>
        /// <param name="sort">argument po kojem se tablica sortira</param>
        /// <param name="ascending">argument oznacava uzlazno ili silazno sortiranje</param>
        /// <returns>View edit stranice osobe</returns>
        [HttpGet]
        public async Task<IActionResult> Edit(int id, int sort = 1, int page = 1, bool ascending = true)
        {
            var osoba = await _context.Osobe
                            .AsNoTracking()
                            .Where(z => z.IdSuradnik == id)
                            .SingleOrDefaultAsync();
            if (osoba != null)
            {
                var pagingInfo = new PagingInfo
                {
                    CurrentPage = page,
                    Sort = sort,
                    Ascending = ascending
                };
                ViewBag.PagingInfo = pagingInfo;

                return View(osoba);
            }
            else
            {
                return NotFound($"Neispravan id osobe: {id}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var osoba = await _context.Osobe
                                  .Where(o => o.IdSuradnik == id)
                                  .Select(o => new OsobaViewModel
                                  {
                                      Ime = o.Ime,
                                      Email = o.Email,
                                      Oib = o.Oib,
                                      BrMob = o.BrMob,
                                      IbanOsoba = o.IbanOsoba,
                                      IdSuradnik = o.IdSuradnik
                                  })
                                  .SingleOrDefaultAsync();
            if (osoba != null)
            {
                return PartialView(osoba);
            }
            else
            {
                return NotFound($"Neispravan id osobe: {id}");
            }
        }

        /// <summary>
        /// Pokusa azurirati podatke osobe.
        /// </summary>
        /// <param name="osoba">model osobe koju se pokusava azurirati</param>
        /// <param name="id">identifikator partnera kojeg se brise</param>
        /// <param name="page">index tablice na kojoj ce se stranica otvoriti</param>
        /// <param name="sort">argument po kojem se tablica sortira</param>
        /// <param name="ascending">argument oznacava uzlazno ili silazno sortiranje</param>
        /// <returns>View glavne stranice</returns>
        [HttpPost]
        public async Task<IActionResult> EditPost(Osoba osoba, int id, int sort = 1, int page = 1, bool ascending = true)
        {
            OsobaValidator validator = new(_context);
            ValidationResult result = validator.Validate(osoba);

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
                    var model = await _context.Osobe
                                .Where(o => o.IdSuradnik == id)
                                .FirstOrDefaultAsync();

                    if (model == null)
                    {
                        return NotFound($"Ne postoji osoba s identifikatorom: {id}");
                    }

                    if (await TryUpdateModelAsync<Osoba>(model, "", o => o.Ime, o => o.Email, o => o.Oib, o => o.BrMob, o => o.IbanOsoba))
                    {
                        try
                        {
                            await _context.SaveChangesAsync();

                            TempData[Constants.Message] = "Osoba uspješno ažurirana";
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
                        ModelState.AddModelError(string.Empty, "Osobu nije bilo moguće ažurirati");

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

            osoba.IdSuradnik = id;
            foreach (ValidationFailure error in result.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return View(viewName: "Edit", osoba);
        }
    }
}
