
namespace KUKA.RSI.Sensors {
    /// <summary>
    /// Событие, возникающее при смещении значения в режиме <see cref="ShiftedValueMode.User">User</see>
    /// </summary>
    /// <param name="key">Тег</param>
    /// <param name="value">Значение тега</param>
    /// <returns></returns>
    public delegate string? EventShifted(string key, string value);
    /// <summary>
    /// Предоставляет возможность смещения значений, отправляемых на робот
    /// </summary>
    /// <typeparam name="T">Тип смещаемого значения</typeparam>
    public interface IShifted<T> {
        /// <summary>
        /// Смещения для режима смещения <see cref="ShiftedValueMode.Constant">Constant</see>
        /// </summary>
        T ConstantOffset { get; set; }
        /// <summary>
        /// Таблица смещения для режимов, использующих таблицу смещения
        /// </summary>
        Dictionary<string, T> Table { get; set; }
        /// <summary>
        /// Значение для FALSE режима смещения <see cref="ShiftedValueMode.TableBinareBool">TableBinareBool</see>
        /// </summary>
        T TableBinareFalse { get; set; }
        /// <summary>
        /// Значение для TRUE режима смещения <see cref="ShiftedValueMode.TableBinareBool">TableBinareBool</see>
        /// </summary>
        T TableBinareTrue { get; set; }
        /// <summary>
        /// Режим смещения
        /// </summary>
        public ShiftedValueMode Mode { get; set; }
        /// <summary>
        /// Метод для пользовательского смещения значений в режиме <see cref="ShiftedValueMode.User">User</see>
        /// </summary>
        event EventShifted? UserShiftedValueEvent;
        /// <summary>
        /// Смещает <paramref name="value"/>, согласно режиму смещения.
        /// Вызывается перед отправкой значения на робот.
        /// </summary>
        /// <param name="value">Исходная строка</param>
        /// <returns>Смещенную строку</returns>
        string ShiftedValue(string key, string value);
    }
}