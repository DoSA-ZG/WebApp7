using RPPP_WebApp.Models;

namespace RPPP_WebApp.Extensions.Selectors
{
    public static class OsobaSort
    {
        public static IQueryable<Osoba> ApplySort(this IQueryable<Osoba> query, int sort, bool ascending)
        {
            System.Linq.Expressions.Expression<Func<Osoba, object>> orderSelector = null;
            switch (sort)
            {
                case 1:
                    orderSelector = o => o.Ime;
                    break;
                case 2:
                    orderSelector = o => o.Email;
                    break;
                case 3:
                    orderSelector = o => o.Oib;
                    break;
                case 4:
                    orderSelector = o => o.BrMob;
                    break;
                case 5:
                    orderSelector = o => o.IbanOsoba;
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
