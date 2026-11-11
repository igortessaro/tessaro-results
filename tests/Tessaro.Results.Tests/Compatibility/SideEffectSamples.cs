// Spec 004 NFR-3 / plan 004 §5: the side-effect steps compile and run, without aliases, from a
// namespace unrelated to Tessaro that imports ASP.NET Core (with its ILogger), and inside a Tessaro.*
// namespace next to Tessaro.Optional. The handler and worker below are spec 004 §2 ("Success looks like").
using Microsoft.Extensions.Logging;
using Tessaro;

namespace Consumer.Compat.SideEffects
{
    public sealed record UpdateQuoteStatus(Guid Id, string Status);

    public sealed record Quote(Guid Id, string Status)
    {
        public Result<Quote> ChangeStatus(string status) =>
            status == Status
                ? Error.Conflict("quote.status", $"Quote is already {status}")
                : this with { Status = status };
    }

    public sealed record QuoteStatusChanged(Quote Quote);

    public sealed class QuoteRepository(params Quote[] quotes)
    {
        private readonly Dictionary<Guid, Quote> _quotes = quotes.ToDictionary(q => q.Id);

        public async Task<Result<Quote>> FindAsync(Guid id, CancellationToken ct)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            return _quotes.TryGetValue(id, out Quote? quote)
                ? quote
                : Error.NotFound("quote.not_found", $"Quote {id} was not found");
        }

        public async Task<Result<Quote>> UpdateAsync(Quote quote, CancellationToken ct)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            _quotes[quote.Id] = quote;
            return quote;
        }
    }

    // Stand-in for MediatR's IPublisher / ISender, so the samples read as in the spec.
    public sealed class Mediator
    {
        public List<object> Published { get; } = [];

        public async Task Publish(object notification, CancellationToken ct)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            Published.Add(notification);
        }

        public List<string> Sent { get; } = [];

        public Task<Result> Send(string command, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Sent.Add(command);
            return Task.FromResult(command == "valid" ? Result.Ok() : Result.Fail(Error.Validation("message.invalid", $"Message {command} is invalid")));
        }
    }

    // Stand-in for a RabbitMQ channel.
    public sealed class Channel
    {
        public List<ulong> Acked { get; } = [];

        public void BasicAck(ulong deliveryTag, bool multiple) => Acked.Add(deliveryTag);
    }

    public sealed class RecordingLogger : ILogger
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
        }
    }

    // CA1848 (LoggerMessage delegates) is a performance rule; these samples reproduce spec 004 §2 as
    // written, with the plain LogWarning calls the services use.
#pragma warning disable CA1848
    public sealed class UpdateQuoteStatusHandler(QuoteRepository repository, Mediator mediator, ILogger logger)
    {
        // Handler: publish after a success, log after a failure, return the outcome unchanged.
        public Task<Result<Quote>> Handle(UpdateQuoteStatus request, CancellationToken ct) =>
            repository.FindAsync(request.Id, ct)                                  // Task<Result<Quote>>
                .BindAsync(quote => quote.ChangeStatus(request.Status))
                .BindAsync(quote => repository.UpdateAsync(quote, ct))
                .OnSuccessAsync(quote => mediator.Publish(new QuoteStatusChanged(quote), ct)) // awaited
                .OnFailureAsync(errors => logger.LogWarning("Quote {Id} not updated: {Errors}", request.Id, errors));
    }

    public sealed class MessagesConsumer(Mediator mediator, Channel channel, ILogger logger)
    {
        // Worker: log a failed outcome, then ack either way.
        public async Task Handle(string command, ulong tag, CancellationToken ct)
        {
            Result outcome = await mediator.Send(command, ct);
            outcome.OnFailure(errors => logger.LogWarning("Message {Tag} had errors: {Errors}", tag, errors));
            channel.BasicAck(tag, multiple: false);
        }
    }
