/*
    Datos de prueba opcionales.
    Limpia las tablas para dejar un escenario reproducible.
    No ejecutar en un ambiente con datos reales.
*/
SET NOCOUNT ON;
GO

USE IRouteCommerce;
GO

TRUNCATE TABLE dbo.commerce_quarantine;
TRUNCATE TABLE dbo.commerce;
GO

INSERT INTO dbo.commerce (pc_nomcomred, pc_numdoc, pc_processdate)
VALUES
    (N'Comercio Uno',    N'10203040', '2026-10-20'),
    (N'Comercio Dos',    N'99887766', '2026-10-20'),
    (NULL,               N'12345678', '2026-10-20'),
    (N'   ',             N'87654321', '2026-10-20'),
    (N'Comercio Tres',   NULL,       '2026-10-20'),
    (N'Comercio Cuatro', N'12AB34',  '2026-10-20'),
    (N'Comercio Cinco',  N'12-34',   '2026-10-20'),
    (N'Comercio Seis',   N'55667788', '2026-10-21');
GO

SELECT id, pc_nomcomred, pc_numdoc, pc_processdate
FROM dbo.commerce
ORDER BY id;
GO
