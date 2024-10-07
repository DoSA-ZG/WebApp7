using FluentValidation;
using RPPP_WebApp.Models;
using System.Linq;

namespace RPPP_WebApp.ModelsValidation
{
    public class PartnerValidator : AbstractValidator<Partner>
    {
        private readonly Rppp07Context _context;
        public PartnerValidator(Rppp07Context context)
        {
            _context = context;

            RuleFor(p => p.IdSuradnik)
                .NotEmpty().WithMessage("Polje ne smije biti prazno.")
                .Must(a => {
                    return !_context.Partneri.Any(p => p.IdSuradnik == a);
                }).WithMessage("Osoba je već partner.")
                .Must(a => a != 0).WithMessage("Neispravan ID.");
        }
    }
}