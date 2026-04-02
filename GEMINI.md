# Gavel - Auction System
Modern ASP.NET Core 10 Web API. Organized via **Vertical Slice Architecture**, optimized for **Native AOT**.

## Tech Stack
- **Orchestration:** .NET Aspire (replaces manual Docker Compose for local dev)
- **API:** Native ASP.NET Core OpenAPI (No Swashbuckle/Swagger)
- **Real-time:** SignalR with Source Generation (AOT Compatible)
- **Database:** EF Core 10 with JSON columns & Built-in Raw SQL
- **Testing:** TUnit + NSubstitute (Source-generated)
- **Auth:** ASP.NET Identity + Keycloak (OIDC)

## Architecture & Patterns
- **Vertical Slices:** No `MediatR`. Logic resides within feature folders. Minimal API endpoints call handlers/services directly.
- **SignalR Integration:** Hubs are defined in `Core` or within specific `Features`. Use **Typed Hubs** to ensure type safety with the Angular client.
- **Infrastructure:** Use `.AppHost` for service orchestration and `.ServiceDefaults` for OpenTelemetry/Resilience.

## Commands
- `dotnet build --configuration Release`
- `dotnet test`
- `dotnet format --verify-no-changes`
- `dotnet ef migrations add <Name> --project src/Gavel.Api --startup-project src/Gavel.Api`
- `dotnet run --project src/Gavel.AppHost` (Starts API, DB, and Keycloak)

## Code Style
- **The `field` keyword:** Use for property logic without manual backing fields.
- **Extensions:** Use C# 14 Extension Properties for domain logic.
- **Primary Constructors:** Default for all Classes/Records.
- **Collection Expressions:** Use `[1, 2, 3]` instead of `new[] {1, 2, 3}`.

## Important Rules
- **TDD:** Create tests *before* features. Use `await That(result).IsEqualTo(expected)`.
- **Native AOT:** Avoid `System.Text.Json` without Source Generators. No heavy reflection.
- **Time:** Use `TimeProvider`. All timestamps in the DB must be **UTC**.
- **SignalR:** For AOT compatibility, use `MapHub<THub>` and ensure DTOs are marked with `[JsonSerializable]`.
- **Data:** Use EF Core 10 `.ComplexProperty().ToJson()` for Value Objects.
- **Calculations:** The 5% Auctioneer Commission logic must be encapsulated in a Domain Service or Value Object to ensure immutability.

## Domain-Specific Technical Constraints (Critical)
- **Financial Precision:** NEVER use `float` or `double` for values. Always use `decimal`.
- **Audit Trail:** The `Bids` table must be APPEND-ONLY. No `Updates` or `Deletes` on bids.
- **Concurrency:** Use EF Core **Row Versioning (Optimistic Concurrency)** to handle "last-second" bids.
- **State Machine:** Implement a strict State Machine for the `Lot` status. Transitions must be validated (e.g., a "Sold" lot cannot go back to "Active").

## Refined Tech Stack
- **Real-time:** SignalR is mandatory for "Soft Close" (Time extension) and live bid updates.
- **Validation:** Use `FluentValidation` with specific rules for Brazilian CPF/CNPJ (as per business_logic.md).

## Migrations Naming Convention
- **Descriptive & Intent-based:** Migration names must follow the pattern `[Action][Target][Details]` in **PascalCase**.
- **No Generic Names:** Strictly forbid names like `PhaseN`, `Initial`, `Fix`, or `Migration1`.
- **Examples:** - ✅ `CreateBidderTable`
    - ✅ `AddStatusReasonToBidder`
    - ✅ `ConfigureLotJsonColumns`
    - ✅ `RenameCommissionRateInLot`
    - ❌ `Phase2`
    - ❌ `FinalizePhase3`