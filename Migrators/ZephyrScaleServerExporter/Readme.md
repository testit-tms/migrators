# Zephyr Scale Server Exporter

You can use this exporter to export your test cases from Zephyr Scale Server.

## Download

You can download the latest version of the ZephyrScaleServerExporter from the [releases](https://github.com/testit-tms/migrators/releases/latest) page.


## Feature: exporting archived

these testcases will have 'Archived' tag

To enable archived export add this line to your configuration

```json
{
  "zephyr": {
    "exportArchived": true
  }
}
```


## Feature: ignore filter

You can specify "ignoreFilter": true (by default - false)
to export without filtering these statuses:

```csharp
private readonly List<string> _ignoredStatuses = [];
```

```json
{
  "zephyr": {
    "ignoreFilter": true
  }
}
```

## Feature: partial export

There are 3 new parameters in the configuration:

```json
{
  "zephyr": {
    "partial": true,
    "partialFolderName": "batch",
    "count": 1000
  }
}
```

Where:
```
"partial": true,              // set partial export
"partialFolderName": "batch", // folder prefix
"count": 1000                 // test cases per one batch
```

## Merge functionality for batches

```json
{
  "zephyr": {
    "merge": true
  }
}
```

It will take in account `resultPath` and `projectKey`

## Mapping for attribute "Состояние":

```json
{
  "mappings": [
    {
      "name": "Состояние",
      "values": [
        {
          "value": "Да",
          "mappingFile": "mappings/Да.txt"
        },
        {
          "value": "Нет",
          "mappingFile": "mappings/Нет.txt"
        }
      ]
    }
  ]
}
```

And folder mappings with:

Да.txt:
```txt
Первое_Значение
Второе_Значение
Определенно
И_Так_Далее
```

Нет.txt
```txt
Первое_Значение
Второе_Значение
Определенно
И_Так_Далее
```

## How to use

1. Configure connection in the `zephyr.config.json` file and save it in the ZephyrScaleServerExporter location.

Note: there are new items for confluence API usage:
  1. `"confluence"` for base confluence url,
  2. `"confluenceToken", "confluenceLogin", "confluencePassword"` for authrorization.

```json
{
  "resultPath": "/Users/user01/Documents/importer",
  "zephyr": {
    "url": "https://jira.instance.ru",
    "confluence": "https://confluence.instance.ru",
    "token": "MDc2MjIxNjVzNjg40OkJCA43J4AfsIRBXomRs8bKw81+D",
    "confluenceToken": "MDc2MjIxNjVzNjg40OkJCA43J4AfsIRBXomRs8bKw81+D",
    "projectKey": "PK"
  }
}
```

or

```json
{
  "resultPath" : "/Users/user01/Documents/importer",
  "zephyr": {
    "url": "https://jira.instance.ru",
    "confluence": "https://confluence.instance.ru",
    "login": "User",
    "password": "pass",
    "confluenceLogin": "User",
    "confluencePassword": "pass",
    "projectKey": "PK"
  }
}
```

Where:

- resultPath - path to the folder where the results will be saved
- zephyr.url - url to the Zephyr server with organization name
- zephyr.confluence - url to the Confluence server
- zephyr.token - key for access to the Jira server
- zephyr.login - login to the Jira server
- zephyr.password - password to the Jira server
- zephyr.confluenceToken - key for accessing Confluence API
- zephyr.confluenceLogin - login to the Confluence server
- zephyr.confluencePassword - password to the Confluence server
- zephyr.projectKey - key of the project in the Zephyr server

1. Run the exporter with the following command:

```bash
sudo chmod +x .\ZephyrScaleServerExporter
.\ZephyrScaleServerExporter
```

3. Check the results in the folder specified in the `resultPath` parameter.

4. Use the results in the [importer](https://github.com/testit-tms/migrators/tree/main/Migrators/Importer/Readme.md).

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
