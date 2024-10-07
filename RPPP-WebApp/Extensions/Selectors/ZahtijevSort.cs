using RPPP_WebApp.Models;
using System;
using System.Linq;

namespace RPPP_WebApp.Extensions.Selectors
{
  public static class ZahtijevSort
  {
    public static IQueryable<Zahtijev> ApplySort(this IQueryable<Zahtijev> query, int sort, bool ascending)
    {
      System.Linq.Expressions.Expression<Func<Zahtijev, object>> orderSelector = null;
      switch (sort)
      {
        case 1:
          orderSelector = p => p.OpisZahtijev;
          break;
        case 2:
          orderSelector = p => p.Prioritet;
          break;
        case 3:
          orderSelector = p => p.IdVrstaZahNavigation.NazivVrstaZah;
          break;
        case 4:
          orderSelector = p => p.IdSuradnikNavigation.IdSuradnik;
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
