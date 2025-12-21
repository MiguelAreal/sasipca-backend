# SASIPCA - Backend API (.NET)
Este repositório contém a API RESTful e os serviços de backend para o Sistema de Apoio Social do IPCA (SASIPCA). Desenvolvido em .NET, serve como o núcleo de processamento de dados, autenticação e lógica de negócio para as aplicações cliente (Android e Desktop).

## Visão Geral 
O backend é responsável pela gestão centralizada de inventário, controlo de movimentos de stock, gestão de entregas e integração com serviços externos.
A arquitetura segue padrões de design robustos para garantir escalabilidade e manutenção.

### Principais Funcionalidades
* **API RESTful:** Endpoints para gestão de produtos, beneficiários, entregas e stock.
* **Autenticação e Autorização:** Integração com Microsoft Identity Platform (Azure Active Directory) para validação de tokens JWT e gestão de papéis (Roles).
* **Comunicação em Tempo Real:** Utilização de SignalR e Firebase Messaging para notificar os clientes sobre alterações de estado (alertas).
* **Tarefas em Segundo Plano (Background Jobs):** Utilização do Hangfire com persistência em MariaDB para agendamento de tarefas críticas (validades, limpezas).
* **Geração de Relatórios:** Motor de renderização de HTML para PDF para guias de entrega e relatórios de inventário.
* **Integração Externa:** Sincronização de dados de produtos com a API OpenFoodFacts e notificações via Firebase (FCM).

## Stack Tecnológico
* **Framework:** .NET 10
* **Linguagem:** C#
* **Base de Dados:** MariaDB via Entity Framework Core.
* **Documentação API:** Swagger / OpenAPI.
* **Tarefas Agendadas:** Hangfire.
* **Tempo Real:** ASP.NET Core SignalR.
* **Logging:** Serilog (Logs em ficheiro e consola).

## Configuração
A aplicação utiliza variáveis de ambiente (ficheiro `.env` ou sistema) para secrets, e o `appsettings.json` para configurações gerais.

### 1. Ficheiro .env (Segredos)
Crie um ficheiro `.env` na raiz do projeto (ao lado do `Program.cs`) com as seguintes chaves:

```env
JWT_KEY=UmaChaveMuitoSeguraeLongaParaAssinarOsTokensJWT123456
DB_CONNECTION_KEY=server=localhost;user=sasipca_user;password=tua_password;database=sasipca_db;
AZURE_CLIENT_ID=O_Client_ID_Da_Tua_App_No_Azure

```

### 2. Firebase
Coloque o ficheiro de credenciais do Firebase (`sasipca-2ea18-firebase-adminsdk-fbsvc-5d72cf6e66.json`) na raiz do projeto para habilitar as notificações push para Android.

### 3. Ficheiros de Template
A pasta `ReportTemplates` deve existir na raiz da aplicação (onde está o executável) e conter os ficheiros `.html` base para a geração de relatórios PDF. Estes ficheiros já estão incluídos no repositório.

## Base de Dados
O projeto utiliza Entity Framework Core Code-First com MariaDB.
Ao iniciar, o **Hangfire** cria automaticamente as suas tabelas necessárias (`Hangfire_*`).

Para aplicar as migrações iniciais da aplicação:

```bash
dotnet ef database update
```

```

A API ficará disponível (por defeito) em `https://localhost:7226` ou `http://localhost:5226`.
O Swagger UI: `/swagger`.
O Hangfire Dashboard: `/hangfire`.

###Em Produção (Raspberry Pi / Linux)Recomenda-se publicar a aplicação e correr como um serviço (Systemd).

1. **Publicar:**
```bash
dotnet publish -c Release -r linux-arm64 --self-contained false -o ./publish

```


2. **Executar:**
```bash
cd ./publish
./sasipca_API

```



## Logs
Os logs são gerados via Serilog e guardados na pasta `Logs/` na raiz da aplicação. É criado um ficheiro novo por dia (`sasipca-log-YYYYMMDD.txt`) e retido por 7 dias.

## Resolução de Problemas Comuns
### Erro de Conexão à Base de Dados
Verifique se a `DB_CONNECTION_KEY` no ficheiro `.env` está correta e se o utilizador do MariaDB tem permissões para criar tabelas (necessário para o EF Core e Hangfire).
