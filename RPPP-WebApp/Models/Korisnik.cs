using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Models;

public partial class Korisnik
{
    public string IbanKorisnik { get; set; }

    public string NazivKorisnik { get; set; }

    public virtual ICollection<Transakcija> Transakcijas { get; set; } = new List<Transakcija>();
}
