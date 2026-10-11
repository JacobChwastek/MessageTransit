# Redistribution notes

MessageTransit is based on [MassTransit v8.5.10](https://github.com/MassTransit/MassTransit/tree/v8.5.10).

- Keep the Apache-2.0 `LICENSE`, inherited MassTransit and Chris Patterson attribution, and existing source notices. The `COPYRIGHT` and `NOTICE` files also credit Jacob Chwastek.
- Mark modified inherited files with a short notice near the top stating they were changed for MessageTransit.
- Keep the MIT notices for FastExpressionCompiler and the inherited MongoDB integration in `THIRD-PARTY-NOTICES`. The affected core and MongoDB packages declare `Apache-2.0 AND MIT`.
- Include `LICENSE`, `NOTICE`, `COPYRIGHT`, and `THIRD-PARTY-NOTICES` in source releases and applicable NuGet packages.

The inherited logo has been removed from the repository and packages. The blog-derived string splitting helper and the Stack Overflow-derived `ConcurrentHashSet` have been replaced with independent implementations. Before each public distribution, inspect the actual release archives for correct notices and metadata; the [release process](../releasing.md) automates that inspection.
