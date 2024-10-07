using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// Kontroler razred za autocomplete
    /// </summary>
    public class AutoCompleteController : Controller
    {
        private readonly Rppp07Context ctx;
        private readonly AppSettings appData;


        public AutoCompleteController(Rppp07Context ctx, IOptionsSnapshot<AppSettings> options)
        {
            this.ctx = ctx;
            appData = options.Value;
        }

        public async Task<IEnumerable<IdLabel>> Zadatak(string term)
        {
            var query = ctx.Zadaci.Select(s => new IdLabel
            {
                Id = s.IdZad,
                Label = s.Status
            }).Where(l => l.Label.Contains(term));

            var list = await query.OrderBy(l => l.Label)
                                  .ThenBy(l => l.Id)
                                  .Take(appData.AutoCompleteCount)
                                  .ToListAsync();
            return list;
        }

		public async Task<IEnumerable<IdLabel>> Partner(string term)
		{
			var query = ctx.Partneri.Select(p => new IdLabel
			{
				Id = p.IdSuradnik,
				Label = p.IdSuradnikNavigation.Ime
			}).Where(l => l.Label.Contains(term));

			var list = await query.OrderBy(l => l.Label)
								  .ThenBy(l => l.Id)
								  .Take(appData.AutoCompleteCount)
								  .ToListAsync();
			return list;
		}

		public async Task<IEnumerable<IdLabel>> VrstaZahtijev(string term)
		{
			var query = ctx.VrstaZahtjeva.Select(p => new IdLabel
			{
				Id = p.IdVrstaZah,
				Label = p.NazivVrstaZah
			}).Where(l => l.Label.Contains(term));

			var list = await query.OrderBy(l => l.Label)
								  .ThenBy(l => l.Id)
								  .Take(appData.AutoCompleteCount)
								  .ToListAsync();
			return list;
		}

        public async Task<IEnumerable<IdLabel>> Prioritet(string term)
        {
            var query = ctx.Zahtjevi.Select(p => new IdLabel
            {
                Id = 0,
                Label = p.Prioritet
            }).Distinct().Where(l => l.Label.Contains(term));

            var list = await query.OrderBy(l => l.Label)
                                  .ThenBy(l => l.Id)
                                  .Take(appData.AutoCompleteCount)
                                  .ToArrayAsync();
            
            return list;
        }

        public async Task<IEnumerable<IdLabel>> VrstaZadatak(string term)
        {
            var query = ctx.VrstaZadatka.Select(p => new IdLabel
            {
                Id = p.IdVrstaZad,
                Label = p.NazivVrstaZad
            }).Where(l => l.Label.Contains(term));

            var list = await query.OrderBy(l => l.Label)
                                  .ThenBy(l => l.Id)
                                  .Take(appData.AutoCompleteCount)
                                  .ToListAsync();
            return list;
        }
    }
}