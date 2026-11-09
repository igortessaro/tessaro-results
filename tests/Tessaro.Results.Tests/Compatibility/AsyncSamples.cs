// Spec 002 NFR-3 / plan 002 §5: the async members compile and run, without aliases, from a namespace
// unrelated to Tessaro that imports ASP.NET Core, and inside a Tessaro.* namespace next to
// Tessaro.Optional. The handler and controller below are spec 002 §2 ("Success looks like").
using Microsoft.AspNetCore.Http;
using Tessaro;

namespace Consumer.Compat.Async
{
    public sealed record UpdateStatus(Guid Id, string Status);

    public sealed record UpdateStatusRequest(string Status)
    {
        public UpdateStatus ToCommand(Guid id) => new(id, Status);
    }

    public sealed record ServiceOrder(Guid Id, string Status)
    {
        public Result<ServiceOrder> ChangeStatus(string status) =>
            string.IsNullOrWhiteSpace(status)
                ? Error.Validation("order.status", "Status is required")
                : this with { Status = status };
    }

    public sealed record ServiceOrderDto(Guid Id, string Status)
    {
        public static ServiceOrderDto From(ServiceOrder order) => new(order.Id, order.Status);
    }

    public sealed class ServiceOrderRepository(params ServiceOrder[] orders)
    {
        private readonly Dictionary<Guid, ServiceOrder> _orders = orders.ToDictionary(o => o.Id);

        public async Task<Result<ServiceOrder>> FindAsync(Guid id, CancellationToken ct)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            return _orders.TryGetValue(id, out ServiceOrder? order)
                ? order
                : Error.NotFound("order.not_found", $"Order {id} was not found");
        }

        public async Task<Result<ServiceOrder>> UpdateAsync(ServiceOrder order, CancellationToken ct)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            _orders[order.Id] = order;
            return order;
        }

        public async Task<ServiceOrder> GetDetailedAsync(Guid id, CancellationToken ct)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            return _orders[id] with { Status = _orders[id].Status + " (detailed)" };
        }
    }

    public sealed class UpdateStatusHandler(ServiceOrderRepository repository)
    {
        // Handler: async steps in one chain.
        public Task<Result<ServiceOrder>> Handle(UpdateStatus request, CancellationToken ct) =>
            repository.FindAsync(request.Id, ct)                              // Task<Result<ServiceOrder>>
                .BindAsync(order => order.ChangeStatus(request.Status))      // sync step on a pending result
                .BindAsync(order => repository.UpdateAsync(order, ct))       // async step
                .MapAsync(order => repository.GetDetailedAsync(order.Id, ct)); // async mapper
    }

    // Stand-in for MediatR's ISender, so the controller reads as in the spec.
    public sealed class Mediator(UpdateStatusHandler handler)
    {
        public Task<Result<ServiceOrder>> Send(UpdateStatus command, CancellationToken ct) => handler.Handle(command, ct);
    }

    public static class ErrorPresentation
    {
        // ToProblem: your mapping (spec 001 §5).
        public static IResult ToProblem(this IReadOnlyList<Error> errors) =>
            Results.Problem(detail: errors[0].Message, statusCode: errors[0].Type switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError,
            });
    }

    public sealed class ServiceOrdersController(Mediator mediator)
    {
        // Controller: map and branch on a pending result.
        public async Task<IResult> Update(Guid id, UpdateStatusRequest body, CancellationToken ct) =>
            await mediator.Send(body.ToCommand(id), ct)                      // Task<Result<ServiceOrder>>
                .MapAsync(ServiceOrderDto.From)
                .MatchAsync(dto => TypedResults.Ok(dto), errors => errors.ToProblem()); // ToProblem: your mapping (spec 001 §5)
    }

    // README "Async" samples that are not part of the spec example above.
    public static class ReadmeAsyncSamples
    {
        public static async Task<Result<ServiceOrder>> SaveCompleted(ServiceOrder order, ServiceOrderRepository repository, CancellationToken ct)
        {
            Result<ServiceOrder> saved = await order.ChangeStatus("approved").BindAsync(o => repository.UpdateAsync(o, ct));
            return saved;
        }
    }

    [Trait("Spec", "002")]
    public class AsyncSamplesTests
    {
        private static readonly Guid _id = Guid.Parse("00000000-0000-0000-0000-000000000042");

        private static ServiceOrdersController Controller() =>
            new(new Mediator(new UpdateStatusHandler(new ServiceOrderRepository(new ServiceOrder(_id, "new")))));

        [Fact]
        [Trait("NFR", "NFR-3")]
        public async Task The_spec_handler_chain_runs_from_an_unrelated_namespace()
        {
            var handler = new UpdateStatusHandler(new ServiceOrderRepository(new ServiceOrder(_id, "new")));

            (await handler.Handle(new UpdateStatus(_id, "approved"), CancellationToken.None)).Value.Status.ShouldBe("approved (detailed)");
            (await handler.Handle(new UpdateStatus(_id, " "), CancellationToken.None)).Errors.ShouldHaveSingleItem().Code.ShouldBe("order.status");
            (await handler.Handle(new UpdateStatus(Guid.Empty, "approved"), CancellationToken.None)).Errors.ShouldHaveSingleItem().Type.ShouldBe(ErrorType.NotFound);
        }

        [Fact]
        [Trait("NFR", "NFR-3")]
        public async Task The_spec_controller_maps_to_ASP_NET_Core_results()
        {
            (await Controller().Update(_id, new UpdateStatusRequest("approved"), CancellationToken.None))
                .ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ServiceOrderDto>>()
                .Value.ShouldNotBeNull().Status.ShouldBe("approved (detailed)");
            (await Controller().Update(Guid.Empty, new UpdateStatusRequest("approved"), CancellationToken.None))
                .ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>()
                .StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        }

        [Fact]
        [Trait("Docs", "README")]
        public async Task The_README_completed_result_sample_saves_the_order()
        {
            var repository = new ServiceOrderRepository(new ServiceOrder(_id, "new"));

            Result<ServiceOrder> saved = await ReadmeAsyncSamples.SaveCompleted(new ServiceOrder(_id, "new"), repository, CancellationToken.None);

            saved.Value.Status.ShouldBe("approved");
            (await repository.FindAsync(_id, CancellationToken.None)).Value.Status.ShouldBe("approved");
        }

        [Fact]
        [Trait("NFR", "NFR-3")]
        public async Task Cancellation_of_the_callers_token_surfaces_as_cancellation() =>
            await Should.ThrowAsync<OperationCanceledException>(() =>
                Controller().Update(_id, new UpdateStatusRequest("approved"), new CancellationToken(canceled: true)));
    }
}

namespace Tessaro.Compat
{
    // Inside a Tessaro.* namespace: no `using`, next to Tessaro.Optional's Optional<T>.
    public static class AsyncParsing
    {
        public static async Task<Result<int>> ParseAsync(string text)
        {
            await Task.Yield();
            return Parsing.Parse(text);
        }

        public static Task<Optional<int>> DoubledAsync(string text) =>
            ParseAsync(text)
                .MapAsync(v => v * 2)
                .MatchAsync(Optional.Of, _ => Optional.Empty<int>());
    }

    [Trait("Spec", "002")]
    public class AsyncSiblingNamespaceTests
    {
        [Fact]
        [Trait("NFR", "NFR-3")]
        public async Task Async_members_and_Optional_interoperate_inside_a_Tessaro_namespace()
        {
            (await AsyncParsing.DoubledAsync("21")).ShouldBe(Optional.Of(42));
            (await AsyncParsing.DoubledAsync("x")).IsEmpty.ShouldBeTrue();
        }
    }
}
