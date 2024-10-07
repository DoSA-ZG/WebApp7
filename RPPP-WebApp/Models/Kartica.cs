using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Models;

public partial class Kartica
{
    public int BrRacuna { get; set; }

    public decimal Stanje { get; set; }

    public int IdProjekt { get; set; }

    public string NazivProjekt { get; set; }

    public string KraticaProjekt { get; set; }

    public virtual Projekt IdProjektNavigation { get; set; }

    public virtual ICollection<Transakcija> Transakcijas { get; set; } = new List<Transakcija>();
}
