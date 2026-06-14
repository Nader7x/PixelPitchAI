using AutoFixture;
using AutoFixture.Xunit2;

namespace Footex.UnitTests.Common;

/// <summary>
///     Custom AutoFixture configuration that prevents circular reference issues
/// </summary>
public class NoRecursionFixture : Fixture
{
    public NoRecursionFixture()
    {
        // Remove the default ThrowingRecursionBehavior that causes circular reference exceptions
        Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => Behaviors.Remove(b));

        // Add OmitOnRecursionBehavior to handle circular references gracefully
        Behaviors.Add(new OmitOnRecursionBehavior());
        Customizations.Add(new IFormFileSpecimenBuilder());

        // Limit recursion depth as an additional safety measure
        RepeatCount = 3;

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

