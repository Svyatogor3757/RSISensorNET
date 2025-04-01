using KUKA.RSI.Sensors.Exceptions;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Xml;


namespace KUKA.RSI.Sensors {
    /// <summary>
    /// Класс для общения с KUKA роботом по RSI протоколу.
    /// Имеет возможность отправлять и принимать данные с робота.
    /// Отправление данных происходит согласно очереди, что позволяет не терять данные при отправке.
    /// Имеет режим FeedBack, который постоянно отправляет заданные теги до подтверждения перезаписи на стороне робота.
    /// ExchangePeriodicAsync - возможность приема и передачи данных с заданым интервалом для снижения нагрузки на сеть и компьютер. 
    /// (для поддержания соединения отправляется только обратная связь IPOC на робот)
    /// </summary>
    public class RSIFBSensor : RSIClient {
        private bool ischecksenddata = false;
        private List<RSITag> tagsSendingQueue;
        private Stopwatch ExchangeSW = new Stopwatch();

        /// <summary>
        /// Возращает последние принятые данные с робота
        /// </summary>
        public RSIData LastData { get => Data.LastOrDefault(RSIData.Empty); }
        /// <summary>
        /// Список подготовленных к отправке тегов
        /// </summary>
        public RSITag[] TagsSendingQueue { get => tagsSendingQueue.ToArray(); }
        /// <summary>
        /// Время, после которого будет считаться, что данные не приняты
        /// </summary>
        public int TimeoutExchange { get; set; } = 8;
        /// <summary>
        /// Число допустимых пакетов, которые превысили лимит
        /// </summary>
        public int TimeoutExchangeDataLimit { get; set; } = 5;
        /// <summary>
        /// Текущее число пакетов, которые превысили лимит
        /// </summary>
        protected int TimeoutExchangeDataCurrent = 0;
        /// <summary>
        /// Настройка параметров очистки принятых данных
        /// </summary>
        public ClearedData ClearedProperties { get; protected set; } = new();

