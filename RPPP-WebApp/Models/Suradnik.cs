using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Models;

public partial class Suradnik
{
    public int IdSuradnik { get; set; }

    public int IdVrsteSur { get; set; }

    public virtual Osoba IdSuradnikNavigation { get; set; }

    public virtual VrstaSuradnika IdVrsteSurNavigation { get; set; }
}
