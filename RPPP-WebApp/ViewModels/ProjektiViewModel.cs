using System.Collections.Generic;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels; 

namespace RPPP_WebApp.ViewModels
{
    public class ProjektiViewModel
    {
        public List<ProjektViewModel> Projekti { get; set; }
        public PagingInfo PagingInfo { get; set; }

    }
}