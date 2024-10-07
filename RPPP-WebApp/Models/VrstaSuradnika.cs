using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Models;

public partial class VrstaSuradnika
{
    public int IdVrsteSur { get; set; }

    public string NazivVrstaSuradnik { get; set; }

    public virtual ICollection<Suradnik> Suradniks { get; set; } = new List<Suradnik>();
}
