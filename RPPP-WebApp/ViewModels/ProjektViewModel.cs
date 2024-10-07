using System.Collections.Generic;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels; 

namespace RPPP_WebApp.ViewModels
{
    public class ProjektViewModel
    {
        public string NazivProjekta { get; set; }
        public string Kratica { get; set; }
        public string Cilj { get; set; }
        public int IdProjekta { get; set; }
        public string NazivVrsteProjekta { get; set; } 

        public string NaziviDokumentata { get; set; } 

        public virtual ICollection<DokumentViewModel> Dokumenti { get; set; } = new List<DokumentViewModel>();
        public virtual ICollection<Kartica> Kartice { get; set; } = new List<Kartica>();
        public virtual ICollection<Voditelj> Voditelji { get; set; } = new List<Voditelj>();
    }
}