# Chapter 5: Конвертация Тест-кейсов (ITestCaseService)


В [предыдущей главе](04_запись_чтение_json__iwriteservice__iparserservice__.md) мы разобрались, как `IWriteService` и `IParserService` работают с промежуточным хранилищем - JSON-файлами. Мы узнали, что [Экспортер](02_архитектура_экспортера__iexportservice__.md) "упаковывает" данные из исходной TMS в [Общие Модели](01_общие_модели_данных__models_project__.md) и записывает их в JSON, а [Импортер](03_архитектура_импортера__iimportservice__.md) читает эти JSON и использует [Общие Модели](01_общие_модели_данных__models_project__.md) для загрузки в целевую TMS.

Но как именно происходит это волшебство преобразования? Кто берет специфический формат данных, например, из Allure TestOps, и превращает его в нашу универсальную `Models.TestCase`? А кто затем, в Импортере, берет `Models.TestCase` и преобразует его в формат, понятный Test IT?

Представьте себе профессионального переводчика, специализирующегося на технических заданиях. Он получает описание задачи на одном "языке" (формат Allure, Azure DevOps) и переводит его на общий "язык" (`Models.TestCase`), сохраняя все важные детали: шаги, атрибуты, связи, вложения. В импортере он выполняет обратный перевод — с общего языка на язык целевой системы.

В `migrators` роль такого переводчика для тест-кейсов выполняет **`ITestCaseService`**.

## Зачем Нужен Специальный Конвертер для Тест-кейсов?

Тест-кейсы — это самая сложная и важная часть миграции. Они содержат множество полей: название, описание, приоритет, статус, шаги (с действиями, ожидаемыми результатами, тестовыми данными), предусловия, постусловия, теги, ссылки, вложения, пользовательские атрибуты и так далее.

Каждая TMS (Allure, TestRail, Azure, Test IT) хранит эту информацию по-своему:

*   Поля могут называться иначе (например, "Importance" в TestLink против "Priority" в `Models`).
*   Структура шагов может отличаться.
*   Способы хранения вложений и ссылок разные.
*   Пользовательские атрибуты могут иметь разные типы и представления.

Нам нужен компонент, который знает **специфику конкретной TMS** и умеет точно сопоставлять ее поля и структуры с полями и структурами наших [Общих Моделей](01_общие_модели_данных__models_project__.md) (и наоборот).

Именно эту задачу решает `ITestCaseService`. Для каждой TMS (как в Экспортере, так и в Импортере) существует своя реализация этого сервиса, свой "переводчик".

## Что Такое `ITestCaseService`?

`ITestCaseService` — это интерфейс, то есть "должностная инструкция" для сервиса, отвечающего за преобразование тест-кейсов. Инструкция немного отличается для Экспортера и Импортера, так как направление перевода разное.

**В Экспортере (например, `AllureExporter`)**

Интерфейс `ITestCaseService` говорит: "Ты должен уметь брать специфичные данные о тест-кейсах из исходной TMS (например, Allure) и конвертировать их в список объектов нашего общего формата `Models.TestCase`".

```csharp
// Файл: Migrators/AllureExporter/Services/ITestCaseService.cs
using AllureExporter.Models.Project; // Модели, специфичные для Allure
using Models; // Наши Общие Модели

namespace AllureExporter.Services;

// Инструкция для конвертера тест-кейсов в AllureExporter
public interface ITestCaseService
{
    // Метод должен взять ID проекта и другую информацию из Allure,
    // получить тест-кейсы и вернуть их в виде списка Models.TestCase
    Task<List<TestCase>> ConvertTestCases(
        long projectId, // ID проекта в Allure
        Dictionary<string, Guid> sharedStepMap, // Карта для общих шагов
        Dictionary<string, Guid> attributes, // Карта для атрибутов
        SectionInfo sectionInfo // Информация о секциях
    );
}
```

*   Этот интерфейс требует реализовать метод `ConvertTestCases`.
*   На вход он получает информацию, нужную для получения и корректной конвертации тест-кейсов из Allure (ID проекта, карты для связи с другими сущностями).
*   На выходе он **возвращает** `Task<List<TestCase>>` — асинхронную операцию, результатом которой будет список тест-кейсов в нашем **общем формате `Models.TestCase`**.

**В Импортере (например, `Importer` для Test IT)**

