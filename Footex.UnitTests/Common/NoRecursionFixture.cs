using AutoFixture;
using AutoFixture.Xunit2;

namespace Footex.UnitTests.Common;

/// <summary>
/// Custom AutoFixture configuration that prevents circular reference issues
/// </summary>
public class NoRecursionFixture : Fixture
{
    public NoRecursionFixture()
    {
        // Remove the default ThrowingRecursionBehavior that causes circular reference exceptions
        this.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => this.Behaviors.Remove(b));

        // Add OmitOnRecursionBehavior to handle circular references gracefully
        this.Behaviors.Add(new OmitOnRecursionBehavior());
        this.Customizations.Add(new IFormFileSpecimenBuilder());

        // Limit recursion depth as an additional safety measure
        this.RepeatCount = 3;

        // Configure specific customizations for domain models that might cause issues
        ConfigureDomainModelCustomizations();
    }

    private void ConfigureDomainModelCustomizations()
    {
        // Add any specific customizations for domain models here
        // For example, if certain properties cause issues, you can omit them:
        // this.Customize<ApplicationUser>(c => c.Without(x => x.RefreshTokens));
        // this.Customize<Team>(c => c.Without(x => x.Players));
    }
}

/// <summary>
/// AutoData attribute that uses the NoRecursionFixture
/// </summary>
public class NoRecursionAutoDataAttribute : AutoDataAttribute
{
    public NoRecursionAutoDataAttribute()
        : base(() => new NoRecursionFixture()) { }
}

/// <summary>
/// InlineAutoData attribute that uses the NoRecursionFixture
/// </summary>
public class NoRecursionInlineAutoDataAttribute : InlineAutoDataAttribute
{
    public NoRecursionInlineAutoDataAttribute(params object[] values)
        : base(new NoRecursionAutoDataAttribute(), values) { }
}