#pragma warning restore CA1848

    // README "Side effects" samples that are not part of the spec example above.
    public static class ReadmeSideEffectSamples
    {
        public static Result<string> Audit(Result<Quote> result, List<string> audit) =>
            result
                .OnSuccess(quote => audit.Add($"quote {quote.Id} is {quote.Status}"))
                .OnFailure(errors => audit.Add($"{errors.Count} error(s)"))
                .Map(quote => quote.Status);                                       // still a Result<Quote> before Map

        public static Task<Result<Quote>> SaveAndPublish(Quote quote, QuoteRepository repository, Mediator mediator, CancellationToken ct) =>
            quote.ChangeStatus("approved")
                .OnSuccessAsync(q => mediator.Publish(new QuoteStatusChanged(q), ct));  // completed result, async action
    }

    [Trait("Spec", "004")]
    public class SideEffectSamplesTests
    {
        private static readonly Guid _id = Guid.Parse("00000000-0000-0000-0000-000000000042");

        [Fact]
        [Trait("NFR", "NFR-3")]
        public async Task The_spec_handler_publishes_after_a_success_and_logs_after_a_failure()
        {
            var mediator = new Mediator();
            var logger = new RecordingLogger();
            var handler = new UpdateQuoteStatusHandler(new QuoteRepository(new Quote(_id, "pending")), mediator, logger);

            Result<Quote> approved = await handler.Handle(new UpdateQuoteStatus(_id, "approved"), CancellationToken.None);
            Result<Quote> again = await handler.Handle(new UpdateQuoteStatus(_id, "approved"), CancellationToken.None);
            Result<Quote> missing = await handler.Handle(new UpdateQuoteStatus(Guid.Empty, "approved"), CancellationToken.None);

            approved.Value.Status.ShouldBe("approved");
            mediator.Published.ShouldHaveSingleItem().ShouldBe(new QuoteStatusChanged(approved.Value));
            again.Errors.ShouldHaveSingleItem().Code.ShouldBe("quote.status");
            missing.Errors.ShouldHaveSingleItem().Type.ShouldBe(ErrorType.NotFound);
            logger.Warnings.Count.ShouldBe(2);
            logger.Warnings[0].ShouldStartWith($"Quote {_id} not updated");
        }

        [Fact]
        [Trait("NFR", "NFR-3")]
        public async Task The_spec_worker_logs_a_failed_outcome_and_acks_either_way()
        {
            var channel = new Channel();
            var logger = new RecordingLogger();
            var consumer = new MessagesConsumer(new Mediator(), channel, logger);

            await consumer.Handle("valid", 1, CancellationToken.None);
            await consumer.Handle("broken", 2, CancellationToken.None);

            channel.Acked.ShouldBe([1UL, 2UL]);
            logger.Warnings.ShouldHaveSingleItem().ShouldStartWith("Message 2 had errors");
        }

        [Fact]
        [Trait("Docs", "README")]
        public async Task The_README_side_effect_samples_run()
        {
            var audit = new List<string>();
            var mediator = new Mediator();

            ReadmeSideEffectSamples.Audit(new Quote(_id, "approved"), audit).Value.ShouldBe("approved");
            ReadmeSideEffectSamples.Audit(Error.NotFound("quote.not_found", "missing"), audit).IsFailure.ShouldBeTrue();
            audit.ShouldBe([$"quote {_id} is approved", "1 error(s)"]);

            Result<Quote> saved = await ReadmeSideEffectSamples.SaveAndPublish(new Quote(_id, "pending"), new QuoteRepository(), mediator, CancellationToken.None);
            saved.Value.Status.ShouldBe("approved");
            mediator.Published.ShouldHaveSingleItem();
        }

        [Fact]
        [Trait("NFR", "NFR-3")]
        public async Task Cancellation_of_a_side_effect_surfaces_as_cancellation()
        {
            var handler = new UpdateQuoteStatusHandler(new QuoteRepository(new Quote(_id, "pending")), new Mediator(), new RecordingLogger());

            await Should.ThrowAsync<OperationCanceledException>(() =>
                handler.Handle(new UpdateQuoteStatus(_id, "approved"), new CancellationToken(canceled: true)));
        }
    }
}

namespace Tessaro.Compat
{
    // Inside a Tessaro.* namespace: no `using`, next to Tessaro.Optional's Optional<T>.
    public static class SideEffectParsing
    {
        public static Task<Optional<int>> ParsedAndCountedAsync(string text, List<string> log) =>
            AsyncParsing.ParseAsync(text)
                .OnSuccessAsync(v => log.Add($"parsed {v}"))
                .OnFailureAsync(errors => log.Add($"rejected: {errors[0].Code}"))
                .MatchAsync(Optional.Of, _ => Optional.Empty<int>());
    }

    [Trait("Spec", "004")]
    public class SideEffectSiblingNamespaceTests
    {
        [Fact]
        [Trait("NFR", "NFR-3")]
        public async Task Side_effect_steps_and_Optional_interoperate_inside_a_Tessaro_namespace()
        {
            var log = new List<string>();

            (await SideEffectParsing.ParsedAndCountedAsync("21", log)).ShouldBe(Optional.Of(21));
            (await SideEffectParsing.ParsedAndCountedAsync("x", log)).IsEmpty.ShouldBeTrue();
            log.Count.ShouldBe(2);
            log[0].ShouldBe("parsed 21");
            log[1].ShouldStartWith("rejected: ");
        }
    }
}
