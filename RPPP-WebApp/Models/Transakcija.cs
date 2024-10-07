using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Models;

public partial class Transakcija
{
    public decimal Iznos { get; set; }

    public int IdTransakcija { get; set; }

    public string OpisTrans { get; set; }

    public DateOnly Vrijeme { get; set; }

    public int BrRacuna { get; set; }

    public int IdVrstaTrans { get; set; }

    public string IbanKorisnik { get; set; }

    public virtual Kartica BrRacunaNavigation { get; set; }

    public virtual Korisnik IbanKorisnikNavigation { get; set; }

    public virtual VrstaTran IdVrstaTransNavigation { get; set; }
}
