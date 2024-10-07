using System.ComponentModel.DataAnnotations;

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// Model osobe s podatcima
    /// </summary>
    public class OsobaViewModel
    {
        public string Ime { get; set; }

        public string Email { get; set; }

        public string Oib { get; set; }

        public string BrMob { get; set; }

        public string IbanOsoba { get; set; }

        public int IdSuradnik { get; set; }
    }
}
