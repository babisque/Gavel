# Gavel 🔨

**Gavel** is a modern, real-time Auction API built with .NET 10. It allows users to browse auction items, view details, and place bids in real-time. The system utilizes SignalR to push instant notifications to connected clients whenever a new bid is placed.

## 🚀 Features

  * **Auction Management**: Retrieve paginated lists of auction items and view specific item details.
  * **Bidding System**: Place bids on active items with validation logic (e.g., bid must be higher than the current price).
  * **Real-Time Notifications**: Uses **SignalR** to broadcast new bids to all clients viewing a specific auction item.
  * **Optimistic Concurrency**: Implements `RowVersion` to handle concurrent updates and prevent data race conditions during bidding.
  * **Clean Architecture**: Organized into API, Application, Core (Domain), and Infrastructure layers.
  * **CQRS Pattern**: Uses **MediatR** to separate read (Queries) and write (Commands) operations.

## 🛠️ Tech Stack

  * **Framework**: .NET 10
  * **Web API**: ASP.NET Core
  * **Database**: SQL Server
  * **ORM**: Entity Framework Core 10.0.0
  * **Real-Time**: ASP.NET Core SignalR
  * **Mapping**: AutoMapper
  * **Mediator**: MediatR
  * **Documentation**: Swagger / OpenAPI
  * **Testing**: xUnit, Moq

## 🏗️ Architecture

The solution follows the **Clean Architecture** principles:

  * **`src/Gavel.Core`**: The Domain layer containing Entities (`AuctionItem`, `Bid`), Enums, and Repository Interfaces. This layer has no dependencies.
  * **`src/Gavel.Application`**: Contains business logic, CQRS Handlers (Commands/Queries), AutoMapper Profiles, and Interfaces.
  * **`src/Gavel.Infrastructure`**: Implements database context (`ApplicationDbContext`), Repositories, and Migrations.
  * **`src/Gavel.API`**: The entry point. Contains Controllers, SignalR Hubs, and Dependency Injection configuration.

## ⚙️ Getting Started

### Prerequisites

  * .NET 10 SDK
  * SQL Server (LocalDB or standard instance)

### Configuration

1.  Clone the repository.
2.  Update the connection string in `src/Gavel.API/appsettings.json` (or `appsettings.Development.json`) if necessary. The default configuration looks for a connection string named `SqlServerConnection`.

### Database Setup

The project uses EF Core Code First Migrations. To apply the database schema:

```bash
cd src/Gavel.API
dotnet ef database update --project ../Gavel.Infrastructure/Gavel.Infrastructure.csproj
```

*Note: The initial migrations include the `RowVersion` column for concurrency control.*

### Running the API

```bash
cd src/Gavel.API
dotnet run
```

The API will start (default ports are usually http://localhost:5065 or https://localhost:7294).

## 📖 API Documentation

Once the application is running, you can access the Swagger UI to explore and test the endpoints:

  * **URL**: `/swagger/index.html`

### Key Endpoints

  * **`GET /api/AuctionItem`**: Get a paginated list of auction items.
      * *Query Params*: `Page`, `Size`.
  * **`GET /api/AuctionItem/{id}`**: Get details for a specific auction item.
  * **`POST /api/Bid`**: Place a new bid.
      * *Body*: `{ "auctionItemId": "guid", "bidderId": "guid", "amount": 0.00 }`.

## ⚡ Real-Time Usage (SignalR)

Clients can connect to the SignalR hub to receive updates.

  * **Hub URL**: `/hubs/bidHub`.
  * **Methods**:
      * `JoinAuctionRoom(Guid auctionItemId)`: Subscribe to updates for a specific item.
      * `LeaveAuctionGroup(string auctionItemId)`: Unsubscribe.
  * **Events**:
      * `NewBidPlaced`: Triggered when a valid bid is processed. Returns the `Bid` object.

## 🧪 Testing

The solution includes unit tests using xUnit and Moq.

To run the tests:

```bash
dotnet test src/Gavel.Tests/Gavel.Tests.csproj
```

Test coverage includes:

  * **Controllers**: `AuctionItemController`, `BidController`.
  * **Handlers**: `PlaceBidHandler` (Business logic validation), `GetAuctionItemsHandler`.