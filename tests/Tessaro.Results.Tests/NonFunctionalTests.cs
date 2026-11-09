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

    [Theory]
    [InlineData(typeof(Error))]
    [InlineData(typeof(Result))]
    [InlineData(typeof(Result<int>))]
    [InlineData(typeof(Result<string>))]
    [Trait("NFR", "NFR-2")]
    public void Public_types_expose_no_mutable_state(Type type)
    {
        const System.Reflection.BindingFlags Instance =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

        type.GetProperties(Instance).ShouldAllBe(p => p.SetMethod == null || !p.SetMethod.IsPublic);
        type.GetFields(Instance).ShouldAllBe(f => f.IsInitOnly);
    }

    [Fact]
    [Trait("NFR", "NFR-2")]
    public async Task A_shared_failure_reads_the_same_from_many_threads()
    {
        Result<int> failure = Result.Fail<int>([Error.Failure("a", "A"), Error.Conflict("b", "B")]);

        string[] seen = await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(failure.ToString)));

        seen.Distinct().ShouldHaveSingleItem();
    }
}
