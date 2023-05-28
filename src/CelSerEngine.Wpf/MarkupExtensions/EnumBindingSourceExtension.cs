using System;
using System.Windows.Markup;

namespace CelSerEngine.MarkupExtensions;

/// <summary>
/// https://brianlagunas.com/a-better-way-to-data-bind-enums-in-wpf/
/// <example>
/// Binding Source={local:EnumBindingSource {x:Type local:ScanDataType}}
/// </example>
/// </summary>
[MarkupExtensionReturnType(typeof(Array))]
public class EnumBindingSourceExtension : MarkupExtension
{
    public Type EnumType { get; set; }

    public EnumBindingSourceExtension(Type enumType)
    {
        if (!enumType.IsEnum)
            throw new ArgumentException("Type must be for an Enum.");

        EnumType = enumType;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (EnumType == null)
            throw new InvalidOperationException("The EnumType must be specified.");

        var actualEnumType = Nullable.GetUnderlyingType(EnumType) ?? EnumType;
        var enumValues = Enum.GetValues(actualEnumType);

        if (actualEnumType == EnumType)
            return enumValues;

        var tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
        enumValues.CopyTo(tempArray, 1);
        return tempArray;
    }
}
