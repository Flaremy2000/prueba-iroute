# Tests del backend

Proyecto xUnit para pruebas unitarias, de integración y de cobertura.

## Ejecución

```powershell
dotnet test backend\backend.slnx
```

Las pruebas unitarias y de API se ejecutan siempre. Las pruebas de integración
con SQL Server se habilitan con `IROUTE_DB_TESTS=1`:

```powershell
$env:IROUTE_DB_TESTS = "1"
dotnet test backend\backend.slnx
```

La cadena de conexión se puede cambiar con `IROUTE_TEST_DB`. Por defecto se usa:

```text
Server=.\SQLEXPRESS;Database=IRouteCommerce;Trusted_Connection=True;TrustServerCertificate=True;
```

## Cobertura

```powershell
dotnet test backend\backend.slnx `
  /p:CollectCoverage=true `
  /p:CoverletOutputFormat=cobertura `
  /p:CoverletOutput="coverage\coverage.cobertura.xml"
```

La cobertura incluye el proyecto `backend`; los tests de integración requieren
SQL Server y los stored procedures de `db/` aplicados.
