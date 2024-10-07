using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Models;

/// <summary>
/// razred model zahtijeva
/// </summary>
public partial class Zahtijev
{
    public int IdZah { get; set; }

    public string OpisZahtijev { get; set; }

    public string Prioritet { get; set; }

    public int IdVrstaZah { get; set; }

    public int IdSuradnik { get; set; }

    public virtual Partner IdSuradnikNavigation { get; set; }

    public virtual VrstaZahtjeva IdVrstaZahNavigation { get; set; }

    public virtual ICollection<Zadatak> Zadataks { get; set; } = new List<Zadatak>();
}
