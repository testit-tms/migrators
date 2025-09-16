# TestRail Exporter

You can use this exporter to export your test cases from TestRail.

## Download

You can download the latest version of the TestRailExporter from the [releases](https://github.com/testit-tms/migrators/releases/latest) page.

## How to use

1. Configure paths in the `testrail.config.json` file and save it in the TestRailExporter location.

```json
{
  "resultPath": "/Users/user01/Documents/importer",
  "testrail": {
    "url": "https://instance.testrail.io/",
    "login": "User",
    "password": "pass",
    "projectName": "ProjectName"
  }
}
```

Where:

- resultPath - path to the folder where the results will be saved
- testrail.url - url to the TestRail server
- testrail.login - login to the TestRail server
- testrail.password - password to the TestRail server
- testrail.projectName - key of the project in the TestRail server

1. Run the exporter with the following command:

```bash
sudo chmod +x .\TestRailExporter
.\TestRailExporter
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
