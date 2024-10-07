using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.Rendering;
using RPPP_WebApp.Extensions;
using RPPP_WebApp.Extensions.Selectors;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;
using System.Text.Json;
using RPPP_WebApp.ModelsValidation;
using FluentValidation.Results;
using FluentValidation;
using NLog.Targets;
using System.ComponentModel;

namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// Klasa za upravljanje linkova sa stranice Partneri
    /// </summary>
    public class PartnerController : Controller
    {
        private readonly Rppp07Context _context;
        private readonly AppSettings appData;
        private readonly ILogger<PartnerController> logger;
        public PartnerController(Rppp07Context context, IOptionsSnapshot<AppSettings> options, ILogger<PartnerController> logger)
        {
            _context = context;
            appData = options.Value;
            this.logger = logger;
        }

        /// <summary>
        /// Metoda za prikaz pocetne stranice Partneri
        /// </summary>
        /// <param name="page">index tablice na kojoj ce se stranica otvoriti</param>
        /// <param name="sort">argument po kojem se tablica sortira</param>
        /// <param name="ascending">argument oznacava uzlazno ili silazno sortiranje</param>
        /// <returns>View glavne stranice</returns>
        public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
        {
            int page_size = appData.PageSize;
            var query = _context.Partneri
                                .Include(p => p.IdSuradnikNavigation)
                                .Include(p => p.Zahtijevs)
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

            var partneri = await query
                .Select(p => new PartnerViewModel
                {
                    IdSuradnik = p.IdSuradnik,
                    Partner = p.IdSuradnikNavigation,
                    BrZahtijeva = _context.Zahtjevi.Where(i => i.IdSuradnik == p.IdSuradnik).Count()
                })
                .Skip((page - 1) * page_size)
                .Take(page_size)
                .ToListAsync();

            var model = new PartneriViewModel
            {
                Partneri = partneri,
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

        /// <summary>
        /// Stvara novog partnera. U slucaju da stvaranje novog partnera nije uspjesno, vraca odgovarajucu poruku.
        /// </summary>
        /// <param name="partner">Entitet partnera koji se sprema u bazu</param>
        /// <returns>Prikaz stranice azuriranja novo dodanog partnera</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Partner partner)
        {
            PartnerValidator validator = new(_context);
            ValidationResult result = validator.Validate(partner);

            if (result.IsValid)
            {
                var exists = await _context.Partneri.FindAsync(partner.IdSuradnik);

                if (exists != null)
                {
                    TempData[Constants.Message] = $"Osoba sa identifikatorom {partner.IdSuradnik} je već partner.";
                    TempData[Constants.ErrorOccurred] = true;
                    await PrepareDropDownLists();
                    return View(partner);
                }

                logger.LogTrace(JsonSerializer.Serialize(partner));
                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Add(partner);
                        await _context.SaveChangesAsync();

                        logger.LogInformation(new EventId(1000), $"Osoba s identifikatorom {partner.IdSuradnik} je dodan kao partner.");

                        TempData[Constants.Message] = $"Osoba s identifikatorom {partner.IdSuradnik} je dodan kao partner.";
                        TempData[Constants.ErrorOccurred] = false;

                        return RedirectToAction(nameof(Edit), new { id = partner.IdSuradnik });

                    }
                    catch (Exception exc)
                    {
                        logger.LogError("Pogreška prilikom dodavanja novog partnera: {e}", exc.CompleteExceptionMessage());
                        ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                        await PrepareDropDownLists();
                        return View(partner);
                    }
                }
                else
                {
                    await PrepareDropDownLists();
                    return View(partner);
                }
            }

            foreach (ValidationFailure error in result.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            await PrepareDropDownLists();
            return View(partner);
        }

        /// <summary>
        /// Dohvaca podatke za padajuce liste i sprema ih za view model
        /// </summary>
        private async Task PrepareDropDownLists()
        {
            var osobe = await _context.Osobe
                                    .Select(v => new { v.IdSuradnik, v.Ime })
                                    .ToListAsync();
            var partneri = await _context.Partneri
                                    .Select(v => new { v.IdSuradnik, v.IdSuradnikNavigation.Ime })
                                    .ToListAsync();
            ViewBag.Osobe = new SelectList(osobe, "IdSuradnik", "Ime");
            ViewBag.Partneri = new SelectList(partneri, "IdSuradnik", "Ime");

        }

        [HttpGet]
        [ActionName("Delete")]
        public async Task<IActionResult> Remove(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            return await Delete(id, page, sort, ascending);
        }

        /// <summary>
        /// Brise partnera iz baze.
        /// </summary>
        /// <param name="id">identifikator partnera kojeg se brise</param>
        /// <param name="page">index tablice na kojoj ce se stranica otvoriti</param>
        /// <param name="sort">argument po kojem se tablica sortira</param>
        /// <param name="ascending">argument oznacava uzlazno ili silazno sortiranje</param>
        /// <returns>View glavne stranice</returns>
        [HttpPost]
        public async Task<IActionResult> Delete(int id, int sort, int page, bool ascending)
        {
            var partner = await _context.Partneri.FindAsync(id);

            if (partner != null)
            {
                try
                {
                    _context.Remove(partner);
                    await _context.SaveChangesAsync();

                    logger.LogInformation(new EventId(1000), $"Partner s identifikatorom {id} više nije partner.");

                    TempData[Constants.Message] = $"Partner s identifikatorom {id} više nije partner.";
                    TempData[Constants.ErrorOccurred] = false;
                }
                catch (Exception exc)
                {
                    logger.LogError(new EventId(1000), $"Pogreška prilikom brisanja partnera: {exc.CompleteExceptionMessage()}");

                    TempData[Constants.Message] = $"Pogreška prilikom brisanja partnera: {exc.CompleteExceptionMessage()}";
                    TempData[Constants.ErrorOccurred] = true;
                }

            }
            else
            {
                logger.LogError(new EventId(1000), $"Ne postoji partner s identifikatorom: {id}");

                TempData[Constants.Message] = $"Ne postoji partner s identifikatorom: {id}";
                TempData[Constants.ErrorOccurred] = true;
            }

            return RedirectToAction(nameof(Index), new { sort, page, ascending });
        }

        /// <summary>
        /// Dohvaca podatke partnera kojeg se zeli azurirati.
        /// </summary>
        /// <param name="id">identifikator partnera kojeg se azurira</param>
        /// <returns>View edit stranice partnera</returns>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var partner = await _context.Partneri
                                  .Include(p => p.IdSuradnikNavigation)
                                  .Include(p => p.Zahtijevs)
                                  .AsNoTracking()
                                  .Where(p => p.IdSuradnik == id)
                                  .SingleOrDefaultAsync();

            var zahtijevModeli = new List<ZahtijevViewModel>();
            foreach (var zahtijev in partner.Zahtijevs)
            {
                ZahtijevViewModel m = new ZahtijevViewModel
                {
                    IdZah = zahtijev.IdZah,
                    OpisZahtijev = zahtijev.OpisZahtijev,
                    Prioritet = zahtijev.Prioritet,
                    NazivVrsteZahtijeva = _context.VrstaZahtjeva.Where(i => i.IdVrstaZah == zahtijev.IdVrstaZah).SingleOrDefault().NazivVrstaZah,
                    NazivSuradnika = partner.IdSuradnikNavigation.Ime
                };

                zahtijevModeli.Add(m);
            }


            PartnerViewModel model = new PartnerViewModel
            {
                IdSuradnik = partner.IdSuradnik,
                Partner = partner.IdSuradnikNavigation,
                BrZahtijeva = partner.Zahtijevs.Count,
                Zahtijevi = zahtijevModeli
            };

            ViewBag.ZahtijevNextId = _context.Zahtjevi.Max(p => p.IdZah) + 1;

            if (partner != null)
            {
                await PrepareDropDownLists();
                return View(model);
            }
            else
            {
                return NotFound($"Neispravan id partnera: {id}");
            }
        }

        /// <summary>
        /// Pokusa promijeniti podatke partnera i njegove zahtjeve.
        /// </summary>
        /// <param name="model">model partnera kojeg se azurira</param>
        /// <param name="id">identifikator partnera kojeg se azurira</param>
        /// <param name="page">index tablice na kojoj ce se stranica otvoriti</param>
        /// <param name="sort">argument po kojem se tablica sortira</param>
        /// <param name="ascending">argument oznacava uzlazno ili silazno sortiranje</param>
        /// <returns>View edit stranice partnera</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PartnerViewModel model, int id, int page = 1, int sort = 1, bool ascending = true)
        {
            if (ModelState.IsValid)
            {
                var partner = await _context.Partneri
                                        .Include(p => p.IdSuradnikNavigation)
                                        .Include(p => p.Zahtijevs)
                                        .Where(e => e.IdSuradnik == id)
                                        .FirstOrDefaultAsync();

                if (partner == null)
                {
                    return NotFound("Ne postoji partner s id-om: " + id);
                }

                List<int> IdZahtjevi = model.Zahtijevi
                                        .Where(z => z.IdZah != 0)
                                        .Select(z => z.IdZah)
                                        .ToList();

                
                var delZahtijevi = partner.Zahtijevs.Where(e => !IdZahtjevi.Contains(e.IdZah));

                foreach (var zahtijev in delZahtijevi)
                {
                    _context.Remove(zahtijev);
                }

                var nextId = _context.Zahtjevi.Max(p => p.IdZah);
                foreach (var zahtijev in model.Zahtijevi)
                {
                    Zahtijev noviZah;
                    
                    if (zahtijev.IdZah > 0)
                    {
                        noviZah = partner.Zahtijevs.Where(z => z.IdZah == zahtijev.IdZah).SingleOrDefault();
                    }
                    else
                    {
                        nextId++;
                        noviZah = new Zahtijev
                        {
                            IdZah = nextId,
                        };

                        partner.Zahtijevs.Add(noviZah);
                    }
                    var IdVrstaZah = _context.VrstaZahtjeva
                                        .Where(z => z.NazivVrstaZah
                                        .Equals(zahtijev.NazivVrsteZahtijeva))
                                        .SingleOrDefault()
                                        .IdVrstaZah;

                    noviZah.OpisZahtijev = zahtijev.OpisZahtijev;
                    noviZah.Prioritet = zahtijev.Prioritet;
                    noviZah.IdVrstaZah = IdVrstaZah;
                    noviZah.IdSuradnik = partner.IdSuradnik;
                }

                try
                {
                    await _context.SaveChangesAsync();

                    if (model.IdSuradnik != id)
                    {
                        DetachZahtijevs();

                        if (!await ChangePartner(id, model.IdSuradnik))
                        {
                            logger.LogError("Neuspješno prebacivanje zahtjeva");

                            TempData[Constants.Message] = $"Neuspješno prebacivanje zahtjeva";
                            TempData[Constants.ErrorOccurred] = true;

                            return RedirectToAction(nameof(Edit), new { id, page, sort, ascending });
                        }

                        id = model.IdSuradnik;
                    }

                    logger.LogInformation($"Partner sa ID '{partner.IdSuradnik}' uspjesno azuriran");

                    TempData[Constants.Message] = $"Partner sa ID '{partner.IdSuradnik}' uspjesno azuriran";
                    TempData[Constants.ErrorOccurred] = false;

                    return RedirectToAction(nameof(Edit), new { id, page, sort, ascending });
                }
                catch (Exception exc)
                {
                    logger.LogError($"Pogreska prilikom azuriranja partnera: " + exc.CompleteExceptionMessage());

                    TempData[Constants.Message] = "Pogreška prilikom ažuriranja partnera: " + exc.CompleteExceptionMessage();
                    TempData[Constants.ErrorOccurred] = true;
                }
            }

            return View(model);
        }

        public void DetachZahtijevs()
        {
            var undetachedEntriesCopy = _context.ChangeTracker.Entries()
                .Where(e => e.State != EntityState.Detached && e.Entity is Zahtijev)
                .ToList();

            foreach (var entry in undetachedEntriesCopy)
                entry.State = EntityState.Detached;
        }

        /// <summary>
        /// Pokusa prebaciti zahtjeve sa jednog partnera na drugog.
        /// </summary>
        /// <returns>True ako je uspjesno, false ako nije.</returns>
        public async Task<bool> ChangePartner(int oldId, int newId)
        {
            bool result;
            var zahtijevi = await _context.Zahtjevi
                              .AsNoTracking()
                              .Where(p => p.IdSuradnik == oldId)
                              .ToListAsync();

            foreach (var zahtijev in zahtijevi)
            {
                zahtijev.IdSuradnik = newId;
                _context.Update(zahtijev);
            }

            try
            {
                await _context.SaveChangesAsync();
                result = true;
            }
            catch (Exception exc)
            {
                result = false;
            }

            return result;
        }

        [HttpGet]
        public async Task<IActionResult> Get(int id, int page, int sort, bool ascending)
        {
            var partner = await _context.Partneri
                                  .Where(p => p.IdSuradnik == id)
                                  .Select(p => new PartnerViewModel
                                  {
                                      IdSuradnik = p.IdSuradnik,
                                      Partner = p.IdSuradnikNavigation,
                                  })
                                  .SingleOrDefaultAsync();

            if (partner != null)
            {
                return PartialView(partner);
            }
            else
            {
                return NotFound($"Neispravan id partnera: {id}");
            }
        }

        /// <summary>
        /// Dohvaca partnera s podatcima i njegove zahtjeve za tablicni prikaz
        /// </summary>
        /// <param name="id">identifikator partnera</param>
        /// <param name="page">index tablice na kojoj ce se stranica otvoriti</param>
        /// <param name="sort">argument po kojem se tablica sortira</param>
        /// <param name="ascending">argument oznacava uzlazno ili silazno sortiranje</param>
        /// <returns>View show stranice partnera</returns>
        public async Task<IActionResult> Show(int id, int page = 1, int sort = 1, bool ascending = true)
        {
            var partner = await _context.Partneri
                .Where(p => p.IdSuradnik == id)
                .Select(p => new PartnerViewModel
                {
                    IdSuradnik = p.IdSuradnik,
                    Partner = p.IdSuradnikNavigation,
                })
                .FirstOrDefaultAsync();

            if (partner == null)
            {
                return NotFound($"Partner s identifikatorom {id} ne postoji.");
            }
            else
            {
                var zahtijevi = await _context.Zahtjevi
                    .Include(p => p.IdSuradnikNavigation)
                    .Include(p => p.IdVrstaZahNavigation)
                    .Where(d => d.IdSuradnik == id)
                    .OrderBy(d => d.IdZah)
                    .Select(d => new ZahtijevViewModel
                    {
                        OpisZahtijev = d.OpisZahtijev,
                        Prioritet = d.Prioritet,
                        IdZah = d.IdZah,
                        NazivSuradnika = d.IdSuradnikNavigation.IdSuradnikNavigation.Ime,
                        NazivVrsteZahtijeva = d.IdVrstaZahNavigation.NazivVrstaZah,
                    })
                    .ToListAsync();

                partner.Zahtijevi = zahtijevi;

                await SetPreviousAndNextProjekt(partner.IdSuradnik);

                ViewBag.Page = page;
                ViewBag.Sort = sort;
                ViewBag.Ascending = ascending;
                ViewBag.Position = partner.IdSuradnik;

                return View("Show", partner);
            }
        }

        private async Task SetPreviousAndNextProjekt(int IdSuradnik)
        {
            var partneri = await _context.Partneri
           .OrderBy(p => p.IdSuradnik)
           .Select(p => new
           {
               IdSuradnik = p.IdSuradnik
           })
           .ToListAsync();

            var currentIndex = partneri.FindIndex(p => p.IdSuradnik == IdSuradnik);

            if (currentIndex > 0)
            {
                ViewBag.Previous = partneri[currentIndex - 1].IdSuradnik;
            }

            if (currentIndex < partneri.Count - 1)
            {
                ViewBag.Next = partneri[currentIndex + 1].IdSuradnik;
            }
        }
    }
}
