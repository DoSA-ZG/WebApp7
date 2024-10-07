using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Models;

public partial class VrstaDoc
{
    public string NazivVrstaDoc { get; set; }

    public int IdVrsteDoc { get; set; }

    public virtual ICollection<Dokument> Dokuments { get; set; } = new List<Dokument>();
}
