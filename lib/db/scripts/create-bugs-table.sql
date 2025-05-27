CREATE TABLE Bugs (
  id UNIQUEIDENTIFIER PRIMARY KEY,
  jiraKey NVARCHAR(50) NOT NULL,
  title NVARCHAR(255) NOT NULL,
  description NVARCHAR(MAX),
  severity NVARCHAR(50) NOT NULL,
  foundInBuild NVARCHAR(50),
  affectedVersions NVARCHAR(MAX),
  productId UNIQUEIDENTIFIER,
  assignedTo NVARCHAR(100),
  status NVARCHAR(50) NOT NULL,
  createdAt DATETIME NOT NULL,
  updatedAt DATETIME NOT NULL,
  isAssessmentCompleted BIT DEFAULT 0,
  assessedProductType NVARCHAR(50)
);

CREATE INDEX idx_bugs_jiraKey ON Bugs(jiraKey);
CREATE INDEX idx_bugs_status ON Bugs(status);
CREATE INDEX idx_bugs_severity ON Bugs(severity);
CREATE INDEX idx_bugs_productId ON Bugs(productId);
