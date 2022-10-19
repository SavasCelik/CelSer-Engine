using System.ComponentModel.DataAnnotations;

namespace CelSerEngine.Models
{
    public enum ScanDataType
    {
        [Display(Name = "Short (2 Bytes)")]
        Short,
        [Display(Name = "Integer (4 Bytes)")]
        Integer,
        [Display(Name = "Long (8 Bytes)")]
        Long,
        [Display(Name = "Float")]
        Float,
        [Display(Name = "Double")]
        Double
    }
}