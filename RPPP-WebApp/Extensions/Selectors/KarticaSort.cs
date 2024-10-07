using RPPP_WebApp.Models;
using System;
using System.Linq;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class KarticaSort
    {
        public static IQueryable<Kartica> ApplySort(this IQueryable<Kartica> query, int sort, bool ascending)
        {
            System.Linq.Expressions.Expression<Func<Kartica, object>> orderSelector = null;
            switch (sort)
            {
                case 1:
                    orderSelector = p => p.BrRacuna;
                    break;
                case 2:
                    orderSelector = p => p.Stanje;
                    break;
                case 3:
                    orderSelector = p => p.IdProjekt;
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