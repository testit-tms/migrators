# DOQA Exporter

You can use this exporter to export your test cases from [DOQA TMS](https://doqa.app).

## Download

You can download the latest version of the DoqaExporter from the [releases](https://github.com/testit-tms/migrators/releases/latest) page.

## Features

- Full test case export: title, description, steps, preconditions, priority, status
- Checklists exported as test cases (tag `checklist`)
- Folder structure → Test IT sections (with nesting)
- Inline base64 images → extracted as attachment files
- File attachments → downloaded from DOQA CDN
- Link extraction: Yandex Tracker (Issue), Figma, Google Docs, Wiki (Related)
- HTML → clean text conversion with proper line breaks
- Rate limiting to avoid DOQA API throttling

## How to use

1. Configure connection in the `doqa.config.json` file and save it in the DoqaExporter location.

```json
{
  "resultPath": "/Users/user01/Documents/importer",
  "doqa": {
    "url": "https://your-workspace.doqa.app",
    "email": "user@example.com",
    "password": "your_password",
    "spaceId": 1
  }
}
```

Where:

- `resultPath` — path to the folder where the results will be saved
- `doqa.url` — DOQA workspace URL
- `doqa.email` — login email
- `doqa.password` — login password
- `doqa.spaceId` — workspace ID to export (default: `1`)

2. Run the exporter:

```bash
sudo chmod +x ./DoqaExporter
./DoqaExporter
```

3. Check the results in the folder specified in the `resultPath` parameter.

4. Use the results in the [importer](https://github.com/testit-tms/project-importer).

## DOQA API

This exporter uses the following DOQA API endpoints (discovered via browser network inspection):

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Authentication (email/password → JWT) |
| GET | `/api/config/get` | Configuration (CDN URL for attachments) |
| GET | `/api/projects` | List projects and spaces |
| GET | `/api/folders/space/{id}/case` | Folder structure |
| POST | `/api/cases/list` | List cases in folder (recursive) |
| GET | `/api/cases/{id}` | Full test case details |
| POST | `/api/checklists/list` | List checklists |
| GET | `/api/checklists/{id}` | Full checklist details |

## Output structure

```
<resultPath>/
├── main.json
├── {GUID-1}/
│   ├── testcase.json
│   ├── step1_action_1.png
│   └── report.xlsx
├── {GUID-2}/
│   └── testcase.json
└── ...
```

## Contributing

You can help to develop the project. Any contributions are **greatly appreciated**.

- If you have suggestions for adding or removing projects, feel free
  to [open an issue](https://github.com/testit-tms/migrators/issues/new) to discuss it, or create a direct pull
  request after you edit the *README.md* file with necessary changes.
- Make sure to check your spelling and grammar.
- Create individual PR for each suggestion.
- Read the [Code Of Conduct](https://github.com/testit-tms/migrators/blob/main/CODE_OF_CONDUCT.md) before posting
  your first idea as well.

## License

Distributed under the Apache-2.0 License.
See [LICENSE](https://github.com/testit-tms/migrators/blob/main/LICENSE) for more information.
