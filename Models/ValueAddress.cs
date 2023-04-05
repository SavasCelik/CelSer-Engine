using CelSerEngine.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace CelSerEngine.Models
{
    public partial class ValueAddress : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ValueString))]
        private dynamic value;
        public dynamic? PrevoiusValue { get; set; }
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AddressDisplayString))]
        private IntPtr baseAddress;
        public int Offset { get; set; }
        public ScanDataType ScanDataType { get; private set; }
        public string ValueString => ((object)Value).ValueToString(ScanDataType);
        public string AddressDisplayString { get; set; }
        public IntPtr Address => BaseAddress + Offset;

        public ValueAddress(ulong baseAddress, int offset, dynamic value, ScanDataType scanDataType)
        {
            BaseAddress = (IntPtr)baseAddress;
            Offset = offset;
            this.value = value;
            ScanDataType = scanDataType;
            AddressDisplayString = Address.ToString("X");
        }

        public ValueAddress(ValueAddress valueAddress)
        {
            BaseAddress = valueAddress.BaseAddress;
            Offset = valueAddress.Offset;
            value = valueAddress.Value;
            ScanDataType = valueAddress.ScanDataType;
            AddressDisplayString = Address.ToString("X");
        }

        #region CommunityToolkit bug fix
        // ******************* this fixes the Bug from CommunityToolkit with dynamic datatype, where it asks for the implementation from these generated methods *************/
        partial void OnValueChanging(dynamic value) {}
        partial void OnValueChanged(dynamic value) {}
        #endregion

    }
}