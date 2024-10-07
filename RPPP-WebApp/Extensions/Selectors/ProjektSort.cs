using RPPP_WebApp.Models;
using System;
using System.Linq;

namespace RPPP_WebApp.Extensions.Selectors
{
  public static class ProjektSort
  {
    public static IQueryable<Projekt> ApplySort(this IQueryable<Projekt> query, int sort, bool ascending)
    {
      System.Linq.Expressions.Expression<Func<Projekt, object>> orderSelector = null;
      switch (sort)
      {
        case 1:
          orderSelector = p => p.IdProjekt;
          break;
        case 2:
          orderSelector = p => p.NazivProjekt;
          break;
        case 3:
          orderSelector = p => p.Kratica;
          break;
        case 4:
          orderSelector = p => p.Cilj;
          break;
        case 5:
          orderSelector = p => p.IdVrsteProjektaNavigation.NazivVrsteProjekta;
          break;
        case 6:
          query = ascending ?
                query.OrderBy(p => p.Dokumenti.Min(d => d.IdDoc)) :
                query.OrderByDescending(p => p.Dokumenti.Min(d => d.IdDoc));
          return query;
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
