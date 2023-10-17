# Zephyr Squad Exporter

You can use this exporter to export your test cases from Zephyr Squad.

## Download

You can download the latest version of the ZephyrSquadExporter from the [releases](https://github.com/testit-tms/migrators/releases/latest) page.

## How to use

1. Configure connection in the `zephyr.config.json` file and save it in the ZephyrSquadExporter location.

```json
{
  "resultPath" : "/Users/user01/Documents/importer",
  "zephyr" : {
    "url" : "https://prod-api.zephyr4jiracloud.com/",
    "accessKey" : "MmEwMjd4OWYtNGMxMC0zYjhkLWExMmUtNTZjYmY0OTE0MGExIDcxMjAyMCUzQTNjOWYwMzJkLWVlZWEtNGRjMC04NjIyLTliMGY2ODQzZWMzNCBVU0VSX0RFRkFVTFRfTkFNRQ",
    "secretKey" : "GkxCBle-7ib1mBOf9Oeiy8HZjJDw4ESnTZzYh3rz1Og",
    "accountId" : "712021:3c9f032d-eeea-4dc0-8622-9b0f6843ec34",
    "projectId" : 10000,
    "projectName": "ProjectName"
  }
}
```

Where:

- resultPath - path to the folder where the results will be saved
- zephyr.url - url to the Zephyr server with organization name
- zephyr.accessKey - key for access to the Zephyr server
- zephyr.secretKey - [secret key](https://support.smartbear.com/zephyr-squad-cloud/docs/api/api-keys.html) for access to the Zephyr server
- zephyr.accountId - account id for access to the Zephyr server
- zephyr.projectId - id of the project in the Zephyr server
- zephyr.projectName - name of the project in the Zephyr server

1. Run the exporter with the following command:

```bash
.\ZephyrSquadExporter
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
