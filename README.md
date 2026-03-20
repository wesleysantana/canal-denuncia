🧾 Canal de Denúncias API

Este projeto é uma API backend para gerenciamento de canal de denúncias, permitindo o registro de ocorrências (anônimas ou identificadas), comunicação via mensagens, controle de status e gerenciamento de anexos.
A aplicação foi desenvolvida com foco em boas práticas de arquitetura, separação de responsabilidades e escalabilidade.

🚀 Funcionalidades

📩 Registro de denúncias (anônimas ou identificadas)
🔐 Geração de protocolo único para acompanhamento
💬 Troca de mensagens vinculadas ao protocolo
📎 Upload de anexos com compressão (GZip)
📥 Download de anexos
🔄 Atualização de status da solicitação
📧 Envio de notificações por e-mail
🔑 Autenticação e autorização via Keycloak (para administradores)
⚡ Rate limiting para proteção da API
🏗️ Arquitetura

O projeto segue uma estrutura baseada em separação de camadas (inspirada em Clean Architecture):

CanalDenuncias.API          → Camada de apresentação (Controllers, configuração)
CanalDenuncias.Application  → Casos de uso, serviços e DTOs
CanalDenuncias.Domain       → Regras de negócio e entidades
CanalDenuncias.Infra        → Persistência, e-mail e storage
CanalDenuncias.Tests        → Testes unitários

Princípios aplicados

Separação de responsabilidades (SRP)
Inversão de dependência (DI)
Domínio rico com validações
Uso de interfaces para desacoplamento
Result Pattern para controle de fluxo

🛠️ Tecnologias utilizadas

.NET 10 / ASP.NET Core
C#
Entity Framework Core
Oracle (via Oracle EF Core Provider)
Keycloak (Autenticação/Autorização)
FluentValidation
Swagger / OpenAPI
SendGrid (SMTP)
Rate Limiting (ASP.NET Core)
xUnit / Moq / FluentAssertions / Bogus

📦 Upload de arquivos

Os anexos são armazenados no sistema de arquivos com:
Organização por protocolo
Nome único com GUID
Compressão utilizando GZip
Processamento em streaming (baixo consumo de memória)

🔐 Segurança

Autenticação baseada em JWT via Keycloak
Endpoints administrativos protegidos
Rate limiting para evitar abuso
Sanitização de nomes de arquivos (Path traversal prevention)

🧪 Testes

O projeto possui testes unitários cobrindo:
Regras de domínio
Serviços de aplicação
Casos de uso principais

Ferramentas utilizadas:
xUnit
Moq
FluentAssertions
Bogus

⚙️ Configuração
Pré-requisitos

.NET SDK instalado
Banco Oracle configurado
Keycloak configurado
Conta SendGrid (ou SMTP equivalente)
Variáveis importantes
String de conexão com Oracle
Configuração do Keycloak (Authority, ClientId, etc.)

Credenciais SMTP

▶️ Execução
dotnet restore
dotnet build
dotnet run --project CanalDenuncias.API

A API estará disponível em:

https://localhost:{porta}/swagger
