using FluentValidation;
using RPPP_WebApp.Models;

namespace RPPP_WebApp.ModelsValidation
{
    public class TransackijaValidator : AbstractValidator<Transakcija>
    {
        private readonly Rppp07Context _context;
        public TransackijaValidator(Rppp07Context context)
        {
            _context = context;

            RuleFor(z => z.Iznos)
                .NotEmpty().WithMessage("Potrebno je unijeti iznos");

            RuleFor(z => z.Vrijeme)
                .NotEmpty()
                .WithMessage("Potrebno je unijeti vrijeme transkacije");

            RuleFor(z => z.BrRacuna)
                .NotEmpty().WithMessage("Potrebno je odabrati broj racuna");

            RuleFor(z => z.IdVrstaTrans)
                .NotEmpty().WithMessage("Potrebno je odabrati vrstu trans");
        }
    }
}
