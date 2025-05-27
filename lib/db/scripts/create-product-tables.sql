-- Create TMs table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TMs')
BEGIN
    CREATE TABLE TMs (
        id UNIQUEIDENTIFIER PRIMARY KEY,
        name NVARCHAR(255) NOT NULL,
        version NVARCHAR(50) NOT NULL,
        tmLink NVARCHAR(500),         -- Ensure this matches exactly with the model
        jiraLink NVARCHAR(500),
        createdAt DATETIME NOT NULL,
        updatedAt DATETIME NOT NULL
    );
    
    CREATE INDEX IX_TMs_Name ON TMs(name);
    CREATE INDEX IX_TMs_Version ON TMs(version);
END

-- Create ECOAs table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ECOAs')
BEGIN
    CREATE TABLE ECOAs (
        id UNIQUEIDENTIFIER PRIMARY KEY,
        name NVARCHAR(255) NOT NULL,
        version NVARCHAR(50) NOT NULL,
        createdAt DATETIME NOT NULL,
        updatedAt DATETIME NOT NULL
    );
    
    CREATE INDEX IX_ECOAs_Name ON ECOAs(name);
    CREATE INDEX IX_ECOAs_Version ON ECOAs(version);
END

-- Create ExternalModuleTypes table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExternalModuleTypes')
BEGIN
    CREATE TABLE ExternalModuleTypes (
        id UNIQUEIDENTIFIER PRIMARY KEY,
        name NVARCHAR(50) NOT NULL,
        createdAt DATETIME NOT NULL,
        updatedAt DATETIME NOT NULL,
        CONSTRAINT UQ_ExternalModuleTypes_Name UNIQUE (name)
    );
    
    CREATE INDEX IX_ExternalModuleTypes_Name ON ExternalModuleTypes(name);
END

-- Create IRTs table (depends on TMs and ECOAs)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'IRTs')
BEGIN
    CREATE TABLE IRTs (
        id UNIQUEIDENTIFIER PRIMARY KEY,
        name NVARCHAR(255) NOT NULL,
        protocol NVARCHAR(100) NOT NULL,
        version NVARCHAR(50) NOT NULL,
        isOpenLabel BIT NOT NULL DEFAULT 0,
        irtLink NVARCHAR(500),
        jiraLink NVARCHAR(500),
        tmId UNIQUEIDENTIFIER NOT NULL,
        ecoaId UNIQUEIDENTIFIER,
        createdAt DATETIME NOT NULL,
        updatedAt DATETIME NOT NULL,
        CONSTRAINT FK_IRTs_TMs FOREIGN KEY (tmId) REFERENCES TMs(id) ON DELETE CASCADE,
        CONSTRAINT FK_IRTs_ECOAs FOREIGN KEY (ecoaId) REFERENCES ECOAs(id)
    );
    
    CREATE INDEX IX_IRTs_Name ON IRTs(name);
    CREATE INDEX IX_IRTs_Protocol ON IRTs(protocol);
    CREATE INDEX IX_IRTs_Version ON IRTs(version);
    CREATE INDEX IX_IRTs_TmId ON IRTs(tmId);
    CREATE INDEX IX_IRTs_EcoaId ON IRTs(ecoaId);
END

-- Create ExternalModules table (depends on IRTs and ExternalModuleTypes)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExternalModules')
BEGIN
    CREATE TABLE ExternalModules (
        id UNIQUEIDENTIFIER PRIMARY KEY,
        name NVARCHAR(255) NOT NULL,
        isCustom BIT NOT NULL DEFAULT 0,
        version NVARCHAR(50) NOT NULL,
        typeId UNIQUEIDENTIFIER NOT NULL,
        irtId UNIQUEIDENTIFIER NOT NULL,
        createdAt DATETIME NOT NULL,
        updatedAt DATETIME NOT NULL,
        CONSTRAINT FK_ExternalModules_ExternalModuleTypes FOREIGN KEY (typeId) REFERENCES ExternalModuleTypes(id),
        CONSTRAINT FK_ExternalModules_IRTs FOREIGN KEY (irtId) REFERENCES IRTs(id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_ExternalModules_Name ON ExternalModules(name);
    CREATE INDEX IX_ExternalModules_Version ON ExternalModules(version);
    CREATE INDEX IX_ExternalModules_TypeId ON ExternalModules(typeId);
    CREATE INDEX IX_ExternalModules_IrtId ON ExternalModules(irtId);
END
