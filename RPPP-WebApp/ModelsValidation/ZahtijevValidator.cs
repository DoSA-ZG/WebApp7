using FluentValidation;
using RPPP_WebApp.Models;
using System.Linq;

namespace RPPP_WebApp.ModelsValidation
{
    public class ZahtijevValidator : AbstractValidator<Zahtijev>
    {
        private readonly Rppp07Context _context;
        public ZahtijevValidator(Rppp07Context context)
        {
            _context = context;

            RuleFor(p => p.OpisZahtijev)
                .NotEmpty().WithMessage("Potrebno je unijeti opis zahtijeva");

            RuleFor(p => p.Prioritet)
                .NotEmpty().WithMessage("Potrebno je unijeti prioritet")
                .MaximumLength(50).WithMessage("Prioritet ne smije biti duža od 50 znakova");

            RuleFor(p => p.IdVrstaZah)
                .NotEmpty().WithMessage("Potrebno je odabrati vrstu zahtijeva");

            RuleFor(p => p.IdSuradnik)
                .NotEmpty().WithMessage("Potrebno je odabrati suradnika");
        }
    }
}