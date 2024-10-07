using System.Collections.Generic;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels; 

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// razred za prikaz više zahtijeva
    /// </summary>
    public class ZahtijeviViewModel
    {
        public List<ZahtijevViewModel> Zahtijevi { get; set; }
        public PagingInfo PagingInfo { get; set; }

    }
}