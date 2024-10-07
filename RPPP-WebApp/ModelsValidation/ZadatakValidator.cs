using FluentValidation;
using RPPP_WebApp.Models;

namespace RPPP_WebApp.ModelsValidation
{
    public class ZadatakValidator : AbstractValidator<Zadatak>
    {
        private readonly Rppp07Context _context;
        public ZadatakValidator(Rppp07Context context)
        {
            _context = context;

            RuleFor(z => z.Status)
                .NotEmpty().WithMessage("Potrebno je unijeti status zadatka");

            RuleFor(z => z.Trajanje)
                .NotEmpty().WithMessage("Potrebno je unijeti trajanje zadatka");

            RuleFor(z => z.IdZah)
                .NotEmpty().WithMessage("Potrebno je odabrati zahtijev");

            RuleFor(z => z.IdVrstaZad)
                .NotEmpty().WithMessage("Potrebno je odabrati vrstu zadatka");
        }
    }
}
