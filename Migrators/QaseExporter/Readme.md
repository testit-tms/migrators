# Qase Exporter

You can use this exporter to export your test cases from Qase.

## Download

You can download the latest version of the QaseExporter from the [releases](https://github.com/testit-tms/migrators/releases/latest) page.

## How to use

1. Configure connection in the `qase.config.json` file and save it in the QaseExporter location.

```json
{
  "resultPath": "/Users/user01/Documents/importer",
  "qase": {
    "url": "http://api.qase.io/",
    "token": "e62b232d32e69d82h53e8f0211209ccf652efdccddf3d1fb11ef764ghi998a4c",
    "projectKey": "KEY"
  }
}
```

Where:

- resultPath - path to the folder where the results will be saved
- qase.url - url to the Qase API
- qase.token - key for access to the Qase Cloud
- qase.projectKey - key of the project in the Qase Cloud

1. Run the exporter with the following command:

```bash
sudo chmod +x .\QaseExporter
.\QaseExporter
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
