using CelSerEngine.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace CelSerEngine.NativeCore
{
    public partial class ValueAddress : ObservableObject
    {
        [ObservableProperty]
        [AlsoNotifyChangeFor(nameof(ValueString))]
        private dynamic value;
        public dynamic? PrevoiusValue { get; set; }
        public IntPtr BaseAddress { get; set; }
        public int Offset { get; set; }
        public ScanDataType EnumDataType { get; }
        public string ValueString => ((object)Value).ValueToString(EnumDataType);
        public string AddressString => ((long)BaseAddress + Offset).ToString("X");
        public IntPtr Address => BaseAddress + Offset;

        public ValueAddress(ulong baseAddress, int offset, dynamic value, ScanDataType enumDataType)
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
                ScanDataType.Short => sizeof(short),
                ScanDataType.Integer => sizeof(int),
                ScanDataType.Float => sizeof(float),
                ScanDataType.Double => sizeof(double),
                ScanDataType.Long => sizeof(long),
                _ => throw new Exception($"Type: {EnumDataType} is not supported"),
            };
        }

        #region CommunityToolkit bug fix
        // ******************* this fixes the Bug from CommunityToolkit with dynamic datatype, where it asks for the implementation from these generated methods *************/
        partial void OnValueChanging(dynamic value) {}
        partial void OnValueChanged(dynamic value) {}
        #endregion

    }
}