-- LumaLog SQL Server Tables

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LumaLog_Entries')
BEGIN
    CREATE TABLE LumaLog_Entries (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        Level INT NOT NULL,
        Message NVARCHAR(MAX) NOT NULL,
        Exception NVARCHAR(500) NULL,
        StackTrace NVARCHAR(MAX) NULL,
        Source NVARCHAR(500) NULL,
        TraceId NVARCHAR(50) NULL,
        SpanId NVARCHAR(50) NULL,
        ParentSpanId NVARCHAR(50) NULL,
        UserId NVARCHAR(100) NULL,
        UserName NVARCHAR(256) NULL,
        IpAddress NVARCHAR(50) NULL,
        RequestPath NVARCHAR(2048) NULL,
        RequestMethod NVARCHAR(10) NULL,
        StatusCode INT NULL,
        MachineName NVARCHAR(100) NULL,
        Environment NVARCHAR(50) NULL,
        CustomData NVARCHAR(MAX) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        IsResolved BIT NOT NULL DEFAULT 0,
        ResolvedAt DATETIMEOFFSET NULL,
        ResolvedBy NVARCHAR(256) NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LumaLog_Traces')
BEGIN
    CREATE TABLE LumaLog_Traces (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        TraceId NVARCHAR(50) NOT NULL,
        SpanId NVARCHAR(50) NOT NULL,
        ParentSpanId NVARCHAR(50) NULL,
        Name NVARCHAR(500) NOT NULL,
        StartTime DATETIMEOFFSET NOT NULL,
        EndTime DATETIMEOFFSET NULL,
        DurationMs BIGINT NULL,
        Status INT NOT NULL DEFAULT 0,
        StatusMessage NVARCHAR(MAX) NULL,
        Tags NVARCHAR(MAX) NULL,
        Events NVARCHAR(MAX) NULL,
        ServiceName NVARCHAR(256) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
END
GO
