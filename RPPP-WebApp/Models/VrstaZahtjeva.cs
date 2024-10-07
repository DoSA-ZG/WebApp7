using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Models;

/// <summary>
/// razred model vrste zahtijeva
/// </summary>
public partial class VrstaZahtjeva
{
    public int IdVrstaZah { get; set; }

    public string NazivVrstaZah { get; set; }

    public virtual ICollection<Zahtijev> Zahtijevs { get; set; } = new List<Zahtijev>();
}
