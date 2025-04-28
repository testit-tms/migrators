# Chapter 2: Клиент HP ALM


В [предыдущей главе](01_сервис_экспорта_.md) мы узнали о `Сервисе Экспорта` — главном координаторе нашего процесса переноса данных. Он, как менеджер переезда, раздает задачи другим компонентам. Но чтобы что-то забрать из старого "офиса" (HP ALM), нам нужен кто-то, кто умеет разговаривать с его "администрацией" и получать разрешение на вынос "мебели" (данных). Эту роль выполняет **Клиент HP ALM**.

Представьте, что HP ALM — это другая страна со своим языком и правилами (API). Чтобы получить оттуда нужные нам сведения (тест-кейсы, папки, файлы), нам нужен:

*   **Переводчик:** Тот, кто знает язык HP ALM и может правильно сформулировать наши запросы.
*   **Дипломат:** Тот, кто знает, как правильно представиться (пройти аутентификацию), чтобы нас вообще стали слушать.

**Клиент HP ALM (HP ALM Client)** в `HPALMExporter` — это и есть наш переводчик и дипломат. Он отвечает за всё непосредственное общение с сервером HP ALM.

## Зачем нужен Клиент HP ALM?

Основная задача — наладить связь с HP ALM и получить от него "сырые" данные. Без этого компонента мы просто не сможем начать экспорт. Вот ключевые функции Клиента:

1.  **Аутентификация:** "Представиться" системе HP ALM, используя логин и пароль (или другие учетные данные), чтобы доказать, что у нас есть право доступа к проекту. Без успешной аутентификации сервер HP ALM не отдаст нам никакую информацию.
2.  **Отправка Запросов:** Формировать и отправлять запросы к API HP ALM. Например: "дай мне список папок в корне проекта" или "дай мне детали тест-кейса с ID 123". Клиент знает, в каком формате и по какому адресу отправлять эти запросы.
3.  **Обработка Ответов:** Получать ответы от сервера HP ALM и "распаковывать" их. Ответы обычно приходят в специфическом формате (часто XML), и Клиент преобразует их в более удобные для нашей программы структуры данных.

**Проще говоря:** Если [Сервис Экспорта](01_сервис_экспорта_.md) говорит: "Мне нужны все папки!", то именно Клиент HP ALM идет к серверу HP ALM, правильно его спрашивает и приносит ответ.

## Как Клиент взаимодействует с HP ALM? (Высокоуровнево)

Общение происходит через веб-запросы, похожие на те, что ваш браузер отправляет, когда вы заходите на сайт. Клиент использует стандартный протокол HTTP(S).

1.  **Аутентификация:** Клиент отправляет ваши учетные данные (из файла конфигурации `hpalm.config.json`) на специальный адрес (URL) сервера HP ALM. Если данные верны, сервер запоминает нашу сессию (часто с помощью cookies), и позволяет делать следующие запросы.
2.  **Запрос данных:** Клиент формирует URL, указывающий, какие данные ему нужны (например, список тестов в папке с ID 5). Он отправляет GET-запрос по этому адресу.
3.  **Получение ответа:** Сервер HP ALM отвечает, присылая данные в определенном формате (например, XML).
4.  **Парсинг (Разбор) ответа:** Клиент берет полученный XML, "читает" его и извлекает нужную информацию (например, имена и ID тестов), превращая ее во внутренние объекты программы.

## Использование Клиента: Интерфейс `IClient`

Чтобы другие части программы могли использовать Клиент, не зная всех деталей его внутренней работы, существует "контракт" или **интерфейс** `IClient`. Он определяет, *что* Клиент умеет делать.

