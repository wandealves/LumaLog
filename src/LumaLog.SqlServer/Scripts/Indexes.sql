-- LumaLog SQL Server Indexes

-- Entries indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LumaLog_Entries_CreatedAt')
    CREATE INDEX IX_LumaLog_Entries_CreatedAt ON LumaLog_Entries (CreatedAt DESC);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LumaLog_Entries_Level')
    CREATE INDEX IX_LumaLog_Entries_Level ON LumaLog_Entries (Level);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LumaLog_Entries_TraceId')
    CREATE INDEX IX_LumaLog_Entries_TraceId ON LumaLog_Entries (TraceId) WHERE TraceId IS NOT NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LumaLog_Entries_UserId')
    CREATE INDEX IX_LumaLog_Entries_UserId ON LumaLog_Entries (UserId) WHERE UserId IS NOT NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LumaLog_Entries_Level_CreatedAt')
    CREATE INDEX IX_LumaLog_Entries_Level_CreatedAt ON LumaLog_Entries (Level, CreatedAt DESC);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LumaLog_Entries_IsResolved')
    CREATE INDEX IX_LumaLog_Entries_IsResolved ON LumaLog_Entries (IsResolved) WHERE IsResolved = 0;
GO

-- Traces indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LumaLog_Traces_TraceId')
    CREATE INDEX IX_LumaLog_Traces_TraceId ON LumaLog_Traces (TraceId);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LumaLog_Traces_SpanId')
    CREATE UNIQUE INDEX IX_LumaLog_Traces_SpanId ON LumaLog_Traces (SpanId);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LumaLog_Traces_StartTime')
    CREATE INDEX IX_LumaLog_Traces_StartTime ON LumaLog_Traces (StartTime DESC);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LumaLog_Traces_ServiceName')
    CREATE INDEX IX_LumaLog_Traces_ServiceName ON LumaLog_Traces (ServiceName) WHERE ServiceName IS NOT NULL;
GO
