<div align="center">

# ERP Canecas

**Enterprise Resource Planning Platform**

Plataforma ERP modular desenvolvida com C# e .NET, organizada em serviços independentes e preparada para isolamento de dados por empresa.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-Backend-239120?style=flat-square&logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Database-4169E1?style=flat-square&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Architecture](https://img.shields.io/badge/Architecture-Microservices-2563EB?style=flat-square)]()
[![Status](https://img.shields.io/badge/Status-In%20Development-D97706?style=flat-square)]()

[Português](#português) · [English](#english)

</div>

---

# Português

## Visão geral

O **ERP Canecas** é uma plataforma de gestão empresarial desenvolvida com foco em modularidade, segurança, escalabilidade e separação de responsabilidades.

A solução adota uma arquitetura baseada em microserviços, organizando os domínios do sistema em serviços independentes. A infraestrutura também contempla uma estratégia de **Multi-Tenancy com bancos de dados separados por empresa**, permitindo manter os dados operacionais de cada organização isolados.

O projeto está em desenvolvimento. Esta documentação descreve a estrutura e os recursos presentes no repositório no estágio atual.

## Arquitetura

A solução é composta por serviços ASP.NET Core e uma biblioteca compartilhada para a infraestrutura de Multi-Tenancy.

\`\`\`mermaid
flowchart TB
    CLIENT["Aplicação cliente"]
    ID["MS-Identidade"]
    CAD["MS-Cadastro"]
    SHARED["Shared.MultiTenancy"]

    MASTER[("PostgreSQL<br/>Master Database")]
    TENANT[("PostgreSQL<br/>Tenant Database")]

    CLIENT --> ID
    CLIENT --> CAD
    ID --> MASTER
    ID --> SHARED
    SHARED --> TENANT

    classDef service fill:#1d4ed8,color:#fff,stroke:#1e40af
    classDef shared fill:#475569,color:#fff,stroke:#334155
    classDef database fill:#0f766e,color:#fff,stroke:#115e59

    class ID,CAD service
    class SHARED shared
    class MASTER,TENANT database
\`\`\`

*O diagrama apresenta a visão conceitual da solução. O MS-Cadastro está em estágio inicial e suas integrações ainda não estão implementadas.*

## Serviços e componentes

### MS-Identidade

Responsável pela autenticação, gerenciamento de usuários e autorização de acesso à plataforma.

**Recursos implementados:**

- Autenticação baseada em JSON Web Tokens (JWT).
- Validação de credenciais e situação do usuário e da empresa.
- Hash e verificação de senhas com os recursos do ASP.NET Core Identity.
- Geração de tokens com identificação do usuário e da empresa.
- Autorização baseada em funcionalidades (*Feature-Based Authorization*).
- Policies e handler de autorização personalizados.
- Repositórios e serviços para organização das regras de acesso.
- Contextos de dados separados para informações centrais e dados do tenant.

### MS-Cadastro

Serviço destinado à centralização das operações de cadastro e gerenciamento de informações utilizadas pelos módulos do ERP.

Atualmente em fase inicial de desenvolvimento.

### Shared.MultiTenancy

Biblioteca compartilhada responsável por componentes de infraestrutura relacionados à resolução de conexões por empresa.

**Responsabilidades:**

- Resolução da conexão do banco de dados do tenant.
- Integração com PostgreSQL por meio de Npgsql.
- Compartilhamento de componentes de infraestrutura entre serviços.

## Autenticação e autorização

A autenticação utiliza JWT. A validação dos tokens contempla assinatura, emissor, audiência e período de validade.

O controle de acesso utiliza autorização baseada em funcionalidades. Cada funcionalidade possui um código que pode ser utilizado em uma Policy do ASP.NET Core, permitindo centralizar as regras de autorização.

| Código | Descrição |
|---|---|
| \`USUARIO\` | Acesso às funcionalidades de usuários |
| \`USUARIO_CADASTRAR\` | Permissão para cadastrar usuários |
| \`USUARIO_PERMISSOES\` | Gerenciamento de permissões |

## Multi-Tenancy e persistência

A estratégia de persistência utiliza PostgreSQL e separa as informações centrais das empresas dos dados operacionais de cada tenant.

| Componente | Responsabilidade |
|---|---|
| Master Database | Armazena informações centrais das empresas e a identificação de suas bases de dados |
| Tenant Database | Armazena os dados operacionais de uma empresa |

A resolução da conexão do tenant é realizada pela biblioteca compartilhada \`Shared.MultiTenancy\`. O acesso aos dados utiliza Entity Framework Core e Npgsql.

## Tecnologias

| Tecnologia | Aplicação |
|---|---|
| C# | Linguagem principal |
| .NET 10 | Plataforma de desenvolvimento |
| ASP.NET Core | Construção de APIs |
| PostgreSQL | Banco de dados relacional |
| Entity Framework Core | Mapeamento objeto-relacional |
| Npgsql | Integração com PostgreSQL |
| JWT | Autenticação baseada em tokens |
| Git e GitHub | Versionamento e hospedagem do código |

## Estrutura do repositório

\`\`\`text
ERP_Canecas/
├── MS-Identidade/
│   ├── Authorization/
│   ├── Controllers/
│   ├── DTOs/
│   ├── Data/
│   ├── Entities/
│   ├── Repositories/
│   ├── Services/
│   └── Program.cs
│
├── MS-Cadastro/
│   ├── Controllers/
│   └── Program.cs
│
├── Shared/
│   └── MultiTenancy/
│       ├── Interfaces/
│       └── Services/
│
└── ERP_Canecas.sln
\`\`\`

## Execução local

### Pré-requisitos

- .NET 10 SDK
- PostgreSQL
- Git

### Clonar o repositório

\`\`\`bash
git clone https://github.com/spedrosantos/ERP_Canecas.git
cd ERP_Canecas
\`\`\`

### Configuração

Antes de executar o serviço, configure os parâmetros necessários para conexão com o PostgreSQL e autenticação JWT. As configurações devem ser fornecidas por arquivos locais não versionados, User Secrets ou variáveis de ambiente.

Não armazene credenciais, strings de conexão com senhas ou chaves JWT diretamente no repositório.

### Executar o serviço de identidade

\`\`\`bash
dotnet run --project MS-Identidade
\`\`\`

O serviço depende das configurações de banco de dados e JWT para inicialização.

## Estado do projeto

O ERP Canecas está em desenvolvimento. Os serviços possuem diferentes níveis de implementação; portanto, nem todos os componentes descritos na arquitetura conceitual estão disponíveis para execução ou integração nesta etapa.

---

# English

## Overview

**ERP Canecas** is an Enterprise Resource Planning platform developed with a focus on modularity, security, scalability, and separation of concerns.

The solution follows a microservices-based approach, organizing system domains into independent services. Its infrastructure also includes a **Multi-Tenancy strategy with separate databases per company**, keeping each organization's operational data isolated.

The project is currently under development. This documentation describes the repository structure and capabilities available at its current stage.

## Architecture

The solution consists of ASP.NET Core services and a shared library for Multi-Tenancy infrastructure.

\`\`\`mermaid
flowchart TB
    CLIENT["Client application"]
    ID["MS-Identidade"]
    CAD["MS-Cadastro"]
    SHARED["Shared.MultiTenancy"]

    MASTER[("PostgreSQL<br/>Master Database")]
    TENANT[("PostgreSQL<br/>Tenant Database")]

    CLIENT --> ID
    CLIENT --> CAD
    ID --> MASTER
    ID --> SHARED
    SHARED --> TENANT

    classDef service fill:#1d4ed8,color:#fff,stroke:#1e40af
    classDef shared fill:#475569,color:#fff,stroke:#334155
    classDef database fill:#0f766e,color:#fff,stroke:#115e59

    class ID,CAD service
    class SHARED shared
    class MASTER,TENANT database
\`\`\`

*The diagram represents the conceptual solution architecture. MS-Cadastro is at an early development stage, and its integrations have not yet been implemented.*

## Services and components

### MS-Identidade

Responsible for authentication, user management, and platform access authorization.

**Implemented capabilities:**

- JSON Web Token (JWT) authentication.
- Credential and company/user status validation.
- Password hashing and verification using ASP.NET Core Identity components.
- Token generation with user and company identification.
- Feature-Based Authorization.
- Custom authorization policies and handlers.
- Repositories and services for access-rule organization.
- Separate data contexts for central information and tenant data.

### MS-Cadastro

Service intended to centralize registration operations and information management used by ERP modules.

Currently in its initial development stage.

### Shared.MultiTenancy

Shared library responsible for infrastructure components related to company-specific database connection resolution.

**Responsibilities:**

- Tenant database connection resolution.
- PostgreSQL integration through Npgsql.
- Shared infrastructure components across services.

## Authentication and authorization

Authentication is implemented using JWT. Token validation includes signature, issuer, audience, and expiration checks.

Access control follows a feature-based authorization model. Each feature has a unique code that can be associated with an ASP.NET Core Policy, centralizing authorization rules.

| Code | Description |
|---|---|
| \`USUARIO\` | Access to user management features |
| \`USUARIO_CADASTRAR\` | Permission to register users |
| \`USUARIO_PERMISSOES\` | Permission management |

## Multi-Tenancy and data persistence

The persistence strategy uses PostgreSQL and separates central company information from each tenant's operational data.

| Component | Responsibility |
|---|---|
| Master Database | Stores central company information and identifies its database |
| Tenant Database | Stores a company's operational data |

Tenant connection resolution is handled by the shared \`Shared.MultiTenancy\` library. Data access uses Entity Framework Core and Npgsql.

## Technology stack

| Technology | Purpose |
|---|---|
| C# | Main programming language |
| .NET 10 | Development platform |
| ASP.NET Core | API development |
| PostgreSQL | Relational database |
| Entity Framework Core | Object-relational mapping |
| Npgsql | PostgreSQL integration |
| JWT | Token-based authentication |
| Git and GitHub | Version control and source code hosting |

## Repository structure

\`\`\`text
ERP_Canecas/
├── MS-Identidade/
│   ├── Authorization/
│   ├── Controllers/
│   ├── DTOs/
│   ├── Data/
│   ├── Entities/
│   ├── Repositories/
│   ├── Services/
│   └── Program.cs
│
├── MS-Cadastro/
│   ├── Controllers/
│   └── Program.cs
│
├── Shared/
│   └── MultiTenancy/
│       ├── Interfaces/
│       └── Services/
│
└── ERP_Canecas.sln
\`\`\`

## Local setup

### Prerequisites

- .NET 10 SDK
- PostgreSQL
- Git

### Clone the repository

\`\`\`bash
git clone https://github.com/spedrosantos/ERP_Canecas.git
cd ERP_Canecas
\`\`\`

### Configuration

Before running the service, configure the required PostgreSQL connection and JWT authentication settings. Supply configuration through local untracked files, User Secrets, or environment variables.

Do not commit credentials, connection strings containing passwords, or JWT signing keys to the repository.

### Run the identity service

\`\`\`bash
dotnet run --project MS-Identidade
\`\`\`

The service requires database and JWT configuration to start.

## Project status

ERP Canecas is under development. Services are at different implementation stages; therefore, not every component shown in the conceptual architecture is currently available for execution or integration.

---

<div align="center">

**ERP Canecas**

C# · .NET 10 · PostgreSQL

[Pedro Santos](https://github.com/spedrosantos)

</div>
