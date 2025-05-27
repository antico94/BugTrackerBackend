CREATE TABLE Notes (
  id UNIQUEIDENTIFIER PRIMARY KEY,
  content NVARCHAR(MAX) NOT NULL,
  entityType NVARCHAR(50) NOT NULL,
  entityId UNIQUEIDENTIFIER NOT NULL,
  createdAt DATETIME2 NOT NULL DEFAULT GETDATE(),
  updatedAt DATETIME2 NULL,
  isDeleted BIT NOT NULL DEFAULT 0,
  deletedAt DATETIME2 NULL
);

-- Create indexes for faster querying
CREATE INDEX IX_Notes_EntityType_EntityId ON Notes (entityType, entityId);
CREATE INDEX IX_Notes_CreatedAt ON Notes (createdAt DESC);
CREATE INDEX IX_Notes_IsDeleted ON Notes (isDeleted);
