# Diagrama ERD

ERD basado en las entidades y configuraciones EF Core actuales. ASP.NET Core Identity aporta las siete tablas estándar; `ActorUserId` en historial y auditoría referencia `AspNetUsers`, mientras que no se almacenan relaciones de usuario creador en clientes u operaciones.

```mermaid
erDiagram
    AspNetUsers ||--o{ AspNetUserRoles : asigna
    AspNetRoles ||--o{ AspNetUserRoles : contiene
    AspNetUsers ||--o{ AspNetUserClaims : declara
    AspNetRoles ||--o{ AspNetRoleClaims : declara
    AspNetUsers ||--o{ AspNetUserLogins : autentica
    AspNetUsers ||--o{ AspNetUserTokens : posee
    AspNetUsers o|--o{ OperationStatusHistory : actor
    AspNetUsers o|--o{ AuditLogs : actor
    Customers ||--o{ FinancialOperations : origina
    FinancialOperations ||--o{ OperationStatusHistory : registra
    FinancialOperations ||--o{ PaymentAttempts : procesa
    PaymentProviders ||--o{ PaymentAttempts : atiende
    PaymentProviders ||--o{ IntegrationLogs : produce
    PaymentAttempts o|--o{ IntegrationLogs : genera

    AspNetUsers {
        nvarchar Id PK
        nvarchar Email
        nvarchar NormalizedEmail UK
        bit IsActive
        datetimeoffset CreatedAtUtc
    }
    AspNetRoles {
        nvarchar Id PK
        nvarchar NormalizedName UK
    }
    AspNetUserRoles {
        nvarchar UserId PK,FK
        nvarchar RoleId PK,FK
    }
    AspNetUserClaims {
        int Id PK
        nvarchar UserId FK
    }
    AspNetRoleClaims {
        int Id PK
        nvarchar RoleId FK
    }
    AspNetUserLogins {
        nvarchar LoginProvider PK
        nvarchar ProviderKey PK
        nvarchar UserId FK
    }
    AspNetUserTokens {
        nvarchar UserId PK,FK
        nvarchar LoginProvider PK
        nvarchar Name PK
    }
    Customers {
        uniqueidentifier Id PK
        nvarchar CustomerCodeNormalized UK
        nvarchar EmailNormalized UK
        nvarchar DocumentNumberNormalized
        bit IsActive
    }
    FinancialOperations {
        uniqueidentifier Id PK
        nvarchar OperationCodeNormalized UK
        uniqueidentifier CustomerId FK
        decimal Amount
        char Currency
        nvarchar Status
        nvarchar IdempotencyKey UK
        binary RowVersion
    }
    OperationStatusHistory {
        bigint Id PK
        uniqueidentifier OperationId FK
        nvarchar FromStatus
        nvarchar ToStatus
        nvarchar ActorUserId FK
        datetimeoffset ChangedAtUtc
    }
    PaymentProviders {
        uniqueidentifier Id PK
        nvarchar ProviderCode UK
        nvarchar Name
        nvarchar Type
        nvarchar ConfigurationJson
        bit IsEnabled
        bit IsAvailable
    }
    PaymentAttempts {
        uniqueidentifier Id PK
        uniqueidentifier OperationId FK
        uniqueidentifier PaymentProviderId FK
        int AttemptNumber
        nvarchar IdempotencyKey UK
        nvarchar Status
    }
    IntegrationLogs {
        bigint Id PK
        uniqueidentifier PaymentProviderId FK
        uniqueidentifier PaymentAttemptId FK
        nvarchar CorrelationId
        nvarchar Outcome
    }
    AuditLogs {
        bigint Id PK
        nvarchar ActorUserId FK
        nvarchar Action
        nvarchar EntityType
        datetimeoffset OccurredAtUtc
        nvarchar CorrelationId
    }
```
