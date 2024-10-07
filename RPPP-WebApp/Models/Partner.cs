using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RPPP_WebApp.Models;

public partial class Partner
{
    public int IdSuradnik { get; set; }

    public virtual Osoba IdSuradnikNavigation { get; set; }

    public virtual ICollection<Zahtijev> Zahtijevs { get; set; } = new List<Zahtijev>();
}
