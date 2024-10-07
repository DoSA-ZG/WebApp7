using System.Collections.Generic;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;

namespace RPPP_WebApp.ViewModels
{
    public class KarticaViewModel
    {
        public int BrRacuna { get; set; }

        public decimal Stanje { get; set; }

        public int IdProjekt { get; set; }

        public string NazivProjekt { get; set; }

        public string KraticaProjekt { get; set; }

        public virtual Projekt IdProjektNavigation { get; set; }

        public virtual ICollection<Transakcija> Transakcijas { get; set; } = new List<Transakcija>();
    }
}
