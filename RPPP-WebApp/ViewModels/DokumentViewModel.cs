using System.Collections.Generic;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels; 

namespace RPPP_WebApp.ViewModels
{
    public class DokumentViewModel
    {
        public string NazivDokument { get; set; }
        public string NazivDatoteke { get; set; }
        public int IdProjekt { get; set; }
        public int IdVrsta { get; set; }
        public int IdDoc { get; set; }
        public byte[] Dokument1 { get; set; }
        public string TextContent { get; set; }
        public string FilePath { get; set; }
        public string NazivProjekt{ get; set; }
        public string NazivVrsteDoc { get; set; }
        public virtual Projekt IdProjektNavigation { get; set; }
        public virtual VrstaDoc IdVrstaNavigation { get; set; }
    }
}