```csharp
// Файл: Client/IClient.cs (упрощенно)
// Определяет возможности Клиента HP ALM
public interface IClient
{
    // Выполнить аутентификацию
    Task Auth();

    // Получить список папок (тест-фолдеров) по ID родительской папки
    Task<List<HPALMFolder>> GetTestFolders(int parentId);

    // Получить список тестов в папке с указанными атрибутами
    Task<List<HPALMTest>> GetTests(int folderId, IEnumerable<string> attributes);

    // Получить шаги для конкретного теста
    Task<List<HPALMStep>> GetSteps(int testId);

    // Получить вложения для теста
    Task<List<HPALMAttachment>> GetAttachmentsFromTest(int testId);

    // Скачать содержимое вложения
    Task<byte[]> DownloadAttachment(int testId, string attachName);

    // Получить пользовательские атрибуты (поля) для тестов
    Task<HPALMAttributes> GetTestAttributes();

    // ... и другие методы для получения разной информации ...
}
```

**Объяснение:**

*   `IClient` — это как меню в ресторане: оно перечисляет блюда (методы), которые можно заказать.
*   `Task` означает, что операция может занять время (она асинхронная), и нам нужно будет дождаться ее завершения.
*   Методы названы так, чтобы было понятно, что они делают: `Auth` для входа, `GetTestFolders` для получения папок, `GetTests` для тестов и т.д.
*   Другие сервисы, например [Сервис Секций (Папок)](03_сервис_секций__папок__.md), будут использовать метод `GetTestFolders`, чтобы получить структуру папок проекта.

## Заглянем под капот: Реализация `Client.cs`

Теперь посмотрим, как эти методы реализованы внутри класса `Client`, который "исполняет" контракт `IClient`.

### 1. Инициализация и Конфигурация

Когда приложение запускается, создается объект `Client`. В конструкторе он читает настройки из файла `hpalm.config.json` и настраивает **HTTP-клиент** (`HttpClient`) — инструмент для отправки веб-запросов.

```csharp
// Файл: Client/Client.cs (фрагмент конструктора)
using Microsoft.Extensions.Configuration;
using System.Net.Http; // Для HttpClient

public class Client : IClient
{
    private readonly ILogger _logger; // Для записи логов
    private readonly string _clientId; // ID клиента для аутентификации
    private readonly string _secret;   // Секрет для аутентификации
    private readonly string _domain;   // Домен HP ALM
    private readonly string _projectName; // Имя проекта HP ALM
    private readonly HttpClient _httpClient; // Инструмент для HTTP-запросов

    public Client(ILogger logger, IConfiguration configuration)
    {
        _logger = logger;

        // Читаем настройки из конфигурации
        var section = configuration.GetSection("hpalm");
        var url = section["url"]; // Адрес сервера HP ALM
        _clientId = section["clientId"];
        _secret = section["secret"];
        _domain = section["domainName"];
        _projectName = section["projectName"];

        // Проверяем, что все настройки указаны (пропущено для краткости)

        // Настраиваем HttpClient
        var handler = new HttpClientHandler();
        handler.CookieContainer = new CookieContainer(); // Важно для хранения сессии!
        _httpClient = new HttpClient(handler);
        _httpClient.BaseAddress = new Uri(url); // Устанавливаем базовый адрес
    }

    // ... остальные методы ...
}
```

**Объяснение:**

*   Конструктор получает `IConfiguration`, который предоставляет доступ к настройкам из `hpalm.config.json`.
*   Он считывает URL сервера, ID клиента, секрет, домен и имя проекта.
*   Самое главное: создается `HttpClient`. Это стандартный класс в .NET для отправки HTTP-запросов. Мы настраиваем его так, чтобы он автоматически управлял cookies (`CookieContainer`) — это нужно, чтобы сервер HP ALM "помнил" нас после аутентификации.

### 2. Аутентификация (`Auth`)

Это первый шаг перед получением любых данных.

