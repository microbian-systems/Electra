
USE master
GO
-- Uncomment the ALTER DATABASE statement below to set the database to SINGLE_USER mode if the drop database command fails because the database is in use.
ALTER DATABASE Electra SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
-- Drop the database if it exists
IF EXISTS (
  SELECT [name]
   FROM sys.databases
   WHERE [name] = N'Electra')
DROP DATABASE Electra
GO

-- Create the new database if it does not exist already
IF NOT EXISTS (
    SELECT [name]
        FROM sys.databases
        WHERE [name] = N'Electra')

CREATE DATABASE Electra
GO

CREATE LOGIN Electra WITH password='*StrongPassword1';
GO

USE Electra
GO

CREATE USER [Electra] FROM LOGIN [Electra];
EXEC sp_addrolemember 'db_owner', 'Electra';
GO