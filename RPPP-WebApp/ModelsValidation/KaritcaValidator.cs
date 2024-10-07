using System.ComponentModel.DataAnnotations;
using FluentValidation;
using RPPP_WebApp.Models;

namespace RPPP_WebApp.ModelsValidation
{
    public class KarticaValidator : AbstractValidator<Kartica>
    {
        private readonly Rppp07Context _context;
        public KarticaValidator(Rppp07Context context)
        {
            _context = context;

            RuleFor(k => k.BrRacuna)
                .NotEmpty()
                .WithMessage("Potrebno je unijeti broja racuna");

            RuleFor(k => k.Stanje)
                .NotEmpty()
                .WithMessage("Potrebno je unijeti stanje.");

            RuleFor(k => k.Stanje)
                .GreaterThan(0)
                .WithMessage("Stanje mora biti pozitivno");

        }
    }
}