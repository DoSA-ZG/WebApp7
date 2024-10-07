using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RPPP_WebApp.Models;

public partial class Dokument
{
    [Required(ErrorMessage = "Potrebno je unijeti naziv dokumenta")]
    [StringLength(30, ErrorMessage = "Naziv dokumenta ne smije biti duži od 30 znakova")]
    public string NazivDokument { get; set; }

    [Required(ErrorMessage = "Potrebno je unijeti naziv datoteke")]
    [StringLength(30, ErrorMessage = "Naziv datoteke ne smije biti duži od 30 znakova")]
    [CustomUniqueNazivDatoteke(ErrorMessage = "Naziv datoteke već postoji za odabrani projekt")]
    public string NazivDatoteke { get; set; }

    [Required(ErrorMessage = "Potrebno je odabrati projekt")]
    public int IdProjekt { get; set; }

    [Required(ErrorMessage = "Potrebno je odabrati vrstu dokumenta")]
    public int IdVrsta { get; set; }

    public int IdDoc { get; set; }

    public byte[] Dokument1 { get; set; }

    public virtual Projekt IdProjektNavigation { get; set; }

    public virtual VrstaDoc IdVrstaNavigation { get; set; }

    [NotMapped]
    public IFormFile UploadedFile { get; set; }
}
