-- Migration: Add VehicleType, DistanceKm, Fare columns to Trips table
-- Run on: RideHailing_North, RideHailing_North_Replica, RideHailing_South, RideHailing_South_Replica

USE [RideHailing_North];
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Trips') AND name = 'VehicleType')
    ALTER TABLE dbo.Trips ADD VehicleType NVARCHAR(50) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Trips') AND name = 'DistanceKm')
    ALTER TABLE dbo.Trips ADD DistanceKm FLOAT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Trips') AND name = 'Fare')
    ALTER TABLE dbo.Trips ADD Fare DECIMAL(10, 2) NULL;
GO

USE [RideHailing_North_Replica];
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Trips') AND name = 'VehicleType')
    ALTER TABLE dbo.Trips ADD VehicleType NVARCHAR(50) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Trips') AND name = 'DistanceKm')
    ALTER TABLE dbo.Trips ADD DistanceKm FLOAT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Trips') AND name = 'Fare')
    ALTER TABLE dbo.Trips ADD Fare DECIMAL(10, 2) NULL;
GO

USE [RideHailing_South];
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Trips') AND name = 'VehicleType')
    ALTER TABLE dbo.Trips ADD VehicleType NVARCHAR(50) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Trips') AND name = 'DistanceKm')
    ALTER TABLE dbo.Trips ADD DistanceKm FLOAT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Trips') AND name = 'Fare')
    ALTER TABLE dbo.Trips ADD Fare DECIMAL(10, 2) NULL;
GO

USE [RideHailing_South_Replica];
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Trips') AND name = 'VehicleType')
    ALTER TABLE dbo.Trips ADD VehicleType NVARCHAR(50) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Trips') AND name = 'DistanceKm')
    ALTER TABLE dbo.Trips ADD DistanceKm FLOAT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Trips') AND name = 'Fare')
    ALTER TABLE dbo.Trips ADD Fare DECIMAL(10, 2) NULL;
GO
