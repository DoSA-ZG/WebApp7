using FluentValidation;
using RPPP_WebApp.Models;

namespace RPPP_WebApp.ModelsValidation
{
    public class OsobaValidator : AbstractValidator<Osoba>
    {
        private readonly Rppp07Context _context;
        public OsobaValidator(Rppp07Context context)
        {
            _context = context;

            RuleFor(o => o.Ime)
                .NotEmpty().WithMessage("Potrebno je unijeti ime osobe");

            RuleFor(o => o.Email)
                .NotEmpty()
                .WithMessage("Potrebno je unijeti e-mail.");

            RuleFor(o => o.Email)
                .Must(e => !string.IsNullOrEmpty(e) && e.Contains('@'))
                .WithMessage("Netočan format e-maila.");

            RuleFor(o => o.Oib)
                .NotEmpty()
                .WithMessage("Potrebno je unijeti OIB osobe");

            RuleFor(o => o.BrMob)
                .NotEmpty().WithMessage("Potrebno je unijeti broj mobitela osobe");

            RuleFor(o => o.IbanOsoba)
                .NotEmpty().WithMessage("Potrebno je unijeti IBAN");
        }
    }
}