Интерфейс `ITestCaseService` здесь говорит: "Ты должен уметь брать список ID тест-кейсов (которые нужно импортировать), читать их детальные данные из JSON (в формате `Models.TestCase`), преобразовывать их в формат, понятный для API целевой TMS (Test IT), и инициировать их загрузку".

```csharp
// Файл: Migrators/Importer/Services/ITestCaseService.cs
using Importer.Models; // Модели, специфичные для API Test IT

namespace Importer.Services;

// Инструкция для конвертера/загрузчика тест-кейсов в Importer
public interface ITestCaseService
{
    // Метод должен взять ID проекта в Test IT, список ID тест-кейсов из Models,
    // прочитать детали каждого из файла, конвертировать и загрузить в Test IT.
    Task ImportTestCases(
        Guid projectId, // ID проекта в Test IT
        IEnumerable<Guid> testCases, // Список ID тест-кейсов (из Models) для импорта
        Dictionary<Guid, Guid> sections, // Карта для секций (Models ID -> Test IT ID)
        Dictionary<Guid, TmsAttribute> attributes, // Карта для атрибутов
        Dictionary<Guid, Guid> sharedSteps // Карта для общих шагов
    );
}
```

*   Этот интерфейс требует реализовать метод `ImportTestCases`.
*   На вход он получает ID проекта в целевой системе и списки/карты, необходимые для связи данных (ID тест-кейсов из `main.json`, карты соответствия ID секций, атрибутов, общих шагов).
*   Внутри своей реализации он использует [Сервис Чтения JSON (`IParserService`)](04_запись_чтение_json__iwriteservice__iparserservice__.md) для загрузки деталей `Models.TestCase` по ID.
*   Затем он выполняет **обратное преобразование**: из `Models.TestCase` в специфические модели для API Test IT.
*   Наконец, он использует [Адаптер API Импортера (`IClientAdapter`)](07_адаптер_api_импортера__iclientadapter__.md), чтобы отправить эти преобразованные данные в Test IT. Он **ничего не возвращает** (`Task`), так как его задача — выполнить действие по импорту.

## Как Это Работает: Пример Экспорта из Allure

Давайте подробнее рассмотрим, как `ITestCaseService` работает на стороне Экспортера (например, `AllureExporter`).

1.  **Вызов из `ExportService`:** [Главный сервис экспорта (`IExportService`)](02_архитектура_экспортера__iexportservice__.md) решает, что пора конвертировать тест-кейсы. Он вызывает метод `ConvertTestCases` у своей реализации `ITestCaseService`.
    ```csharp
    // Внутри AllureExporter/Services/Implementations/ExportService.cs
    // ...
    var testCases = await _testCaseService.ConvertTestCases(project.Id, sharedStepsMap, attributesMap, sectionInfo);
    // ...
    // Теперь в переменной 'testCases' лежит список объектов Models.TestCase
    ```

2.  **Получение Данных из Источника:** Внутри `ConvertTestCases` реализация `ITestCaseService` сначала должна получить "сырые" данные о тест-кейсах из Allure. Для этого она использует [Клиент API Экспортера (`IClient`)](06_клиент_api_экспортера__iclient__.md).
    ```csharp
    // Внутри AllureExporter/Services/Implementations/TestCaseService.cs
    // (Упрощенно)
    public async Task<List<TestCase>> ConvertTestCases(...)
    {
        // ... определить, какие ID тест-кейсов нужны ...
        foreach (var testCaseId in idsToConvert)
        {
            // Запрашиваем 'сырой' тест-кейс у Allure через IClient
            var allureRawTestCase = await client.GetTestCaseById(testCaseId);

            // Конвертируем этот сырой кейс в нашу модель
            var convertedTestCase = await ConvertSingleTestCase(allureRawTestCase, ...);
            testCases.Add(convertedTestCase);
        }
        return testCases;
    }
    ```

