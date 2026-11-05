# EventEase Management System (Part 3 Complete)

EventEase is an enterprise-grade ASP.NET Core MVC web application deployed to Microsoft Azure. It empowers event booking specialists to coordinate venues, manage incoming client event requests, prevent scheduling conflicts, upload imagery to Azure Blob Storage, and perform multi-criteria filtering across Event Types, Venue Availability, Date Ranges, and Booking Statuses.

---

## Key Highlights & Feature Matrix

### Part 1: Foundation & Cloud Deployment
- **ASP.NET Core MVC Architecture**: Clean separation of Concerns across Models, Views, Controllers, and Services.
- **Relational Azure SQL Database**: Strict relational model featuring Primary Keys, Foreign Keys, Check Constraints, and Unique Indexes.
- **Entity & Referential Integrity**:
  - `dbo_Venues`: Space specifications, capacities, and hourly rates.
  - `dbo_Events`: Client details, attendance, preferred venues, and event dates.
  - `dbo_Bookings`: Associative booking entity linking Events and Venues with duration cost computation.
- **Azure App Service & Azure SQL Database Deployment**.

### Part 2: Cloud Storage, Validation & Consolidated Display
- **Azure Blob Storage Integration**: Secure containerized image management (`eventease-images`) with image MIME validation, thumbnail generation, and resilient CDN fallback.
- **Error Handling & Validation**:
  - Overlap prevention algorithm preventing venue double-booking on overlapping dates.
  - Foreign key deletion restriction preventing accidental deletion of active venues/events.
  - Defensive Model State validation and user-friendly alert banners.
- **Consolidated Bookings Hub**:
  - Executive KPI summary cards (Total Bookings, Confirmed, Pending Approval, Gross Revenue).
  - Dual viewing modes: Consolidated Master-Detail Cards vs High-Density Data Table.
- **Global Search Hub**: Cross-entity unified search across Venues, Events, and Bookings.

### Part 3: Advanced Filtering, Classification & Cloud Insights
- **EventType Classification Lookup Table (`dbo_EventTypes`)**:
  - Predefined categories: Conference, Wedding, Concert, Workshop, Gala Dinner, Exhibition, Seminar.
  - 1:N relationship with `dbo_Events` (`EventTypeId` FK).
- **Venue Availability (`IsAvailable`)**:
  - Space availability tracking for real-time booking scheduling.
  - Specialist toggle switches on venue creation and editing.
- **Advanced Multi-Criteria Filtering**:
  - Filter bookings by Event Type, Venue Availability, Start/End Date Range, Venue, and Booking Status.
  - Filter events by Event Type, Venue, Date Range, and Status.
  - Filter venues by Availability (Available / Unavailable), Capacity, Rate, and Status.
- **Consolidated Database View (`vw_ConsolidatedBookings`)**:
  - High-performance flattened view joining Bookings, Events, EventTypes, and Venues.

---

## Database Architecture & ERD Specifications

```
  +------------------+         +------------------+
  |  dbo_EventTypes  |         |    dbo_Venues    |
  +------------------+         +------------------+
  | PK Id            |         | PK Id            |
  |    Name (UQ)     |         |    Name          |
  |    Description   |         |    Location      |
  |    Icon          |         |    Capacity      |
  +--------+---------+         |    HourlyRate    |
           | 1                 |    ImageUrl      |
           |                   |    Status        |
           | N                 |    IsAvailable   | <--- Part 3
  +--------v---------+         +--------+---------+
  |    dbo_Events    |                  | 1
  +------------------+                  |
  | PK Id            |                  |
  |    Title         |                  |
  |    ClientName    |                  |
  |    ClientEmail   |                  |
  |    StartDate     |                  |
  |    EndDate       |                  |
  | FK VenueId       |                  |
  | FK EventTypeId   | <--- Part 3      |
  |    ImageUrl      |                  |
  |    Status        |                  |
  +--------+---------+                  |
           | 1                          | N
           | (1:1 Unique)               |
  +--------v----------------------------v---------+
  |                 dbo_Bookings                  |
  +-----------------------------------------------+
  | PK Id                                         |
  |    BookingCode (UQ)                           |
  | FK VenueId                                    |
  | FK EventId (UQ)                               |
  |    SpecialistName                             |
  |    StartDate                                  |
  |    EndDate                                    |
  |    TotalCost                                  |
  |    Status                                     |
  +-----------------------------------------------+
```

---

## Azure SQL Database Setup Script (`Database/script.sql`)

Run the complete setup script in `Database/script.sql` via Azure SQL Query Editor or SQL Server Management Studio (SSMS).

### Verification Queries for Azure SQL Query Editor
```sql
-- 1. Verify EventTypes Table & Seed Data
SELECT Id, Name, Description, Icon FROM dbo_EventTypes ORDER BY Name;

-- 2. Verify Venue Availability Field
SELECT Id, Name, Location, Capacity, HourlyRate, Status, IsAvailable FROM dbo_Venues;

-- 3. Verify Consolidated View
SELECT BookingCode, EventTitle, EventTypeName, VenueName, VenueIsAvailable, SpecialistName, TotalCost FROM vw_ConsolidatedBookings;

-- 4. Verify Multi-Criteria Filter Query
SELECT b.BookingCode, e.Title AS EventTitle, et.Name AS EventType, v.Name AS VenueName, v.IsAvailable AS VenueAvailable, b.StartDate, b.TotalCost
FROM dbo_Bookings b
INNER JOIN dbo_Events e ON b.EventId = e.Id
LEFT JOIN dbo_EventTypes et ON e.EventTypeId = et.Id
INNER JOIN dbo_Venues v ON b.VenueId = v.Id
WHERE et.Name = 'Conference' AND v.IsAvailable = 1 AND b.StartDate >= GETDATE();
```

---

## Running the Application Locally
```bash
# Clone the repository
git clone https://github.com/SDevelops11/EventEaseApp.git
cd EventEaseApp/EventEaseApp

# Restore packages & build
dotnet restore
dotnet build

# Launch the app
dotnet run
```
The application will launch and automatically apply all migrations and seed initial data.