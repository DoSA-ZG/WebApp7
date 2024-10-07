using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Models;

public partial class Voditelj
{
    public int IdSuradnik { get; set; }

    public int IdProjekt { get; set; }

    public virtual Projekt IdProjektNavigation { get; set; }

    public virtual Osoba IdSuradnikNavigation { get; set; }
}
