-- ============================================================================
-- EventEase Database Setup & Migration Script (Parts 1, 2, and 3 Complete)
-- Module: CLDV7111 - Cloud Development A
-- ============================================================================

-- ----------------------------------------------------------------------------
-- SECTION 1: CLEANUP / RESET (OPTIONAL FOR FRESH SETUP)
-- ----------------------------------------------------------------------------
/*
IF OBJECT_ID('vw_ConsolidatedBookings', 'V') IS NOT NULL DROP VIEW vw_ConsolidatedBookings;
IF OBJECT_ID('dbo_Bookings', 'U') IS NOT NULL DROP TABLE dbo_Bookings;
IF OBJECT_ID('dbo_Events', 'U') IS NOT NULL DROP TABLE dbo_Events;
IF OBJECT_ID('dbo_Venues', 'U') IS NOT NULL DROP TABLE dbo_Venues;
IF OBJECT_ID('dbo_EventTypes', 'U') IS NOT NULL DROP TABLE dbo_EventTypes;
GO
*/

-- ----------------------------------------------------------------------------
-- SECTION 2: TABLE CREATION (WITH PART 3 EXTENSIONS)
-- ----------------------------------------------------------------------------

-- 1. EventType Lookup Table (Part 3 Requirement)
IF OBJECT_ID('dbo_EventTypes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo_EventTypes (
        Id NVARCHAR(450) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(250) NULL,
        Icon NVARCHAR(50) NULL CONSTRAINT DF_dbo_EventTypes_Icon DEFAULT 'bi-calendar-event',
        
        CONSTRAINT PK_dbo_EventTypes PRIMARY KEY (Id),
        CONSTRAINT UQ_dbo_EventTypes_Name UNIQUE (Name)
    );
END
GO

-- 2. Venues Table (With Part 3 Availability Field)
IF OBJECT_ID('dbo_Venues', 'U') IS NULL
BEGIN
    CREATE TABLE dbo_Venues (
        Id NVARCHAR(450) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Location NVARCHAR(200) NOT NULL,
        Capacity INT NOT NULL,
        HourlyRate DECIMAL(18, 2) NOT NULL,
        ImageUrl NVARCHAR(MAX) NULL,
        AmenitiesJson NVARCHAR(MAX) NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_dbo_Venues_Status DEFAULT 'Active',
        IsAvailable BIT NOT NULL CONSTRAINT DF_dbo_Venues_IsAvailable DEFAULT 1,
        Description NVARCHAR(MAX) NULL,
        
        CONSTRAINT PK_dbo_Venues PRIMARY KEY (Id),
        CONSTRAINT CK_dbo_Venues_Capacity CHECK (Capacity > 0),
        CONSTRAINT CK_dbo_Venues_HourlyRate CHECK (HourlyRate >= 0.00)
    );
END
ELSE
BEGIN
    -- Ensure IsAvailable column exists on existing dbo_Venues table (Part 3 Migration)
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo_Venues') AND name = 'IsAvailable')
    BEGIN
        ALTER TABLE dbo_Venues ADD IsAvailable BIT NOT NULL CONSTRAINT DF_dbo_Venues_IsAvailable DEFAULT 1;
    END
END
GO

-- 3. Events Table (With Part 3 EventTypeId Foreign Key)
IF OBJECT_ID('dbo_Events', 'U') IS NULL
BEGIN
    CREATE TABLE dbo_Events (
        Id NVARCHAR(450) NOT NULL,
        Title NVARCHAR(250) NOT NULL,
        ClientName NVARCHAR(150) NOT NULL,
        ClientEmail NVARCHAR(200) NOT NULL,
        ExpectedAttendance INT NOT NULL CONSTRAINT DF_dbo_Events_ExpectedAttendance DEFAULT 0,
        StartDate DATETIME2 NOT NULL,
        EndDate DATETIME2 NOT NULL,
        VenueId NVARCHAR(450) NULL,
        EventTypeId NVARCHAR(450) NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_dbo_Events_Status DEFAULT 'Unassigned',
        ImageUrl NVARCHAR(MAX) NULL,
        
        CONSTRAINT PK_dbo_Events PRIMARY KEY (Id),
        CONSTRAINT CK_dbo_Events_Dates CHECK (EndDate >= StartDate),
        CONSTRAINT CK_dbo_Events_Attendance CHECK (ExpectedAttendance >= 0)
    );
