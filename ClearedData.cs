namespace KUKA.RSI.Sensors {
    public class ClearedData {
        internal ClearedData() {
            Enabled = true;
            ClearedDataLimit = 1000;
            ClearedDataSave = 100;
        }

        /// <summary>
        /// True, если необходимо периодически очищать значения
        /// </summary>
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// Предел записанных данных с робота
        /// </summary>
        public int ClearedDataLimit { get; set; } = 1000;
        /// <summary>
        /// Количество сохраняемых данных при очистке
        /// </summary>
        public int ClearedDataSave { get; set; } = 100;


    }
}