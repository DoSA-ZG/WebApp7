using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Models;

public partial class VrstaTran
{
    public int IdVrstaTrans { get; set; }

    public string NazivVrstaTrans { get; set; }

    public virtual ICollection<Transakcija> Transakcijas { get; set; } = new List<Transakcija>();
}
