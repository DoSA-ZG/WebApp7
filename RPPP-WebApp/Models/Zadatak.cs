using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Models;

/// <summary>
/// razred model zadatka
/// </summary>
public partial class Zadatak
{
    public int IdZad { get; set; }

    public string Status { get; set; }

    public DateOnly Trajanje { get; set; }

    public int IdZah { get; set; }

    public int IdVrstaZad { get; set; }

    public virtual VrstaZadatka IdVrstaZadNavigation { get; set; }

    public virtual Zahtijev IdZahNavigation { get; set; }
}
