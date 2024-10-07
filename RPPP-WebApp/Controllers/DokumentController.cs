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


namespace RPPP_WebApp.Controllers
{

  public class DokumentController : Controller
  {
      private readonly Rppp07Context _context;
      private readonly AppSettings appData;
      private readonly ILogger<DokumentController> logger;
      private readonly string fixedPath = "Documents";

      public DokumentController(Rppp07Context context, IOptionsSnapshot<AppSettings> options, ILogger<DokumentController> logger)
      {
          _context = context;
          appData = options.Value;
          this.logger = logger;
      }

      public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
      {
          int pagesize = appData.PageSize;

          var query = _context.Dokumenti
                              .Include(d => d.IdProjektNavigation)
                              .Include(d => d.IdVrstaNavigation)
                              .AsNoTracking();

          int count = await query.CountAsync();

          if (sort == 1 && TempData["SortOrder"] != null)
          {
              sort = Convert.ToInt32(TempData["SortOrder"]);
              ascending = Convert.ToBoolean(TempData["Ascending"]);
          }

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

          query = query.ApplySort(sort, ascending);

          var dokumenti = await query.Select(d => new DokumentViewModel
                                      {
                                          NazivDokument = d.NazivDokument,
                                          NazivDatoteke = d.NazivDatoteke,
                                          IdProjekt = d.IdProjekt,
                                          IdVrsta = d.IdVrsta,
                                          IdDoc = d.IdDoc,
                                          Dokument1 = d.Dokument1,
                                          IdProjektNavigation = d.IdProjektNavigation,
                                          IdVrstaNavigation = d.IdVrstaNavigation 
                                      })
                                      .Skip((page - 1) * pagesize)
                                      .Take(pagesize)
                                      .ToListAsync();

          var model = new DokumentiViewModel
          {
              Dokumenti = dokumenti,
              PagingInfo = pagingInfo
          };

          UpdateFiles(sort,ascending, page);
          ViewBag.Sort = sort;
          ViewBag.Ascending = ascending;
          ViewBag.Page = page;

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
      public async Task<IActionResult> Create(Dokument dokument)
      {
      logger.LogTrace(JsonSerializer.Serialize(dokument));
      if (ModelState.IsValid)
      {
          try
          {
          if (dokument.UploadedFile != null && dokument.UploadedFile.Length > 0)
          {
              using (var memoryStream = new MemoryStream())
              {
                  dokument.UploadedFile.CopyTo(memoryStream);
                  dokument.Dokument1 = memoryStream.ToArray();
              }
          }

          _context.Add(dokument);
          await _context.SaveChangesAsync();

          logger.LogInformation(new EventId(1000), $"Dokument {dokument.NazivDokument} dodan.");
          logger.LogInformation(new EventId(1000), $"Dokument na projektu {dokument.IdProjekt} dodan.");

          TempData[Constants.Message] = $"Dokument {dokument.NazivDokument} dodan.";
          TempData[Constants.ErrorOccurred] = false;

          return RedirectToAction(nameof(Index));

          }
          catch (Exception exc)
          {
          logger.LogError("Pogreška prilikom dodavanje novog dokumenta: {0}", exc.CompleteExceptionMessage());
          ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
          await PrepareDropDownLists();
          return View(dokument);
          }
      }
      else
      {
          await PrepareDropDownLists();
          return View(dokument);
      }
      }
      private async Task PrepareDropDownLists()
      {
          var vrsteDokumenata = await _context.VrstaDocsa                  
                                      .Select(v => new { v.IdVrsteDoc, v.NazivVrstaDoc })
                                      .ToListAsync();
          var naziviProjekta = await _context.Projekti                  
                                      .Select(p => new { p.IdProjekt, p.NazivProjekt })
                                      .ToListAsync();
          ViewBag.VrsteDokumenata = new SelectList(vrsteDokumenata, "IdVrsteDoc", "NazivVrstaDoc");
          ViewBag.NaziviProjekata = new SelectList(naziviProjekta, "IdProjekt", "NazivProjekt");
      }

      [HttpDelete]
      public async Task<IActionResult> Delete(int id)
      {
        ActionResponseMessage responseMessage;
        var dokument = await _context.Dokumenti.FindAsync(id);          
        if (dokument != null)
        {
          try
          {
            string naziv = dokument.NazivDokument;
            string filePath = Path.Combine(fixedPath,$"{dokument.IdDoc}_{dokument.NazivDatoteke}");

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.Remove(dokument);
            await _context.SaveChangesAsync();
            responseMessage = new ActionResponseMessage(MessageType.Success, $"Dokument {naziv} sa šifrom {id} uspješno obrisan.");          
          }
          catch (Exception exc)
          {        
            responseMessage = new ActionResponseMessage(MessageType.Error, $"Pogreška prilikom brisanja dokumenta: {exc.CompleteExceptionMessage()}");
          }
        }
        else
        {
          responseMessage = new ActionResponseMessage(MessageType.Error, $"Dokument sa šifrom {id} ne postoji");
        }
        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });
        return responseMessage.MessageType == MessageType.Success ?
          new EmptyResult() : await Get(id);
      }


