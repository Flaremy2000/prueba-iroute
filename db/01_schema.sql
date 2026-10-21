/*
    Esquema base de la prueba.
    - commerce: registros cargados desde el CSV.
    - commerce_quarantine: registros rechazados. La columna motivo se agrega en el paso 02.
    - CommerceTvp: tipo tabla para insertar el CSV en una sola llamada al SP.
*/
SET NOCOUNT ON;
GO

USE IRouteCommerce;
GO

IF OBJECT_ID(N'dbo.commerce', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.commerce
    (
        id             BIGINT IDENTITY(1,1) NOT NULL,
        pc_nomcomred   NVARCHAR(200) NULL,
        pc_numdoc      NVARCHAR(50)  NULL,
        pc_processdate DATE          NULL,
        CONSTRAINT PK_commerce PRIMARY KEY CLUSTERED (id)
    );
END;
GO

IF OBJECT_ID(N'dbo.commerce_quarantine', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.commerce_quarantine
    (
        id             BIGINT IDENTITY(1,1) NOT NULL,
        pc_nomcomred   NVARCHAR(200) NULL,
        pc_numdoc      NVARCHAR(50)  NULL,
        pc_processdate DATE          NULL,
        CONSTRAINT PK_commerce_quarantine PRIMARY KEY CLUSTERED (id)
    );
END;
GO

IF OBJECT_ID(N'dbo.commerce_uploads', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.commerce_uploads
    (
        id             BIGINT IDENTITY(1,1) NOT NULL,
        file_name      NVARCHAR(255) NOT NULL,
        content_hash   CHAR(64)      NOT NULL,
        process_date   DATE          NOT NULL,
        rows_inserted  INT           NOT NULL,
        uploaded_at    DATETIME2(0)  NOT NULL
            CONSTRAINT DF_commerce_uploads_uploaded_at DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_commerce_uploads PRIMARY KEY CLUSTERED (id),
        CONSTRAINT UQ_commerce_uploads_content_hash UNIQUE (content_hash)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_commerce_pc_processdate'
      AND object_id = OBJECT_ID(N'dbo.commerce')
)
BEGIN
    CREATE INDEX IX_commerce_pc_processdate
        ON dbo.commerce (pc_processdate);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_commerce_quarantine_pc_processdate'
      AND object_id = OBJECT_ID(N'dbo.commerce_quarantine')
)
BEGIN
    CREATE INDEX IX_commerce_quarantine_pc_processdate
        ON dbo.commerce_quarantine (pc_processdate);
END;
GO

IF TYPE_ID(N'dbo.CommerceTvp') IS NULL
BEGIN
    CREATE TYPE dbo.CommerceTvp AS TABLE
    (
        pc_nomcomred   NVARCHAR(200) NULL,
        pc_numdoc      NVARCHAR(50)  NULL,
        pc_processdate DATE          NULL
    );
END;
GO
