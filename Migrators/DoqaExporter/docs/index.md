# Tutorial: DoqaExporter

*DoqaExporter* exports test cases and checklists from [DOQA TMS](https://doqa.app)
into the JSON format used by [Test IT project-importer](https://github.com/testit-tms/project-importer).

It authenticates against the DOQA API, walks folder trees, converts HTML content and attachments,
then writes `main.json` and per-case folders via the shared `JsonWriter` library.

```mermaid
flowchart TD
    A0["Program / DI"]
    A1["DoqaClient"]
    A2["ExportService"]
    A3["HtmlProcessor"]
    A4["JsonWriter"]
    A0 --> A2
    A2 --> A1
    A2 --> A3
    A2 --> A4
```

## Chapters

1. [Integration changes](01_интеграция_и_доработки_.md)
