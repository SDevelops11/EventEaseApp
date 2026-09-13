-- Database Setup Script for EventEase

-- Cleanup existing tables if present
IF OBJECT_ID('dbo_Bookings', 'U') IS NOT NULL DROP TABLE dbo_Bookings;
IF OBJECT_ID('dbo_Events', 'U') IS NOT NULL DROP TABLE dbo_Events;
IF OBJECT_ID('dbo_Venues', 'U') IS NOT NULL DROP TABLE dbo_Venues;
GO

-- 1. Table Creation & Constraints

-- Venues Table
CREATE TABLE dbo_Venues (
    Id NVARCHAR(450) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    Capacity INT NOT NULL,
    HourlyRate DECIMAL(18, 2) NOT NULL,
    ImageUrl NVARCHAR(MAX) NULL,
    AmenitiesJson NVARCHAR(MAX) NULL,
    Status NVARCHAR(50) NOT NULL CONSTRAINT DF_dbo_Venues_Status DEFAULT 'Active',
    Description NVARCHAR(MAX) NULL,
    
    CONSTRAINT PK_dbo_Venues PRIMARY KEY (Id),
    CONSTRAINT CK_dbo_Venues_Capacity CHECK (Capacity > 0),
    CONSTRAINT CK_dbo_Venues_HourlyRate CHECK (HourlyRate >= 0.00)
);
GO

-- Events Table
CREATE TABLE dbo_Events (
    Id NVARCHAR(450) NOT NULL,
    Title NVARCHAR(250) NOT NULL,
    ClientName NVARCHAR(150) NOT NULL,
    ClientEmail NVARCHAR(200) NOT NULL,
    ExpectedAttendance INT NOT NULL CONSTRAINT DF_dbo_Events_ExpectedAttendance DEFAULT 0,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    VenueId NVARCHAR(450) NULL,
    Status NVARCHAR(50) NOT NULL CONSTRAINT DF_dbo_Events_Status DEFAULT 'Unassigned',
    ImageUrl NVARCHAR(MAX) NULL,
    
    CONSTRAINT PK_dbo_Events PRIMARY KEY (Id),
    CONSTRAINT CK_dbo_Events_Dates CHECK (EndDate >= StartDate),
    CONSTRAINT CK_dbo_Events_Attendance CHECK (ExpectedAttendance >= 0)
);
GO

-- Bookings Table
CREATE TABLE dbo_Bookings (
    Id NVARCHAR(450) NOT NULL,
    BookingCode NVARCHAR(100) NOT NULL,
    VenueId NVARCHAR(450) NOT NULL,
    EventId NVARCHAR(450) NOT NULL,
    SpecialistName NVARCHAR(150) NOT NULL,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    TotalCost DECIMAL(18, 2) NOT NULL,
    Status NVARCHAR(50) NOT NULL CONSTRAINT DF_dbo_Bookings_Status DEFAULT 'Confirmed',
    
    CONSTRAINT PK_dbo_Bookings PRIMARY KEY (Id),
    CONSTRAINT UQ_dbo_Bookings_BookingCode UNIQUE (BookingCode),
    CONSTRAINT UQ_dbo_Bookings_EventId UNIQUE (EventId),
    CONSTRAINT CK_dbo_Bookings_Dates CHECK (EndDate >= StartDate),
    CONSTRAINT CK_dbo_Bookings_TotalCost CHECK (TotalCost >= 0.00)
);
GO

-- 2. Foreign Keys & Indexes

-- Events -> Venues (Set NULL on delete)
ALTER TABLE dbo_Events
    ADD CONSTRAINT FK_dbo_Events_dbo_Venues_VenueId
    FOREIGN KEY (VenueId) REFERENCES dbo_Venues (Id)
    ON DELETE SET NULL;
GO

-- Bookings -> Venues (Restrict delete)
ALTER TABLE dbo_Bookings
    ADD CONSTRAINT FK_dbo_Bookings_dbo_Venues_VenueId
    FOREIGN KEY (VenueId) REFERENCES dbo_Venues (Id)
    ON DELETE NO ACTION;
GO

-- Bookings -> Events (Restrict delete)
ALTER TABLE dbo_Bookings
    ADD CONSTRAINT FK_dbo_Bookings_dbo_Events_EventId
    FOREIGN KEY (EventId) REFERENCES dbo_Events (Id)
    ON DELETE NO ACTION;
GO

-- Indexes
CREATE INDEX IX_dbo_Events_VenueId ON dbo_Events (VenueId);
CREATE INDEX IX_dbo_Bookings_VenueId ON dbo_Bookings (VenueId);
CREATE UNIQUE INDEX IX_dbo_Bookings_EventId ON dbo_Bookings (EventId);
GO
