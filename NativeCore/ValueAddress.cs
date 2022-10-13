using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace CelSerEngine.NativeCore
{
    public partial class ValueAddress : ObservableObject
    {
        [ObservableProperty]
        [AlsoNotifyChangeFor(nameof(ValueString))]
        private dynamic value;
        public object? PrevoiusValue { get; set; }
        public IntPtr BaseAddress { get; set; }
        public int Offset { get; set; }
        public EnumDataType EnumDataType { get; }

        public string ValueString => ((object)Value).ValueToString(EnumDataType);
        public string AddressString => ((long)BaseAddress + Offset).ToString("X");
        public IntPtr Address => BaseAddress + Offset;

        public ValueAddress(ulong baseAddress, int offset, dynamic value, EnumDataType enumDataType)
        {
            BaseAddress = (IntPtr)baseAddress;
            Offset = offset;
            this.value = value;
            EnumDataType = enumDataType;
        }

        public ValueAddress(ValueAddress valueAddress)
        {
            BaseAddress = valueAddress.BaseAddress;
            Offset = valueAddress.Offset;
            value = valueAddress.Value;
            EnumDataType = valueAddress.EnumDataType;
        }

        public int GetDataTypeSize()
        {
            return EnumDataType switch
            {
                EnumDataType.Short => sizeof(short),
                EnumDataType.Integer => sizeof(int),
                EnumDataType.Float => sizeof(float),
                EnumDataType.Double => sizeof(double),
                EnumDataType.Long => sizeof(long),
                _ => throw new Exception($"Type: {EnumDataType} is not supported"),
            };
        }
    }
}