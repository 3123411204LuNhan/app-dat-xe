-- Migration: Add Ratings table + IsLocked to Users/Drivers
-- Run on: RideHailing_North, RideHailing_North_Replica, RideHailing_South, RideHailing_South_Replica

USE [RideHailing_North];
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'IsLocked')
    ALTER TABLE dbo.Users ADD IsLocked BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Drivers') AND name = 'IsLocked')
    ALTER TABLE dbo.Drivers ADD IsLocked BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ratings')
CREATE TABLE dbo.Ratings (
    RatingId  INT IDENTITY(1,1) PRIMARY KEY,
    TripID    INT NOT NULL,
    UserID    INT NOT NULL,
    Score     INT NOT NULL CHECK (Score BETWEEN 1 AND 5),
    Comment   NVARCHAR(500) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_Ratings_Trip UNIQUE (TripID),
    CONSTRAINT FK_Ratings_Trips FOREIGN KEY (TripID) REFERENCES dbo.Trips(TripID)
);
GO

USE [RideHailing_North_Replica];
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'IsLocked')
    ALTER TABLE dbo.Users ADD IsLocked BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Drivers') AND name = 'IsLocked')
    ALTER TABLE dbo.Drivers ADD IsLocked BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ratings')
CREATE TABLE dbo.Ratings (
    RatingId  INT IDENTITY(1,1) PRIMARY KEY,
    TripID    INT NOT NULL,
    UserID    INT NOT NULL,
    Score     INT NOT NULL CHECK (Score BETWEEN 1 AND 5),
    Comment   NVARCHAR(500) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_Ratings_Trip UNIQUE (TripID),
    CONSTRAINT FK_Ratings_Trips FOREIGN KEY (TripID) REFERENCES dbo.Trips(TripID)
);
GO

USE [RideHailing_South];
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'IsLocked')
    ALTER TABLE dbo.Users ADD IsLocked BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Drivers') AND name = 'IsLocked')
    ALTER TABLE dbo.Drivers ADD IsLocked BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ratings')
CREATE TABLE dbo.Ratings (
    RatingId  INT IDENTITY(1,1) PRIMARY KEY,
    TripID    INT NOT NULL,
    UserID    INT NOT NULL,
    Score     INT NOT NULL CHECK (Score BETWEEN 1 AND 5),
    Comment   NVARCHAR(500) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_Ratings_Trip UNIQUE (TripID),
    CONSTRAINT FK_Ratings_Trips FOREIGN KEY (TripID) REFERENCES dbo.Trips(TripID)
);
GO

USE [RideHailing_South_Replica];
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'IsLocked')
    ALTER TABLE dbo.Users ADD IsLocked BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Drivers') AND name = 'IsLocked')
    ALTER TABLE dbo.Drivers ADD IsLocked BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ratings')
CREATE TABLE dbo.Ratings (
    RatingId  INT IDENTITY(1,1) PRIMARY KEY,
    TripID    INT NOT NULL,
    UserID    INT NOT NULL,
    Score     INT NOT NULL CHECK (Score BETWEEN 1 AND 5),
    Comment   NVARCHAR(500) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_Ratings_Trip UNIQUE (TripID),
    CONSTRAINT FK_Ratings_Trips FOREIGN KEY (TripID) REFERENCES dbo.Trips(TripID)
);
GO
