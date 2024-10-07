using RPPP_WebApp.Models;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class ZadatakSort
    {
        public static IQueryable<Zadatak> ApplySort(this IQueryable<Zadatak> query, int sort, bool ascending)
        {
            System.Linq.Expressions.Expression<Func<Zadatak, object>> orderSelector = null;
            switch (sort)
            {
                case 1:
                    orderSelector = z => z.Status;
                    break;
                case 2:
                    orderSelector = z => z.Trajanje;
                    break;
                case 3:
                    orderSelector = p => p.IdZahNavigation.Prioritet;
                    break;
                case 4:
                    orderSelector = p => p.IdVrstaZadNavigation.NazivVrstaZad;
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
