using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Models;

public partial class Trajanje
{
    public DateOnly DatumPoc { get; set; }

    public DateOnly? DatumKraj { get; set; }

    public int IdProjekt { get; set; }

    public virtual Projekt IdProjektNavigation { get; set; }
}
