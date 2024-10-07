using RPPP_WebApp.Models;
using System;
using System.Linq;

namespace RPPP_WebApp.Extensions.Selectors
{
  public static class PartnerSort
  {
    public static IQueryable<Partner> ApplySort(this IQueryable<Partner> query, int sort, bool ascending)
    {
      System.Linq.Expressions.Expression<Func<Partner, object>> orderSelector = null;
      switch (sort)
      {
        case 1:
          orderSelector = p => p.IdSuradnik;
          break;
        case 2:
          orderSelector = p => p.IdSuradnikNavigation.Ime;
          break;
        case 3:
          orderSelector = p => p.Zahtijevs.Count;
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
