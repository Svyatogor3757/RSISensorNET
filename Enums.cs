namespace KUKA.RSI.Sensors {
    public enum ShiftedValueMode {
        None,
        Constant,
        Table,
        TableBinareBool,
        User
    }
    public enum SensorMode {
        /// <summary>
        /// 12 ms
        /// </summary>
        IPO = 16,
        /// <summary>
        /// 4 ms
        /// </summary>
        IPO_FAST = 8
    }
}

