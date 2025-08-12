using Models;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models.Client;
using ZephyrScaleServerExporter.Models.Common;

namespace ZephyrScaleServerExporter.Services.Implementations;

internal class ParameterService(
    IDetailedLogService detailedLogService,
    IClient client) 
    : IParameterService
{
    public async Task<List<Iteration>> ConvertParameters(string testCaseKey)
    {
        detailedLogService.LogInformation("Converting parameters");

        var parametersData = await client.GetParametersByTestCaseKey(testCaseKey);

        return parametersData.Type switch
        {
            ZephyrParameterType.TEST_DATA => ConvertParametersWithTestDataType(parametersData.TestData!),
            ZephyrParameterType.PARAMETER => ConvertParametersWithParameterType(parametersData.Parameters!),
            _ => []
        };
    }

    public List<Iteration> MergeIterations(List<Iteration> mainIterations, List<Iteration> subIterations)
    {
        detailedLogService.LogInformation("Merging parameters:\nMain: {@MainParameters}\n Sub: {@SubParameters}",
            mainIterations, subIterations);

        foreach (var mainIteration in mainIterations)
        {
            var noConflictingIterationParametersList = subIterations
                .Select(subIteration => subIteration.Parameters
                    .Where(subp => mainIteration.Parameters
                        .Find(mainp => subp.Name == mainp.Name) == null));
            
            foreach (var noConflictingIterationParameters in noConflictingIterationParametersList)
            {
                mainIteration.Parameters.AddRange(noConflictingIterationParameters);
            }
        }

        detailedLogService.LogInformation("Merged parameters: {@Parameters}", mainIterations);

        return mainIterations;
    }

    private List<Iteration> ConvertParametersWithTestDataType(List<Dictionary<string, ZephyrDataParameter>> zephyrTestData)
    {
        var iterations = new List<Iteration>();

        foreach (var zephyrDataParameters in zephyrTestData)
        {
            var iteration = new Iteration
            {
                Parameters = []
            };

            foreach (var name in zephyrDataParameters.Keys)
            {
                iteration.Parameters.Add(
                    new Parameter
                    {
                        Name = name,
                        Value = zephyrDataParameters[name].Value!
                    });
            }

            iterations.Add(iteration);
        }

        detailedLogService.LogInformation("Converted parameters: {@Parameters}", iterations);

        return iterations;
    }

    private List<Iteration> ConvertParametersWithParameterType(List<ZephyrParameter> zephyrParameters)
    {
        var parameters = zephyrParameters.Select(zephyrParameter => 
            new Parameter { Name = zephyrParameter.Name!, Value = zephyrParameter.Value! }).ToList();


        detailedLogService.LogInformation("Converted parameters: {@Parameters}", parameters);

        return
        [
            new Iteration
            {
                Parameters = parameters
            }
        ];
    }
}