END
ELSE
BEGIN
    -- Ensure ImageUrl column exists on existing dbo_Events table (Part 2 Migration)
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo_Events') AND name = 'ImageUrl')
    BEGIN
        ALTER TABLE dbo_Events ADD ImageUrl NVARCHAR(MAX) NULL;
    END

    -- Ensure EventTypeId column exists on existing dbo_Events table (Part 3 Migration)
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo_Events') AND name = 'EventTypeId')
    BEGIN
        ALTER TABLE dbo_Events ADD EventTypeId NVARCHAR(450) NULL;
    END
END
GO

-- 4. Bookings Table
IF OBJECT_ID('dbo_Bookings', 'U') IS NULL
BEGIN
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
END
GO

-- ----------------------------------------------------------------------------
-- SECTION 3: FOREIGN KEYS & CONSTRAINTS
-- ----------------------------------------------------------------------------

-- Events -> EventTypes (Part 3 FK)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dbo_Events_dbo_EventTypes_EventTypeId')
BEGIN
    ALTER TABLE dbo_Events
        ADD CONSTRAINT FK_dbo_Events_dbo_EventTypes_EventTypeId
        FOREIGN KEY (EventTypeId) REFERENCES dbo_EventTypes (Id)
        ON DELETE SET NULL;
END
GO

-- Events -> Venues (Set NULL on delete)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dbo_Events_dbo_Venues_VenueId')
BEGIN
    ALTER TABLE dbo_Events
        ADD CONSTRAINT FK_dbo_Events_dbo_Venues_VenueId
        FOREIGN KEY (VenueId) REFERENCES dbo_Venues (Id)
        ON DELETE SET NULL;
END
GO

-- Bookings -> Venues (Restrict delete)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dbo_Bookings_dbo_Venues_VenueId')
BEGIN
    ALTER TABLE dbo_Bookings
        ADD CONSTRAINT FK_dbo_Bookings_dbo_Venues_VenueId
        FOREIGN KEY (VenueId) REFERENCES dbo_Venues (Id)
        ON DELETE NO ACTION;
END
GO

-- Bookings -> Events (Restrict delete)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dbo_Bookings_dbo_Events_EventId')
BEGIN
    ALTER TABLE dbo_Bookings
        ADD CONSTRAINT FK_dbo_Bookings_dbo_Events_EventId
        FOREIGN KEY (EventId) REFERENCES dbo_Events (Id)
        ON DELETE NO ACTION;
END
GO

-- Performance Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_dbo_Events_VenueId')
    CREATE INDEX IX_dbo_Events_VenueId ON dbo_Events (VenueId);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_dbo_Events_EventTypeId')
    CREATE INDEX IX_dbo_Events_EventTypeId ON dbo_Events (EventTypeId);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_dbo_Bookings_VenueId')
    CREATE INDEX IX_dbo_Bookings_VenueId ON dbo_Bookings (VenueId);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_dbo_Bookings_EventId')
    CREATE UNIQUE INDEX IX_dbo_Bookings_EventId ON dbo_Bookings (EventId);
GO

-- ----------------------------------------------------------------------------
-- SECTION 4: CONSOLIDATED SQL VIEW (PART 2 REQUIREMENT)
-- ----------------------------------------------------------------------------
CREATE OR ALTER VIEW vw_ConsolidatedBookings AS
SELECT 
    b.Id AS BookingId,
    b.BookingCode,
    b.SpecialistName,
    b.StartDate AS BookingStartDate,
    b.EndDate AS BookingEndDate,
    DATEDIFF(HOUR, b.StartDate, b.EndDate) AS DurationHours,
    b.TotalCost,
    b.Status AS BookingStatus,
    -- Event Attributes
    e.Id AS EventId,
    e.Title AS EventTitle,
    e.ClientName,
    e.ClientEmail,
    e.ExpectedAttendance,
    e.Status AS EventStatus,
    e.ImageUrl AS EventPosterUrl,
    -- EventType Attributes (Part 3)
    et.Id AS EventTypeId,
    ISNULL(et.Name, 'General') AS EventTypeName,
    et.Icon AS EventTypeIcon,
    -- Venue Attributes
    v.Id AS VenueId,
    v.Name AS VenueName,
    v.Location AS VenueLocation,
    v.Capacity AS VenueCapacity,
    v.HourlyRate AS VenueHourlyRate,
    v.ImageUrl AS VenueImageUrl,
    v.Status AS VenueStatus,
    v.IsAvailable AS VenueIsAvailable
