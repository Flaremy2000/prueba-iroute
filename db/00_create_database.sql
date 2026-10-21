/*
    Crea la base de datos si no existe.
    Se ejecuta antes que los demás scripts.
*/
SET NOCOUNT ON;
GO

IF DB_ID(N'IRouteCommerce') IS NULL
BEGIN
    CREATE DATABASE IRouteCommerce;
END;
GO
