# Code Style & Conventions
- C# uses Allman brace style, explicit access modifiers, PascalCase for types/methods/properties, camelCase for locals/parameters, private fields may use `_camelCase`.
- Unity scripts retain existing tab indentation; server-side .NET code uses four spaces.
- Protobuf `.proto` files stay snake_case with message names in PascalCase under `Game.*` or `EnhancedMinecraftProtocol` packages.
- Keep documentation in `docs/` up to date when protocol or architecture changes occur.
- Avoid introducing non-ASCII characters unless necessary; keep comments succinct and purposeful.