namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// Kolekcija osoba i informacije za stranicu
    /// </summary>
    public class OsobeViewModel
    {
        public List<OsobaViewModel> Osobe { get; set; }
        public PagingInfo PagingInfo { get; set; }
    }
}
