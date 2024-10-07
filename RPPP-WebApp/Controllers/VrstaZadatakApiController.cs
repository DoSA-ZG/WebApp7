using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RPPP_WebApp.Extensions;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;
using System.Diagnostics;
using System.Linq.Expressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace RPPP_WebApp.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [TypeFilter(typeof(ProblemDetailsForSqlException))]
    public class VrstaZadatakApiController : ControllerBase, ICustomController<int, VrstaZadatakViewModel>
    {
        private readonly Rppp07Context ctx;
        private static Dictionary<string, Expression<Func<VrstaZadatka, object>>> orderSelectors = new Dictionary<string, Expression<Func<VrstaZadatka, object>>>
        {
            [nameof(VrstaZadatakViewModel.IdVrstaZad).ToLower()] = r => r.IdVrstaZad,
            [nameof(VrstaZadatakViewModel.NazivVrstaZad).ToLower()] = r => r.NazivVrstaZad
        };

        public VrstaZadatakApiController(Rppp07Context ctx)
        {
            this.ctx = ctx;
        }

        [HttpGet("count", Name = "Broj vrsta zadataka")]
        public async Task<int> Count([FromQuery] string filter)
        {
            var query = ctx.VrstaZadatka.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(r => r.NazivVrstaZad.Contains(filter));
            }
            int count = await query.CountAsync();
            return count;
        }

        [HttpGet(Name = "Dohvati vrste zadataka")]
        public async Task<List<VrstaZadatakViewModel>> GetAll([FromQuery] LoadParams loadParams)
        {
            var query = ctx.VrstaZadatka.AsQueryable();

            if (!string.IsNullOrWhiteSpace(loadParams.Filter))
            {
                query = query.Where(r => r.NazivVrstaZad.Contains(loadParams.Filter));
            }

            if (loadParams.SortColumn != null)
            {
                if (orderSelectors.TryGetValue(loadParams.SortColumn.ToLower(), out var expr))
                {
                    query = loadParams.Descending ? query.OrderByDescending(expr) : query.OrderBy(expr);
                }
            }

            var list = await query.Select(r => new VrstaZadatakViewModel
                                                    {
                                                        IdVrstaZad = r.IdVrstaZad,
                                                        NazivVrstaZad = r.NazivVrstaZad
                                                    })
                                                    .Skip(loadParams.StartIndex)
                                                    .Take(loadParams.Rows)
                                                    .ToListAsync();
            foreach (var l in list)
            {
                Debug.WriteLine(l.NazivVrstaZad);
            }

            return list;
        }

        [HttpGet("{id}", Name = "Dohvati zadatak")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<VrstaZadatakViewModel>> Get(int id)
        {
            var vrstaZadatka = await ctx.VrstaZadatka
                                   .Where(r => r.IdVrstaZad == id)
                                   .Select(r => new VrstaZadatakViewModel
                                   {
                                       IdVrstaZad = r.IdVrstaZad,
                                       NazivVrstaZad = r.NazivVrstaZad
                                   })
                                   .FirstOrDefaultAsync();
            if (vrstaZadatka == null)
            {
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"No data for id = {id}");
            }
            else
            {
                return vrstaZadatka;
            }
        }


        [HttpPost(Name = "Dodaj vrstu zadatka")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(VrstaZadatakViewModel model)
        {
            VrstaZadatka vrstaZadatka = new VrstaZadatka
            {
                NazivVrstaZad = model.NazivVrstaZad
            };
            ctx.Add(vrstaZadatka);
            await ctx.SaveChangesAsync();

            var addedItem = await Get(vrstaZadatka.IdVrstaZad);

            return CreatedAtAction(nameof(Get), new { id = vrstaZadatka.IdVrstaZad }, addedItem.Value);
        }

        [HttpPut("{id}", Name = "Azuriraj vrstu zadatka")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, VrstaZadatakViewModel model)
        {
            if (model.IdVrstaZad != id) //ModelState.IsValid i model != null provjera se automatski zbog [ApiController]
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"Different ids - {id} vs {model.IdVrstaZad}");
            }
            else
            {
                var vrstaZadatka = await ctx.VrstaZadatka.FindAsync(id);
                if (vrstaZadatka == null)
                {
                    return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Invalid id = {id}");
                }

                vrstaZadatka.NazivVrstaZad = model.NazivVrstaZad;

                await ctx.SaveChangesAsync();
                return NoContent();
            }
        }

        [HttpDelete("{id}", Name = "Obrisi vrstu zadatka")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var vrstaZadatka = await ctx.VrstaZadatka.FindAsync(id);
            if (vrstaZadatka == null)
            {
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Invalid id = {id}");
            }
            else
            {
                ctx.Remove(vrstaZadatka);
                await ctx.SaveChangesAsync();
                return NoContent();
            };
        }

    }
}
