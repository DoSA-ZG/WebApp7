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


    public class TransakcijaController : Controller
    {
        private readonly Rppp07Context _context;
        private readonly AppSettings appData;
        private readonly ILogger<TransakcijaController> logger;

        public TransakcijaController(Rppp07Context context, IOptionsSnapshot<AppSettings> options, ILogger<TransakcijaController> logger)
        {
            _context = context;
            appData = options.Value;
            this.logger = logger;
        }

        public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
        {
            int page_size = appData.PageSize;
            var query = _context.Transakcije
                                .Include(t => t.IbanKorisnikNavigation)
                                .Include(t => t.IdVrstaTransNavigation)
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

            var transakcije = await query.Select(w => new TransakcijaViewModel
            {
                IdTransakcija = w.IdTransakcija,
                Iznos = w.Iznos,
                OpisTrans = w.OpisTrans,
                Vrijeme = w.Vrijeme,
                BrRacuna = w.BrRacuna,
                IbanKorisnik = w.IbanKorisnik,
                NazivKorisnik = w.IbanKorisnikNavigation.NazivKorisnik,
                NazivVrstaTransakcija = w.IdVrstaTransNavigation.NazivVrstaTrans,

            }).Skip((page - 1) * page_size).Take(page_size).ToListAsync();

            var model = new TransakcijeViewModel
            {
                Transakcije = transakcije,
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
            var vrsteDokumenata = await _context.Kartice
                                    .Select(v => new { v.BrRacuna })
                                    .ToListAsync();
            var vrsteTransakcija = await _context.VrstaTrans
                                                .Select(v => new { v.IdVrstaTrans, v.NazivVrstaTrans })
                                                .ToListAsync();

            ViewBag.VrsteDokumenata = new SelectList(vrsteDokumenata, "IdVrsteDoc", "NazivVrstaDoc");
            ViewBag.IdVrstaTrans = new SelectList(vrsteTransakcija, "IdVrstaTrans", "NazivVrstaTrans");

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Transakcija transakcija)
        {
            Console.WriteLine(transakcija.Iznos + " " + transakcija.OpisTrans);

            TransackijaValidator validator = new(_context);
            ValidationResult result = validator.Validate(transakcija);

            if (result.IsValid)
            {
                logger.LogTrace(JsonSerializer.Serialize(transakcija));
                if (ModelState.IsValid)
                {
                    try
                    {
                        var maxID = _context.Transakcije.Max(p => p.IdTransakcija);
                        var nextId = maxID + 1;
                        var novaTrans = new Transakcija
                        {
                            IdTransakcija = nextId,
                            OpisTrans = transakcija.OpisTrans,
                            Iznos = transakcija.Iznos,
                            IbanKorisnik = transakcija.IbanKorisnik,
                            IdVrstaTrans = transakcija.IdVrstaTrans
                        };
                        _context.Add(novaTrans);
                        await _context.SaveChangesAsync();

                        logger.LogInformation(new EventId(1000), $"Transakcija {novaTrans.IdTransakcija} dodan.");

                        TempData[Constants.Message] = $"Transakcija {novaTrans.IdTransakcija} dodan.";
                        TempData[Constants.ErrorOccurred] = false;
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception exc)
                    {
                        logger.LogError("Pogreška prilikom dodavanje novoe transakcije: {0}", exc.CompleteExceptionMessage());
                        ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                        await PrepareDropDownLists();
                        return View(transakcija);
                    }
                }
                else
                {
                    await PrepareDropDownLists();
                    return View(transakcija);
                }
            }

            foreach (ValidationFailure error in result.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            await PrepareDropDownLists();
            return View(transakcija);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int sort, int page, bool ascending)
        {
            Console.WriteLine("ALO");
            var transakcija = await _context.Transakcije.FindAsync(id);
            if (transakcija != null)
            {
                try
                {
                    _context.Remove(transakcija);
                    await _context.SaveChangesAsync();

                    TempData[Constants.Message] = $"Transakcija s identifikatorom {id} uspješno obrisana";
                    TempData[Constants.ErrorOccurred] = false;
                }
                catch (Exception exc)
                {
                    TempData[Constants.Message] = $"Pogreška prilikom brisanja transakcije: {exc.CompleteExceptionMessage()}";
                    TempData[Constants.ErrorOccurred] = true;
                }
            }
            else
            {
                TempData[Constants.Message] = $"Ne postoji Transakcija s identifikatorom: {id}";
                TempData[Constants.ErrorOccurred] = true;
            }

            return RedirectToAction(nameof(Index), new { sort, page, ascending });
        }





        [HttpGet]
        public async Task<IActionResult> Edit(int id, int sort = 1, int page = 1, bool ascending = true)
        {
            var transakcija = await _context.Transakcije
                            .AsNoTracking()
                            .Where(z => z.IdTransakcija == id)
                            .SingleOrDefaultAsync();
            if (transakcija!= null)
            {
                var pagingInfo = new PagingInfo
                {
                    CurrentPage = page,
                    Sort = sort,
                    Ascending = ascending,
                };
                ViewBag.PagingInfo = pagingInfo;

                await PrepareDropDownLists();
                return View(transakcija);
            }
            else
            {
                return NotFound($"Neispravan id transakcije: {id}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var transakcija = await _context.Transakcije
                                  .Where(z => z.IdTransakcija == id)
                                  .Select(z => new TransakcijaViewModel
                                  {
                                      IdTransakcija = z.IdTransakcija,
                                      Iznos = z.Iznos,
                                      OpisTrans = z.OpisTrans,
                                      Vrijeme = z.Vrijeme,
                                      BrRacuna = z.BrRacuna,
                                      IbanKorisnik = z.IbanKorisnik,
                                      NazivKorisnik = z.IbanKorisnikNavigation.NazivKorisnik,
                                      NazivVrstaTransakcija = z.IdVrstaTransNavigation.NazivVrstaTrans,
                                  })
                                  .SingleOrDefaultAsync();
            if (transakcija != null)
            {
                return PartialView(transakcija);
            }
            else
            {
                return NotFound($"Neispravan id transakcije: {id}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Transakcija transakcija, int id, int sort = 1, int page = 1, bool ascending = true)
        {
            TransackijaValidator validator = new(_context);
            ValidationResult result = validator.Validate(transakcija);

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
                    var model = await _context.Transakcije
                                .Where(e => e.IdTransakcija == id)
                                .FirstOrDefaultAsync();

                    if (model == null)
                    {
                        return NotFound($"Ne postoji transakcija s identifikatorom: {id}");
                    }

                    if (await TryUpdateModelAsync<Transakcija>(model, "", z => z.Iznos, z => z.OpisTrans, z => z.Vrijeme, z => z.BrRacuna, Z=>Z.IbanKorisnik))
                    {
                        try
                        {
                            await _context.SaveChangesAsync();

                            TempData[Constants.Message] = "Transakcija uspješno ažurirana";
                            TempData[Constants.ErrorOccurred] = false;

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
                        ModelState.AddModelError(string.Empty, "Transakciju nije bilo moguće ažurirati");

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
            return View(transakcija);

        }
    }

