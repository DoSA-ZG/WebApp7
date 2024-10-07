using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Models;

/// <summary>
/// razred model vrste zadatka
/// </summary>
public partial class VrstaZadatka
{
    public int IdVrstaZad { get; set; }

    public string NazivVrstaZad { get; set; }

    public virtual ICollection<Zadatak> Zadataks { get; set; } = new List<Zadatak>();
}
