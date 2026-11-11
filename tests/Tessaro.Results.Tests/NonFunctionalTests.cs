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

    // Spec 002 NFR-2: the async members never resume on the caller's synchronization context.
    private sealed class CountingContext : SynchronizationContext
    {
        private int _posts;

        public int Posts => _posts;

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _posts);
            ThreadPool.QueueUserWorkItem(_ => d(state));
        }
    }

    [Fact]
    [Trait("Spec", "002")]
    [Trait("NFR", "NFR-2")]
    public async Task Async_members_do_not_resume_on_the_callers_context()
    {
        var context = new CountingContext();
        SynchronizationContext? previous = SynchronizationContext.Current;
        Task<string> chain;

        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            Task<Result<int>> later = Task.Run(async () =>
            {
                await Task.Delay(20);
                return Result.Ok(1);
            });

            chain = later
                .MapAsync(async v =>
                {
                    await Task.Delay(1);
                    return v + 1;
                })
                .BindAsync(v => Result.Ok(v * 10))
                .BindAsync(async v =>
                {
                    await Task.Delay(1);
                    return Result.Ok(v);
                })
                .MatchAsync(v => Task.FromResult($"value {v}"), _ => Task.FromResult("failure"));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        (await chain).ShouldBe("value 20");
        context.Posts.ShouldBe(0);
    }

    [Fact]
    [Trait("Spec", "004")]
    [Trait("NFR", "NFR-2")]
    public async Task Side_effect_steps_do_not_resume_on_the_callers_context()
    {
        var context = new CountingContext();
        SynchronizationContext? previous = SynchronizationContext.Current;
        Task<Result<int>> chain;
        Task<Result<int>> onCompletedSuccess;
        Task<Result> onCompletedFailure;

        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            Task<Result<int>> later = Task.Run(async () =>
            {
                await Task.Delay(20);
                return Result.Ok(1);
            });

            chain = later
                .OnSuccessAsync(_ => { })
                .OnSuccessAsync(async _ => await Task.Delay(1))
                .OnSuccessAsync(async () => await Task.Delay(1))
                .OnFailureAsync(async _ => await Task.Delay(1));

            // Completed receivers, called on the context: the actions return pending tasks without
            // awaiting themselves, so only the library's own await could resume on the context.
            onCompletedSuccess = Result.Ok(2).OnSuccessAsync(_ => Task.Delay(5));
            onCompletedFailure = Result.Fail(Error.Conflict("c", "C")).OnFailureAsync(_ => Task.Delay(5));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        (await chain).Value.ShouldBe(1);
        (await onCompletedSuccess).Value.ShouldBe(2);
        (await onCompletedFailure).IsFailure.ShouldBeTrue();
        context.Posts.ShouldBe(0);
    }
}
