/*
    Stored procedures de la prueba.
    Se ejecutan después del esquema y de la columna motivo.
*/
SET NOCOUNT ON;
GO

USE IRouteCommerce;
GO

/* ------------------------------------------------------------------
   Inserta el contenido del CSV en bloque usando un TVP.
   Devuelve la cantidad de filas insertadas.
   ------------------------------------------------------------------ */
CREATE OR ALTER PROCEDURE dbo.sp_create_commerce
    @rows dbo.CommerceTvp READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.commerce (pc_nomcomred, pc_numdoc, pc_processdate)
        SELECT
            NULLIF(LTRIM(RTRIM(r.pc_nomcomred)), N''),
            NULLIF(LTRIM(RTRIM(r.pc_numdoc)), N''),
            r.pc_processdate
        FROM @rows AS r;

        DECLARE @inserted INT = @@ROWCOUNT;

        COMMIT TRANSACTION;

        SELECT @inserted AS inserted;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;
GO

/* ------------------------------------------------------------------
   Valida los registros de una fecha de proceso:
     - pc_nomcomred no puede estar vacío ni contener solo espacios.
     - pc_numdoc no puede estar vacío ni contener letras o símbolos.
   Los inválidos se copian a commerce_quarantine con su motivo y se
   eliminan de commerce en la misma transacción.
   Devuelve la cantidad movida a cuarentena y la que quedó válida.
   ------------------------------------------------------------------ */
CREATE OR ALTER PROCEDURE dbo.sp_process_commerce
    @pc_processdate DATE
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @pc_processdate IS NULL
    BEGIN
        THROW 50001, N'La fecha de proceso es obligatoria.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @invalid TABLE
        (
            id     BIGINT        NOT NULL PRIMARY KEY,
            motivo NVARCHAR(400) NOT NULL
        );

        INSERT INTO @invalid (id, motivo)
        SELECT
            c.id,
            CONCAT_WS(N'; ',
                CASE
                    WHEN NULLIF(LTRIM(RTRIM(c.pc_nomcomred)), N'') IS NULL
                        THEN N'El nombre del comercio (pc_nomcomred) se encuentra vacío'
                END,
                CASE
                    WHEN NULLIF(LTRIM(RTRIM(c.pc_numdoc)), N'') IS NULL
                        THEN N'El número de documento (pc_numdoc) se encuentra vacío'
                    WHEN LTRIM(RTRIM(c.pc_numdoc)) LIKE N'%[^0-9]%'
                        THEN N'El número de documento (pc_numdoc) contiene letras o caracteres especiales'
                END)
        FROM dbo.commerce AS c WITH (UPDLOCK, HOLDLOCK)
        WHERE c.pc_processdate = @pc_processdate
          AND (
                NULLIF(LTRIM(RTRIM(c.pc_nomcomred)), N'') IS NULL
             OR NULLIF(LTRIM(RTRIM(c.pc_numdoc)), N'') IS NULL
             OR LTRIM(RTRIM(c.pc_numdoc)) LIKE N'%[^0-9]%'
          );

        INSERT INTO dbo.commerce_quarantine (pc_nomcomred, pc_numdoc, pc_processdate, motivo)
        SELECT c.pc_nomcomred, c.pc_numdoc, c.pc_processdate, i.motivo
        FROM dbo.commerce AS c
        INNER JOIN @invalid AS i ON i.id = c.id;

        DECLARE @quarantined INT = @@ROWCOUNT;

        DELETE c
        FROM dbo.commerce AS c
        INNER JOIN @invalid AS i ON i.id = c.id;

        DECLARE @remaining INT =
        (
            SELECT COUNT(*)
            FROM dbo.commerce
            WHERE pc_processdate = @pc_processdate
        );

        COMMIT TRANSACTION;

        SELECT
            @quarantined AS quarantined,
            @remaining   AS remaining;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;
GO