```csharp
// Файл: Client/Client.cs (фрагмент метода Auth)
using System.Text;
using System.Text.Json; // Для работы с JSON
using System.Net.Mime; // Для MediaTypeNames

public async Task Auth()
{
    _logger.Information("Authorizing in HP ALM"); // Пишем в лог

    // 1. Подготовка данных для запроса
    var dto = new AuthDto { ClientId = _clientId, Secret = _secret };
    var jsonContent = JsonSerializer.Serialize(dto); // Превращаем в JSON
    var content = new StringContent(jsonContent, Encoding.UTF8, MediaTypeNames.Application.Json); // Упаковываем для отправки

    // 2. Адрес для входа (специфичен для HP ALM OAuth)
    var loginUrl = "/qcbin/rest/oauth2/login";

    // 3. Отправка POST-запроса
    _logger.Debug("Connect to {Url}{LoginPath}", _httpClient.BaseAddress, loginUrl);
    var response = await _httpClient.PostAsync(loginUrl, content);

    // 4. Проверка ответа
    if (response.IsSuccessStatusCode)
    {
        _logger.Information("Login to HP QLM success");
        // Успех! HttpClient автоматически сохранил cookie сессии.
        return;
    }
    else
    {
        // Ошибка! Логируем и выбрасываем исключение.
        var errorResponse = await response.Content.ReadAsStringAsync();
        _logger.Error("Connect to HP ALM failed: {error}", errorResponse);
        throw new Exception($"Authentication failed: {response.ReasonPhrase}");
    }
}
```

**Объяснение:**

1.  Создается объект `AuthDto` с ID клиента и секретом.
2.  Этот объект преобразуется в формат JSON, который понятен многим веб-сервисам.
3.  Формируется HTTP POST-запрос на специальный адрес `/qcbin/rest/oauth2/login`.
4.  `_httpClient.PostAsync` отправляет запрос и ждет (`await`) ответа.
5.  Проверяется статус ответа. Если статус успешный (например, `200 OK`), значит аутентификация прошла. `HttpClient` сам сохранит нужные cookies для последующих запросов. Если нет — записываем ошибку.

### 3. Получение данных (Пример: `GetTestFolders`)

После успешной аутентификации мы можем запрашивать данные.

```csharp
// Файл: Client/Client.cs (фрагмент метода GetTestFolders)
using System.Xml.Serialization; // Для работы с XML

public async Task<List<HPALMFolder>> GetTestFolders(int parentId)
{
    _logger.Information("Get test folders from HP ALM with parent id {id}", parentId);

    // 1. Формирование URL для запроса папок
    var requestUrl = $"/qcbin/rest/domains/{_domain}/projects/{_projectName}/test-folders" +
                     $"?query={{parent-id[={parentId}]}}&page-size=max"; // Запрашиваем все папки с нужным родителем

    _logger.Debug("Connect to {Url}{RequestPath}", _httpClient.BaseAddress, requestUrl);

    // 2. Отправка GET-запроса
    var responseString = await _httpClient.GetStringAsync(requestUrl); // Получаем ответ как строку (XML)

    // 3. Разбор (парсинг) XML-ответа
    var serializer = new XmlSerializer(typeof(Entities)); // Инструмент для чтения XML
    Entities entities;
    using (var sr = new StringReader(responseString))
    {
        entities = (Entities)serializer.Deserialize(sr); // Преобразуем XML в объекты
    }

    // 4. Преобразование в наши внутренние объекты HPALMFolder
    var folders = entities.Entity.Select(e => e.ToTestFolder()).ToList();

    // (пропущена логика для обработки постраничных результатов, если папок очень много)

    _logger.Information("Found {count} folders", folders.Count);
    return folders;
}
```

**Объяснение:**

1.  Формируется URL, который точно указывает HP ALM, что мы хотим: папки (`test-folders`) в нашем домене (`_domain`) и проекте (`_projectName`), у которых родительская папка имеет ID `parentId`. `page-size=max` просит вернуть как можно больше результатов за раз.
2.  `_httpClient.GetStringAsync` отправляет GET-запрос по этому адресу и получает ответ в виде строки, которая содержит XML-данные.
3.  Используется `XmlSerializer` для разбора этого XML. Ответ от HP ALM имеет сложную структуру (объекты `Entities`, `Entity`, `Fields`), и сериализатор помогает превратить этот XML в понятные C# объекты. `ToTestFolder()` — это вспомогательный метод, который извлекает нужные данные (ID, имя папки) из этих объектов.
4.  Метод возвращает список объектов `HPALMFolder`, готовых к использованию другими сервисами.

