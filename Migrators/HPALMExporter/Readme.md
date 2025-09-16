# HP ALM Exporter

You can use this exporter to export your test cases from HP ALM.

## Download

You can download the latest version of the HPALMExporter from the [releases](https://github.com/testit-tms/migrators/releases/latest) page.

## How to use

1. Configure connection in the `hpalm.config.json` file and save it in the HPALMExporter location.

```json
{
  "resultPath": "/Users/user01/Documents/importer/hpalm",
  "hpalm": {
    "url": "http://alm.server.ru:8080",
    "clientId": "apikey-egaoktntfhidsjoddmcp",
    "secret": "dpojbgifjifbjplf",
    "domainName": "DEFAULT",
    "projectName": "Migration"
  }
}
```

Where:

- resultPath - path to the folder where the results will be saved
- hpalm.url - url to the HP ALM server with organization name
- hpalm.clientId - clientId for access to the HP ALM server
- hpalm.secret - secret for access to the HP ALM server
- hpalm.domainName - name of the domain in the HP ALM server
- hpalm.projectName - name of the project in the HP ALM server

1. Run the exporter with the following command:

```bash
sudo chmod +x .\HPALMExporter
.\HPALMExporter
```

3. Check the results in the folder specified in the `resultPath` parameter.

4. Use the results in the [importer](https://github.com/testit-tms/project-importer).

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
