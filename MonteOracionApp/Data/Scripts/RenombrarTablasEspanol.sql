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

