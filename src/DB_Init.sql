
USE master
GO
-- Uncomment the ALTER DATABASE statement below to set the database to SINGLE_USER mode if the drop database command fails because the database is in use.
ALTER DATABASE AppX SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
-- Drop the database if it exists
IF EXISTS (
  SELECT [name]
   FROM sys.databases
   WHERE [name] = N'AppX')
DROP DATABASE AppX
GO

-- Create the new database if it does not exist already
IF NOT EXISTS (
    SELECT [name]
        FROM sys.databases
        WHERE [name] = N'AppX')

CREATE DATABASE AppX
GO

CREATE LOGIN AppX WITH password='*StrongPassword1';
GO

USE AppX
GO

CREATE USER [AppX] FROM LOGIN [AppX];
EXEC sp_addrolemember 'db_owner', 'AppX';
GO