FROM dbo_Bookings b
INNER JOIN dbo_Events e ON b.EventId = e.Id
LEFT JOIN dbo_EventTypes et ON e.EventTypeId = et.Id
INNER JOIN dbo_Venues v ON b.VenueId = v.Id;
GO

-- ----------------------------------------------------------------------------
-- SECTION 5: PREDEFINED SEED DATA
-- ----------------------------------------------------------------------------

-- Seed Event Types (Part 3 Lookup Table)
MERGE INTO dbo_EventTypes AS Target
USING (VALUES
    ('et-001', 'Conference', 'Corporate summits, annual general meetings, and professional conventions.', 'bi-building-fill'),
    ('et-002', 'Wedding', 'Wedding ceremonies, banquets, and reception celebrations.', 'bi-heart-fill'),
    ('et-003', 'Concert', 'Live music performances, orchestra recitals, and outdoor festivals.', 'bi-music-note-beamed'),
    ('et-004', 'Workshop', 'Interactive training sessions, hands-on masterclasses, and bootcamps.', 'bi-tools'),
    ('et-005', 'Gala Dinner', 'Charity galas, awards ceremonies, and formal banquets.', 'bi-trophy-fill'),
    ('et-006', 'Exhibition', 'Trade shows, art galleries, and product showcase expos.', 'bi-easel-fill'),
    ('et-007', 'Seminar', 'Educational lectures, academic symposiums, and keynotes.', 'bi-mortarboard-fill')
) AS Source (Id, Name, Description, Icon)
ON Target.Id = Source.Id
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Description, Icon) VALUES (Source.Id, Source.Name, Source.Description, Source.Icon);
GO

-- Seed Initial Venues with Availability Flag
MERGE INTO dbo_Venues AS Target
USING (VALUES
    ('v-001', 'Grand Horizon Ballroom', 'Cape Town City Centre', 500, 1500.00, 'https://images.unsplash.com/photo-1519167758481-83f550bb49b3?auto=format&fit=crop&w=800&q=80', '["WiFi","Projector","Stage","Sound System","Catering Service"]', 'Active', 1, 'A luxurious ballroom featuring crystal chandeliers, full sound system, and panoramic ocean views suitable for corporate galas and weddings.'),
    ('v-002', 'Sunset Garden Pavilion', 'Stellenbosch Wine Route', 250, 950.00, 'https://images.unsplash.com/photo-1527529482837-4698179dc6ce?auto=format&fit=crop&w=800&q=80', '["Outdoor Lighting","Bar Area","Stage","Parking"]', 'Active', 1, 'Stunning outdoor pavilion set amongst vineyard gardens, ideal for cocktail parties, outdoor receptions, and corporate retreats.'),
    ('v-003', 'Apex Innovation Hub', 'Sandton Financial District', 80, 600.00, 'https://images.unsplash.com/photo-1431540015161-0bf868a2d407?auto=format&fit=crop&w=800&q=80', '["High-Speed Fiber","Smart Boards","Video Conferencing"]', 'Active', 1, 'State-of-the-art auditorium and conference space with high-speed fiber internet and interactive smart displays for tech summits.')
) AS Source (Id, Name, Location, Capacity, HourlyRate, ImageUrl, AmenitiesJson, Status, IsAvailable, Description)
ON Target.Id = Source.Id
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Location, Capacity, HourlyRate, ImageUrl, AmenitiesJson, Status, IsAvailable, Description)
    VALUES (Source.Id, Source.Name, Source.Location, Source.Capacity, Source.HourlyRate, Source.ImageUrl, Source.AmenitiesJson, Source.Status, Source.IsAvailable, Source.Description);
GO

-- Update existing venues to ensure IsAvailable = 1
UPDATE dbo_Venues SET IsAvailable = 1 WHERE IsAvailable IS NULL;
GO