Остальные методы (`GetTests`, `GetSteps`, `GetAttachmentsFromTest` и т.д.) работают по схожему принципу: формируют специфичный URL, отправляют запрос, получают ответ (обычно XML) и разбирают его, чтобы извлечь нужную информацию.

### Диаграмма Последовательности: Аутентификация

Вот как выглядит процесс аутентификации:

```mermaid
sequenceDiagram
    participant Requester as Запрашивающий Сервис (напр., ExportService)
    participant AlmClient as Клиент HP ALM (Client)
    participant Http as HttpClient
    participant Server as Сервер HP ALM

    Requester->>+AlmClient: Auth()
    AlmClient->>AlmClient: Собрать ClientId и Secret
    AlmClient->>+Http: PostAsync("/qcbin/rest/oauth2/login", {ClientId, Secret})
    Http->>+Server: Отправить POST запрос
    Server->>Server: Проверить учетные данные
    alt Успешно
        Server-->>-Http: Ответ 200 OK (с Cookie сессии)
        Http-->>-AlmClient: Ответ 200 OK (Cookie сохранен)
        AlmClient-->>-Requester: Успешное завершение Task
    else Ошибка
        Server-->>-Http: Ответ 401 Unauthorized (или др. ошибка)
        Http-->>-AlmClient: Ответ с ошибкой
        AlmClient->>AlmClient: Залогировать ошибку
        AlmClient-->>-Requester: Выбросить Exception
    end
```

**Объяснение диаграммы:**

1.  `Запрашивающий сервис` вызывает метод `Auth()` у `Клиента HP ALM`.
2.  `Клиент` готовит данные и просит `HttpClient` отправить POST-запрос на сервер.
3.  `HttpClient` отправляет запрос на `Сервер HP ALM`.
4.  `Сервер` проверяет данные.
5.  Если все верно, `Сервер` отвечает успехом (`200 OK`) и устанавливает cookie сессии. `HttpClient` получает ответ и сохраняет cookie. `Клиент` сообщает об успехе.
6.  Если данные неверны, `Сервер` отвечает ошибкой (например, `401 Unauthorized`). `HttpClient` передает эту ошибку `Клиенту`, который логирует ее и сообщает об ошибке (выбрасывает исключение).

## Заключение

В этой главе мы познакомились с **Клиентом HP ALM** — важнейшим компонентом `HPALMExporter`, который отвечает за все прямое общение с сервером HP ALM. Он как опытный дипломат и переводчик:

*   **Аутентифицируется** (представляется) в системе.
*   **Отправляет запросы** на языке API HP ALM для получения различных данных (папок, тестов, шагов, атрибутов, вложений).
*   **Обрабатывает ответы** сервера, переводя их из формата HP ALM (часто XML) во внутренние структуры данных нашей программы.

Мы увидели, как устроен интерфейс `IClient` и как реализованы ключевые методы внутри класса `Client`, использующие `HttpClient` для веб-запросов и `XmlSerializer` для разбора ответов.

Теперь, когда мы знаем, *как* получить данные из HP ALM, мы можем перейти к тому, *какие* данные мы будем получать и как их организовывать. В следующей главе мы рассмотрим, как с помощью Клиента получить структуру папок проекта.

**Перейти к следующей главе:** [Глава 3: Сервис Секций (Папок)](03_сервис_секций__папок__.md)

---

Generated by [AI Codebase Knowledge Builder](https://github.com/The-Pocket/Tutorial-Codebase-Knowledge)