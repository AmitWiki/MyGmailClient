CREATE TABLE [dbo].[UserRoles]
(
	[Id] INT IDENTITY(1,1) NOT NULL,
	[UserRole] varchar(100) not null PRIMARY KEY,
	[RoleDescription] varchar(100) not null
)

CREATE TABLE [dbo].[Logins]
(
	[Id] INT identity(1,1) NOT NULL,
	[Username] varchar(100) NOT NULL PRIMARY KEY,
	[Password]  varchar(100) NOT NULL,
	[UserRole]  varchar(100) NOT NULL,
	[Active]  varchar(100) NOT NULL
)


CREATE TABLE [dbo].[GmailConfig]
(
	[Id] INT NOT NULL PRIMARY KEY,
	[Username] varchar(100) foreign key references Logins,
	[GmailUsername] varchar(100) not null,
	[GmailPassword] varchar(100) not null,
	[IncomingServerAddress] varchar(100) not null,
	[OutgoingServerAddress] varchar(100) not null,
	[IncomingServerPort] varchar(100) not null,
	[OutgoingServerPort] varchar(100) not null,
	[UseSSL] bit not null
)
