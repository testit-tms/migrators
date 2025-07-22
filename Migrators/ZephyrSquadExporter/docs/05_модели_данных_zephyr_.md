# Chapter 5: Модели Данных Zephyr


В [предыдущей главе: Менеджер Токенов Доступа](04_менеджер_токенов_доступа_.md) мы разобрались, как `ZephyrSquadExporter` безопасно аутентифицируется при общении с Zephyr API с помощью JWT-токенов. Мы узнали, как [Клиент Zephyr API](03_клиент_zephyr_api_.md) получает "пропуск" от `TokenManager` и отправляет запросы. Но что происходит, когда Zephyr API присылает ответ? Как наше приложение понимает и использует полученные данные? В этой главе мы рассмотрим "чертежи" для этих данных — **Модели Данных Zephyr**.

## Зачем нужны Модели Данных?

Представьте, что [Клиент Zephyr API](03_клиент_zephyr_api_.md) успешно запросил у Zephyr список всех циклов тестирования для вашего проекта. Zephyr API возвращает ответ в виде текста в формате JSON, который может выглядеть примерно так (упрощенно):

```json
[
  {
    "id": "a1b2c3d4",
    "name": "Релиз Q1 - Регресс",
    "projectId": 10000,
    "versionId": -1,
    "folderId": null
  },
  {
    "id": "e5f6g7h8",
    "name": "Спринт 24.1 - Новые фичи",
    "projectId": 10000,
    "versionId": -1,
    "folderId": null
  }
]
```

Это структурированный текст, но для программы на C# это просто длинная строка. Как программе легко и надежно извлечь, например, имя (`name`) второго цикла? Работать напрямую с текстом JSON — сложно, неудобно и чревато ошибками. Нам нужен способ представить эти данные в виде, привычном для C#.

**Проблема:** Как превратить текстовый JSON-ответ от API в удобные и понятные структуры данных внутри нашего C# приложения?

**Решение:** Использовать **Модели Данных**!

## Что такое Модели Данных Zephyr?

**Модели Данных Zephyr** — это набор простых C# классов, которые точно описывают структуру данных, получаемых от Zephyr Squad API. Каждый класс служит как **шаблон** или **чертеж** для определенного типа данных из Zephyr.

