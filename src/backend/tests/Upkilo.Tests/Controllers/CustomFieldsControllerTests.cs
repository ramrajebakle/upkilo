using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Upkilo.API.Controllers;
using Upkilo.Core.Entities;
using Upkilo.Tests.Helpers;
using MockFactory = Upkilo.Tests.Helpers.MockFactory;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace Upkilo.Tests.Controllers;

/// <summary>
/// Unit tests for CustomFieldsController.
/// Covers definitions CRUD, contact values retrieval, setting, and cross-tenant isolation.
/// </summary>
public class CustomFieldsControllerTests : ControllerTestBase
{
    private readonly CustomFieldsController _sut;

    public CustomFieldsControllerTests()
    {
        var logger = MockFactory.CreateLogger<CustomFieldsController>();
        _sut = new CustomFieldsController(logger.Object, Context, TenantProvider.Object);
        WithAuth(_sut);
    }

    [Fact]
    public async Task GetDefinitions_ReturnsOk_FilteredByEntityType()
    {
        // Arrange
        var def1 = new CustomFieldDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Name = "custom_text",
            Label = "Custom Text",
            FieldType = CustomFieldType.Text,
            EntityType = "Contact",
            TargetEntity = "Contact",
            SortOrder = 1,
            IsActive = true
        };
        var def2 = new CustomFieldDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Name = "custom_num",
            Label = "Custom Number",
            FieldType = CustomFieldType.Number,
            EntityType = "Booking",
            TargetEntity = "Booking",
            SortOrder = 2,
            IsActive = true
        };

        Context.CustomFieldDefinitions.AddRange(def1, def2);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetDefinitions("contacts"); // Plural entity type to test normalization

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateDefinition_ValidRequest_CreatesDefinition()
    {
        // Arrange
        var request = new CreateCustomFieldRequest
        {
            Name = "My New Field",
            Label = "My New Field Label",
            FieldType = CustomFieldType.Text,
            EntityType = "contacts",
            IsRequired = true,
            IsSearchable = true
        };

        // Act
        var result = await _sut.CreateDefinition(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        
        var definition = await Context.CustomFieldDefinitions
            .FirstOrDefaultAsync(d => d.TenantId == TenantId && d.Label == "My New Field Label");
        definition.Should().NotBeNull();
        definition!.Name.Should().Be("my_new_field");
        definition.EntityType.Should().Be("Contact"); // normalized
    }

    [Fact]
    public async Task UpdateDefinition_ValidRequest_UpdatesDefinition()
    {
        // Arrange
        var def = new CustomFieldDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Name = "old_field",
            Label = "Old Label",
            FieldType = CustomFieldType.Text,
            EntityType = "Contact",
            TargetEntity = "Contact",
            IsActive = true
        };
        Context.CustomFieldDefinitions.Add(def);
        await Context.SaveChangesAsync();

        var request = new UpdateCustomFieldRequest
        {
            Label = "New Label",
            IsRequired = true
        };

        // Act
        var result = await _sut.UpdateDefinition(def.Id, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var updated = await Context.CustomFieldDefinitions.FindAsync(def.Id);
        updated!.Label.Should().Be("New Label");
        updated.IsRequired.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDefinition_ValidId_SoftDeletes()
    {
        // Arrange
        var def = new CustomFieldDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Name = "delete_me",
            Label = "Delete Me",
            FieldType = CustomFieldType.Text,
            EntityType = "Contact",
            TargetEntity = "Contact",
            IsActive = true
        };
        Context.CustomFieldDefinitions.Add(def);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteDefinition(def.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        var deleted = await Context.CustomFieldDefinitions.FindAsync(def.Id);
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetContactValues_ValidContact_ReturnsValues()
    {
        // Arrange
        var client = TestFixtures.CreateClient(TenantId);
        Context.Clients.Add(client);

        var def = new CustomFieldDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Name = "contact_notes",
            Label = "Notes",
            FieldType = CustomFieldType.Text,
            EntityType = "Contact",
            TargetEntity = "Contact",
            IsActive = true
        };
        Context.CustomFieldDefinitions.Add(def);

        var value = new CustomFieldValue
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            CustomFieldDefinitionId = def.Id,
            EntityId = client.Id,
            EntityType = "Contact",
            TextValue = "Important client details"
        };
        Context.CustomFieldValues.Add(value);

        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetContactValues(client.Id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SetContactValues_ValidRequest_SetsValues()
    {
        // Arrange
        var client = TestFixtures.CreateClient(TenantId);
        Context.Clients.Add(client);

        var def = new CustomFieldDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Name = "contact_age",
            Label = "Age",
            FieldType = CustomFieldType.Number,
            EntityType = "Contact",
            TargetEntity = "Contact",
            IsActive = true
        };
        Context.CustomFieldDefinitions.Add(def);
        await Context.SaveChangesAsync();

        var request = new SetContactCustomFieldValuesRequest
        {
            Values = new List<CustomFieldValueEntry>
            {
                new()
                {
                    FieldId = def.Id,
                    NumberValue = 30
                }
            }
        };

        // Act
        var result = await _sut.SetContactValues(client.Id, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        var savedVal = await Context.CustomFieldValues
            .FirstOrDefaultAsync(v => v.CustomFieldDefinitionId == def.Id && v.EntityId == client.Id);
        savedVal.Should().NotBeNull();
        savedVal!.NumberValue.Should().Be(30);
    }

    [Fact]
    public async Task CrossTenantIsolation_TenantCannotAccessOtherTenantDefinition()
    {
        // Arrange
        var otherTenantId = Guid.NewGuid();
        var def = new CustomFieldDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = otherTenantId,
            Name = "secret_field",
            Label = "Secret Field",
            FieldType = CustomFieldType.Text,
            EntityType = "Contact",
            TargetEntity = "Contact",
            IsActive = true
        };
        Context.CustomFieldDefinitions.Add(def);
        await Context.SaveChangesAsync();

        // Act & Assert
        // 1. Update definition should return NotFound for other tenant's definition
        var updateResult = await _sut.UpdateDefinition(def.Id, new UpdateCustomFieldRequest { Label = "Hack" });
        updateResult.Should().BeOfType<NotFoundResult>();

        // 2. Delete definition should return NotFound for other tenant's definition
        var deleteResult = await _sut.DeleteDefinition(def.Id);
        deleteResult.Should().BeOfType<NotFoundResult>();
    }
}
