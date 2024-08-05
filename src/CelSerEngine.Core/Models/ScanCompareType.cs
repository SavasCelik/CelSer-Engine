using System.ComponentModel.DataAnnotations;

namespace CelSerEngine.Core.Models;

public enum ScanCompareType
{
    [Display(Name = "Exact Value")]
    ExactValue,
    [Display(Name = "Bigger than...")]
    BiggerThan,
    [Display(Name = "Smaller than...")]
    SmallerThan,
    [Display(Name = "Value between...")]
    ValueBetween,
    [Display(Name = "Unknown initial value")]
    UnknownInitialValue,
    [Display(Name = "Increased value")]
    IncreasedValue,
    [Display(Name = "Increased value by...")]
    IncreasedValueBy,
    [Display(Name = "Decreased value")]
    DecreasedValue,
    [Display(Name = "Decreased value by...")]
    DecreasedValueBy,
}
