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

