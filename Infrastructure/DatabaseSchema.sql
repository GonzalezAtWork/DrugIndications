-- Create database schema
CREATE DATABASE DrugIndications;
GO

USE DrugIndications;
GO

-- Create tables
CREATE TABLE Drugs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Manufacturer NVARCHAR(100) NULL
);

CREATE TABLE Indications (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Description NVARCHAR(500) NOT NULL,
    ICD10Code NVARCHAR(10) NULL,
    DrugId INT NOT NULL,
    FOREIGN KEY (DrugId) REFERENCES Drugs(Id)
);

CREATE TABLE CopayPrograms (
    ProgramId INT PRIMARY KEY,
    ProgramName NVARCHAR(200) NOT NULL,
    ProgramType NVARCHAR(50) NOT NULL
);

CREATE TABLE CoverageEligibilities (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProgramId INT NOT NULL,
    Eligibility NVARCHAR(100) NOT NULL,
    FOREIGN KEY (ProgramId) REFERENCES CopayPrograms(ProgramId)
);

CREATE TABLE Requirements (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProgramId INT NOT NULL,
    Name NVARCHAR(50) NOT NULL,
    Value NVARCHAR(50) NOT NULL,
    FOREIGN KEY (ProgramId) REFERENCES CopayPrograms(ProgramId)
);

CREATE TABLE Benefits (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProgramId INT NOT NULL,
    Name NVARCHAR(50) NOT NULL,
    Value NVARCHAR(50) NOT NULL,
    FOREIGN KEY (ProgramId) REFERENCES CopayPrograms(ProgramId)
);

CREATE TABLE Forms (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProgramId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Link NVARCHAR(500) NOT NULL,
    FOREIGN KEY (ProgramId) REFERENCES CopayPrograms(ProgramId)
);

CREATE TABLE Funding (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProgramId INT NOT NULL,
    Evergreen NVARCHAR(10) NOT NULL,
    CurrentFundingLevel NVARCHAR(100) NOT NULL,
    FOREIGN KEY (ProgramId) REFERENCES CopayPrograms(ProgramId)
);

CREATE TABLE ProgramDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProgramId INT NOT NULL,
    Eligibility NVARCHAR(500) NOT NULL,
    Program NVARCHAR(500) NOT NULL,
    Renewal NVARCHAR(500) NOT NULL,
    Income NVARCHAR(100) NOT NULL,
    FOREIGN KEY (ProgramId) REFERENCES CopayPrograms(ProgramId)
);

-- Create Users table for authentication
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(100) NOT NULL,
    Salt NVARCHAR(50) NOT NULL,
    Role NVARCHAR(20) NOT NULL
);