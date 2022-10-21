using System.ComponentModel.DataAnnotations;

namespace CelSerEngine.Models
{
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
        UnknownInitialValue
    }
}
