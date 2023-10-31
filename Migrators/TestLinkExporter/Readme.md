# TestLink Exporter

You can use this exporter to export your test cases from TestLink.

## Download

You can download the latest version of the TestLinkExporter from the [releases](https://github.com/testit-tms/migrators/releases/latest) page.

## How to use

1. Configure connection in the `testlink.config.json` file and save it in the TestLinkExporter location.

```json
{
  "resultPath": "/Users/user01/Documents/importer",
  "testlink": {
    "url": "http://testlink/lib/api/xmlrpc/v1/xmlrpc.php",
    "token": "15fb632cdd8b606561a8b60d69a7149e",
    "projectName": "ProjectName"
  }
}
```

Where:

- resultPath - path to the folder where the results will be saved
- testlink.url - TestLink API url
- testlink.token - token for access to the TestLink
- testlink.projectName - name of the project in the TestLink

1. Run the exporter with the following command:

```bash
.\TestLinkExporter
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
