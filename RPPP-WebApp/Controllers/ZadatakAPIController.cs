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
    [Route("[controller]")]
    [ApiController]
    [TypeFilter(typeof(ProblemDetailsForSqlException))]
    public class ZadatakAPIController : ControllerBase, ICustomController<int, ZadatakViewModel>
    {

        private readonly Rppp07Context ctx;
        private readonly ILogger<ZadatakAPIController> logger;
        private static Dictionary<string, Expression<Func<Zadatak, object>>> orderSelectors = new()
        {
            [nameof(ZadatakViewModel.IdZad).ToLower()] = m => m.IdZad,
            [nameof(ZadatakViewModel.Status).ToLower()] = m => m.Status,
            [nameof(ZadatakViewModel.Trajanje).ToLower()] = m => m.Trajanje,
            [nameof(ZadatakViewModel.OpisZahtijev).ToLower()] = m => m.IdZahNavigation.OpisZahtijev,
            [nameof(ZadatakViewModel.NazivVrstaZad).ToLower()] = m => m.IdVrstaZadNavigation.NazivVrstaZad
        };

        private static Expression<Func<Zadatak, ZadatakViewModel>> projection = m => new ZadatakViewModel
        {
            IdZad = m.IdZad,
            Status = m.Status,
            Trajanje = m.Trajanje,
            OpisZahtijev = m.IdZahNavigation.OpisZahtijev,
            NazivVrstaZad = m.IdVrstaZadNavigation.NazivVrstaZad
        };

        public ZadatakAPIController(Rppp07Context ctx, ILogger<ZadatakAPIController> logger)
        {
            this.ctx = ctx;
            this.logger = logger;
        }

        [HttpGet("count", Name = "BrojZadataka")]
        public async Task<int> Count([FromQuery] string filter)
        {
            var query = ctx.Zadaci.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(m => m.Status.Contains(filter));
            }
            int count = await query.CountAsync();
            return count;
        }

        [HttpPost(Name = "DodajZadatak")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(ZadatakViewModel model)
        {
            var maxID = ctx.Zadaci.Max(a => a.IdZad);
            Zadatak zadatak = new Zadatak
            {
                IdZad = maxID + 1,
                Status = model.Status,
                Trajanje = model.Trajanje,
                IdZah = ctx.Zahtjevi.FirstOrDefault(d => d.OpisZahtijev == model.OpisZahtijev).IdZah,
                IdVrstaZad = ctx.VrstaZadatka.FirstOrDefault(d => d.NazivVrstaZad == model.NazivVrstaZad).IdVrstaZad
            };
            ctx.Add(zadatak);
            await ctx.SaveChangesAsync();
            logger.LogInformation(new EventId(1000), $"Zadatak s identifikatorom {maxID + 1} dodan.");

            var addedItem = await Get(zadatak.IdZad);

            return CreatedAtAction(nameof(Get), new { id = zadatak.IdZad }, addedItem.Value);
        }


        [HttpDelete("{id}", Name = "ObrisiZadatak")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var zadatak = await ctx.Zadaci.FindAsync(id);
            if (zadatak == null)
            {
                return NotFound();
            }
            else
            {
                ctx.Remove(zadatak);
                await ctx.SaveChangesAsync();
                logger.LogInformation(new EventId(1000), $"Zadatak s identifikatorom {id} obrisan.");
                return NoContent();
            };
        }

        [HttpGet("{id}", Name = "DohvatiZadatak")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]

        public async Task<ActionResult<ZadatakViewModel>> Get(int id)  //mozda ne valja ???
        {
            var zadatak = await ctx.Zadaci
                            .Where(m => m.IdZad == id)
                            .Select(projection)
                            .FirstOrDefaultAsync();
            if (zadatak == null)
            {
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"No data for id = {id}");
            }
            else
            {
                return zadatak;
            }
        }

        [HttpGet(Name = "DohvatiZadatke")]
        public async Task<List<ZadatakViewModel>> GetAll([FromQuery] LoadParams loadParams)
        {
            var query = ctx.Zadaci.AsQueryable();

            if (!string.IsNullOrWhiteSpace(loadParams.Filter))
            {
                query = query.Where(m => m.Status.Contains(loadParams.Filter));
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

        [HttpPut("{id}", Name = "AzurirajZadatak")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, ZadatakViewModel model)
        {
            if (model.IdZad != id)
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"Different ids {id} vs {model.IdZad}");
            }
            else
            {
                var zadatak = await ctx.Zadaci.FindAsync(id);
                if (zadatak == null)
                {
                    return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Invalid id = {id}");
                }

                zadatak.Status = model.Status;
                zadatak.Trajanje = model.Trajanje;
                zadatak.IdZah = ctx.Zahtjevi.FirstOrDefault(d => d.OpisZahtijev == model.OpisZahtijev).IdZah;
                zadatak.IdVrstaZad = ctx.VrstaZadatka.FirstOrDefault(d => d.NazivVrstaZad == model.NazivVrstaZad).IdVrstaZad;

                await ctx.SaveChangesAsync();
                logger.LogInformation(new EventId(1000), $"Zadatak s identifikatorom {id} ažuriran.");
                return NoContent();
            }
        }
    }
}
