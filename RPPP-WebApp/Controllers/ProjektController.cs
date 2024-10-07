using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.Rendering;
using RPPP_WebApp.Extensions;
using RPPP_WebApp.Extensions.Selectors;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;
using System.Text.Json;

namespace RPPP_WebApp.Controllers
{
  public class ProjektController : Controller
{
    private readonly Rppp07Context _context;
    private readonly AppSettings appData;
    private readonly ILogger<ProjektController> logger;
    public ProjektController(Rppp07Context context, IOptionsSnapshot<AppSettings> options, ILogger<ProjektController> logger)
    {
        _context = context;
        appData = options.Value;
        this.logger = logger;
    }

    public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
    {
        int page_size = appData.PageSize;
        var query = _context.Projekti
                            .Include(p => p.IdVrsteProjektaNavigation)
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
        query = query.ApplySort(sort, ascending);

        var projekti = await query
            .Select(p => new ProjektViewModel
            {
                NazivProjekta = p.NazivProjekt,
                Kratica = p.Kratica,
                Cilj = p.Cilj,
                IdProjekta = p.IdProjekt,
                NazivVrsteProjekta = p.IdVrsteProjektaNavigation.NazivVrsteProjekta,
                Dokumenti = _context.Dokumenti.Where(d => d.IdProjekt == p.IdProjekt)
                                                                .Select(d => new DokumentViewModel
                                                                {
                                                                  IdDoc = d.IdDoc
                                                                })
                                                                .ToList()
            })
            .Skip((page - 1) * page_size)
            .Take(page_size)
            .ToListAsync();

        var model = new ProjektiViewModel
        {
            Projekti = projekti,
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
    public async Task<IActionResult> Create(Projekt projekt)
    {
      logger.LogTrace(JsonSerializer.Serialize(projekt));
      if (ModelState.IsValid)
      {
        try
        {
          var maxID = _context.Projekti.Max(p => p.IdProjekt);
          var nextId = maxID + 1;

          var novi_projekt = new Projekt {
            IdProjekt = nextId,
            NazivProjekt = projekt.NazivProjekt,
            Kratica = projekt.Kratica,
            Cilj = projekt.Cilj,
            IdVrsteProjekta = projekt.IdVrsteProjekta
          };
          _context.Add(novi_projekt);
          await _context.SaveChangesAsync();

          logger.LogInformation(new EventId(1000), $"Projekt {novi_projekt.NazivProjekt} dodan.");

          TempData[Constants.Message] = $"Projekt {novi_projekt.NazivProjekt} dodan.";
          TempData[Constants.ErrorOccurred] = false;

          return RedirectToAction(nameof(Index));

        }
        catch (Exception exc)
        {
          logger.LogError("Pogreška prilikom dodavanje novog projekta: {0}", exc.CompleteExceptionMessage());
          ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
          await PrepareDropDownLists();
          return View(projekt);
        }
      }
      else
      {
        await PrepareDropDownLists();
        return View(projekt);
      }
    }
    private async Task PrepareDropDownLists()
    {
        var vrste = await _context.VrstaProjekta                  
                                    .Select(v => new { v.IdVrsteProjekta, v.NazivVrsteProjekta })
                                    .ToListAsync();
        ViewBag.VrsteProjekta = new SelectList(vrste, "IdVrsteProjekta", "NazivVrsteProjekta");
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
      ActionResponseMessage responseMessage;
      var projekt = await _context.Projekti.FindAsync(id);
      var dokumenti = await _context.Dokumenti.Where(d => d.IdProjekt == id).ToListAsync();
      if (projekt != null)
      {
        try
        {
          string naziv = projekt.NazivProjekt;
          foreach (var dokument in dokumenti) {
             _context.Remove(dokument);
          }
          _context.Remove(projekt);
          await _context.SaveChangesAsync();
          responseMessage = new ActionResponseMessage(MessageType.Success, $"Projekt {naziv} sa šifrom {id} uspješno obrisan.");          
        }
        catch (Exception exc)
        {        
          responseMessage = new ActionResponseMessage(MessageType.Error, $"Pogreška prilikom brisanja projekta: {exc.CompleteExceptionMessage()}");
        }
      }
      else
      {
        responseMessage = new ActionResponseMessage(MessageType.Error, $"Projekt sa šifrom {id} ne postoji");
      }
      Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });
      return responseMessage.MessageType == MessageType.Success ?
        new EmptyResult() : await Get(id,1,1,true);
    }


    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
      var projekt = await _context.Projekti
                            .AsNoTracking()
                            .Include(p => p.Dokumenti)
                            .Where(p => p.IdProjekt == id)
                            .SingleOrDefaultAsync();
      if (projekt != null)
      {        
        await PrepareDropDownLists();
        return PartialView(projekt);
      }
      else
      {
        return NotFound($"EDITGET Neispravan id projekta: {id}");
      }
    }

