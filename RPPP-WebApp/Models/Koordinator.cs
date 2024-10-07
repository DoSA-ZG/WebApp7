using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Models;

public partial class Koordinator
{
    public int IdSuradnik { get; set; }

    public virtual Osoba IdSuradnikNavigation { get; set; }
}
