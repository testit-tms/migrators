using Importer.Client;
using Importer.Models;
using Importer.Services.Implementations;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Attribute = Models.Attribute;

namespace ImporterTests;

public class AttributeServiceTests
{
    private ILogger<AttributeService> _logger = null!;
    private IClientAdapter _clientAdapter = null!;
    private AttributeService _attributeService = null!;

    private static readonly Guid ProjectId = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbee7");
    private static readonly Guid AttributeId1 = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbee6");
    private static readonly Guid AttributeId2 = Guid.Parse("9767ce0e-a214-4ebc-af69-71aa88b0ad0d");
    private static readonly Guid TmsAttributeId1 = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbe10");
    private static readonly Guid TmsAttributeId2 = Guid.Parse("9767ce0e-a214-4ebc-af69-71aa88b0ad10");

    private List<Attribute> _attributes = null!;
    private List<TmsAttribute> _tmsAttributes = null!;
    private Dictionary<Guid, TmsAttribute> _attributesMap = null!;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AttributeService>>();
        _clientAdapter = Substitute.For<IClientAdapter>();

        InitializeTestData();

        _attributeService = new AttributeService(_logger, _clientAdapter);
    }

    private void InitializeTestData()
    {
        _attributes = new List<Attribute>
        {
            new()
            {
                Id = AttributeId1,
                Name = "TestAttribute",
                IsActive = true,
                IsRequired = false,
                Type = AttributeType.String,
                Options = []
            },
            new()
            {
                Id = AttributeId2,
                Name = "TestAttribute2",
                IsActive = true,
                IsRequired = false,
                Type = AttributeType.Options,
                Options = ["Option1", "Option2"]
            }
        };

        _tmsAttributes = new List<TmsAttribute>
        {
            new()
            {
                Id = TmsAttributeId1,
                Name = "TestAttribute",
                IsRequired = false,
                IsEnabled = true,
                Type = "String",
                Options = []
            },
            new()
            {
                Id = TmsAttributeId2,
                Name = "TestAttribute2",
                IsRequired = false,
                IsEnabled = true,
                Type = "Options",
                Options = new List<TmsAttributeOptions>
                {
                    new() { Id = Guid.NewGuid(), Value = "Option1", IsDefault = true },
                    new() { Id = Guid.NewGuid(), Value = "Option2", IsDefault = false }
                }
            }
        };

        _attributesMap = new Dictionary<Guid, TmsAttribute>
        {
            { AttributeId1, _tmsAttributes[0] },
            { AttributeId2, _tmsAttributes[1] }
        };
    }

    [Test]
    public async Task ImportAttributes_WhenGetProjectAttributesFails_ThrowsException()
    {
        // Arrange
        _clientAdapter.GetProjectAttributes()
            .ThrowsAsync(new Exception("Failed to get project attributes"));
        _clientAdapter.GetRequiredProjectAttributesByProjectId(ProjectId)
            .Returns(new List<TmsAttribute>());

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(
            async () => await _attributeService.ImportAttributes(ProjectId, _attributes));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Is.EqualTo("Failed to get project attributes"));
            _clientAdapter.DidNotReceive().ImportAttribute(Arg.Any<Attribute>());
            _clientAdapter.DidNotReceive().UpdateAttribute(Arg.Any<TmsAttribute>());
            _clientAdapter.DidNotReceive().AddAttributesToProject(ProjectId, Arg.Any<IEnumerable<Guid>>());
        });
    }

    [Test]
    public async Task ImportAttributes_WhenImportAttributeFails_ThrowsException()
    {
        // Arrange
        _clientAdapter.GetProjectAttributes().Returns(new List<TmsAttribute>());
        _clientAdapter.GetRequiredProjectAttributesByProjectId(ProjectId).Returns(new List<TmsAttribute>());
        _clientAdapter.ImportAttribute(_attributes[0])
            .ThrowsAsync(new Exception("Failed to import attribute"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(
            async () => await _attributeService.ImportAttributes(ProjectId, _attributes));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Is.EqualTo("Failed to import attribute"));
            _clientAdapter.Received(1).ImportAttribute(_attributes[0]);
            _clientAdapter.DidNotReceive().ImportAttribute(_attributes[1]);
            _clientAdapter.DidNotReceive().UpdateAttribute(Arg.Any<TmsAttribute>());
            _clientAdapter.DidNotReceive().AddAttributesToProject(ProjectId, Arg.Any<IEnumerable<Guid>>());
        });
    }

    [Test]
    public async Task ImportAttributes_WhenUpdateAttributeFails_ThrowsException()
    {
        // Arrange
        _clientAdapter.GetProjectAttributes().Returns(new List<TmsAttribute> { _tmsAttributes[1] });
        _clientAdapter.GetRequiredProjectAttributesByProjectId(ProjectId).Returns(new List<TmsAttribute>());
        _clientAdapter.UpdateAttribute(_tmsAttributes[1])
            .ThrowsAsync(new Exception("Failed to update attribute"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(
            async () => await _attributeService.ImportAttributes(ProjectId, new[] { _attributes[1] }));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Is.EqualTo("Failed to update attribute"));
            _clientAdapter.DidNotReceive().ImportAttribute(Arg.Any<Attribute>());
            _clientAdapter.DidNotReceive().AddAttributesToProject(ProjectId, Arg.Any<IEnumerable<Guid>>());
        });
    }

    [Test]
    public async Task ImportAttributes_WhenUpdatingExistingAttribute_Success()
    {
        // Arrange
        _clientAdapter.GetProjectAttributes().Returns(new List<TmsAttribute> { _tmsAttributes[1] });
        _clientAdapter.GetRequiredProjectAttributesByProjectId(ProjectId).Returns(new List<TmsAttribute>());
        _clientAdapter.UpdateAttribute(Arg.Any<TmsAttribute>()).Returns(_tmsAttributes[1]);
        _clientAdapter.GetProjectAttributeById(_tmsAttributes[1].Id).Returns(_tmsAttributes[1]);

        // Act
        var result = await _attributeService.ImportAttributes(ProjectId, new[] { _attributes[1] });

        // Assert
        Assert.Multiple(() =>
        {
            _clientAdapter.DidNotReceive().ImportAttribute(Arg.Any<Attribute>());
            _clientAdapter.Received().AddAttributesToProject(ProjectId, Arg.Any<IEnumerable<Guid>>());

            Assert.That(result, Is.Not.Null, "Result dictionary should not be null");
            Assert.That(result.Count, Is.EqualTo(1), "Dictionary should contain exactly one element");
            Assert.That(result.ContainsKey(_attributes[1].Id), Is.True, "Dictionary should contain the correct attribute ID");

            var resultAttribute = result[_attributes[1].Id];
            Assert.That(resultAttribute, Is.Not.Null, "Result attribute should not be null");

            var expectedAttribute = _attributesMap[_attributes[1].Id];
            Assert.That(expectedAttribute, Is.Not.Null, "Expected attribute should not be null");

            Assert.That(resultAttribute.Id, Is.EqualTo(expectedAttribute.Id), "Attribute IDs should match");
            Assert.That(resultAttribute.Name, Is.EqualTo(expectedAttribute.Name), "Attribute names should match");
            Assert.That(resultAttribute.Type, Is.EqualTo(expectedAttribute.Type), "Attribute types should match");
            Assert.That(resultAttribute.IsRequired, Is.EqualTo(expectedAttribute.IsRequired), "IsRequired should match");
            Assert.That(resultAttribute.IsEnabled, Is.EqualTo(expectedAttribute.IsEnabled), "IsEnabled should match");

            // Compare options
            Assert.That(resultAttribute.Options, Is.Not.Null, "Options list should not be null");
            Assert.That(resultAttribute.Options.Count, Is.EqualTo(expectedAttribute.Options.Count), "Options count should match");
            for (var i = 0; i < resultAttribute.Options.Count; i++)
            {
                Assert.That(resultAttribute.Options[i], Is.Not.Null, $"Option {i} should not be null");
                Assert.That(resultAttribute.Options[i].Value, Is.EqualTo(expectedAttribute.Options[i].Value), $"Option {i} value should match");
                Assert.That(resultAttribute.Options[i].IsDefault, Is.EqualTo(expectedAttribute.Options[i].IsDefault), $"Option {i} IsDefault should match");
            }
        });
    }

    [Test]
    public async Task ImportAttributes_WhenImportingNewAttributes_Success()
    {
        // Arrange
        _clientAdapter.GetProjectAttributes().Returns(new List<TmsAttribute>());
        _clientAdapter.GetRequiredProjectAttributesByProjectId(ProjectId).Returns(new List<TmsAttribute>());
        _clientAdapter.ImportAttribute(_attributes[1]).Returns(_attributesMap[_attributes[1].Id]);
        _clientAdapter.GetAttribute(_attributesMap[_attributes[1].Id].Id).Returns(_attributesMap[_attributes[1].Id]);
        _clientAdapter.ImportAttribute(_attributes[0]).Returns(_attributesMap[_attributes[0].Id]);
        _clientAdapter.GetAttribute(_attributesMap[_attributes[0].Id].Id).Returns(_attributesMap[_attributes[0].Id]);

        // Act
        var result = await _attributeService.ImportAttributes(ProjectId, _attributes);

        // Assert
        Assert.Multiple(() =>
        {
            _clientAdapter.DidNotReceive().UpdateAttribute(Arg.Any<TmsAttribute>());
            _clientAdapter.Received().AddAttributesToProject(ProjectId, Arg.Any<IEnumerable<Guid>>());
            Assert.That(result, Is.EqualTo(_attributesMap));
        });
    }

    [Test]
    public async Task ImportAttributes_WhenImportingAttributeWithNewName_Success()
    {
        // Arrange
        var existingAttributes = new List<TmsAttribute> { _tmsAttributes[0] };
        existingAttributes[0].Type = "data";
        _clientAdapter.GetProjectAttributes().Returns(existingAttributes);
        _clientAdapter.GetRequiredProjectAttributesByProjectId(ProjectId).Returns(new List<TmsAttribute>());

        var newAttribute = new Attribute
        {
            Id = _attributes[0].Id,
            Name = "TestAttribute (1)",
            IsActive = _attributes[0].IsActive,
            IsRequired = _attributes[0].IsRequired,
            Type = _attributes[0].Type,
            Options = _attributes[0].Options
        };

        var expectedTmsAttribute = new TmsAttribute
        {
            Id = _tmsAttributes[0].Id,
            Name = "TestAttribute (1)",
            IsRequired = _tmsAttributes[0].IsRequired,
            IsEnabled = _tmsAttributes[0].IsEnabled,
            Type = _tmsAttributes[0].Type,
            Options = _tmsAttributes[0].Options
        };

        // Setup mock for original attribute first
        _clientAdapter.ImportAttribute(_attributes[0]).Returns(expectedTmsAttribute);
        _clientAdapter.GetAttribute(expectedTmsAttribute.Id).Returns(expectedTmsAttribute);

        // Then setup mock for renamed attribute
        _clientAdapter.ImportAttribute(newAttribute).Returns(expectedTmsAttribute);
        _clientAdapter.GetAttribute(expectedTmsAttribute.Id).Returns(expectedTmsAttribute);
        _clientAdapter.GetProjectAttributeById(expectedTmsAttribute.Id).Returns(expectedTmsAttribute);

        // Act
        var result = await _attributeService.ImportAttributes(ProjectId, new[] { _attributes[0] });

        // Assert
        var expectedMap = new Dictionary<Guid, TmsAttribute> { { _attributes[0].Id, expectedTmsAttribute } };

        Assert.Multiple(() =>
        {
            _clientAdapter.DidNotReceive().UpdateAttribute(Arg.Any<TmsAttribute>());
            _clientAdapter.Received().AddAttributesToProject(ProjectId, Arg.Any<IEnumerable<Guid>>());
            Assert.That(result, Is.EqualTo(expectedMap));
        });
    }
}
