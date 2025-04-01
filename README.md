# RSISensorNET
RSISensorNET — это проект, разработанный для интеграции и управления роботами KUKA с использованием интерфейса RSI и .NET C#. Он предоставляет возможность создания сенсора для взаимодействия с роботом, обеспечивая высокую степень автоматизации и гибкости в производственных процессах.

Поддерживает .NET 6.0 и .NET 8.0.

## Описание класса RSIFBSensor
Класс RSIFBSensor расширяет функциональность класса RSIClient, делая его более высокоуровневым и все также предоставляя возможности для общения с роботами KUKA по протоколу RSI. Он позволяет отправлять и получать теги с робота, используя асинхронные методы для повышения производительности и отзывчивости системы.
Отправление данных происходит согласно очереди, что позволяет не терять данные при отправке.
- Имеет режим FeedBack, который постоянно отправляет заданные теги до подтверждения перезаписи на стороне робота.
- ExchangeAsync - цикл приема и передачи данных робота.
- ExchangePeriodicAsync - возможность приема и передачи данных с заданым интервалом для снижения нагрузки на сеть и компьютер. 
(для поддержания соединения отправляет только обратную связь IPOC на робот)
- ShiftedProperties - Имеет возможность смещать передаваемые значения на робот.
При включенном режиме HOLD OFF возможно отправлять 0 со смещением и отправлять неполные пакеты (так как незаполненные равны нулю). 
Должен поддерживаться алгоритм на стророне робота, который не пересаписывает значение принятого тега, который равен нулю.
Используйте смещения значений, если необходимо изменять только X значений тегов из X + n, где n > 0.

### Основные методы класса
#### Обмен данными
* ExchangeAsync() позволяет асинхронно получать данные от робота, интерпретировать их в формате XML и отправлять обратную связь с тегами в очереди отправки.
#### Периодический обмен данными
* ExchangePeriodicAsync() позволяет поддерживать соединение с роботом и обмениваться данными с заданным интервалом.
#### Добавление данных в очередь
* SendTags(IDictionary<string, string> ValueTable, bool isfeedback = false) позволяет отправлять таблицы ключ-значение на робот
* SendTag(string key, string value, bool isfeedback = false) позволяет отправлять тег на робот
* ClearSendingQueue() очищает очередь отправки
### Свойства класса
* Data: возвращает все принятые данные с робота
* LastData: Возвращает последние принятые данные с робота.
* TagsSendingQueue: Список подготовленных к отправке тегов.
* Connected: Указывает, подключен ли клиент к роботу.
* ShiftedProperties: Настраивает смещение значений, отправляемых на робот.
* ClearedProperties: Настраивает параметры очистки принятых данных.
* TimeoutExchange: Время, после которого данные считаются не принятыми.
* GetDataElapsed: Время, затраченное на прием данных.
* ExchangeElapsed: Время, затраченное на прием и передачу данных.
* ExchangePeriod: Время, после которого будут структурированы полученные данные и отправлены данные в очереди. **Используется только для ExchangePeriodicAsync**.

## Пример использования RSIFBSensor для управления роботом
### 1. Использование ExchangeAsync
#### Поток 1 для соединения с роботом:
``` cs
using KUKA.RSI.Sensors;

//...

public RSIFBSensor Sensor = new RSIFBSensor(49150);

private async void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e) {
	while (!(backgroundWorker1.CancellationPending || e.Cancel)) { // спроси напрямую
		try {
			if (Sensor != null)
				await Sensor.ExchangeAsync();
			else
				await Task.Delay(1);
		} catch (System.OperationCanceledException) {

		} catch (DifferenceSendAndGetDataException err) {
			Debug.WriteLine(err.Message);
		} catch (Exception) {
			throw;
		}
	}
	Debug.WriteLine("Cancel");
}
```
#### Поток 2 для чтения данных с робота (можно в основном):
``` cs
private void timer1_Tick(object sender, EventArgs e) {

	if (Sensor?.LastData.Tags != null) {
		foreach (var tag in Sensor.LastData.Tags) {
			dict[tag.Key] = tag.Value;
		}
		// в данном контексте, источником данных DataGridView является dict
		// ...
	}
	// ...
	double? ping = Sensor?.GetDataElapsed.TotalMilliseconds;
	// toolStripLabelPing.Text = Math.Round(ping ?? 0).ToString();
	// if (panelSend.Visible)
	// 	label5.Text = Sensor?.TagsSendingQueue.Length.ToString() ?? 0.ToString();
}
```
#### Пример добавление тегов для записи на робот в очередь:
``` cs
Dictionary<string, string> DataTable;

if (radioButton1.Checked) {
	XmlDocument doc = new XmlDocument();
	doc.LoadXml(textBoxSend.Text);
	DataTable = KUKA.RSI.DATA.ConvertXmlToDict(doc);
} else {
	DataTable = new Dictionary<string, string>();
	foreach (DataGridViewRow item in dataGridViewSend.Rows) {
		string? key = item.Cells[0].Value?.ToString();
		string? value = item.Cells[1].Value?.ToString();
		if (key != null && value != null)
		if (!DataTable.TryAdd(key, value))
			DataTable[key] = value;
	}
}
//checkBoxSendFeedBack.Checked - FeedBack режим
Sensor?.SendTags(DataTable, checkBoxSendFeedBack.Checked);
```

### 2. Использование ExchangePeriodicAsync
#### Поток 1 для соединения с роботом все так же, только используем ExchangePeriodicAsync:
``` cs
using KUKA.RSI.Sensors;

//...

public RSIFBSensor Sensor = new RSIFBSensor(49150);

private async void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e) {
	while (!(backgroundWorker1.CancellationPending || e.Cancel)) { // спроси напрямую
		try {
			if (Sensor != null)
				await Sensor.ExchangePeriodicAsync();
			else
				await Task.Delay(1);
		} catch (System.OperationCanceledException) {

		} catch (DifferenceSendAndGetDataException err) {
			Debug.WriteLine(err.Message);
		} catch (Exception) {
			throw;
		}
	}
	Debug.WriteLine("Cancel");
}
```
### Базовая настройка класса
Для создания экземпляра класса RSIFBSensor можно указать порт для приема данных, по умолчанию 49150:
``` cs
RSIFBSensor sensor = new RSIFBSensor(49151);
```
Вы также можете использовать IP-адрес и порт для создания экземпляра:
``` cs
IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse("192.168.1.1"), 49151);
RSIFBSensor sensor = new RSIFBSensor(endpoint);
```
