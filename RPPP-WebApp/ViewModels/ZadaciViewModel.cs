namespace RPPP_WebApp.ViewModels
{
    /// <summary>
    /// razred za prikaz više zadataka
    /// </summary>
    public class ZadaciViewModel
    {
        public List<ZadatakViewModel> Zadaci { get; set; }
        public PagingInfo PagingInfo { get; set; }
    }
}
