/* ============================================================================
   RelayHR - Esquema de base de datos (SQL Server)
   ----------------------------------------------------------------------------
   Equivale EXACTAMENTE a la migración 20261101180000_InitialCreate de Entity Framework.
   Incluye la fila de __EFMigrationsHistory para que, al arrancar, la aplicación vea la
   migración como ya aplicada y no intente crear las tablas otra vez.

   Cómo usarlo:  ejecutar en SSMS / Azure Data Studio conectado a (localdb)\MSSQLLocalDB
   o a su servidor SQL Server.  Crea la base RelayHR solo si no existe.
   Para reiniciar desde cero:  DROP DATABASE RelayHR;  y volver a ejecutar este script.
   ============================================================================ */

IF DB_ID(N'RelayHR') IS NULL
    CREATE DATABASE [RelayHR];
GO

USE [RelayHR];
GO

IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') IS NOT NULL
BEGIN
    RAISERROR(N'La base RelayHR ya tiene el esquema. Ejecute DROP DATABASE RelayHR; para reiniciar.', 16, 1);
    SET NOEXEC ON;   -- evita que se ejecute el resto del script
END
GO

CREATE TABLE [dbo].[__EFMigrationsHistory] (
    [MigrationId] nvarchar(150) NOT NULL,
    [ProductVersion] nvarchar(32) NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
);
GO

CREATE TABLE [dbo].[Users] (
    [UserId] int NOT NULL IDENTITY(1,1),
    [FullName] nvarchar(150) NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [Role] nvarchar(30) NOT NULL,
    [CompanyName] nvarchar(150) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserId])
);
GO

CREATE TABLE [dbo].[Departments] (
    [DepartmentId] int NOT NULL IDENTITY(1,1),
    [Name] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY ([DepartmentId])
);
GO

CREATE TABLE [dbo].[Positions] (
    [PositionId] int NOT NULL IDENTITY(1,1),
    [Title] nvarchar(150) NOT NULL,
    [DepartmentId] int NULL,
    [Description] nvarchar(4000) NULL,
    [Requirements] nvarchar(4000) NULL,
    CONSTRAINT [PK_Positions] PRIMARY KEY ([PositionId]),
    CONSTRAINT [FK_Positions_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments] ([DepartmentId])
);
GO

CREATE TABLE [dbo].[Employees] (
    [EmployeeId] int NOT NULL IDENTITY(1,1),
    [FullName] nvarchar(150) NOT NULL,
    [PositionId] int NULL,
    [DepartmentId] int NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Employees] PRIMARY KEY ([EmployeeId]),
    CONSTRAINT [FK_Employees_Positions_PositionId] FOREIGN KEY ([PositionId]) REFERENCES [dbo].[Positions] ([PositionId]),
    CONSTRAINT [FK_Employees_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments] ([DepartmentId])
);
GO

CREATE TABLE [dbo].[EmployeeExits] (
    [EmployeeExitId] int NOT NULL IDENTITY(1,1),
    [EmployeeId] int NOT NULL,
    [ExitDate] date NOT NULL,
    [Reason] nvarchar(500) NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [RegisteredBy] nvarchar(150) NOT NULL,
    [ClosedAt] datetime2 NULL,
    [ClosedBy] nvarchar(150) NULL,
    CONSTRAINT [PK_EmployeeExits] PRIMARY KEY ([EmployeeExitId]),
    CONSTRAINT [FK_EmployeeExits_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees] ([EmployeeId])
);
GO

CREATE TABLE [dbo].[ExitTasks] (
    [ExitTaskId] int NOT NULL IDENTITY(1,1),
    [EmployeeExitId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Area] nvarchar(30) NOT NULL,
    [ResponsibleName] nvarchar(150) NOT NULL,
    [DueDate] date NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [IsMandatory] bit NOT NULL,
    [Kind] nvarchar(30) NOT NULL,
    [CompletedAt] datetime2 NULL,
    [CompletedBy] nvarchar(150) NULL,
    CONSTRAINT [PK_ExitTasks] PRIMARY KEY ([ExitTaskId]),
    CONSTRAINT [FK_ExitTasks_EmployeeExits_EmployeeExitId] FOREIGN KEY ([EmployeeExitId]) REFERENCES [dbo].[EmployeeExits] ([EmployeeExitId])
);
GO

CREATE TABLE [dbo].[KnowledgeItems] (
    [KnowledgeItemId] int NOT NULL IDENTITY(1,1),
    [EmployeeExitId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NOT NULL,
    [CurrentStatus] nvarchar(200) NOT NULL,
    [AssignedToEmployeeId] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_KnowledgeItems] PRIMARY KEY ([KnowledgeItemId]),
    CONSTRAINT [FK_KnowledgeItems_EmployeeExits_EmployeeExitId] FOREIGN KEY ([EmployeeExitId]) REFERENCES [dbo].[EmployeeExits] ([EmployeeExitId]),
    CONSTRAINT [FK_KnowledgeItems_Employees_AssignedToEmployeeId] FOREIGN KEY ([AssignedToEmployeeId]) REFERENCES [dbo].[Employees] ([EmployeeId])
);
GO

