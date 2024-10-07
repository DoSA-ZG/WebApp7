using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RPPP_WebApp.Models;

public partial class Projekt
{
    [Required(ErrorMessage = "Potrebno je unijeti naziv projekta")]
    [StringLength(100, ErrorMessage = "Naziv projekta ne smije biti duži od 100 znakova")]
    [CustomUniqueName(ErrorMessage = "Naziv projekta već postoji")]
    public string NazivProjekt { get; set; }

    [Required(ErrorMessage = "Potrebno je unijeti kraticu projekta")]
    [StringLength(100, ErrorMessage = "Kratica projekta ne smije biti duža od 100 znakova")]
    public string Kratica { get; set; }

    [Required(ErrorMessage = "Potrebno je unijeti cilj projekta")]
    [StringLength(100, ErrorMessage = "Cilj projekta ne smije biti duži od 100 znakova")]
    public string Cilj { get; set; }

    [Required(ErrorMessage = "IdProjekt ne smije biti prazan")]
    public int IdProjekt { get; set; }

    [Required(ErrorMessage = "Potrebno je odabrati vrstu projekta")]
    public int IdVrsteProjekta { get; set; }

    public virtual ICollection<Dokument> Dokumenti { get; set; } = new List<Dokument>();

    public virtual VrstaProjekta IdVrsteProjektaNavigation { get; set; }

    public virtual ICollection<Kartica> Karticas { get; set; } = new List<Kartica>();

    public virtual ICollection<Voditelj> Voditeljs { get; set; } = new List<Voditelj>();
}
