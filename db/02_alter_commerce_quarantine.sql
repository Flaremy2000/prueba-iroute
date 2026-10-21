/*
    Paso 02: agregar la columna motivo a commerce_quarantine.
    Es idempotente: se puede ejecutar varias veces sin error.
*/
SET NOCOUNT ON;
GO

USE IRouteCommerce;
GO

IF COL_LENGTH(N'dbo.commerce_quarantine', N'motivo') IS NULL
BEGIN
    ALTER TABLE dbo.commerce_quarantine
        ADD motivo NVARCHAR(400) NOT NULL
            CONSTRAINT DF_commerce_quarantine_motivo DEFAULT (N'');
END;
GO
