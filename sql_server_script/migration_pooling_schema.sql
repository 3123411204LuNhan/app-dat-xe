-- ===== SQL Migration: Thêm Pooling Support =====
-- Chạy script này trên cả 4 databases: north.sql, north_rep.sql, south.sql, south_rep.sql

-- Thêm cột vào Trips table để support pooling
ALTER TABLE Trips
ADD PooledWithTripID INT NULL,
    MaxPassengers INT DEFAULT 2,
    CurrentPassengers INT DEFAULT 1;

-- Thêm index để tìm kiếm cuốc ghép nhanh hơn
CREATE INDEX IX_Trips_PooledWith ON Trips(PooledWithTripID)
WHERE PooledWithTripID IS NOT NULL;

-- Thêm constraint để đảm bảo tính toàn vẹn (optional)
ALTER TABLE Trips
ADD CONSTRAINT FK_Trips_PooledWith 
FOREIGN KEY (PooledWithTripID) REFERENCES Trips(TripID);

-- Thêm cột GPS (latitude, longitude) cho mỗi trip để tính khoảng cách dễ hơn
-- (Optional: nếu muốn lưu GPS riêng, không chỉ trong tên địa điểm)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'PickupLatitude' AND Object_ID = OBJECT_ID('dbo.Trips'))
BEGIN
    ALTER TABLE Trips
    ADD PickupLatitude FLOAT NULL,
        PickupLongitude FLOAT NULL,
        DropoffLatitude FLOAT NULL,
        DropoffLongitude FLOAT NULL;
END

-- Thêm bảng PoolingHistory để track lịch sử ghép cuốc (optional)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE Name = 'PoolingHistory')
BEGIN
    CREATE TABLE PoolingHistory (
        PoolingID INT PRIMARY KEY IDENTITY(1,1),
        MainTripID INT NOT NULL,
        SecondaryTripID INT NOT NULL,
        PooledAt DATETIME DEFAULT GETDATE(),
        UnpooledAt DATETIME NULL,
        PickupDistance FLOAT,
        DropoffDistance FLOAT,
        FOREIGN KEY (MainTripID) REFERENCES Trips(TripID),
        FOREIGN KEY (SecondaryTripID) REFERENCES Trips(TripID)
    );
END

-- Thêm index trên Status và Region để tìm Pending trips nhanh hơn
CREATE INDEX IX_Trips_StatusRegion ON Trips(Status, Region)
WHERE Status = 'Pending';

GO
