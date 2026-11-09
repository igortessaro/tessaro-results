namespace Tessaro.Tests;

public class NonFunctionalTests
{
    [Fact]
    [Trait("NFR", "NFR-1")]
    public void Library_references_only_base_class_library_assemblies()
    {
        string[] references = [.. typeof(Error).Assembly.GetReferencedAssemblies().Select(a => a.Name!)];

        references.ShouldNotBeEmpty();
        references.ShouldAllBe(name => name == "netstandard" || name == "mscorlib" || name == "System" || name.StartsWith("System.", StringComparison.Ordinal));
    }
}
