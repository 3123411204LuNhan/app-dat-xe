-- Run on: North_Primary, North_Replica, South_Primary, South_Replica
IF NOT EXISTS (SELECT 1 FROM sysobjects WHERE name='ScheduledTrips' AND xtype='U')
BEGIN
    CREATE TABLE ScheduledTrips (
        ScheduledTripId     INT IDENTITY(1,1) PRIMARY KEY,
        UserId              INT NOT NULL,
        PickupAddress       NVARCHAR(500) NOT NULL,
        PickupLat           FLOAT NULL,
        PickupLng           FLOAT NULL,
        DropoffAddress      NVARCHAR(500) NOT NULL,
        DropoffLat          FLOAT NULL,
        DropoffLng          FLOAT NULL,
        VehicleType         NVARCHAR(50)  NOT NULL DEFAULT 'Xe may',
        ScheduledPickupTime DATETIME      NOT NULL,
        Status              NVARCHAR(50)  NOT NULL DEFAULT 'Scheduled',
        Region              NVARCHAR(50)  NOT NULL DEFAULT 'South',
        TripId              INT NULL,
        EstimatedFare       DECIMAL(10,2) NULL,
        DistanceKm          FLOAT NULL,
        CreatedAt           DATETIME      NOT NULL DEFAULT GETDATE(),
        UpdatedAt           DATETIME      NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_ScheduledTrips_UserId  ON ScheduledTrips(UserId);
    CREATE INDEX IX_ScheduledTrips_Status  ON ScheduledTrips(Status, ScheduledPickupTime);
END
