IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Studies')
BEGIN
    CREATE TABLE Studies (
        id UNIQUEIDENTIFIER PRIMARY KEY,
        jiraKey NVARCHAR(100) NOT NULL,
        jiraLink NVARCHAR(500) NOT NULL,
        summary NVARCHAR(500) NOT NULL,
        irtVersion NVARCHAR(100) NOT NULL,
        clientName NVARCHAR(200) NOT NULL,
        protocol NVARCHAR(200) NOT NULL,
        irtLink NVARCHAR(500) NOT NULL,
        tmLink NVARCHAR(500) NOT NULL,
        openLabel BIT NOT NULL DEFAULT 0,
        createdAt DATETIME NOT NULL,
        updatedAt DATETIME NOT NULL
    );
    
    CREATE INDEX IX_Studies_JiraKey ON Studies(jiraKey);
    CREATE INDEX IX_Studies_ClientName ON Studies(clientName);
END