    [HttpPost]    
    public async Task<IActionResult> Edit(Projekt projekt)
    {
      if (projekt == null)
      {
        return NotFound("Nema poslanih podataka");
      }
      bool checkId = await _context.Projekti.AnyAsync(p => p.IdProjekt == projekt.IdProjekt);
      if (!checkId)
      {
        return NotFound($"EDITPOST Neispravan id projekta: {projekt?.IdProjekt} , {projekt?.NazivProjekt}.");
      }

      if (ModelState.IsValid)
      {
        try
        {
          _context.Update(projekt);
          await _context.SaveChangesAsync();

          return RedirectToAction(nameof(Get), new { id = projekt.IdProjekt });
        }
        catch (Exception exc)
        {
          ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
          await PrepareDropDownLists();
          return PartialView(projekt);
        }
      }
      else
      {
        await PrepareDropDownLists();
        return PartialView(projekt);
      }
    }

    [HttpGet]
    public async Task<IActionResult> Get(int id, int page, int sort, bool ascending)
    {
      var projekt = await _context.Projekti                            
                            .Where(p => p.IdProjekt == id)
                            .Select(p => new ProjektViewModel
                            {
                              NazivProjekta = p.NazivProjekt,
                              Kratica = p.Kratica,
                              Cilj = p.Cilj,
                              IdProjekta = p.IdProjekt,
                              NazivVrsteProjekta = p.IdVrsteProjektaNavigation.NazivVrsteProjekta,
                              Dokumenti = _context.Dokumenti.Where(d => d.IdProjekt == p.IdProjekt)
                                                                .Select(d => new DokumentViewModel
                                                                {
                                                                  IdDoc = d.IdDoc
                                                                })
                                                                .ToList()
                            })
                            .SingleOrDefaultAsync();

      if (projekt != null)
      {
        return PartialView(projekt);
      }
      else
      {
        return NotFound($"GET Neispravan id projekta: {id}");
      }
    }

    public async Task<IActionResult> Show(int id, int page = 1, int sort = 1, bool ascending = true)
    {

        var projekt = await _context.Projekti
            .Where(p => p.IdProjekt == id)
            .Select(p => new ProjektViewModel
            {
                NazivProjekta = p.NazivProjekt,
                Kratica = p.Kratica,
                Cilj = p.Cilj,
                IdProjekta = p.IdProjekt,
                NazivVrsteProjekta = p.IdVrsteProjektaNavigation.NazivVrsteProjekta
            })
            .FirstOrDefaultAsync();

        if (projekt == null)
        {
            return NotFound($"Projekt {id} ne postoji");
        }
        else
        {
            var dokumenti = await _context.Dokumenti
                .Include(p => p.IdVrstaNavigation)
                .Where(d => d.IdProjekt == id)
                .OrderBy(d => d.IdDoc)
                .Select(d => new DokumentViewModel
                {
                    NazivDokument = d.NazivDokument,
                    NazivDatoteke = d.NazivDatoteke,
                    IdProjekt = d.IdProjekt,
                    IdVrsta = d.IdVrsta,
                    IdDoc = d.IdDoc,
                    Dokument1 = d.Dokument1,
                    IdVrstaNavigation = d.IdVrstaNavigation
                })
                .ToListAsync();

            projekt.Dokumenti = dokumenti;

            if (projekt == null)
            {
                return NotFound($"Projekt {id} ne postoji");
            }
            else
            {
                await SetPreviousAndNextProjekt(projekt.IdProjekta);

                ViewBag.Page = page;
                ViewBag.Sort = sort;
                ViewBag.Ascending = ascending;
                ViewBag.Position = projekt.IdProjekta;

                return View("Show", projekt);
            }
        }
    }

    private async Task SetPreviousAndNextProjekt(int IdProjekt)
    {
         var orderedProjects = await _context.Projekti
        .OrderBy(p => p.IdProjekt)
        .Select(p => new
        {
            IdProjekt = p.IdProjekt
        })
        .ToListAsync();

        var currentIndex = orderedProjects.FindIndex(p => p.IdProjekt == IdProjekt);

        if (currentIndex > 0)
        {
            ViewBag.Previous = orderedProjects[currentIndex - 1].IdProjekt;
        }

        if (currentIndex < orderedProjects.Count - 1)
        {
            ViewBag.Next = orderedProjects[currentIndex + 1].IdProjekt;
        }
    }
  }
}
