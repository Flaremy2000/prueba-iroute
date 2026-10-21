SET NOCOUNT ON;
GO

USE IRouteCommerce;
GO

IF OBJECT_ID(N'dbo.users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.users
    (
        id             BIGINT IDENTITY(1,1) NOT NULL,
        username       NVARCHAR(100) NOT NULL,
        password_hash  NVARCHAR(256) NOT NULL,
        role           NVARCHAR(50)  NOT NULL,
        is_active      BIT           NOT NULL
            CONSTRAINT DF_users_is_active DEFAULT (1),
        created_at     DATETIME2(0)  NOT NULL
            CONSTRAINT DF_users_created_at DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_users PRIMARY KEY CLUSTERED (id),
        CONSTRAINT UQ_users_username UNIQUE (username)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE username = N'admin')
BEGIN
    INSERT INTO dbo.users (username, password_hash, role, is_active)
    VALUES
    (
        N'admin',
        N'PBKDF2-SHA256$210000$3P3bD+W5i0Qz6OyNuwPC2Q==$HBX4vAeJWV01LD2PKkUvWD8bvARKrk7CIv0dh6zpNA0=',
        N'Admin',
        1
    );
END;
GO
