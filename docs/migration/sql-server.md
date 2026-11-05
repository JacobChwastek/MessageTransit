# Upgrading the SQL Server packages to Microsoft.Data.SqlClient 7

`MessageTransit.SqlTransport.SqlServer` and `MessageTransit.DapperIntegration` depend on `Microsoft.Data.SqlClient` 7. Their APIs, connection strings and behaviour are unchanged; the changes below come from SqlClient itself. SqlClient's [release notes](https://github.com/dotnet/SqlClient/blob/main/release-notes/7.0/7.0.0.md) cover every change.

## Microsoft Entra authentication needs a separate package

SqlClient 7 no longer depends on `Azure.Core`, `Azure.Identity` or the Microsoft Authentication Library. Connection strings that use Microsoft Entra authentication, such as `Authentication=Active Directory Default` or `Authentication=Active Directory Managed Identity`, need the `Microsoft.Data.SqlClient.Extensions.Azure` package:

```sh
dotnet add package Microsoft.Data.SqlClient.Extensions.Azure
```

The MessageTransit packages do not reference it, so applications that use SQL Server logins or integrated security carry no Azure dependencies. Add it to the application when it connects with Entra, including when the SQL Server transport's migrator creates the database user for a managed identity.

`Authentication=Active Directory Password` is obsolete in SqlClient 7. Use `Active Directory Managed Identity` for Azure-hosted workloads and `Active Directory Default` for development and CI.

## EF Core 10

EF Core 10's SQL Server provider depends on `Microsoft.Data.SqlClient` 6.1 or later. An application that also references the SQL Server transport or the Dapper package resolves SqlClient 7, and the MessageTransit EF Core suite runs against SQL Server with that combination.
