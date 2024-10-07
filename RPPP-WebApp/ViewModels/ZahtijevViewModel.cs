using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels; 

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// razred za prikaz jednog zahtijeva
    /// </summary>
    public class ZahtijevViewModel
    {
        public int IdZah { get; set; }

        [Required(ErrorMessage = "Opis zahtijeva ne smije biti prazan!")]
        public string OpisZahtijev { get; set; }

        [Required(ErrorMessage = "Prioritet mora biti postavljen!")]
        public string Prioritet { get; set; }

        [Required(ErrorMessage = "Naziv vrste zahtjeva ne smije biti prazan!")]
        public string NazivVrsteZahtijeva { get; set; }

        public string NazivSuradnika { get; set; }

        public virtual ICollection<ZadatakViewModel> Zadataks { get; set; } = new List<ZadatakViewModel>();
    }
}