        /// <summary>
        /// Время, затраченное на прием данных
        /// </summary>
        public TimeSpan GetDataElapsed { get; private set; } = TimeSpan.Zero;
        /// <summary>
        /// Время, затраченное на прием и передачу данных
        /// </summary>
        public TimeSpan ExchangeElapsed { get; private set; } = TimeSpan.Zero;
        /// <summary>
        /// Время, после которого будут структурированы полученные данные и отправлены данные в очереди.
        /// Используется только для ExchangePeriodicAsync
        /// </summary>
        public TimeSpan ExchangePeriod { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// True, если клиент подключен к роботу
        /// </summary>
        public bool Connected {
            get {
                if (LastData != RSIData.Empty) {
                    TimeSpan dt = DateTime.Now - LastData.ReceivingTime;
                    if (dt < ExchangePeriod.Add(TimeSpan.FromMilliseconds(TimeoutExchange)))
                        return true;
                }
                return false;
            }
        }
        /// <summary>
        /// Параметры смещения значений, отправляемых на робот.
        /// Обычно, используется с режимом тегов HOLD OFF.
        /// При включенном режиме HOLD OFF возможно отправлять 0 со смещением и отправлять неполные пакеты (так как незаполненные равны нулю). 
        /// Должен поддерживаться алгоритм на стророне робота, который не пересаписывает значение принятого тега, который равен нулю.
        /// Используйте смещения значений, если необходимо изменять только X значений тегов из X + n, где n > 0.
        /// </summary>
        public IShifted<int> ShiftedProperties { get; protected set; } = new ShiftedInt();
        /// <summary>
        /// Количество неудачно принятых пакетов
        /// </summary>
        public int ErrorGetCount { get; set; }
        /// <summary>
        /// Количество неудачно отправленных пакетов.
        /// Обычно UDP протокол отправляет пакеты без обратной связи.
        /// Если счетчик растет, то, вероятнее, нет соединения впринципе.
        /// </summary>
        public int ErrorSendCount { get; set; }
        /// <summary>
        /// Коллекция неудачно перезаписанных отслеживаемых переменных.
        /// Возвращает количество неудачных попыток перезаписи переменных каждую итерацию.
        /// </summary>
        public List<int> ErrorFeedBackFSendCounts { get; protected set; }
        /// <summary>
        /// Лимит ошибок, после которых возникает исключение в методе ExchangeAsync
        /// </summary>
        public int ErrorCountLimit { get; set; } = 20;
        /// <summary>
        /// Время, необходимое для приема и передачи данных.
        /// Если время приема и передачи превышает лимит, то возникает исключение
        /// </summary>

        /// <summary>
        /// Создание RSI клиента с указанием порта для приема данных.
        /// </summary>
        /// <param name="sensorport">Порт приёма данных</param>
        public RSIFBSensor(uint sensorport = 49150) : base(sensorport) {
            ErrorFeedBackFSendCounts = new List<int>();
            tagsSendingQueue = new List<RSITag>();
        }
        /// <summary>
        ///  Создание RSI клиента с указанием порта для приема данных.
        /// </summary>
        /// <param name="iPEndPoint">IP и порт для приема данных</param>
        public RSIFBSensor(IPEndPoint iPEndPoint) : base(iPEndPoint) {
            ErrorFeedBackFSendCounts = new List<int>();
            tagsSendingQueue = new List<RSITag>();
        }
        /// <summary>
        /// Произвести приём и передачу данных робота
        /// </summary>
        /// <exception cref="GetErrorCountLimitException">Возникает при превышении предела ошибок получения данных</exception>
        /// <exception cref="SendErrorCountLimitException">Возникает при превышении предела ошибок отправки данных</exception>
        /// <exception cref="DifferenceSendAndGetDataException">Время между приемом и отправной данных слишком большое</exception>
        public async Task ExchangeAsync() {

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            bool getdataresult = await GetDataAsync();
            GetDataElapsed = stopwatch.Elapsed;
            if (!getdataresult && ++ErrorGetCount > ErrorCountLimit)
                throw new GetErrorCountLimitException();
            if (!getdataresult)
                return;

            CheckFeedBack(LastData);

            string ipocvalue = LastData.GetIPOC();
            
            RSITag? itemipoc = tagsSendingQueue.FirstOrDefault(x => x.Key == DATA.IPOC);
            if (itemipoc != null) {
                itemipoc.Value = ipocvalue;
            } else {
                tagsSendingQueue.Add(new RSITag(DATA.IPOC, ipocvalue, false));
            }

            if (tagsSendingQueue.Count > 1 &&
                tagsSendingQueue.Count < 3 &&
                tagsSendingQueue[0].Value.Length == tagsSendingQueue[1].Value.Length) {
                Console.WriteLine();
            }

            bool senddataresult = await Task.Run(
                () =>
                base.SendTags(tagsSendingQueue.ToDictionary(
                x => x.Key,
                y => y.Key != DATA.IPOC ? ShiftedProperties.ShiftedValue(y.Key, y.Value) : y.Value
                )
                )
            );
            if (!senddataresult && ++ErrorSendCount > ErrorCountLimit)
                throw new SendErrorCountLimitException();
            ischecksenddata = tagsSendingQueue.Any(x => x.FeedBacked);
            tagsSendingQueue.RemoveAll(tag => !tag.FeedBacked);

            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > TimeoutExchange && TimeoutExchangeDataCurrent++ > TimeoutExchangeDataLimit)
                throw new DifferenceSendAndGetDataException() { DifferenseTime = stopwatch.Elapsed };
            else if (TimeoutExchangeDataCurrent > 0)
                TimeoutExchangeDataCurrent--;
            if (ClearedProperties.Enabled)
                ClearData(ClearedProperties.ClearedDataLimit, ClearedProperties.ClearedDataSave);

        }
        /// <summary>
        /// Проверяем данные с обратной связью.
        /// Если они перезаписаны на роботе, то удаляем их
        /// </summary>
        /// <param name="lastdata"></param>
        private void CheckFeedBack(RSIData lastdata) {
            if (ischecksenddata && lastdata != RSIData.Empty) {
                int feedbackerrorcount = tagsSendingQueue.Count(x => x.FeedBacked) - tagsSendingQueue.RemoveAll(x =>
                    x.FeedBacked && lastdata.Tags.ContainsKey(x.Key) &&
                        lastdata.Tags[x.Key] == x.Value
                );

                ErrorFeedBackFSendCounts.Add(feedbackerrorcount);
                feedbackerrorcount = 0;
            }
        }

