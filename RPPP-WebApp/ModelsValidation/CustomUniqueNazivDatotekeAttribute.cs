using System.ComponentModel.DataAnnotations;
using System.Linq;
using RPPP_WebApp.Models;

public class CustomUniqueNazivDatotekeAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value != null)
        {
            string nazivDatoteke = value.ToString();
            var dbContext = (Rppp07Context)validationContext.GetService(typeof(Rppp07Context));

            var currentDokument = (Dokument)validationContext.ObjectInstance;

            if (dbContext.Dokumenti.Any(d => d.IdProjekt == currentDokument.IdProjekt && d.NazivDatoteke == nazivDatoteke && d.IdDoc != currentDokument.IdDoc))
            {
                return new ValidationResult(ErrorMessage);
            }
        }

        return ValidationResult.Success;
    }
}