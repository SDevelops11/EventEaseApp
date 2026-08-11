# 📅 EventEase Management System

EventEase is an ASP.NET Core MVC web application designed for managing venues, events, and bookings seamlessly. It allows event specialists to manage venue availability, register corporate/private events, and assign events to venues with automated cost calculations and strict integrity constraints.

---

## 🛠 Database Architecture & ERD Specifications

The application persistence layer is built on SQL Server using Entity Framework Core with the following schema rules:

- **`dbo_Venues`**: Stores venue details, capacity, hourly rate, amenities, status, and description.
- **`dbo_Events`**: Stores event title, client details, attendance, duration, status, and optional assigned venue.
- **`dbo_Bookings`**: Stores confirmed bookings connecting a venue and an event. Enforces a **1:1 relationship with Events** (an event can only be booked once) and a **1:N relationship with Venues**.

---

## 🗄 Database Setup Script (`script.sql`)

The script below contains all relevant T-SQL operations:
- **Table Creation**
- **Entity Integrity** (Primary Keys, Defaults, Check Constraints)
- **Referential Integrity** (Foreign Keys, Cascade/Restrict rules, Unique Constraints, Performance Indexes)
- **Table Data Insertion** (Sample seed data for Venues, Events, and Bookings)

```sql
-- ============================================================================
-- EventEase App Database Setup Script
-- Engine: Microsoft SQL Server (T-SQL)
-- Features: Table Creation, Entity Integrity, Referential Integrity & Sample Data
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 0. CLEANUP (Drop tables in reverse dependency order if re-running)
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo_Bookings', 'U') IS NOT NULL DROP TABLE dbo_Bookings;
IF OBJECT_ID('dbo_Events', 'U') IS NOT NULL DROP TABLE dbo_Events;
IF OBJECT_ID('dbo_Venues', 'U') IS NOT NULL DROP TABLE dbo_Venues;
GO

-- ----------------------------------------------------------------------------
-- 1. TABLE CREATION & ENTITY INTEGRITY
-- ----------------------------------------------------------------------------

-- A. Table: dbo_Venues
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
    
    -- Entity Integrity (Primary Key) & Validation Constraints
    CONSTRAINT PK_dbo_Venues PRIMARY KEY (Id),
    CONSTRAINT CK_dbo_Venues_Capacity CHECK (Capacity > 0),
    CONSTRAINT CK_dbo_Venues_HourlyRate CHECK (HourlyRate >= 0.00)
);
GO

-- B. Table: dbo_Events
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
    
    -- Entity Integrity (Primary Key) & Validation Constraints
    CONSTRAINT PK_dbo_Events PRIMARY KEY (Id),
    CONSTRAINT CK_dbo_Events_Dates CHECK (EndDate >= StartDate),
    CONSTRAINT CK_dbo_Events_Attendance CHECK (ExpectedAttendance >= 0)
);
GO

-- C. Table: dbo_Bookings
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
    
    -- Entity Integrity (Primary Key, Unique Code, Dates & Cost Validation)
    CONSTRAINT PK_dbo_Bookings PRIMARY KEY (Id),
    CONSTRAINT UQ_dbo_Bookings_BookingCode UNIQUE (BookingCode),
    CONSTRAINT UQ_dbo_Bookings_EventId UNIQUE (EventId), -- ERD 1:1 Unique Constraint for Event-Booking
    CONSTRAINT CK_dbo_Bookings_Dates CHECK (EndDate >= StartDate),
    CONSTRAINT CK_dbo_Bookings_TotalCost CHECK (TotalCost >= 0.00)
);
GO

-- ----------------------------------------------------------------------------
-- 2. REFERENTIAL INTEGRITY (Foreign Keys & Indexes)
-- ----------------------------------------------------------------------------

-- FK 1: dbo_Events -> dbo_Venues (1:N, Nullable - Setting VenueId to NULL if Venue is deleted)
ALTER TABLE dbo_Events
    ADD CONSTRAINT FK_dbo_Events_dbo_Venues_VenueId
    FOREIGN KEY (VenueId) REFERENCES dbo_Venues (Id)
    ON DELETE SET NULL;
GO

-- FK 2: dbo_Bookings -> dbo_Venues (1:N, Restrict deletion of Venue if active Bookings exist)
ALTER TABLE dbo_Bookings
    ADD CONSTRAINT FK_dbo_Bookings_dbo_Venues_VenueId
    FOREIGN KEY (VenueId) REFERENCES dbo_Venues (Id)
    ON DELETE NO ACTION;
GO

-- FK 3: dbo_Bookings -> dbo_Events (1:1, Restrict deletion of Event if active Booking exists)
ALTER TABLE dbo_Bookings
    ADD CONSTRAINT FK_dbo_Bookings_dbo_Events_EventId
    FOREIGN KEY (EventId) REFERENCES dbo_Events (Id)
    ON DELETE NO ACTION;
GO

-- Performance & Integrity Indexes
CREATE INDEX IX_dbo_Events_VenueId ON dbo_Events (VenueId);
CREATE INDEX IX_dbo_Bookings_VenueId ON dbo_Bookings (VenueId);
CREATE UNIQUE INDEX IX_dbo_Bookings_EventId ON dbo_Bookings (EventId);
GO

-- ----------------------------------------------------------------------------
-- 3. TABLE DATA INSERTION (Sample Data)
-- ----------------------------------------------------------------------------

-- Insert Sample Venues
INSERT INTO dbo_Venues (Id, Name, Location, Capacity, HourlyRate, ImageUrl, AmenitiesJson, Status, Description)
VALUES 
    ('v-001', 'Grand Horizon Ballroom', 'Cape Town City Centre', 500, 1500.00, 'https://images.unsplash.com/photo-1519167758481-83f550bb49b3', '["WiFi","Projector","Stage","Sound System","Catering Service"]', 'Active', 'A luxurious hall suitable for corporate galas and large weddings.'),
    ('v-002', 'Sunset Garden Pavilion', 'Stellenbosch Wine Route', 250, 950.00, 'https://images.unsplash.com/photo-1527529482837-4698179dc6ce', '["Outdoor Lighting","Bar Area","Stage","Parking"]', 'Active', 'Beautiful outdoor venue surrounded by vineyards.'),
    ('v-003', 'Apex Innovation Hub', 'Sandton Financial District', 80, 600.00, 'https://images.unsplash.com/photo-1431540015161-0bf868a2d407', '["High-Speed Fiber","Smart Boards","Video Conferencing"]', 'Active', 'Modern conference setup tailored for tech summits and workshops.');

-- Insert Sample Events
INSERT INTO dbo_Events (Id, Title, ClientName, ClientEmail, ExpectedAttendance, StartDate, EndDate, VenueId, Status)
VALUES 
    ('e-001', 'Tech Innovation Summit 2026', 'Sarah Jenkins', 'sarah.j@techcorp.co.za', 75, '2026-09-15 09:00:00', '2026-09-15 17:00:00', 'v-003', 'Booked'),
    ('e-002', 'Annual Healthcare Leadership Gala', 'Dr. Michael Vance', 'mvance@medafrica.org', 450, '2026-10-01 18:00:00', '2026-10-01 23:00:00', 'v-001', 'Booked'),
    ('e-003', 'Creative Design Expo', 'Elena Rostova', 'elena@designstudio.io', 200, '2026-11-10 10:00:00', '2026-11-10 16:00:00', NULL, 'Unassigned');

-- Insert Sample Bookings
INSERT INTO dbo_Bookings (Id, BookingCode, VenueId, EventId, SpecialistName, StartDate, EndDate, TotalCost, Status)
VALUES 
    ('b-001', 'BK-A1B2C3D4', 'v-003', 'e-001', 'David Ross', '2026-09-15 09:00:00', '2026-09-15 17:00:00', 4800.00, 'Confirmed'),
    ('b-002', 'BK-E5F6G7H8', 'v-001', 'e-002', 'Amanda Peterson', '2026-10-01 18:00:00', '2026-10-01 23:00:00', 7500.00, 'Confirmed');
GO
```

---

## 🚀 How to Run

1. **Database Setup**: Execute the script above or run EF Core migrations:
   ```bash
   dotnet ef database update
   ```
2. **Run Application**:
   ```bash
   dotnet run
   ```
