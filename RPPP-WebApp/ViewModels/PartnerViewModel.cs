using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels; 

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// Model partnera s podatcima
    /// </summary>
    public class PartnerViewModel
    {
        [Required(ErrorMessage = "Polje ne smije biti prazno!")]
        public int IdSuradnik { get; set; }
        public Osoba Partner { get; set; }
        public int BrZahtijeva { get; set; }
        public virtual ICollection<ZahtijevViewModel> Zahtijevi { get; set; } = new List<ZahtijevViewModel>();


    }
}