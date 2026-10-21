IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(128) NOT NULL,
        [ProviderKey] nvarchar(128) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(128) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'00000000000000_CreateIdentitySchema', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011225256_InitialChurchModules'
)
BEGIN
    CREATE TABLE [ChurchSettings] (
        [Id] int NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [About] nvarchar(3000) NOT NULL,
        [Address] nvarchar(300) NULL,
        [Contact] nvarchar(100) NULL,
        CONSTRAINT [PK_ChurchSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ChurchSettings_Singleton] CHECK ([Id] = 1)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011225256_InitialChurchModules'
)
BEGIN
    CREATE TABLE [Congregants] (
        [Id] int NOT NULL IDENTITY,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Phone] nvarchar(30) NULL,
        [IsActive] bit NOT NULL,
        [UserId] nvarchar(450) NULL,
        CONSTRAINT [PK_Congregants] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Congregants_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011225256_InitialChurchModules'
)
BEGIN
    CREATE TABLE [PaymentMethods] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Instructions] nvarchar(500) NOT NULL,
        [QrStorageKey] nvarchar(max) NULL,
        [QrContentType] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_PaymentMethods] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011225256_InitialChurchModules'
)
BEGIN
    CREATE TABLE [Publications] (
        [Id] int NOT NULL IDENTITY,
        [Kind] int NOT NULL,
        [Title] nvarchar(160) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [EventAt] datetime2 NULL,
        [Location] nvarchar(250) NULL,
        [IsPublished] bit NOT NULL,
        [IsCancelled] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Publications] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011225256_InitialChurchModules'
)
BEGIN
    CREATE TABLE [Donations] (
        [Id] int NOT NULL IDENTITY,
        [TrackingCode] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [Status] int NOT NULL,
        [DonorKind] int NOT NULL,
        [CongregantId] int NULL,
        [ExternalName] nvarchar(180) NULL,
        [ExternalContact] nvarchar(150) NULL,
        [Amount] decimal(12,2) NULL,
        [PaymentMethodId] int NULL,
        [PaymentMethodName] nvarchar(100) NULL,
        [ReceivedOrPaidOn] datetime2 NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [ReceiptStorageKey] nvarchar(max) NULL,
        [ReceiptContentType] nvarchar(max) NULL,
        [RecordedByUserId] nvarchar(max) NULL,
        [Version] rowversion NOT NULL,
        CONSTRAINT [PK_Donations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Donation_Amount] CHECK (([Kind] = 0 AND [Amount] > 0 AND [Amount] IS NOT NULL) OR ([Kind] = 1 AND [Amount] IS NULL)),
        CONSTRAINT [CK_Donation_Donor] CHECK (([DonorKind] = 0 AND [CongregantId] IS NOT NULL) OR ([DonorKind] = 1 AND [CongregantId] IS NULL AND [ExternalName] IS NOT NULL) OR ([DonorKind] = 2 AND [CongregantId] IS NULL AND [ExternalName] IS NULL)),
        CONSTRAINT [FK_Donations_Congregants_CongregantId] FOREIGN KEY ([CongregantId]) REFERENCES [Congregants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Donations_PaymentMethods_PaymentMethodId] FOREIGN KEY ([PaymentMethodId]) REFERENCES [PaymentMethods] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011225256_InitialChurchModules'
)
BEGIN
    CREATE TABLE [DonationDecision] (
        [Id] int NOT NULL IDENTITY,
        [DonationId] int NOT NULL,
        [Status] int NOT NULL,
        [AdministratorId] nvarchar(450) NOT NULL,
        [At] datetimeoffset NOT NULL,
        [Reason] nvarchar(1000) NULL,
        CONSTRAINT [PK_DonationDecision] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DonationDecision_AspNetUsers_AdministratorId] FOREIGN KEY ([AdministratorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DonationDecision_Donations_DonationId] FOREIGN KEY ([DonationId]) REFERENCES [Donations] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011225256_InitialChurchModules'
)
BEGIN
    CREATE TABLE [DonationItem] (
        [Id] int NOT NULL IDENTITY,
        [DonationId] int NOT NULL,
        [Description] nvarchar(250) NOT NULL,
        [Quantity] decimal(12,2) NOT NULL,
        [Unit] nvarchar(40) NOT NULL,
        CONSTRAINT [PK_DonationItem] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DonationItem_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_DonationItem_Donations_DonationId] FOREIGN KEY ([DonationId]) REFERENCES [Donations] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011225256_InitialChurchModules'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Congregants_UserId] ON [Congregants] ([UserId]) WHERE [UserId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011225256_InitialChurchModules'
)
BEGIN
    CREATE INDEX [IX_DonationDecision_AdministratorId] ON [DonationDecision] ([AdministratorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011225256_InitialChurchModules'
)
BEGIN
    CREATE INDEX [IX_DonationDecision_DonationId] ON [DonationDecision] ([DonationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011225256_InitialChurchModules'
)
BEGIN
    CREATE INDEX [IX_DonationItem_DonationId] ON [DonationItem] ([DonationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011225256_InitialChurchModules'
)
BEGIN
    CREATE INDEX [IX_Donations_CongregantId] ON [Donations] ([CongregantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011225256_InitialChurchModules'
)
BEGIN
    CREATE INDEX [IX_Donations_PaymentMethodId] ON [Donations] ([PaymentMethodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011225256_InitialChurchModules'
)
BEGIN
    CREATE INDEX [IX_Donations_Status_CreatedAt] ON [Donations] ([Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011225256_InitialChurchModules'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Donations_TrackingCode] ON [Donations] ([TrackingCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011225256_InitialChurchModules'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261011225256_InitialChurchModules', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [AspNetRoleClaims] DROP CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [AspNetUserClaims] DROP CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [AspNetUserLogins] DROP CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [AspNetUserRoles] DROP CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [AspNetUserRoles] DROP CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [AspNetUserTokens] DROP CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [Congregants] DROP CONSTRAINT [FK_Congregants_AspNetUsers_UserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [DonationDecision] DROP CONSTRAINT [FK_DonationDecision_AspNetUsers_AdministratorId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [DonationDecision] DROP CONSTRAINT [FK_DonationDecision_Donations_DonationId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [DonationItem] DROP CONSTRAINT [FK_DonationItem_Donations_DonationId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [Donations] DROP CONSTRAINT [FK_Donations_Congregants_CongregantId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [Donations] DROP CONSTRAINT [FK_Donations_PaymentMethods_PaymentMethodId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [Publications] DROP CONSTRAINT [PK_Publications];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [PaymentMethods] DROP CONSTRAINT [PK_PaymentMethods];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [Donations] DROP CONSTRAINT [PK_Donations];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [DonationItem] DROP CONSTRAINT [PK_DonationItem];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [DonationDecision] DROP CONSTRAINT [PK_DonationDecision];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [Congregants] DROP CONSTRAINT [PK_Congregants];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [ChurchSettings] DROP CONSTRAINT [PK_ChurchSettings];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [AspNetUserTokens] DROP CONSTRAINT [PK_AspNetUserTokens];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [AspNetUsers] DROP CONSTRAINT [PK_AspNetUsers];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [AspNetUserRoles] DROP CONSTRAINT [PK_AspNetUserRoles];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [AspNetUserLogins] DROP CONSTRAINT [PK_AspNetUserLogins];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [AspNetUserClaims] DROP CONSTRAINT [PK_AspNetUserClaims];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [AspNetRoles] DROP CONSTRAINT [PK_AspNetRoles];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [AspNetRoleClaims] DROP CONSTRAINT [PK_AspNetRoleClaims];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[Publications]', N'tb_publicacion', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[PaymentMethods]', N'tb_medio_pago', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[Donations]', N'tb_donacion', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[DonationItem]', N'tb_donacion_bien', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[DonationDecision]', N'tb_donacion_historial', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[Congregants]', N'tb_congregante', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[ChurchSettings]', N'tb_iglesia', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[AspNetUserTokens]', N'tb_usuario_token', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[AspNetUsers]', N'tb_usuario', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[AspNetUserRoles]', N'tb_usuario_rol', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[AspNetUserLogins]', N'tb_usuario_acceso_externo', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[AspNetUserClaims]', N'tb_usuario_atributo', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[AspNetRoles]', N'tb_rol', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[AspNetRoleClaims]', N'tb_rol_atributo', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[tb_donacion].[IX_Donations_TrackingCode]', N'IX_tb_donacion_TrackingCode', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[tb_donacion].[IX_Donations_Status_CreatedAt]', N'IX_tb_donacion_Status_CreatedAt', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[tb_donacion].[IX_Donations_PaymentMethodId]', N'IX_tb_donacion_PaymentMethodId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[tb_donacion].[IX_Donations_CongregantId]', N'IX_tb_donacion_CongregantId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[tb_donacion_bien].[IX_DonationItem_DonationId]', N'IX_tb_donacion_bien_DonationId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[tb_donacion_historial].[IX_DonationDecision_DonationId]', N'IX_tb_donacion_historial_DonationId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[tb_donacion_historial].[IX_DonationDecision_AdministratorId]', N'IX_tb_donacion_historial_AdministratorId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[tb_congregante].[IX_Congregants_UserId]', N'IX_tb_congregante_UserId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[tb_usuario_rol].[IX_AspNetUserRoles_RoleId]', N'IX_tb_usuario_rol_RoleId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[tb_usuario_acceso_externo].[IX_AspNetUserLogins_UserId]', N'IX_tb_usuario_acceso_externo_UserId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[tb_usuario_atributo].[IX_AspNetUserClaims_UserId]', N'IX_tb_usuario_atributo_UserId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    EXEC sp_rename N'[tb_rol_atributo].[IX_AspNetRoleClaims_RoleId]', N'IX_tb_rol_atributo_RoleId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_publicacion] ADD CONSTRAINT [PK_tb_publicacion] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_medio_pago] ADD CONSTRAINT [PK_tb_medio_pago] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_donacion] ADD CONSTRAINT [PK_tb_donacion] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_donacion_bien] ADD CONSTRAINT [PK_tb_donacion_bien] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_donacion_historial] ADD CONSTRAINT [PK_tb_donacion_historial] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_congregante] ADD CONSTRAINT [PK_tb_congregante] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_iglesia] ADD CONSTRAINT [PK_tb_iglesia] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_usuario_token] ADD CONSTRAINT [PK_tb_usuario_token] PRIMARY KEY ([UserId], [LoginProvider], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_usuario] ADD CONSTRAINT [PK_tb_usuario] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_usuario_rol] ADD CONSTRAINT [PK_tb_usuario_rol] PRIMARY KEY ([UserId], [RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_usuario_acceso_externo] ADD CONSTRAINT [PK_tb_usuario_acceso_externo] PRIMARY KEY ([LoginProvider], [ProviderKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_usuario_atributo] ADD CONSTRAINT [PK_tb_usuario_atributo] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_rol] ADD CONSTRAINT [PK_tb_rol] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_rol_atributo] ADD CONSTRAINT [PK_tb_rol_atributo] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_congregante] ADD CONSTRAINT [FK_tb_congregante_tb_usuario_UserId] FOREIGN KEY ([UserId]) REFERENCES [tb_usuario] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_donacion] ADD CONSTRAINT [FK_tb_donacion_tb_congregante_CongregantId] FOREIGN KEY ([CongregantId]) REFERENCES [tb_congregante] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_donacion] ADD CONSTRAINT [FK_tb_donacion_tb_medio_pago_PaymentMethodId] FOREIGN KEY ([PaymentMethodId]) REFERENCES [tb_medio_pago] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_donacion_bien] ADD CONSTRAINT [FK_tb_donacion_bien_tb_donacion_DonationId] FOREIGN KEY ([DonationId]) REFERENCES [tb_donacion] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_donacion_historial] ADD CONSTRAINT [FK_tb_donacion_historial_tb_donacion_DonationId] FOREIGN KEY ([DonationId]) REFERENCES [tb_donacion] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_donacion_historial] ADD CONSTRAINT [FK_tb_donacion_historial_tb_usuario_AdministratorId] FOREIGN KEY ([AdministratorId]) REFERENCES [tb_usuario] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_rol_atributo] ADD CONSTRAINT [FK_tb_rol_atributo_tb_rol_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [tb_rol] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_usuario_acceso_externo] ADD CONSTRAINT [FK_tb_usuario_acceso_externo_tb_usuario_UserId] FOREIGN KEY ([UserId]) REFERENCES [tb_usuario] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_usuario_atributo] ADD CONSTRAINT [FK_tb_usuario_atributo_tb_usuario_UserId] FOREIGN KEY ([UserId]) REFERENCES [tb_usuario] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_usuario_rol] ADD CONSTRAINT [FK_tb_usuario_rol_tb_rol_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [tb_rol] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_usuario_rol] ADD CONSTRAINT [FK_tb_usuario_rol_tb_usuario_UserId] FOREIGN KEY ([UserId]) REFERENCES [tb_usuario] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    ALTER TABLE [tb_usuario_token] ADD CONSTRAINT [FK_tb_usuario_token_tb_usuario_UserId] FOREIGN KEY ([UserId]) REFERENCES [tb_usuario] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012013447_RenombrarTablasEspanol'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012013447_RenombrarTablasEspanol', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    DROP INDEX [IX_tb_donacion_PaymentMethodId] ON [tb_donacion];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    ALTER TABLE [tb_publicacion] ADD [EventEndsAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    ALTER TABLE [tb_publicacion] ADD [ImageKey] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    ALTER TABLE [tb_publicacion] ADD [ImageType] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    ALTER TABLE [tb_donacion_bien] ADD [ExpiresOn] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    ALTER TABLE [tb_donacion_bien] ADD [ProductId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    ALTER TABLE [tb_donacion_bien] ADD [StorageLocation] nvarchar(160) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    ALTER TABLE [tb_donacion] ADD [FundId] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    ALTER TABLE [tb_donacion] ADD [OperationNumber] nvarchar(80) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    ALTER TABLE [tb_congregante] ADD [Dni] nvarchar(8) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE TABLE [tb_auditoria] (
        [Id] bigint NOT NULL IDENTITY,
        [ActorId] nvarchar(450) NOT NULL,
        [Action] nvarchar(100) NOT NULL,
        [Reference] nvarchar(100) NOT NULL,
        [Detail] nvarchar(2000) NOT NULL,
        [At] datetimeoffset NOT NULL,
        CONSTRAINT [PK_tb_auditoria] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE TABLE [tb_beneficiario] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(160) NOT NULL,
        [Address] nvarchar(250) NOT NULL,
        [Contact] nvarchar(160) NOT NULL,
        [Phone] nvarchar(30) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_tb_beneficiario] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE TABLE [tb_cierre_caja] (
        [Id] int NOT NULL IDENTITY,
        [Channel] int NOT NULL,
        [EvidenceKey] nvarchar(max) NULL,
        [EvidenceType] nvarchar(max) NULL,
        [Day] datetime2 NOT NULL,
        [Opening] decimal(12,2) NOT NULL,
        [Income] decimal(12,2) NOT NULL,
        [Outgoing] decimal(12,2) NOT NULL,
        [Expected] decimal(12,2) NOT NULL,
        [Counted] decimal(12,2) NOT NULL,
        [Notes] nvarchar(1000) NOT NULL,
        [PreparedBy] nvarchar(450) NOT NULL,
        [ReviewedBy] nvarchar(450) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_tb_cierre_caja] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE TABLE [tb_fondo] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(120) NOT NULL,
        [Purpose] nvarchar(500) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_tb_fondo] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE TABLE [tb_participante_evento] (
        [Id] int NOT NULL IDENTITY,
        [PublicationId] int NOT NULL,
        [CongregantId] int NOT NULL,
        [Attendance] int NOT NULL,
        [InternalNote] nvarchar(1000) NULL,
        [RecordedBy] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_tb_participante_evento] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_tb_participante_evento_tb_congregante_CongregantId] FOREIGN KEY ([CongregantId]) REFERENCES [tb_congregante] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tb_participante_evento_tb_publicacion_PublicationId] FOREIGN KEY ([PublicationId]) REFERENCES [tb_publicacion] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE TABLE [tb_personal] (
        [UserId] nvarchar(450) NOT NULL,
        [FullName] nvarchar(160) NOT NULL,
        [Position] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_tb_personal] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_tb_personal_tb_usuario_UserId] FOREIGN KEY ([UserId]) REFERENCES [tb_usuario] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE TABLE [tb_producto] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(160) NOT NULL,
        [Unit] nvarchar(40) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [RequiresExpiry] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_tb_producto] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE TABLE [tb_solicitud_bienes] (
        [Id] int NOT NULL IDENTITY,
        [BeneficiaryId] int NOT NULL,
        [Purpose] nvarchar(1000) NOT NULL,
        [Priority] int NOT NULL,
        [Kind] int NOT NULL,
        [Status] int NOT NULL,
        [NeededOn] datetime2 NOT NULL,
        [ReturnDueOn] datetime2 NULL,
        [RequestedBy] nvarchar(450) NOT NULL,
        [ApprovedBy] nvarchar(450) NULL,
        [DecisionReason] nvarchar(1000) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [Version] rowversion NOT NULL,
        CONSTRAINT [PK_tb_solicitud_bienes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_tb_solicitud_bienes_tb_beneficiario_BeneficiaryId] FOREIGN KEY ([BeneficiaryId]) REFERENCES [tb_beneficiario] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE TABLE [tb_solicitud_dinero] (
        [Id] int NOT NULL IDENTITY,
        [FundId] int NOT NULL,
        [Channel] int NOT NULL,
        [Kind] int NOT NULL,
        [Status] int NOT NULL,
        [Amount] decimal(12,2) NOT NULL,
        [Purpose] nvarchar(1000) NOT NULL,
        [Payee] nvarchar(160) NOT NULL,
        [RequestedBy] nvarchar(450) NOT NULL,
        [ApprovedBy] nvarchar(450) NULL,
        [PaidBy] nvarchar(450) NULL,
        [DecisionReason] nvarchar(1000) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [DueOn] datetime2 NULL,
        [PaidOn] datetime2 NULL,
        [SettledOn] datetime2 NULL,
        [ActualSpent] decimal(12,2) NULL,
        [EvidenceKey] nvarchar(max) NULL,
        [EvidenceType] nvarchar(max) NULL,
        [SettlementKey] nvarchar(max) NULL,
        [SettlementType] nvarchar(max) NULL,
        [Version] rowversion NOT NULL,
        CONSTRAINT [PK_tb_solicitud_dinero] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_tb_solicitud_dinero_tb_fondo_FundId] FOREIGN KEY ([FundId]) REFERENCES [tb_fondo] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE TABLE [tb_lote] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [DonationItemId] int NULL,
        [Location] nvarchar(160) NOT NULL,
        [Origin] nvarchar(200) NOT NULL,
        [ExpiresOn] datetime2 NULL,
        [OnHand] decimal(12,2) NOT NULL,
        [Reserved] decimal(12,2) NOT NULL,
        [Version] rowversion NOT NULL,
        CONSTRAINT [PK_tb_lote] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Lote_Saldos] CHECK ([OnHand] >= 0 AND [Reserved] >= 0 AND [Reserved] <= [OnHand]),
        CONSTRAINT [FK_tb_lote_tb_donacion_bien_DonationItemId] FOREIGN KEY ([DonationItemId]) REFERENCES [tb_donacion_bien] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tb_lote_tb_producto_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [tb_producto] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE TABLE [tb_movimiento_dinero] (
        [Id] bigint NOT NULL IDENTITY,
        [FundId] int NOT NULL,
        [Channel] int NOT NULL,
        [Kind] int NOT NULL,
        [Amount] decimal(12,2) NOT NULL,
        [DonationId] int NULL,
        [FinanceRequestId] int NULL,
        [ActorId] nvarchar(450) NOT NULL,
        [Detail] nvarchar(1000) NOT NULL,
        [BusinessDate] datetime2 NOT NULL,
        [At] datetimeoffset NOT NULL,
        CONSTRAINT [PK_tb_movimiento_dinero] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_tb_movimiento_dinero_tb_donacion_DonationId] FOREIGN KEY ([DonationId]) REFERENCES [tb_donacion] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tb_movimiento_dinero_tb_fondo_FundId] FOREIGN KEY ([FundId]) REFERENCES [tb_fondo] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tb_movimiento_dinero_tb_solicitud_dinero_FinanceRequestId] FOREIGN KEY ([FinanceRequestId]) REFERENCES [tb_solicitud_dinero] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE TABLE [tb_solicitud_bien] (
        [Id] int NOT NULL IDENTITY,
        [StockRequestId] int NOT NULL,
        [StockLotId] int NOT NULL,
        [Quantity] decimal(12,2) NOT NULL,
        [Delivered] decimal(12,2) NOT NULL,
        [Returned] decimal(12,2) NOT NULL,
        [WrittenOff] decimal(12,2) NOT NULL,
        CONSTRAINT [PK_tb_solicitud_bien] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SolicitudBien_Saldos] CHECK ([Quantity] > 0 AND [Delivered] >= 0 AND [Delivered] <= [Quantity] AND [Returned] >= 0 AND [WrittenOff] >= 0 AND [Returned] + [WrittenOff] <= [Delivered]),
        CONSTRAINT [FK_tb_solicitud_bien_tb_lote_StockLotId] FOREIGN KEY ([StockLotId]) REFERENCES [tb_lote] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tb_solicitud_bien_tb_solicitud_bienes_StockRequestId] FOREIGN KEY ([StockRequestId]) REFERENCES [tb_solicitud_bienes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE TABLE [tb_movimiento_almacen] (
        [Id] bigint NOT NULL IDENTITY,
        [CommandId] uniqueidentifier NULL,
        [StockLotId] int NOT NULL,
        [StockRequestLineId] int NULL,
        [Kind] int NOT NULL,
        [Quantity] decimal(12,2) NOT NULL,
        [ReservedChange] decimal(12,2) NOT NULL,
        [ActorId] nvarchar(450) NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [Receiver] nvarchar(160) NULL,
        [EvidenceKey] nvarchar(max) NULL,
        [EvidenceType] nvarchar(max) NULL,
        [At] datetimeoffset NOT NULL,
        [BusinessDate] datetime2 NOT NULL,
        CONSTRAINT [PK_tb_movimiento_almacen] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_tb_movimiento_almacen_tb_lote_StockLotId] FOREIGN KEY ([StockLotId]) REFERENCES [tb_lote] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tb_movimiento_almacen_tb_solicitud_bien_StockRequestLineId] FOREIGN KEY ([StockRequestLineId]) REFERENCES [tb_solicitud_bien] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsActive', N'Name', N'Purpose') AND [object_id] = OBJECT_ID(N'[tb_fondo]'))
        SET IDENTITY_INSERT [tb_fondo] ON;
    EXEC(N'INSERT INTO [tb_fondo] ([Id], [IsActive], [Name], [Purpose])
    VALUES (1, CAST(1 AS bit), N''General'', N''Obra y necesidades de la iglesia'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsActive', N'Name', N'Purpose') AND [object_id] = OBJECT_ID(N'[tb_fondo]'))
        SET IDENTITY_INSERT [tb_fondo] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE INDEX [IX_tb_donacion_bien_ProductId] ON [tb_donacion_bien] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE INDEX [IX_tb_donacion_FundId] ON [tb_donacion] ([FundId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_tb_donacion_PaymentMethodId_ReceivedOrPaidOn_OperationNumber] ON [tb_donacion] ([PaymentMethodId], [ReceivedOrPaidOn], [OperationNumber]) WHERE [OperationNumber] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_tb_congregante_Dni] ON [tb_congregante] ([Dni]) WHERE [Dni] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE INDEX [IX_tb_auditoria_At] ON [tb_auditoria] ([At]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tb_cierre_caja_Day_Channel] ON [tb_cierre_caja] ([Day], [Channel]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tb_fondo_Name] ON [tb_fondo] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_tb_lote_DonationItemId] ON [tb_lote] ([DonationItemId]) WHERE [DonationItemId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE INDEX [IX_tb_lote_ProductId] ON [tb_lote] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE INDEX [IX_tb_movimiento_almacen_BusinessDate] ON [tb_movimiento_almacen] ([BusinessDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_tb_movimiento_almacen_CommandId] ON [tb_movimiento_almacen] ([CommandId]) WHERE [CommandId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE INDEX [IX_tb_movimiento_almacen_StockLotId] ON [tb_movimiento_almacen] ([StockLotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE INDEX [IX_tb_movimiento_almacen_StockRequestLineId] ON [tb_movimiento_almacen] ([StockRequestLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_tb_movimiento_dinero_DonationId_Kind] ON [tb_movimiento_dinero] ([DonationId], [Kind]) WHERE [DonationId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE INDEX [IX_tb_movimiento_dinero_FinanceRequestId] ON [tb_movimiento_dinero] ([FinanceRequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE INDEX [IX_tb_movimiento_dinero_FundId_Channel_BusinessDate] ON [tb_movimiento_dinero] ([FundId], [Channel], [BusinessDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE INDEX [IX_tb_participante_evento_CongregantId] ON [tb_participante_evento] ([CongregantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tb_participante_evento_PublicationId_CongregantId] ON [tb_participante_evento] ([PublicationId], [CongregantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tb_producto_Name_Unit] ON [tb_producto] ([Name], [Unit]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE INDEX [IX_tb_solicitud_bien_StockLotId] ON [tb_solicitud_bien] ([StockLotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tb_solicitud_bien_StockRequestId_StockLotId] ON [tb_solicitud_bien] ([StockRequestId], [StockLotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE INDEX [IX_tb_solicitud_bienes_BeneficiaryId] ON [tb_solicitud_bienes] ([BeneficiaryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    CREATE INDEX [IX_tb_solicitud_dinero_FundId_Channel_Status] ON [tb_solicitud_dinero] ([FundId], [Channel], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN

    UPDATE tb_donacion SET FundId = 1;
    INSERT INTO tb_personal (UserId, FullName, Position, IsActive)
    SELECT DISTINCT u.Id, LEFT(COALESCE(u.Email, u.UserName, u.Id),160), NULL, 1
    FROM tb_usuario u INNER JOIN tb_usuario_rol ur ON ur.UserId=u.Id
    INNER JOIN tb_rol r ON r.Id=ur.RoleId
    WHERE r.Name IN (N'Administrador',N'Tesorero',N'Almacenero');
    INSERT INTO tb_movimiento_dinero (FundId,Channel,Kind,Amount,DonationId,FinanceRequestId,ActorId,Detail,BusinessDate,At)
    SELECT 1, CASE WHEN PaymentMethodId IS NULL THEN 0 ELSE 1 END, 0, Amount, Id, NULL,
    COALESCE(RecordedByUserId,N'migracion'), N'Saldo de donación aprobada anterior a Gestión Integral',
    CAST(ReceivedOrPaidOn AS date), CreatedAt FROM tb_donacion WHERE Kind=0 AND Status=1;
    INSERT INTO tb_auditoria (ActorId,Action,Reference,Detail,At)
    VALUES(N'migracion',N'Actualización de módulos',N'GestionIntegral',N'Se conservaron donaciones, usuarios y congregantes. Completar DNI e incorporar bienes tras conteo físico.',SYSUTCDATETIME());
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    ALTER TABLE [tb_donacion] ADD CONSTRAINT [FK_tb_donacion_tb_fondo_FundId] FOREIGN KEY ([FundId]) REFERENCES [tb_fondo] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    ALTER TABLE [tb_donacion_bien] ADD CONSTRAINT [FK_tb_donacion_bien_tb_producto_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [tb_producto] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184128_GestionIntegral'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018184128_GestionIntegral', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184944_HistorialParticipacionYDestinos'
)
BEGIN
    ALTER TABLE [tb_solicitud_bienes] ADD [BeneficiaryAddress] nvarchar(250) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184944_HistorialParticipacionYDestinos'
)
BEGIN
    ALTER TABLE [tb_solicitud_bienes] ADD [BeneficiaryContact] nvarchar(160) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184944_HistorialParticipacionYDestinos'
)
BEGIN
    ALTER TABLE [tb_solicitud_bienes] ADD [BeneficiaryName] nvarchar(160) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184944_HistorialParticipacionYDestinos'
)
BEGIN
    ALTER TABLE [tb_participante_evento] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184944_HistorialParticipacionYDestinos'
)
BEGIN
    ALTER TABLE [tb_participante_evento] ADD [RemovalReason] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184944_HistorialParticipacionYDestinos'
)
BEGIN
    ALTER TABLE [tb_participante_evento] ADD [Version] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184944_HistorialParticipacionYDestinos'
)
BEGIN
    UPDATE r SET BeneficiaryName=b.Name, BeneficiaryAddress=b.Address, BeneficiaryContact=b.Contact FROM tb_solicitud_bienes r INNER JOIN tb_beneficiario b ON b.Id=r.BeneficiaryId;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184944_HistorialParticipacionYDestinos'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018184944_HistorialParticipacionYDestinos', N'10.0.12');
END;

COMMIT;
GO

