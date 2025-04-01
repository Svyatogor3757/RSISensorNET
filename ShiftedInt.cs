namespace KUKA.RSI.Sensors {

    public class ShiftedInt : IShifted<int> {
        public int ConstantOffset { get; set; } = 1;
        public int TableBinareTrue { get; set; } = 1;
        public int TableBinareFalse { get; set; } = -1;
        public ShiftedValueMode Mode { get; set; } = ShiftedValueMode.None;
        public Dictionary<string, int> Table { get; set; } = new();

        public event EventShifted? UserShiftedValueEvent;

        internal ShiftedInt() {

        }
        
        public string ShiftedValue(string key, string value) {
            string result = value;
            switch (Mode) {
                case ShiftedValueMode.None:
                default:
                break;
                case ShiftedValueMode.Constant:
                if (int.TryParse(value, out int res))
                    result = (res + ConstantOffset).ToString();
                break;
                case ShiftedValueMode.Table:
                if (Table.ContainsKey(key) && int.TryParse(value, out int res2))
                    result = (Table[key] + res2).ToString();
                break;
                case ShiftedValueMode.User:
                string? res3 = UserShiftedValueEvent?.Invoke(key, value);
                if (res3 != null)
                    result = res3;
                break;
                case ShiftedValueMode.TableBinareBool:
                if (Table.ContainsKey(key) && Table[key] > 0 && int.TryParse(value, out int res4))
                    result = (res4 > 0 ? TableBinareTrue : TableBinareFalse).ToString();
                break;
            }
            return result;

        }
    }
}

