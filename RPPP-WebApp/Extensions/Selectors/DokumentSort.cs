using RPPP_WebApp.Models;
using System;
using System.Linq;
using System.Text;

namespace RPPP_WebApp.Extensions.Selectors
{
  public static class DokumentSort
  {
    public static IQueryable<Dokument> ApplySort(this IQueryable<Dokument> query, int sort, bool ascending)
    {
      System.Linq.Expressions.Expression<Func<Dokument, object>> orderSelector = null;
      switch (sort)
      {
        case 1:
          orderSelector = d => d.IdDoc;
          break;
        case 2:
          orderSelector = d => d.NazivDokument;
          break;
        case 3:
          orderSelector = d => d.NazivDatoteke;
          break;
        case 4:
          orderSelector = d => d.IdVrstaNavigation.NazivVrstaDoc;
          break;
        case 5:
          orderSelector = d => d.IdProjektNavigation.NazivProjekt;
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
