using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RPPP_WebApp.Models;

public partial class Osoba
{
    [Required(ErrorMessage = "Polje ne smije biti prazno!")]
    public string Ime { get; set; }

    public string Email { get; set; }

    public string Oib { get; set; }

    public string BrMob { get; set; }

    public string IbanOsoba { get; set; }

    public int IdSuradnik { get; set; }

    public virtual Koordinator Koordinator { get; set; }

    public virtual Partner Partner { get; set; }

    public virtual Suradnik Suradnik { get; set; }

    public virtual Voditelj Voditelj { get; set; }
}
