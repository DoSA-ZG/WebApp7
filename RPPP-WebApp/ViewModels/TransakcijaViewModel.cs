using RPPP_WebApp.Models;

namespace RPPP_WebApp.ViewModels
{
    public class TransakcijaViewModel
    {
        public decimal Iznos { get; set; }

        public int IdTransakcija { get; set; }

        public string OpisTrans { get; set; }

        public DateOnly Vrijeme { get; set; }

        public int BrRacuna { get; set; }

        public int IdVrstaTrans { get; set; }

        public string IbanKorisnik { get; set; }

        public string NazivKorisnik { get; set; }

        public string NazivVrstaTransakcija { get; set; }

        public virtual Kartica BrRacunaNavigation { get; set; }

        public virtual Korisnik IbanKorisnikNavigation { get; set; }

        public virtual VrstaTran IdVrstaTransNavigation { get; set; }
    }
}
