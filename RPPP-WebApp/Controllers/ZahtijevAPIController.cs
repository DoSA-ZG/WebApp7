using iText.StyledXmlParser.Jsoup.Parser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using RPPP_WebApp.Extensions;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;
using System.Linq.Expressions;

namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// API kontroler razred za zahtijev
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    [TypeFilter(typeof(ProblemDetailsForSqlException))]
    public class ZahtijevAPIController : ControllerBase, ICustomController<int, ZahtijevViewModel>
    {

        private readonly Rppp07Context ctx;
        private readonly ILogger<ZahtijevAPIController> logger;
        private static Dictionary<string, Expression<Func<Zahtijev, object>>> orderSelectors = new()
        {
            [nameof(ZahtijevViewModel.IdZah).ToLower()] = m => m.IdZah,
            [nameof(ZahtijevViewModel.OpisZahtijev).ToLower()] = m => m.OpisZahtijev,
            [nameof(ZahtijevViewModel.Prioritet).ToLower()] = m => m.Prioritet,
            [nameof(ZahtijevViewModel.NazivVrsteZahtijeva).ToLower()] = m => m.IdVrstaZahNavigation.NazivVrstaZah,
            [nameof(ZahtijevViewModel.NazivSuradnika).ToLower()] = m => m.IdSuradnikNavigation.IdSuradnikNavigation.Ime
        };

        private static Expression<Func<Zahtijev, ZahtijevViewModel>> projection = m => new ZahtijevViewModel
        {
            IdZah = m.IdZah,
            OpisZahtijev = m.OpisZahtijev,
            Prioritet = m.Prioritet,
            NazivVrsteZahtijeva = m.IdVrstaZahNavigation.NazivVrstaZah,
            NazivSuradnika = m.IdSuradnikNavigation.IdSuradnikNavigation.Ime
        };

        public ZahtijevAPIController(Rppp07Context ctx, ILogger<ZahtijevAPIController> logger)
        {
            this.ctx = ctx;
            this.logger = logger;
        }

        [HttpGet("count", Name = "BrojZahtijeva")]
        public async Task<int> Count([FromQuery] string filter)
        {
            var query = ctx.Zahtjevi.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(m => m.IdVrstaZahNavigation.NazivVrstaZah.Contains(filter));
            }
            int count = await query.CountAsync();
            return count;
        }

        [HttpPost(Name = "DodajZahtijev")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(ZahtijevViewModel model)
        {
            var maxID = ctx.Zahtjevi.Max(a => a.IdZah);
            Zahtijev zahtijev = new Zahtijev
            {
                IdZah = maxID + 1,
                OpisZahtijev = model.OpisZahtijev,
                Prioritet = model.Prioritet,
                IdVrstaZah = ctx.VrstaZahtjeva.FirstOrDefault(d => d.NazivVrstaZah == model.NazivVrsteZahtijeva).IdVrstaZah,
                IdSuradnik = ctx.Osobe.FirstOrDefault(d => d.Ime == model.NazivSuradnika).IdSuradnik
            };
            ctx.Add(zahtijev);
            await ctx.SaveChangesAsync();
            logger.LogInformation(new EventId(1000), $"Zahtjev s identifikatorom {maxID + 1} dodan.");

            var addedItem = await Get(zahtijev.IdZah);

            return CreatedAtAction(nameof(Get), new { id = zahtijev.IdZah }, addedItem.Value);
        }


        [HttpDelete("{id}", Name = "ObrisiZahtijev")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var zahtijev = await ctx.Zahtjevi.FindAsync(id);
            if (zahtijev == null)
            {
                return NotFound();
            }
            else
            {
                ctx.Remove(zahtijev);
                await ctx.SaveChangesAsync();
                logger.LogInformation(new EventId(1000), $"Zahtijev s identifikatorom {id} obrisan.");
                return NoContent();
            };
        }

        [HttpGet("{id}", Name = "DohvatiZahtijev")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]

        public async Task<ActionResult<ZahtijevViewModel>> Get(int id)
        {
            var zahtijev = await ctx.Zahtjevi
                            .Where(m => m.IdZah == id)
                            .Select(projection)
                            .FirstOrDefaultAsync();
            if (zahtijev == null)
            {
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"No data for id = {id}");
            }
            else
            {
                return zahtijev;
            }
        }

        [HttpGet(Name = "DohvatiZahtijeve")]
        public async Task<List<ZahtijevViewModel>> GetAll([FromQuery] LoadParams loadParams)
        {
            var query = ctx.Zahtjevi.AsQueryable();

            if (!string.IsNullOrWhiteSpace(loadParams.Filter))
            {
                query = query.Where(m => m.IdVrstaZahNavigation.NazivVrstaZah.Contains(loadParams.Filter));
            }

            if (loadParams.SortColumn != null)
            {
                if (orderSelectors.TryGetValue(loadParams.SortColumn.ToLower(), out var expr))
                {
                    query = loadParams.Descending ? query.OrderByDescending(expr) : query.OrderBy(expr);
                }
            }

            var list = await query.Select(projection)
                                  .Skip(loadParams.StartIndex)
                                  .Take(loadParams.Rows)
                                  .ToListAsync();
            return list;
        }

        [HttpPut("{id}", Name = "AzurirajZahtijev")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, ZahtijevViewModel model)
        {
            if (model.IdZah != id)
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"Different ids {id} vs {model.IdZah}");
            }
            else
            {
                var zahtijev = await ctx.Zahtjevi.FindAsync(id);
                if (zahtijev == null)
                {
                    return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Invalid id = {id}");
                }

                zahtijev.OpisZahtijev = model.OpisZahtijev;
                zahtijev.Prioritet = model.Prioritet;
                zahtijev.IdVrstaZah = ctx.VrstaZahtjeva.FirstOrDefault(d => d.NazivVrstaZah == model.NazivVrsteZahtijeva).IdVrstaZah;
                zahtijev.IdSuradnik = ctx.Osobe.FirstOrDefault(d => d.Ime == model.NazivSuradnika).IdSuradnik;

                await ctx.SaveChangesAsync();
                logger.LogInformation(new EventId(1000), $"Zahtjev s identifikatorom {id} ažuriran.");
                return NoContent();
            }
        }
    }
}
