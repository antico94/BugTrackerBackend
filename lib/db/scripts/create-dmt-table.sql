CREATE TABLE DMTs (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX) NULL,
    dateCreated DATETIME NOT NULL DEFAULT GETDATE(),
    isCompleted BIT NOT NULL DEFAULT 0,
    completedAt DATETIME NULL,
    createdBy NVARCHAR(255) NULL,
    updatedAt DATETIME NULL
);

-- Create a junction table for the many-to-many relationship between DMTs and Bugs
CREATE TABLE DMT_Bugs (
    dmtId UNIQUEIDENTIFIER NOT NULL,
    bugId UNIQUEIDENTIFIER NOT NULL,
    dateAdded DATETIME NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY (dmtId, bugId),
    FOREIGN KEY (dmtId) REFERENCES DMTs(id) ON DELETE CASCADE,
    FOREIGN KEY (bugId) REFERENCES Bugs(id) ON DELETE CASCADE
);

-- Add index for faster lookups
CREATE INDEX IX_DMT_Bugs_BugId ON DMT_Bugs(bugId);
CREATE INDEX IX_DMT_Bugs_DmtId ON DMT_Bugs(dmtId);
