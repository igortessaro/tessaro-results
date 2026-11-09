# Tessaro.Results

> A zero-dependency result type for .NET — return success or categorised errors instead of
> throwing, and compose outcomes with `Map`, `Bind` and `Match`.

[![CI](https://github.com/igortessaro/tessaro-results/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/igortessaro/tessaro-results/actions/workflows/ci.yml)

**Status:** in development ([spec 001](specs/001-result-core/spec.md)). Usage documentation
lands with the spec's closing task.

```csharp
using Tessaro;

Error error = Error.NotFound("quote.not_found", "Quote 42 was not found");
```

## License

[MIT](LICENSE)