3.  **Конвертация ("Перевод"):** Самая важная часть — преобразование данных из формата Allure (`allureRawTestCase`) в нашу общую модель `Models.TestCase`. Это происходит в отдельном методе (например, `ConvertSingleTestCase`).
    ```csharp
    // Внутри AllureExporter/Services/Implementations/TestCaseService.cs
    // (Очень Упрощенно)
    private async Task<TestCase> ConvertSingleTestCase(
        AllureTestCase allureRawTestCase, // Данные из Allure
        Guid sectionId, // ID секции из Models
        /*... другие карты ...*/)
    {
        // Создаем уникальный ID для нашей Модели
        var modelTestCaseId = Guid.NewGuid();

        // 1. Маппинг простых полей
        var modelTestCase = new Models.TestCase
        {
            Id = modelTestCaseId,
            Name = allureRawTestCase.Name, // Простое копирование имени
            Description = allureRawTestCase.Description, // Копируем описание
            SectionId = sectionId, // Используем ID секции из Models
            Priority = ConvertAllurePriority(allureRawTestCase.Priority), // Нужна функция конвертации приоритета
            State = StateType.NotReady, // Задаем статус по умолчанию или конвертируем
            Tags = allureRawTestCase.Tags.Select(t => t.Name).ToList(), // Преобразуем теги
            // ... и другие простые поля ...
        };

        // 2. Конвертация сложных полей (вызов других сервисов)
        // Шаги: используем IStepService (специфичный для Allure)
        modelTestCase.Steps = await stepService.ConvertStepsForTestCase(allureRawTestCase.Id, ...);

        // Предусловия/Постусловия: возможно, тоже через IStepService или напрямую
        modelTestCase.PreconditionSteps = ConvertConditions(allureRawTestCase.PreconditionHtml);
        modelTestCase.PostconditionSteps = ConvertConditions(allureRawTestCase.ExpectedResultHtml);

        // Атрибуты: конвертируем пользовательские поля Allure
        modelTestCase.Attributes = await ConvertAttributes(allureRawTestCase.Id, allureRawTestCase, ...);

        // Вложения: используем IAttachmentService (специфичный для Allure)
        modelTestCase.Attachments = await attachmentService.DownloadAttachmentsforTestCase(allureRawTestCase.Id, modelTestCaseId);

        // Ссылки: конвертируем разные типы ссылок Allure
        modelTestCase.Links = ConvertLinks(allureRawTestCase.Links, ...);

        return modelTestCase;
    }
    ```
    *   Простые поля (имя, описание) часто копируются напрямую.
    *   Поля, требующие преобразования (приоритет, статус), обрабатываются вспомогательными функциями.
    *   Сложные структуры (шаги, атрибуты, вложения) обычно делегируются другим специализированным сервисам (`IStepService`, `IAttachmentService`, `IAttributeService`), которые также являются частью конкретного экспортера (AllureExporter).
    *   Важно: Все идентификаторы (ID секции, ID общих шагов, ID атрибутов) должны быть преобразованы в GUID'ы, используемые в [Общих Моделях](01_общие_модели_данных__models_project__.md). Для этого используются карты соответствия, переданные в `ConvertTestCases`.

4.  **Возврат результата:** Метод `ConvertTestCases` собирает все сконвертированные объекты `Models.TestCase` в список и возвращает его вызывающей стороне ([`IExportService`](02_архитектура_экспортера__iexportservice__.md)).

## Внутренняя Реализация: Диаграмма (Экспорт)

Вот как выглядит упрощенная последовательность действий при конвертации тест-кейса в Экспортере:

```mermaid
sequenceDiagram
    participant ExportSvc as ExportService (IExportService)
    participant TcSvc as TestCaseService (ITestCaseService Allure)
    participant Client as Allure Client (IClient)
    participant StepSvc as StepService (IStepService Allure)
    participant AttachSvc as AttachmentService (IAttachmentService Allure)
    participant Model as Models.TestCase

    ExportSvc->>TcSvc: ConvertTestCases(projectId, ...)
    TcSvc->>Client: GetTestCaseIdsFromSuite(projectId, sectionId)
    Client-->>TcSvc: Список ID тест-кейсов Allure
    loop Для каждого ID
        TcSvc->>Client: GetTestCaseById(testCaseId)
        Client-->>TcSvc: 'Сырые' данные кейса Allure (allureRawTestCase)
        TcSvc->>StepSvc: ConvertStepsForTestCase(testCaseId)
        StepSvc-->>TcSvc: Список шагов (List<Models.Step>)
        TcSvc->>AttachSvc: DownloadAttachmentsforTestCase(testCaseId, newGuid)
        AttachSvc-->>TcSvc: Список имен вложений (List<string>)
        TcSvc->>TcSvc: Маппинг полей (Name, Priority, итд.)
        TcSvc->>Model: new TestCase(...)
        TcSvc-->>ExportSvc: Добавить TestCase в результат
    end
    ExportSvc-->>: Список List<Models.TestCase> готов
```

