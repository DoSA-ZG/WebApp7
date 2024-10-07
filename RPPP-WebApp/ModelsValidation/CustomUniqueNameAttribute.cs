using System.ComponentModel.DataAnnotations;
using System.Linq;
using RPPP_WebApp.Models;

public class CustomUniqueNameAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value != null)
        {
            string nazivProjekt = value.ToString();
            var dbContext = (Rppp07Context)validationContext.GetService(typeof(Rppp07Context));

            var currentProjekt = (Projekt)validationContext.ObjectInstance;

            if (dbContext.Projekti.Any(p => p.NazivProjekt == nazivProjekt && p.IdProjekt != currentProjekt.IdProjekt))
            {
                return new ValidationResult(ErrorMessage);
            }
        }

        return ValidationResult.Success;
    }
}