Думайте об этом как о формах для отливки:
*   У вас есть жидкий металл (JSON-ответ от API).
*   У вас есть форма (C# класс-модель).
*   Вы заливаете металл в форму, и он застывает, принимая нужную структуру.
*   В результате у вас есть готовый объект (объект C#), с которым легко работать.

В `ZephyrSquadExporter` есть несколько таких моделей, каждая для своего вида данных:

*   `ZephyrCycle`: Описывает цикл тестирования (как в примере JSON выше).
*   `ZephyrFolder`: Описывает папку внутри цикла.
*   `ZephyrExecution`: Описывает конкретное выполнение тест-кейса (связь тест-кейса с циклом/папкой).
*   `ZephyrStep`: Описывает один шаг тест-кейса (действие, данные, ожидаемый результат).
*   `ZephyrAttachment`: Описывает файл, прикрепленный к шагу или тест-кейсу.

Эти классы разработаны так, чтобы точно соответствовать полям, которые возвращает Zephyr API. Они используют специальные *атрибуты* (например, `[JsonPropertyName("...")]`), чтобы указать, какому полю в JSON соответствует каждое свойство класса C#.

Благодаря этому, специальные библиотеки C# (в нашем случае `System.Text.Json`) могут автоматически **десериализовать** JSON-ответы — то есть, превращать JSON-строку в один или несколько объектов соответствующих C# классов.

## Преимущества использования Моделей Данных

1.  **Типобезопасность:** Вы работаете с типизированными объектами C#. Компилятор подскажет, если вы попытаетесь обратиться к несуществующему полю или использовать его неправильно (например, применить математическую операцию к строке). Это снижает количество ошибок во время выполнения программы.
2.  **Удобство разработки (IntelliSense):** Ваша среда разработки (например, Visual Studio или Rider) будет подсказывать вам доступные поля и методы для этих объектов, ускоряя написание кода.
3.  **Читаемость кода:** Работать с `cycle.Name` гораздо понятнее, чем пытаться извлечь значение поля "name" из строки JSON вручную.
4.  **Простота:** Всю сложную работу по разбору (парсингу) JSON берет на себя библиотека `System.Text.Json`. [Клиент Zephyr API](03_клиент_zephyr_api_.md) просто вызывает одну команду: "Преобразуй этот JSON в список вот таких объектов".

## Примеры Ключевых Моделей

Давайте посмотрим на структуру некоторых моделей данных в `ZephyrSquadExporter`. Мы покажем только самые важные поля для простоты.

**1. `ZephyrCycle` (Модель Цикла Тестирования)**

Этот класс описывает цикл тестирования в Zephyr.

```csharp
// Файл: Models/ZephyrCycle.cs
using System.Text.Json.Serialization;

namespace ZephyrSquadExporter.Models;

public class ZephyrCycle
{
    // Атрибут [JsonPropertyName("name")] говорит десериализатору:
    // "Значение поля 'name' из JSON нужно поместить в это свойство Name"
    [JsonPropertyName("name")]
    public string Name { get; set; } // Имя цикла

    [JsonPropertyName("id")]
    public string Id { get; set; }   // Уникальный идентификатор цикла
    // ... другие поля могут быть здесь ...
}
```

*   **Объяснение:** Очень простой класс с двумя свойствами: `Name` (имя цикла) и `Id` (его идентификатор). Атрибут `[JsonPropertyName(...)]` связывает свойство C# с полем в JSON.

**2. `ZephyrFolder` (Модель Папки)**

Аналогично циклу, описывает папку внутри Zephyr.

```csharp
// Файл: Models/ZephyrFolder.cs
using System.Text.Json.Serialization;

namespace ZephyrSquadExporter.Models;

public class ZephyrFolder
{
    [JsonPropertyName("name")]
    public string Name { get; set; } // Имя папки

    [JsonPropertyName("id")]
    public string Id { get; set; }   // Уникальный идентификатор папки
    // ... другие поля ...
}
```

*   **Объяснение:** Структура очень похожа на `ZephyrCycle`, так как и папки, и циклы имеют имя и ID в Zephyr.

**3. `ZephyrExecution` (Модель Выполнения Тест-кейса)**

Этот класс немного сложнее, так как он представляет связь между тест-кейсом из Jira и его выполнением в рамках цикла/папки Zephyr.

```csharp
// Файл: Models/ZephyrExecution.cs (упрощено)
using System.Text.Json.Serialization;

namespace ZephyrSquadExporter.Models;
// ... Вспомогательные классы SearchResult, ZephyrExecutions опущены ...

public class ZephyrExecution
{
    // Вложенный объект "execution" в JSON
    [JsonPropertyName("execution")]
    public Execution ExecutionInfo { get; set; } // Информация о самом выполнении

    // Ключ задачи в Jira (например, "PROJ-123")
    [JsonPropertyName("issueKey")]
    public string IssueKey { get; set; }

    // Заголовок задачи в Jira
    [JsonPropertyName("issueSummary")]
    public string IssueSummary { get; set; }

    // Описание задачи в Jira
    [JsonPropertyName("issueDescription")]
    public string IssueDescription { get; set; }
    // ... другие поля ...
}

public class Execution // Описывает вложенный объект 'execution'
{
    // ID задачи в Jira (числовой)
    [JsonPropertyName("issueId")]
    public int IssueId { get; set; }

    // ID самого выполнения в Zephyr
    [JsonPropertyName("id")]
    public string Id { get; set; }
}
```

*   **Объяснение:** Эта модель содержит информацию как о самом тест-кейсе из Jira (`IssueKey`, `IssueSummary`, `IssueDescription`), так и о его выполнении в Zephyr (вложенный объект `ExecutionInfo`, который имеет свой `Id` и `IssueId`). Это показывает, как модели могут отражать вложенные структуры JSON.

**4. `ZephyrStep` (Модель Шага Тест-кейса)**

Представляет один шаг внутри тест-кейса.

```csharp
// Файл: Models/ZephyrStep.cs (упрощено)
using System.Text.Json.Serialization;

namespace ZephyrSquadExporter.Models;
// ... Вспомогательный класс ZephyrSteps опущен ...

public class ZephyrStep
{
    [JsonPropertyName("id")]
    public string Id { get; set; } // ID шага

    [JsonPropertyName("step")]
    public string Step { get; set; } // Описание действия шага

    [JsonPropertyName("data")]
    public string Data { get; set; } // Входные данные для шага (если есть)

    [JsonPropertyName("result")]
    public string Result { get; set; } // Ожидаемый результат шага

    // Список вложений для этого шага (если есть)
    [JsonPropertyName("attachments")]
    public List<ZephyrAttachment> Attachments { get; set; }
}
```

*   **Объяснение:** Модель хранит основные атрибуты шага теста: его описание, данные, ожидаемый результат и список вложений (`ZephyrAttachment`, см. ниже).

**5. `ZephyrAttachment` (Модель Вложения)**

Описывает прикрепленный файл.

```csharp
// Файл: Models/ZephyrAttachment.cs
using System.Text.Json.Serialization;

namespace ZephyrSquadExporter.Models;

public class ZephyrAttachment
{
    [JsonPropertyName("id")]
    public string Id { get; set; } // ID вложения

    [JsonPropertyName("name")]
    public string Name { get; set; } // Имя файла

    [JsonPropertyName("fileExtension")]
    public string FileExtension { get; set; } // Расширение файла (например, "png", "txt")
}
```

*   **Объяснение:** Простая модель для хранения информации о файле: его ID, имя и расширение.

## Под Капотом: Магия Десериализации

Как же происходит превращение JSON в эти объекты? Этим занимается процесс **десериализации**, который выполняет [Клиент Zephyr API](03_клиент_zephyr_api_.md) после получения ответа от сервера.

**Процесс выглядит так:**

1.  **Получение JSON:** [Клиент Zephyr API](03_клиент_zephyr_api_.md) отправляет запрос (например, на получение циклов) и получает от Zephyr ответ в виде строки JSON.
2.  **Вызов Десериализатора:** Клиент берет эту JSON-строку и передает ее методу `JsonSerializer.Deserialize<T>()`, указав в качестве `T` нужный тип модели данных (например, `List<ZephyrCycle>`, если ожидается список циклов).
3.  **Сопоставление и Создание Объектов:** `JsonSerializer` делает следующее:
    *   Читает структуру JSON.
    *   Смотрит на указанный C# класс (`ZephyrCycle`).
    *   Для каждого объекта в JSON создает экземпляр класса `ZephyrCycle`.
    *   Для каждого поля в JSON (например, `"name"`) находит соответствующее свойство в классе C# (используя атрибут `[JsonPropertyName("name")]` или просто по совпадению имени, если атрибута нет).
    *   Копирует значение из JSON в свойство созданного C# объекта.
4.  **Возврат Объектов:** `JsonSerializer.Deserialize<List<ZephyrCycle>>()` возвращает готовый список (`List`) объектов `ZephyrCycle`, полностью заполненных данными из JSON.
5.  **Использование:** [Клиент Zephyr API](03_клиент_zephyr_api_.md) возвращает этот список объектов тому сервису, который его запросил (например, [Сервису Папок и Циклов](06_сервис_папок_и_циклов_.md)), и тот может легко с ним работать.

**Текстовая схема потока данных:**

```
JSON строка (от Zephyr API)
       |
       V
JsonSerializer.Deserialize<List<ZephyrCycle>>(jsonСтрока)
       |
       V
Список объектов List<ZephyrCycle> (для использования в C# коде)
```

**Пример кода десериализации в `Client.cs`:**

Вот фрагмент из метода `GetCycles` в [Клиенте Zephyr API](03_клиент_zephyr_api_.md), где происходит десериализация:

```csharp
// Файл: Client/Client.cs (фрагмент метода GetCycles)

// ... после получения успешного ответа (response) ...

// 1. Читаем тело ответа как строку JSON
var content = await response.Content.ReadAsStringAsync();

// 2. Десериализуем JSON-строку в список объектов ZephyrCycle
//    <List<ZephyrCycle>> - указываем, что ожидаем получить список циклов
var cycles = JsonSerializer.Deserialize<List<ZephyrCycle>>(content);

// 3. Теперь 'cycles' - это обычный C# список объектов, с которым можно работать
_logger.LogDebug($"Найдено {cycles?.Count} циклов.");

// 4. Возвращаем результат
return cycles;
```

*   **Объяснение:** Строка `var cycles = JsonSerializer.Deserialize<List<ZephyrCycle>>(content);` — это ключевой момент. Она берет строку `content` (содержащую JSON) и автоматически превращает ее в `List<ZephyrCycle>`, используя модель `ZephyrCycle` как шаблон. Вся магия происходит внутри метода `Deserialize`.

## Заключение

В этой главе мы познакомились с **Моделями Данных Zephyr** — C# классами, которые служат "чертежами" для данных, получаемых от Zephyr API. Мы узнали, что:

*   Они точно описывают структуру JSON-ответов от Zephyr.
*   Их использование позволяет автоматически преобразовывать (десериализовать) JSON в типизированные C# объекты с помощью `System.Text.Json`.
*   Это делает работу с данными из API гораздо проще, безопаснее и удобнее для разработчика.
*   Основные модели включают `ZephyrCycle`, `ZephyrFolder`, `ZephyrExecution`, `ZephyrStep` и `ZephyrAttachment`.

Теперь, когда мы понимаем, как данные из Zephyr представлены внутри нашего приложения с помощью моделей, мы можем перейти к рассмотрению сервисов, которые используют эти модели для получения и обработки конкретных типов данных. Начнем со структуры проекта: папок и циклов.

**Следующий шаг:** Давайте изучим, как `ZephyrSquadExporter` получает информацию о папках и циклах тестирования. Переходим к [Главе 6: Сервис Папок и Циклов](06_сервис_папок_и_циклов_.md).

---

Generated by [AI Codebase Knowledge Builder](https://github.com/The-Pocket/Tutorial-Codebase-Knowledge)