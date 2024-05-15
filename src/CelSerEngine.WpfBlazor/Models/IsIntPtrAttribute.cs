using System.ComponentModel.DataAnnotations;

namespace CelSerEngine.WpfBlazor.Models;

[AttributeUsage(AttributeTargets.Property)]
public class IsIntPtrAttribute : ValidationAttribute
{
    public string? MinValuePropertyName { get; set; }
    public string? MaxValuePropertyName { get; set; }

    public IsIntPtrAttribute() : base($"Please provide a value from {IntPtr.Zero} to {IntPtr.MaxValue:X}")
    {
    }

    //protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    //{
    //    if (value is string hexString
    //        && IntPtr.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out var result)
    //        && IntPtr.Zero <= result && IntPtr.MaxValue >= result)
    //    {
    //        var boundedMinValue = GetPropertyValue(MinValuePropertyName, validationContext.ObjectInstance);
    //        var boundedMaxValue = GetPropertyValue(MaxValuePropertyName, validationContext.ObjectInstance);

    //        if (boundedMinValue != null && boundedMinValue > result)
    //        {
    //            return new ValidationResult($"The value cannot be smaller than {MinValuePropertyName}");
    //        }

    //        if (boundedMaxValue != null && boundedMaxValue < result)
    //        {
    //            return new ValidationResult($"The value cannot be bigger than {MaxValuePropertyName}");
    //        }

    //        return ValidationResult.Success;
    //    }

    //    return new ValidationResult(ErrorMessage);
    //}

    public override bool IsValid(object? value)
    {
        return value is string hexString
            && IntPtr.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out var result)
            && IntPtr.Zero <= result && IntPtr.MaxValue >= result;
    }

    private static IntPtr? GetPropertyValue(string? proppertyName, object instance)
    {
        if (proppertyName != null)
        {
            var instanceType = instance.GetType();
            var property = instanceType.GetProperty(proppertyName)
                ?? throw new ValidationException($"Property with name {proppertyName} was not found in {instanceType}");
            var propertyValue = property.GetValue(instance) as string;
            
            if (IntPtr.TryParse(propertyValue, System.Globalization.NumberStyles.HexNumber, null, out var result))
                return result;
        }

        return null;
    }
}