-- Seed Events with EventTypeId
MERGE INTO dbo_Events AS Target
USING (VALUES
    ('e-001', 'Tech Innovation Summit 2026', 'Sarah Jenkins', 'sarah.j@techcorp.co.za', 75, DATEADD(DAY, 7, GETDATE()), DATEADD(HOUR, 8, DATEADD(DAY, 7, GETDATE())), 'v-003', 'et-001', 'Booked', 'https://images.unsplash.com/photo-1540575467063-178a50c2df87?auto=format&fit=crop&w=800&q=80'),
    ('e-002', 'Annual Healthcare Leadership Gala', 'Dr. Michael Vance', 'mvance@medafrica.org', 450, DATEADD(DAY, 14, GETDATE()), DATEADD(HOUR, 6, DATEADD(DAY, 14, GETDATE())), 'v-001', 'et-005', 'Booked', 'https://images.unsplash.com/photo-1511795409834-ef04bbd61622?auto=format&fit=crop&w=800&q=80'),
    ('e-003', 'Creative Design Expo', 'Elena Rostova', 'elena@designstudio.io', 200, DATEADD(DAY, 21, GETDATE()), DATEADD(HOUR, 6, DATEADD(DAY, 21, GETDATE())), NULL, 'et-006', 'Unassigned', 'https://images.unsplash.com/photo-1475721027785-f74eccf877e2?auto=format&fit=crop&w=800&q=80')
) AS Source (Id, Title, ClientName, ClientEmail, ExpectedAttendance, StartDate, EndDate, VenueId, EventTypeId, Status, ImageUrl)
ON Target.Id = Source.Id
WHEN NOT MATCHED THEN
    INSERT (Id, Title, ClientName, ClientEmail, ExpectedAttendance, StartDate, EndDate, VenueId, EventTypeId, Status, ImageUrl)
    VALUES (Source.Id, Source.Title, Source.ClientName, Source.ClientEmail, Source.ExpectedAttendance, Source.StartDate, Source.EndDate, Source.VenueId, Source.EventTypeId, Source.Status, Source.ImageUrl);
GO

-- Seed Bookings
MERGE INTO dbo_Bookings AS Target
USING (VALUES
    ('b-001', 'BK-A1B2C3D4', 'v-003', 'e-001', 'David Ross', DATEADD(DAY, 7, GETDATE()), DATEADD(HOUR, 8, DATEADD(DAY, 7, GETDATE())), 4800.00, 'Confirmed'),
    ('b-002', 'BK-E5F6G7H8', 'v-001', 'e-002', 'Amanda Peterson', DATEADD(DAY, 14, GETDATE()), DATEADD(HOUR, 6, DATEADD(DAY, 14, GETDATE())), 9000.00, 'Confirmed')
) AS Source (Id, BookingCode, VenueId, EventId, SpecialistName, StartDate, EndDate, TotalCost, Status)
ON Target.Id = Source.Id
WHEN NOT MATCHED THEN
    INSERT (Id, BookingCode, VenueId, EventId, SpecialistName, StartDate, EndDate, TotalCost, Status)
    VALUES (Source.Id, Source.BookingCode, Source.VenueId, Source.EventId, Source.SpecialistName, Source.StartDate, Source.EndDate, Source.TotalCost, Source.Status);
GO

-- ----------------------------------------------------------------------------
-- SECTION 6: VERIFICATION QUERIES FOR AZURE SQL QUERY EDITOR SCREENSHOTS
-- ----------------------------------------------------------------------------

-- Verification 1: Confirm newly created dbo_EventTypes table & categories (Rubric B item)
SELECT Id, Name, Description, Icon 
FROM dbo_EventTypes 
ORDER BY Name;

-- Verification 2: Confirm updated IsAvailable field in dbo_Venues table (Rubric B item)
SELECT Id, Name, Location, Capacity, HourlyRate, Status, IsAvailable 
FROM dbo_Venues 
ORDER BY Name;

-- Verification 3: Confirm Consolidated Bookings View (vw_ConsolidatedBookings)
SELECT BookingCode, EventTitle, EventTypeName, VenueName, VenueIsAvailable, SpecialistName, BookingStartDate, TotalCost, BookingStatus
FROM vw_ConsolidatedBookings;

-- Verification 4: Advanced Multi-Criteria Filter Query (EventType + Venue Availability + Date Range)
SELECT 
    b.BookingCode,
    e.Title AS EventTitle,
    et.Name AS EventType,
    v.Name AS VenueName,
    v.IsAvailable AS VenueAvailable,
    b.StartDate,
    b.EndDate,
    b.TotalCost
FROM dbo_Bookings b
INNER JOIN dbo_Events e ON b.EventId = e.Id
LEFT JOIN dbo_EventTypes et ON e.EventTypeId = et.Id
INNER JOIN dbo_Venues v ON b.VenueId = v.Id
WHERE et.Name = 'Conference'
  AND v.IsAvailable = 1
  AND b.StartDate >= GETDATE();
GO
