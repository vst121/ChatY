# Architecture Diagram

## Overview

The ChatY solution follows a layered architecture pattern, separating concerns into distinct layers for maintainability and scalability.

## Layers

- **Presentation Layer**: Handles user interface and real-time communication
- **Application Layer**: Contains business logic services
- **Infrastructure Layer**: Manages data access and external services
- **Domain Layer**: Defines core business entities and rules

## Diagram

```mermaid
graph TD
    subgraph "Presentation Layer"
        A[Blazor Server UI<br/>Pages & Components]
        B[SignalR Hub<br/>Real-time Chat]
    end

    subgraph "Application Layer"
        C[ChatService]
        D[MessageService]
        E[UserService]
    end

    subgraph "Infrastructure Layer"
        F[ChatYDbContext<br/>EF Core]
        G[Repository Pattern]
        H[Azure Blob Storage]
        I[Azure Key Vault]
    end

    subgraph "Domain Layer"
        J[Entities<br/>User, Chat, Message, etc.]
    end

    A --> C
    A --> D
    A --> E
    B --> C
    B --> D
    B --> E
    C --> F
    C --> G
    D --> F
    D --> G
    E --> F
    E --> G
    F --> J
    G --> J
```

## Description

The architecture is designed with clean separation of concerns:

- **Blazor Server UI** provides the web interface using Razor components.
- **SignalR Hub** enables real-time messaging and notifications.
- **Services** encapsulate business logic for chats, messages, and users.
- **Infrastructure** handles data persistence via Entity Framework Core and external integrations.
- **Domain Entities** represent the core business objects.

Dependencies flow inward, ensuring the domain layer remains independent and testable.
