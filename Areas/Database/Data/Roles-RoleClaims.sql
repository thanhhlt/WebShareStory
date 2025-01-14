USE [WebShareStory]
GO
DBCC CHECKIDENT ('Roles', RESEED, 0);
INSERT [dbo].[Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'06da6b7b-1c32-4248-ac01-dd02493baad4', N'Member', N'MEMBER', N'66bf5ba2-06ae-4aec-ab3e-dd8b80e587e6')
INSERT [dbo].[Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'474c1b7c-1bca-4ede-b888-704c0fc40bad', N'Guest', N'GUEST', N'2cf2baca-737b-4aed-9e88-7a7f18e7e0f0')
INSERT [dbo].[Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'5d293420-40b0-4626-9e64-8021cf1e2211', N'Editor', N'EDITOR', N'27c28417-d560-4983-a466-154a13e217b2')
INSERT [dbo].[Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'8169cea6-2433-4957-8e18-52fcd173c375', N'Admin', N'ADMIN', N'45955d27-523a-4149-924a-4611fa77d157')
INSERT [dbo].[Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'dec1482d-2a62-4ff7-8916-ba34181997fe', N'Moderator', N'MODERATOR', N'a0475862-5e9a-4823-bc54-7639b8acfc3c')
GO
SET IDENTITY_INSERT [dbo].[RoleClaims] ON 

DBCC CHECKIDENT ('RoleClaims', RESEED, 0);
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (2006, N'474c1b7c-1bca-4ede-b888-704c0fc40bad', N'Feature', N'PostEngage')
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (2007, N'474c1b7c-1bca-4ede-b888-704c0fc40bad', N'Feature', N'UserEngage')
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (2009, N'06da6b7b-1c32-4248-ac01-dd02493baad4', N'Feature', N'TwoFactorAuth')
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (2010, N'06da6b7b-1c32-4248-ac01-dd02493baad4', N'Permission', N'CreatePost')
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (2011, N'dec1482d-2a62-4ff7-8916-ba34181997fe', N'Feature', N'UserManage')
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (2012, N'dec1482d-2a62-4ff7-8916-ba34181997fe', N'Permission', N'UserLock')
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (2013, N'dec1482d-2a62-4ff7-8916-ba34181997fe', N'Permission', N'ResetPassword')
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (2014, N'5d293420-40b0-4626-9e64-8021cf1e2211', N'Feature', N'PostManage')
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (2020, N'5d293420-40b0-4626-9e64-8021cf1e2211', N'Feature', N'ContactManage')
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (2021, N'dec1482d-2a62-4ff7-8916-ba34181997fe', N'Feature', N'ContactManage')
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (2022, N'5d293420-40b0-4626-9e64-8021cf1e2211', N'Feature', N'CateManage')
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (2023, N'8169cea6-2433-4957-8e18-52fcd173c375', N'Permission', N'DeleteUser')
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (2024, N'8169cea6-2433-4957-8e18-52fcd173c375', N'Feature', N'DatabaseManage')
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (2025, N'8169cea6-2433-4957-8e18-52fcd173c375', N'Feature', N'RoleManage')
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (2026, N'8169cea6-2433-4957-8e18-52fcd173c375', N'Feature', N'ContactManage')
INSERT [dbo].[RoleClaims] ([Id], [RoleId], [ClaimType], [ClaimValue]) VALUES (2029, N'8169cea6-2433-4957-8e18-52fcd173c375', N'Permission', N'ViewStatistics')
SET IDENTITY_INSERT [dbo].[RoleClaims] OFF
GO
