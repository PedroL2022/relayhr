/* ============================================================================
   RelayHR - Datos de demostración (opcional)
   ----------------------------------------------------------------------------
   Ejecutar DESPUÉS de RelayHR_01_Esquema.sql. Son los mismos datos que la aplicación
   carga sola en desarrollo (DataSeeder), así que NO hace falta ejecutarlo si va a
   arrancar la aplicación en modo Development con la base vacía.

   Cuentas de acceso:
     admin@relayhr.com        Admin123!         (Administrador de RRHH)
     reclutador@relayhr.com   Reclutador123!    (Reclutador)
     freelancer@relayhr.com   Freelancer123!    (Trabajador independiente)
     pedro@relayhr.com / sofia@relayhr.com      Freelancer123!
   ============================================================================ */

USE [RelayHR];
GO

IF EXISTS (SELECT 1 FROM [dbo].[Users])
BEGIN
    RAISERROR(N'La base ya tiene datos; no se cargan los de demostración.', 16, 1);
    SET NOEXEC ON;
END
GO

DECLARE @now datetime2 = SYSUTCDATETIME();

SET IDENTITY_INSERT [dbo].[Departments] ON;
INSERT INTO [dbo].[Departments] ([DepartmentId], [Name]) VALUES
    (1, N'Tecnología'),
    (2, N'Recursos Humanos'),
    (3, N'Finanzas'),
    (4, N'Operaciones');
SET IDENTITY_INSERT [dbo].[Departments] OFF;

SET IDENTITY_INSERT [dbo].[Positions] ON;
INSERT INTO [dbo].[Positions] ([PositionId], [Title], [DepartmentId], [Description], [Requirements]) VALUES
    (1, N'Desarrollador Backend', 1, N'Diseño y mantenimiento de APIs y servicios internos.', N'3 años de experiencia con C# y SQL Server.'),
    (2, N'Analista Financiero', 3, N'Seguimiento de presupuestos y reportes mensuales.', NULL),
    (3, N'Asistente Administrativo', NULL, NULL, NULL);
SET IDENTITY_INSERT [dbo].[Positions] OFF;

SET IDENTITY_INSERT [dbo].[Employees] ON;
INSERT INTO [dbo].[Employees] ([EmployeeId], [FullName], [PositionId], [DepartmentId], [IsActive]) VALUES
    (1, N'Ana Martínez', 1, 1, 1),
    (2, N'Luis Pérez', 2, 3, 1),
    (3, N'Carla Rodríguez', 3, 4, 1),
    (4, N'Miguel Santos', 1, 1, 1),
    (5, N'Rosa Jiménez', NULL, 2, 1);
SET IDENTITY_INSERT [dbo].[Employees] OFF;

SET IDENTITY_INSERT [dbo].[Users] ON;
INSERT INTO [dbo].[Users] ([UserId], [FullName], [Email], [PasswordHash], [Role], [CompanyName], [CreatedAt]) VALUES
    (1, N'Administrador RRHH', N'admin@relayhr.com', N'AQAAAAIAAYagAAAAEHWUt/STQe8ZCYlzHO0TRJ5ZoR8aZMrBxIyfF22ZwvAxDajUHnWIqaPYj+8KBy8rLg==', N'HRAdmin', N'Demo S.R.L.', @now),
    (2, N'Reclutador Demo', N'reclutador@relayhr.com', N'AQAAAAIAAYagAAAAEGZQXFuXzrRA6kvoMfSdoMJC0A+IRIrgNkLJp8UzJSt6zjOsrpvlmTIq8R4O7gN/GQ==', N'Recruiter', N'Demo S.R.L.', @now),
    (3, N'Daniela Freelancer', N'freelancer@relayhr.com', N'AQAAAAIAAYagAAAAEEQ/jpogGLpwaQA0ckZ1kEdRC9ctsHGjo9cnPeGIhX9Pbs/kUebLxlXLmyNT3dX6Sg==', N'Freelancer', NULL, @now),
    (4, N'Pedro Diseñador', N'pedro@relayhr.com', N'AQAAAAIAAYagAAAAEEQ/jpogGLpwaQA0ckZ1kEdRC9ctsHGjo9cnPeGIhX9Pbs/kUebLxlXLmyNT3dX6Sg==', N'Freelancer', NULL, @now),
    (5, N'Sofía Contadora', N'sofia@relayhr.com', N'AQAAAAIAAYagAAAAEEQ/jpogGLpwaQA0ckZ1kEdRC9ctsHGjo9cnPeGIhX9Pbs/kUebLxlXLmyNT3dX6Sg==', N'Freelancer', NULL, @now);
SET IDENTITY_INSERT [dbo].[Users] OFF;

SET IDENTITY_INSERT [dbo].[FreelancerProfiles] ON;
INSERT INTO [dbo].[FreelancerProfiles] ([FreelancerProfileId], [UserId], [Category], [ExperienceYears], [ExperienceSummary], [PortfolioUrl], [HourlyRate], [IsAvailable], [AvailabilityNote], [UpdatedAt]) VALUES
    (1, 3, N'Desarrollo de software', 5, N'Desarrolladora full stack con experiencia en C#, SQL Server y React.', N'https://portafolio.example.com/daniela', 1500.00, 1, N'Lunes a viernes por la tarde', @now),
    (2, 4, N'Diseño gráfico y UX', 3, N'Diseño de interfaces, identidad visual y prototipos.', NULL, 900.00, 1, NULL, @now),
    (3, 5, N'Contabilidad y finanzas', 8, N'Cierres contables, conciliaciones y reportes para pequeñas empresas.', NULL, 1200.00, 0, N'Disponible a partir del próximo mes', @now);
SET IDENTITY_INSERT [dbo].[FreelancerProfiles] OFF;

SET NOEXEC OFF;
GO
PRINT N'Datos de demostración cargados.';
GO
