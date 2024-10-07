using System.Collections.Generic;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels; 

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// Kolekcija partnera i informacije za stranicu
    /// </summary>
    public class PartneriViewModel
    {
        public List<PartnerViewModel> Partneri { get; set; }
        public PagingInfo PagingInfo { get; set; }

    }
}