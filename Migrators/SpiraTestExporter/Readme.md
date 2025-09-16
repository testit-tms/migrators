# SpiraTest Exporter

You can use this exporter to export your test cases from SpiraTest.

## Download

You can download the latest version of the SpiraTestExporter from the [releases](https://github.com/testit-tms/migrators/releases/latest) page.

## How to use

1. Configure connection in the `spiratest.config.json` file and save it in the SpiraTestExporter location.

```json
{
  "resultPath" : "/Users/user01/Documents/importer/spiratest",
   "spiraTest" : {
    "url" : "https://demo-eu.spiraservice.net/fl/",
    "username": "administrator",
    "token" : "{620F2362-41CB-4F87-ACDA-0CF223C72737}",
    "projectName" : "Library Information System (Sample)"
  }
}
```

Where:

- resultPath - path to the folder where the results will be saved
- spiraTest.url - url to the SpiraTest server with organization name
- spiraTest.username - username for access to the SpiraTest server
- spiraTest.token - token for access to the SpiraTest server
- spiraTest.projectName - name of the project in the SpiraTest server

1. Run the exporter with the following command:

```bash
sudo chmod +x .\SpiraTestExporter
.\SpiraTestExporter
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
