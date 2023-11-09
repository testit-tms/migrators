# TestCollab Exporter

You can use this exporter to export your test cases from TestCollab.

## Download

You can download the latest version of the TestCollabExporter from the [releases](https://github.com/testit-tms/migrators/releases/latest) page.

## How to use

1. Configure connection in the `testcollab.config.json` file and save it in the TestCollabExporter location.

```json
{
  "resultPath" : "/Users/user01/Documents/importer/testcollab",
  "testcollab" : {
    "url" : "https://api.testcollab.io/",
    "token" : "bPM3ji6XEw64fUHq",
    "projectKey" : "Sample Project"
  }
}
```

Where:

- resultPath - path to the folder where the results will be saved
- testcollab.url - url to the TestCollab server with organization name
- testcollab.token - token for access to the TestCollab server
- testcollab.projectName - name of the project in the TestCollab server

1. Run the exporter with the following command:

```bash
.\TestCollabExporter
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
