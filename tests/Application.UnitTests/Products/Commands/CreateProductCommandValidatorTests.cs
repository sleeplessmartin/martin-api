using FluentAssertions;
using FluentValidation.TestHelper;
using ProductsApi.Application.Products.Commands.CreateProduct;
using Xunit;

namespace Application.UnitTests.Products.Commands;

public sealed class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesAllRules()
    {
        var cmd = new CreateProductCommand("Widget", "A great product", 9.99m, "USD");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyName_FailsNameRule(string name)
    {
        var cmd = new CreateProductCommand(name, "Desc", 10m, "USD");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceeds200Chars_FailsNameRule()
    {
        var cmd = new CreateProductCommand(new string('x', 201), "Desc", 10m, "USD");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NegativePrice_FailsPriceRule()
    {
        var cmd = new CreateProductCommand("Widget", "Desc", -0.01m, "USD");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("123")]
    [InlineData("")]
    public void Validate_InvalidCurrency_FailsCurrencyRule(string currency)
    {
        var cmd = new CreateProductCommand("Widget", "Desc", 10m, currency);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void Validate_ZeroPrice_PassesPriceRule()
    {
        var cmd = new CreateProductCommand("Widget", "Desc", 0m, "USD");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.Price);
    }
}
