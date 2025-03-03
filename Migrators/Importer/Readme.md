# Importer

You can use this Importer to import your test cases to Test IT.

## Download

You can download the latest version of the Importer from
the [releases](https://github.com/testit-tms/migrators/releases/latest) page.

## How to use

1. Configure connection in the `tms.config.json` file and save it in the Importer location.

```json
{
    "resultPath" : "/Users/user01/Documents/importer",
    "tms" : {
        "url" : "https://testit.software/",
        "privateToken" : "cmZzWDkYTfBvNvVMcXhzN3Vy",
        "certValidation" : true,
        "importToExistingProject" : false,
        "projectName" : "CustomProjectName",
        "timeout": 600
    }
}
```

Where:

- resultPath - path to the folder where the results will be saved
- tms.url - url to the Test IT server
- tms.privateToken - token for access to the Test IT server
- tms.certValidation - enable/disable certificate validation (Default value - true)
- tms.importToExistingProject - enable/disable import to existing project in the Test IT server (Default value - false)
- tms.projectName - custom name of the project in the Test IT server (Default value - name of the project in the export
  system)
- tms.timeout - timeout for clients in seconds, default - 10 minutes (600)

2. Run the Importer with the following command:

```bash
sudo chmod +x .\Importer
.\Importer
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