CREATE TABLE [dbo].[ExpressVacancies] (
    [ExpressVacancyId] int NOT NULL IDENTITY(1,1),
    [EmployeeExitId] int NOT NULL,
    [PositionId] int NULL,
    [JobTitle] nvarchar(150) NULL,
    [DepartmentId] int NULL,
    [JobDescription] nvarchar(4000) NULL,
    [Requirements] nvarchar(4000) NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [PublishedAt] datetime2 NULL,
    [PublishedBy] nvarchar(150) NULL,
    CONSTRAINT [PK_ExpressVacancies] PRIMARY KEY ([ExpressVacancyId]),
    CONSTRAINT [FK_ExpressVacancies_EmployeeExits_EmployeeExitId] FOREIGN KEY ([EmployeeExitId]) REFERENCES [dbo].[EmployeeExits] ([EmployeeExitId]),
    CONSTRAINT [FK_ExpressVacancies_Positions_PositionId] FOREIGN KEY ([PositionId]) REFERENCES [dbo].[Positions] ([PositionId]),
    CONSTRAINT [FK_ExpressVacancies_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments] ([DepartmentId])
);
GO

CREATE TABLE [dbo].[FreelancerProfiles] (
    [FreelancerProfileId] int NOT NULL IDENTITY(1,1),
    [UserId] int NOT NULL,
    [Category] nvarchar(100) NOT NULL,
    [ExperienceYears] int NOT NULL,
    [ExperienceSummary] nvarchar(2000) NOT NULL,
    [PortfolioUrl] nvarchar(300) NULL,
    [HourlyRate] decimal(18,2) NOT NULL,
    [IsAvailable] bit NOT NULL,
    [AvailabilityNote] nvarchar(200) NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_FreelancerProfiles] PRIMARY KEY ([FreelancerProfileId]),
    CONSTRAINT [FK_FreelancerProfiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
);
GO

CREATE TABLE [dbo].[ServiceRequests] (
    [ServiceRequestId] int NOT NULL IDENTITY(1,1),
    [FreelancerProfileId] int NOT NULL,
    [RequestedByUserId] int NOT NULL,
    [CompanyName] nvarchar(150) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [RespondedAt] datetime2 NULL,
    [ConfirmedAt] datetime2 NULL,
    CONSTRAINT [PK_ServiceRequests] PRIMARY KEY ([ServiceRequestId]),
    CONSTRAINT [FK_ServiceRequests_FreelancerProfiles_FreelancerProfileId] FOREIGN KEY ([FreelancerProfileId]) REFERENCES [dbo].[FreelancerProfiles] ([FreelancerProfileId]),
    CONSTRAINT [FK_ServiceRequests_Users_RequestedByUserId] FOREIGN KEY ([RequestedByUserId]) REFERENCES [dbo].[Users] ([UserId])
);
GO

CREATE UNIQUE INDEX [IX_Users_Email] ON [dbo].[Users] ([Email]);
GO
CREATE INDEX [IX_Positions_DepartmentId] ON [dbo].[Positions] ([DepartmentId]);
GO
CREATE INDEX [IX_Employees_DepartmentId] ON [dbo].[Employees] ([DepartmentId]);
GO
CREATE INDEX [IX_Employees_PositionId] ON [dbo].[Employees] ([PositionId]);
GO
CREATE INDEX [IX_EmployeeExits_EmployeeId] ON [dbo].[EmployeeExits] ([EmployeeId]);
GO
CREATE INDEX [IX_ExitTasks_EmployeeExitId] ON [dbo].[ExitTasks] ([EmployeeExitId]);
GO
CREATE INDEX [IX_KnowledgeItems_AssignedToEmployeeId] ON [dbo].[KnowledgeItems] ([AssignedToEmployeeId]);
GO
CREATE INDEX [IX_KnowledgeItems_EmployeeExitId] ON [dbo].[KnowledgeItems] ([EmployeeExitId]);
GO
CREATE INDEX [IX_ExpressVacancies_DepartmentId] ON [dbo].[ExpressVacancies] ([DepartmentId]);
GO
CREATE UNIQUE INDEX [IX_ExpressVacancies_EmployeeExitId] ON [dbo].[ExpressVacancies] ([EmployeeExitId]);
GO
CREATE INDEX [IX_ExpressVacancies_PositionId] ON [dbo].[ExpressVacancies] ([PositionId]);
GO
CREATE UNIQUE INDEX [IX_FreelancerProfiles_UserId] ON [dbo].[FreelancerProfiles] ([UserId]);
GO
CREATE INDEX [IX_ServiceRequests_FreelancerProfileId] ON [dbo].[ServiceRequests] ([FreelancerProfileId]);
GO
CREATE INDEX [IX_ServiceRequests_RequestedByUserId] ON [dbo].[ServiceRequests] ([RequestedByUserId]);
GO

INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261101180000_InitialCreate', N'10.0.0');
GO

SET NOEXEC OFF;
GO
PRINT N'Esquema de RelayHR creado correctamente.';
GO
