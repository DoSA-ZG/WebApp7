using RPPP_WebApp.Models;

namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// razred za prikaz zadatka
    /// </summary>
    public class ZadatakViewModel
    {
        public int IdZad { get; set; }

        public string Status { get; set; }

        public DateOnly Trajanje { get; set; }

        public String OpisZahtijev { get; set; }

        public String NazivVrstaZad { get; set; }
        
    }
}
