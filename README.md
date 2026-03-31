# Gavel - High-Performance Auction System

Gavel is a modern, mission-critical auction platform built with **ASP.NET Core 10**, optimized for **Native AOT**, and designed to comply strictly with Brazilian auctioneering legislation (Decree No. 21,981/1932).

The system utilizes **Vertical Slice Architecture** to maintain high cohesion and low coupling, ensuring that complex business rules like Proxy Bidding and Soft Close are encapsulated within their respective features.

## 🚀 Tech Stack

- **Orchestration:** [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) (Local dev, OpenTelemetry, Resilience)
- **Backend:** ASP.NET Core 10 (Native AOT compatible)
- **Database:** EF Core 10 (PostgreSQL with JSON columns & Row Versioning)
- **Real-time:** SignalR with Source Generation (AOT-safe WebSockets)
- **Auth:** ASP.NET Identity + Keycloak (OIDC)
- **Testing:** TUnit + NSubstitute
- **Validation:** FluentValidation (CPF/CNPJ specialized rules)

## ⚖️ Core Business Rules (Brazilian Decree 21,981/1932)

- **Immutable Audit Trail:** The `Bids` table is append-only. Every bid is a firm legal commitment.
- **Financial Precision:** All values use `decimal`. No floating-point math.
- **Auctioneer Commission:** Automated 5% calculation encapsulated in domain services.
- **Soft Close:** Automatic time extensions (e.g., +3 minutes) on last-second bids to prevent sniping.
- **State Machine:** Strict transitions between `Draft`, `Scheduled`, `Active`, `Closing`, and `Settlement`.

## 🛠️ Getting Started

### Prerequisites
- .NET 10 SDK
- Docker Desktop (for Aspire resources like Keycloak/PostgreSQL)

### Commands
```bash
# Build the solution in Release mode (AOT check)
dotnet build --configuration Release

# Run tests
dotnet test

# Start the entire ecosystem (API, DB, Keycloak)
dotnet run --project src/Gavel.AppHost
```

## 🗺️ Roadmap

### Phase 1: Foundation & Orchestration (Current)
- [x] Set up .NET Aspire AppHost and ServiceDefaults.
- [x] Configure PostgreSQL with EF Core 10 and Row Versioning.
- [x] Integrate Keycloak for OIDC Authentication.
- [ ] Implement the base `Lot` and `Auction` domain entities.

### Phase 2: User Governance (KYC)
- [ ] Implement Bidder Registration flow.
- [ ] Integrate CPF/CNPJ validation (Brazilian Federal Revenue rules).
- [ ] Document upload and administrative approval workflow.

### Phase 3: Cataloging & Asset Management
- [ ] CRUD for Lots (Draft -> Scheduled).
- [ ] Image/Media management with AOT-compatible JSON metadata.
- [ ] Public Notice (Edital) attachment and versioning.

### Phase 4: The Bidding Engine (Critical)
- [ ] Real-time SignalR Hub (Source Generated).
- [ ] Proxy Bidding (Automatic) logic with tie-break rules.
- [ ] Soft Close (Atomic Time Extension) implementation.
- [ ] High-concurrency validation for minimum increments.

### Phase 5: Financials & Settlement
- [ ] Auctioneer Commission (5%) calculation service.
- [ ] Invoice generation (V_a + 5% + Admin Fees).
- [ ] Conditional Sale approval flow for reserve prices not met.
- [ ] Auto-block delinquent bidders on payment default.

### Phase 6: Legal & Bookkeeping
- [ ] Automated Entry/Exit Log generation (Mandatory recordkeeping).
- [ ] Digital Signature integration for Auction Certificates (Auto de Arrematação).
- [ ] Sale Note (Nota de Venda) generation.

### Phase 7: UI & Notifications
- [ ] Angular-based Real-time Dashboard.
- [ ] Push Notifications for "Outbid" events.
- [ ] SMS/WhatsApp alerts for payment deadlines.
