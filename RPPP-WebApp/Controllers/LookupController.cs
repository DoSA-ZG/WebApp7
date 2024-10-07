using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using RPPP_WebApp.Models.JTable;
using RPPP_WebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace RPPP_WebApp.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class LookupController : ControllerBase
    {
        private readonly Rppp07Context ctx;

        public LookupController(Rppp07Context ctx)
        {
            this.ctx = ctx;
        }

        [HttpGet]
        [HttpPost]
        public async Task<OptionsResult> Zadaci()
        {
            var options = await ctx.Zadaci
                                   .OrderBy(d => d.IdZad)
                                   .Select(d => new TextValue
                                   {
                                       DisplayText = d.Status,
                                       Value = d.IdZad.ToString()
                                   })
                                   .ToListAsync();
            return new OptionsResult(options);
        }

        public async Task<OptionsResult> Zahtijevi()
        {
            var options = await ctx.Zahtjevi
                                   .OrderBy(d => d.IdZah)
                                   .Select(d => new TextValue
                                   {
                                       DisplayText = d.OpisZahtijev,
                                       Value = d.IdZah.ToString()
                                   })
                                   .ToListAsync();
            return new OptionsResult(options);
        }

        public async Task<OptionsResult> VrsteZadataka()
        {
            var options = await ctx.VrstaZadatka
                                   .OrderBy(d => d.NazivVrstaZad)
                                   .Select(d => new TextValue
                                   {
                                       DisplayText = d.NazivVrstaZad,
                                       Value = d.IdVrstaZad.ToString()
                                   })
                                   .ToListAsync();
            return new OptionsResult(options);
        }
    }
}