      [HttpGet]
  public async Task<IActionResult> Edit(int id, int? page, int? sort, bool? ascending)
  {
      var dokument = await _context.Dokumenti
                              .Include(d => d.IdProjektNavigation)
                              .Include(d => d.IdVrstaNavigation)
                              .AsNoTracking()
                              .Where(d => d.IdDoc == id)
                              .SingleOrDefaultAsync();
      if (dokument != null)
      {    
          await PrepareDropDownLists();
          ViewBag.Id = id;
          ViewBag.Sort = sort;
          ViewBag.Ascending = ascending;
          ViewBag.Page = page;
          return View("Edit", dokument);
      }
      else
      {
          logger.LogWarning("Ne postoji dokument s oznakom: {0} ", id); 
          return NotFound($"Neispravan id dokumenta: {id}");
      }
  }

      [HttpPost]    
      public async Task<IActionResult> Edit(Dokument dokument, int page, int sort, bool ascending, bool isEdit)
      {
        if (dokument == null)
        {
          return NotFound("Nema poslanih podataka");
        } 
        bool checkId = await _context.Dokumenti.AnyAsync(d => d.IdDoc == dokument.IdDoc);
        if (!checkId)
        {
          return NotFound($"Neispravan id dokumenta: {dokument?.IdDoc}");
        }

        if (ModelState.IsValid)
        {
          try
          {

            if (dokument.UploadedFile != null && dokument.UploadedFile.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    dokument.UploadedFile.CopyTo(memoryStream);
                    string fileContent = Encoding.UTF8.GetString(memoryStream.ToArray());
                    dokument.Dokument1 = Encoding.UTF8.GetBytes(fileContent);
                }
            }

            _context.Update(dokument);
            await _context.SaveChangesAsync();

            TempData[Constants.Message] = $"Dokument {dokument.NazivDokument} ažuriran.";
            TempData[Constants.ErrorOccurred] = false;

            return RedirectToAction("Index", new { page, sort, ascending, isEdit });
          }
          catch (Exception exc)
          {
            logger.LogError("Pogreška prilikom ažuriranja dokumenta: {0}", exc.CompleteExceptionMessage());
            ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
            await PrepareDropDownLists();
            return View(dokument);
          }
        }
        else
        {
          await PrepareDropDownLists();
          return View(dokument);
        }
      }

      [HttpGet]
      public async Task<IActionResult> Get(int id)
      {
        var dokument = await _context.Dokumenti                            
                              .Where(d => d.IdDoc == id)
                              .Select(d => new DokumentViewModel
                                      {
                                          NazivDokument = d.NazivDokument,
                                          NazivDatoteke = d.NazivDatoteke,
                                          IdProjekt = d.IdProjekt,
                                          IdVrsta = d.IdVrsta,
                                          IdDoc = d.IdDoc,
                                          Dokument1 = d.Dokument1,
                                          IdProjektNavigation = d.IdProjektNavigation,
                                          IdVrstaNavigation = d.IdVrstaNavigation 
                                      })
                              .SingleOrDefaultAsync();
        if (dokument != null)
        {
          return PartialView(dokument);
        }
        else
        {
          return NotFound($"Neispravan id dokumenta: {id}");
        }
      }

    public IActionResult OpenInEditor(int page, int sort, bool ascending, bool isEdit, int? showId, int id = 1, bool isShow = false)
      {
          var dokument = _context.Dokumenti.Find(id);
        
          if (dokument == null)
          {
              return NotFound();
          }

          string uniqueFileName = $"{dokument.IdDoc}_{dokument.NazivDatoteke}";
          string fullPath = Path.Combine(fixedPath, uniqueFileName);

          if (dokument.Dokument1 != null)
          {
              System.IO.File.WriteAllBytes(fullPath, dokument.Dokument1);
          }
          else
          {
            return NotFound("Nepostojeci dokument");
          }

          Process.Start("notepad.exe", fullPath);

          if (isShow) return RedirectToAction("Show", "Projekt", new { id = showId, page, sort, ascending });
          if (isEdit) return RedirectToAction("Edit", new { id, page, sort, ascending });
          return RedirectToAction("Index", new { page, sort, ascending });
      }

      public IActionResult UpdateFiles(int sort, bool ascending, int page)
      {
          ViewData["Sort"] = sort;
          ViewData["Ascending"] = ascending;
          ViewData["Page"] = page;

          if (!Directory.Exists(fixedPath))
          {
              try
              {
                  Directory.CreateDirectory(fixedPath);
                  Console.WriteLine("Stvaram direktorij za dokumente");
              }
              catch
              {
                  Console.WriteLine("Greska tijekom stvaranja direktorija");
                  return RedirectToAction("Index", new { sort, ascending });
              }
          }

          var documents = _context.Dokumenti.ToList();

          foreach (var dokument in documents)
          {
              try
              {
                  if (dokument.Dokument1 != null)
                  {
                      string uniqueFileName = $"{dokument.IdDoc}_{dokument.NazivDatoteke}";
                      string filePath = Path.Combine(fixedPath, uniqueFileName);
                      System.IO.File.WriteAllBytes(filePath, dokument.Dokument1);
                  }
              }
              catch (Exception ex)
              {
                  Console.WriteLine($"Pogreška: {ex.Message}");
              }
          }

          return RedirectToAction("Index", new { sort, ascending });
      }

  }
}
