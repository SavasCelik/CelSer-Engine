using System.ComponentModel.DataAnnotations;

namespace CelSerEngine.WpfBlazor.Models;

/// <summary>
/// Provides conditional validation based on related property value.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class RequiredIfAttribute(string otherProperty, object targetValue) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var otherPropertyValue = validationContext.ObjectType
                                                  .GetProperty(otherProperty)?
                                                  .GetValue(validationContext.ObjectInstance);

        if (otherPropertyValue is null || !otherPropertyValue.Equals(targetValue))
        {
            return ValidationResult.Success;
        }

        if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return new ValidationResult(ErrorMessage ?? "This field is required.", [validationContext.DisplayName]);
        }

        return ValidationResult.Success;
    }
}
