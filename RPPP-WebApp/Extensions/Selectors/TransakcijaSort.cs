using RPPP_WebApp.Models;
using System;
using System.Linq;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class TransakcijaSort
    {
        public static IQueryable<Transakcija> ApplySort(this IQueryable<Transakcija> query, int sort, bool ascending)
        {
            System.Linq.Expressions.Expression<Func<Transakcija, object>> orderSelector = null;
            switch (sort)
            {
                case 1:
                    orderSelector = p => p.BrRacuna;
                    break;
                case 2:
                    orderSelector = p => p.OpisTrans;
                    break;
                case 3:
                    orderSelector = p => p.Iznos;
                    break;
                case 4:
                    orderSelector = p => p.IdVrstaTrans;
                    break;
                case 5:
                    orderSelector = p => p.IdVrstaTransNavigation.NazivVrstaTrans;
                    break;
                case 6:
                    orderSelector = p => p.IbanKorisnikNavigation.IbanKorisnik;
                    break;
            }
            if (orderSelector != null)
            {
                query = ascending ?
                       query.OrderBy(orderSelector) :
                       query.OrderByDescending(orderSelector);
            }

            return query;
        }
    }
}