        /// <summary>
        /// Поддерживает соединение с сервером без записи приходящих данных
        /// </summary>
        /// <exception cref="GetErrorCountLimitException">Возникает при превышении предела ошибок получения данных</exception>
        /// <exception cref="SendErrorCountLimitException">Возникает при превышении предела ошибок отправки данных</exception>
        public async Task KeepConnectionAsync() {
            XmlDocument? doc = null;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            UdpReceiveResult result = await Client.ReceiveAsync(cts.Token);
            if (result.Buffer != null && result.Buffer.Length > 0)
                doc = DATA.ParseXmlData(result.Buffer);
            GetDataElapsed = stopwatch.Elapsed;
            stopwatch.Stop();
            if (doc == null && ++ErrorGetCount > ErrorCountLimit)
                throw new GetErrorCountLimitException();

            XmlNode? ipocNode = doc?.SelectSingleNode($"/root/{DATA.IPOC}");
            string? value = ipocNode?.Value;
            if (value == null)
                return;
            robotEndPoint = result.RemoteEndPoint;
            bool senddataresult = SendStringData(DATA.ConvertDictToXmlDoc(null, value));
            if (!senddataresult && ++ErrorSendCount > ErrorCountLimit)
                throw new SendErrorCountLimitException();

        }

        /// <summary>
        /// Произвести приём и передачу данных робота.
        /// Отличается от обычного приема и получения тем, что прием происходит через заданный интервал <see cref="ExchangePeriod">ExchangePeriod</see>. 
        /// Поддерживание соединения идет всегда. Передача данных в очереди происходит, также, через заданный интервал.
        /// </summary>
        /// <exception cref="GetErrorCountLimitException">Возникает при превышении предела ошибок получения данных</exception>
        /// <exception cref="SendErrorCountLimitException">Возникает при превышении предела ошибок отправки данных</exception>
        /// <exception cref="DifferenceSendAndGetDataException">Время между приемом и отправной данных слишком большое</exception>
        public async Task ExchangePeriodicAsync() {
            if (!ExchangeSW.IsRunning)
                ExchangeSW.Restart();
            if (ExchangeSW.Elapsed >= ExchangePeriod) {
                await ExchangeAsync();
                ExchangeSW.Stop();
            } else
                await KeepConnectionAsync();
        }

        /// <summary>
        /// Добавление в очередь отправки таблицы ключ-значение на робот.
        /// </summary>
        /// <param name="ValueTable">Таблица ключ-значение</param>
        /// <param name="isfeedback">True, если ожидать изменения тега</param>
        public void SendTags(IDictionary<string, string> ValueTable, bool isfeedback = false) {
            foreach (var item in ValueTable)
                SendTag(item.Key, item.Value, isfeedback);
        }
        /// <summary>
        /// Добавление тега в очередь отправки на робот
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="value">Значение</param>
        /// <param name="isfeedback">True, если ожидать изменения тега</param>
        public void SendTag(string key, string value, bool isfeedback = false) {
            var tsq = new List<RSITag>(tagsSendingQueue);
            RSITag? item = tsq.FirstOrDefault(x => x.Key == key);
            if (item == null)
                tagsSendingQueue.Add(new RSITag(key, value, isfeedback));
            else {
                //int dsqi = DataSendingQueue.IndexOf(item);
                //item.Key = key;
                item.Value = value;
                item.FeedBacked = isfeedback;
            }
        }
        /// <summary>
        /// Очищает очередь отправки
        /// </summary>
        public void ClearSendingQueue() {
            tagsSendingQueue.Clear();
            ischecksenddata = false;
        }
        /// <summary>
        /// Задает быстродействие приёма и отправки данных
        /// </summary>
        /// <param name="mode">Режим работы сенсора</param>
        public void SetSensorMode(SensorMode mode) {
            TimeoutExchange = (int)mode;
        }


    }
}