Эта диаграмма показывает, что `TestCaseService` активно взаимодействует с другими сервисами ([`IClient`](06_клиент_api_экспортера__iclient__.md), `IStepService`, `IAttachmentService`) для получения и обработки всех частей тест-кейса перед тем, как собрать финальный объект `Models.TestCase`.

## Реализация в Импортере: Краткий Обзор

Как мы упоминали, `ITestCaseService` в Импортере выполняет обратную задачу.

```csharp
// Файл: Migrators/Importer/Services/Implementations/TestCaseService.cs
// (Очень Упрощенно)
internal class TestCaseService : ITestCaseService
{
    // ... поля для ILogger, IClientAdapter, IParserService, IParameterService, IAttachmentService ...

    public async Task ImportTestCases(Guid projectId, IEnumerable<Guid> testCaseGuids, /*...карты...*/ )
    {
        foreach (var testCaseGuid in testCaseGuids)
        {
            // 1. Получаем данные из JSON с помощью IParserService
            var modelTestCase = await parserService.GetTestCase(testCaseGuid);

            // 2. Конвертируем из Models.TestCase в модель для API Test IT (TmsTestCase)
            var tmsTestCase = ConvertToTmsModel(modelTestCase, /*...карты...*/);

            // 3. Обрабатываем вложения (получаем их ID в Test IT)
            var attachmentIdsMap = await attachmentService.GetAttachments(testCaseGuid, modelTestCase.Attachments);
            tmsTestCase.Attachments = attachmentIdsMap.Select(a => a.Value.ToString()).ToList();

            // 4. Обрабатываем параметры и итерации (если есть)
            // ... вызов parameterService ...

            // 5. Вставляем ссылки на вложения и параметры в шаги
            tmsTestCase.Steps = AddAttachmentsAndParametersToSteps(tmsTestCase.Steps, attachmentIdsMap, /*...параметры...*/);

            // 6. Отправляем готовый tmsTestCase в Test IT через IClientAdapter
            await clientAdapter.ImportTestCase(projectId, tmsTestCase.SectionId /*<- ID секции в Test IT!*/, tmsTestCase);
        }
    }

    // Вспомогательный метод для конвертации из Models в формат Test IT API
    private TmsTestCase ConvertToTmsModel(Models.TestCase modelTestCase, /*...карты...*/){}
    // ... другие вспомогательные методы ...
}
```

Ключевые отличия Импортера:
*   **Источник данных:** Не API исходной TMS, а JSON-файлы, читаемые через [`IParserService`](04_запись_чтение_json__iwriteservice__iparserservice__.md).
*   **Направление конвертации:** `Models.TestCase` -> Модель для API целевой TMS.
*   **Результат:** Не возврат данных, а вызов [`IClientAdapter`](07_адаптер_api_импортера__iclientadapter__.md) для отправки данных в целевую TMS.

## Заключение

В этой главе мы познакомились с `ITestCaseService` — ключевым компонентом, отвечающим за "перевод" данных тест-кейсов между специфическим форматом TMS и нашими [Общими Моделями (`Models`)](01_общие_модели_данных__models_project__.md).

*   `ITestCaseService` действует как **специализированный конвертер** для тест-кейсов.
*   В **Экспортере** он преобразует данные из формата исходной TMS в `Models.TestCase`, используя [`IClient`](06_клиент_api_экспортера__iclient__.md) для получения данных и другие сервисы (IStepService, IAttachmentService) для обработки сложных частей.
*   В **Импортере** он читает `Models.TestCase` из JSON (через [`IParserService`](04_запись_чтение_json__iwriteservice__iparserservice__.md)), преобразует их в формат целевой TMS и отправляет через [`IClientAdapter`](07_адаптер_api_импортера__iclientadapter__.md).
*   Каждая TMS требует своей **уникальной реализации** `ITestCaseService` из-за различий в хранении данных.

Мы увидели, что для получения "сырых" данных из исходной TMS Экспортеру нужен специальный инструмент для взаимодействия с ее API. Что это за инструмент?

**Следующая глава:** [Клиент API Экспортера (IClient)](06_клиент_api_экспортера__iclient__.md)

---

Generated by [AI Codebase Knowledge Builder](https://github.com/The-Pocket/Tutorial-Codebase-Knowledge)