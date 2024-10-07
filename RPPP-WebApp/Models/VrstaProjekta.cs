using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Models;

public partial class VrstaProjekta
{
    public string NazivVrsteProjekta { get; set; }

    public int IdVrsteProjekta { get; set; }

    public virtual ICollection<Projekt> Projekti { get; set; } = new List<Projekt>();
}
