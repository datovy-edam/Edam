-- PSJ_Courts_Canonical

CREATE SCHEMA [Case]
GO

CREATE SCHEMA [Code]
GO

CREATE SCHEMA [Activity]
GO

CREATE SCHEMA [Party]
GO

CREATE SCHEMA [Charge]
GO

CREATE SCHEMA [Warrant]
GO

CREATE SCHEMA [Sentence]
GO

CREATE SCHEMA [Entity]
GO

CREATE SCHEMA [Citation]
GO

-- -----------------------------------------------------------------------------
-- Entity

CREATE TABLE [Entity].[OrganizationType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Entity].[Organization] (
   [OrganizationNo] BIGINT IDENTITY PRIMARY KEY,
   [OrganizationID] VARCHAR(20),
   [TypeID] VARCHAR(20) NOT NULL,
   [ORI] VARCHAR(10),
   [OrganizationName] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_Organization_Type FOREIGN KEY(TypeID)
      REFERENCES [Entity].[OrganizationType](TypeID)
);

CREATE TABLE [Entity].[Officer] (
   [OfficerID] VARCHAR(20) PRIMARY KEY,
   [TypeID] VARCHAR(20) NOT NULL,
   [RegistrationID] VARCHAR(40),
   [Name] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Entity].[LocationType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Entity].[LocationDivisionType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Entity].[LocationDivision] (
   [DivisionNo] BIGINT IDENTITY PRIMARY KEY,
   [DivisionID] VARCHAR(20),
   [TypeID] VARCHAR(20),
   [FipsCodeID] VARCHAR(20),
   [CountryID] VARCHAR(20),
   [CountyID] VARCHAR(20),
   [StateID] VARCHAR(20),
   [CityID] VARCHAR(20),
   [DistrictID] VARCHAR(20),
   [Name] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_LocationDivision_Type FOREIGN KEY (TypeID)
      REFERENCES [Entity].[LocationDivisionType](TypeID)
);

CREATE TABLE [Entity].[Location] (
   [LocationNo] BIGINT IDENTITY PRIMARY KEY,
   [LocationID] VARCHAR(20),
   [TypeID] VARCHAR(20),
   [AddressLine1] VARCHAR(128),
   [AddressLine2] VARCHAR(128),
   [CityCodeID] VARCHAR(20),
   [StateCodeID] VARCHAR(20),
   [PostalCodeID] VARCHAR(20),
   [DivisionNo] BIGINT,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_Location_Type FOREIGN KEY (TypeID)
      REFERENCES [Entity].[LocationType](TypeID),
   CONSTRAINT fk_Location_Division FOREIGN KEY (DivisionNo)
      REFERENCES [Entity].[LocationDivision](DivisionNo)
);

CREATE TABLE [Entity].[OrganizationLocation] (
   [OrganizationNo] BIGINT NOT NULL,
   [LocationNo] BIGINT NOT NULL,
   [OrganizationLocationNo] BIGINT IDENTITY PRIMARY KEY,

   CONSTRAINT fk_OrganizationLocation_Organization FOREIGN KEY(OrganizationNo)
      REFERENCES [Entity].[Organization](OrganizationNo),
   CONSTRAINT fk_OrganizationLocation_Location FOREIGN KEY(LocationNo)
      REFERENCES [Entity].[Location](LocationNo)
)
GO

-- -----------------------------------------------------------------------------
-- Code

CREATE TABLE [Code].[JurisdictionCode] (
   [CodeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Code].[StatuteTitleCode] (
   [CodeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20) DEFAULT '',
   [DataOwnerID] VARCHAR(20) DEFAULT 'MDEC',
   [DataSourceID] VARCHAR(20) DEFAULT 'JUSTICE-DB',
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);
GO

INSERT INTO [Code].[StatuteTitleCode] (
   CodeID, Description) VALUES
  ('AB','Alcoholic Beverages'),
  ('BO','Business Occupations and Professions'),
  ('BR','Business Regulations'),
  ('CJ','Courts and Judicial Proceedings'),
  ('CA','Corporations and Associations'),
  ('CL','Commercial Law'),
  ('CP','Criminal Procedure'),
  ('CR','Criminal Law'),
  ('CS','Correctional Services'),
  ('EC','Economic Development'),
  ('ED','Education'),
  ('EL','Election Law'),
  ('EN','Environment'),
  ('FI','Financial Institutions'),
  ('FL','Family Law'),
  ('HG','Health General'),
  ('HO','Health Occupations'),
  ('HS','Housing and Community Development'),
  ('HU','Human Services'),
  ('IN','Insurance'),
  ('LE','Labor and Employment'),
  ('NR','Natural Resources'),
  ('PS','Public Safety'),
  ('RP','Real Property'),
  ('SF','State Finance and Procurement'),
  ('SG','State Government'),
  ('TG','Tax General'),
  ('TR','Transportation')
GO

CREATE TABLE [Code].[OffenseCategoryCode] (
   [CodeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Code].[OffenseCode] (
   [OffenseID] VARCHAR(20) PRIMARY KEY,
   [StatuteNumber] VARCHAR(20),
   [StatuteCode] VARCHAR(20),
   [StatuteDescription] VARCHAR(128),
   [StatuteDegree] VARCHAR(20),
   [TitleCodeID] VARCHAR(20),
   [CategoryCodeID] VARCHAR(20),
   [CjisCodeID] VARCHAR(20),
   [EnactedDate] DATE,
   [RepealedDate] DATE,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_OffenseCodeTitle FOREIGN KEY (TitleCodeID)
      REFERENCES [Code].[StatuteTitleCode](CodeID),
   CONSTRAINT fk_OffenseCodeCategory FOREIGN KEY (CategoryCodeID)
      REFERENCES [Code].[OffenseCategoryCode](CodeID)
);

CREATE TABLE [Code].[DrugType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Code].[DateDetail] (
   DateID             VARCHAR(8) PRIMARY KEY,
   [Date]             DATETIME NOT NULL,
   MonthAlias         CHAR(3) NOT NULL,
   YearText           CHAR(4) NOT NULL,
   MonthText          CHAR(2) NOT NULL,
   DayText            CHAR(2) NOT NULL,
   DateText           CHAR(10) NOT NULL,
   [Year]             SMALLINT NOT NULL,
   [Month]            SMALLINT NOT NULL,
   [Day]              SMALLINT NOT NULL,
   [DayOfMonth]       SMALLINT NOT NULL,
   YearWeek           SMALLINT NOT NULL,
   MonthWeek          SMALLINT NOT NULL,
   [WeekDay]          SMALLINT NOT NULL,
   DayAlias           VARCHAR(10) NOT NULL,
   QuaterNo           SMALLINT NOT NULL,
   TrimesterNo        SMALLINT NOT NULL,
   OfficialHolidayNo  SMALLINT NULL,
   WeekDayTypeId      CHAR(1) NOT NULL,
   WorkHours          SMALLINT NOT NULL DEFAULT 0,
   [MonthName]        VARCHAR(10) NOT NULL,
   DayCount           SMALLINT NOT NULL
);

-- -----------------------------------------------------------------------------
-- Case

CREATE TABLE [Case].[CaseType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Case].[CaseDetail] (
   [CaseNo] BIGINT IDENTITY PRIMARY KEY,
   [CaseID] VARCHAR(20) NOT NULL,
   [CaseNumber] VARCHAR(20) NOT NULL,
   [TypeID] VARCHAR(20),
   [CaseTitle] VARCHAR(128),
   [FiledDate] DATE,
   [JudgeID] VARCHAR(20),
   [CourtNo] BIGINT,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_CaseDetail_Type FOREIGN KEY(TypeID)
      REFERENCES [Case].[CaseType](TypeID),
   CONSTRAINT fk_CaseDetail_Court FOREIGN KEY(CourtNo)
      REFERENCES [Entity].[Organization](OrganizationNo),
   CONSTRAINT fk_CaseDetail_Officer FOREIGN KEY(JudgeID)
      REFERENCES [Entity].[Officer](OfficerID)
);

CREATE TABLE [Case].[CaseFlag] (
   [CaseNo] BIGINT NOT NULL,
   [FlageID] VARCHAR(20) PRIMARY KEY,
   [FlagCode] VARCHAR(20),
   [FlagDescription] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_CaseFlag_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo)
);

CREATE TABLE [Case].[StatusCode] (
   [CodeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Case].[CaseStatus] (
   [StatusNo] BIGINT IDENTITY PRIMARY KEY,
   [CaseNo] BIGINT NOT NULL,
   [CodeID] VARCHAR(20),
   [Description] VARCHAR(20),
   [StatusDate] DATETIMEOFFSET,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_CaseStatus_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNO),
   CONSTRAINT fk_CaseStatus_StatusCode FOREIGN KEY (CodeID)
      REFERENCES [Case].[StatusCode](CodeID)
);

CREATE TABLE [Case].[CaseFiling] (
   [CaseFilingNo] BIGINT IDENTITY PRIMARY KEY,
   [CaseNo] BIGINT NOT NULL,
   [AuthorityNo] BIGINT,
   [CaseFilingID] VARCHAR(20) NOT NULL,
   [CourtEventID] VARCHAR(20),
   [DateFiled] DATE,
   [DateSubmitted] DATE,
   [StatusCode] VARCHAR(20),
   [StatusDate] DATETIMEOFFSET,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_CaseFilling_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_CaseFilling_Authority FOREIGN KEY(AuthorityNo)
      REFERENCES [Entity].[Organization]([OrganizationNo])
);

CREATE TABLE [Case].[CrossReferenceType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Case].[CaseCrossReference] (
   [CaseNo] BIGINT NOT NULL,
   [CrossReferenceID] VARCHAR(20) PRIMARY KEY,
   [TypeID] VARCHAR(20),
   [Description] VARCHAR(256),
   [CaseNumber] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_CaseCrossReference_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail]([CaseNo]),
   CONSTRAINT fk_CaseCrossReference_Type FOREIGN KEY(TypeID)
      REFERENCES [Case].[CrossReferenceType](TypeID)
);

CREATE TABLE [Case].[RelatedFillingType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Case].[RelatedFilling] (
   [CaseNo] BIGINT NOT NULL,
   [RelatedCaseFilingID] VARCHAR(20) PRIMARY KEY,
   [RelatedFillingID] VARCHAR(20),
   [TypeID] VARCHAR(20),
   [Reason] VARCHAR(256),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_RelatedFilling_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_RelatedFilling_Type FOREIGN KEY(TypeID)
      REFERENCES [Case].[RelatedFillingType](TypeID)
);

CREATE TABLE [Case].[EventType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Case].[Event] (
   [EventNo] BIGINT IDENTITY PRIMARY KEY,
   [CaseNo] BIGINT NOT NULL,
   [EventID] VARCHAR(20) NOT NULL,
   [EventDate] DATE,
   [EventTime] VARCHAR(20),
   [TypeID] VARCHAR(20),
   [Location] VARCHAR(20),
   [Number] VARCHAR(20),
   [ReviewDateTime] VARCHAR(20),
   [ReviewCompletedDateTime] DATETIMEOFFSET,
   [InstrumentNumber] VARCHAR(20),
   [PleaID] VARCHAR(20),
   [Amount] MONEY,
   [Comment] VARCHAR(1024),
   [ChargeID] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_CaseEvent_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_CaseEvent_EventCode FOREIGN KEY(TypeID)
      REFERENCES [Case].[EventType](TypeID)
);

-- -----------------------------------------------------------------------------
-- Activity

CREATE TABLE [Activity].[PleaType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Activity].[ReleaseType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Activity].[BondType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Activity].[BailBondRevocation] (
   [CaseNo] BIGINT NOT NULL,
   [RevocationID] VARCHAR(20) PRIMARY KEY,
   [RevocationDate] DATE,
   [Reason] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_BailBondRevocation_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo)
);

CREATE TABLE [Activity].[BailBondRelease] (
   [CaseNo] BIGINT NOT NULL,
   [ReleaseID] VARCHAR(20) PRIMARY KEY,
   [ReleaseType] VARCHAR(20),
   [BondType] VARCHAR(20),
   [AuthorityID] VARCHAR(20),
   [BondSetDate] DATE,
   [BondPostDate] DATE,
   [AmountOfBond] MONEY,
   [PostDate] DATE,
   [RevocationID] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_BailBondRelease_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_BailBondRelease_Revocation FOREIGN KEY(RevocationID)
      REFERENCES [Activity].[BailBondRevocation](RevocationID)
);

CREATE TABLE [Activity].[ConditionType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(256),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Activity].[ReleaseCondition] (
   [CaseNo] BIGINT NOT NULL,
   [ReleaseConditionID] VARCHAR(20),
   [BailBondReleaseID] VARCHAR(20),
   [ISNoContactOrder] BIT,
   [ConditionTypeID] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_ReleaseCondition_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_ReleaseCondition_Condition FOREIGN KEY(ConditionTypeID)
      REFERENCES [Activity].[ConditionType](TypeID)
);

CREATE TABLE [Activity].[FinancialEventType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(256),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Activity].[FeeType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(256),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Activity].[PriorityCode] (
   [CodeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(256),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Activity].[FinancialEvent] (
   [CaseNo] BIGINT NOT NULL,
   [EventID] VARCHAR(20) PRIMARY KEY,
   [TypeID] VARCHAR(20),
   [ChargeID] VARCHAR(20),
   [DateAssessed] DATE,
   [FeeCodeID] VARCHAR(20),
   [PriorityCodeID] VARCHAR(20),
   [WasReferredToCollections] BIT,
   [Amount] MONEY,
   [BalanceDue] MONEY,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_FinancialEvent_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_FinancialEvent_Condition FOREIGN KEY(TypeID)
      REFERENCES [Activity].[FinancialEventType](TypeID)
);

CREATE TABLE [Activity].[AppealType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Activity].[AppealDispositionType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Activity].[AppealStatus] (
   [CodeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(20),
   [Date] DATE,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Activity].[AppealEvent] (
   [CaseNo] BIGINT NOT NULL,
   [EventID] VARCHAR(20) PRIMARY KEY,
   [EventType] VARCHAR(20),
   [AppealTypeID] VARCHAR(20),
   [FiledDate] VARCHAR(20),
   [FileddBy] VARCHAR(20),
   [DispositionTypeID] VARCHAR(20),
   [AppealStatus] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_AppealEvent_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_AppealEvent_Status FOREIGN KEY(AppealStatus)
      REFERENCES [Activity].[AppealStatus](CodeID),
   CONSTRAINT fk_AppealEvent_AppealType FOREIGN KEY(AppealTypeID)
      REFERENCES [Activity].[AppealType](TypeID),
   CONSTRAINT fk_AppealEvent_DispositionType FOREIGN KEY(DispositionTypeID)
      REFERENCES [Activity].[AppealDispositionType](TypeID)
);

CREATE TABLE [Activity].[HearingType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(256),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Activity].[HearingFlag] (
   [FlagID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(256),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Activity].[HearingResultCode] (
   [CodeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(256),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Activity].[Hearing] (
   [CaseNo] BIGINT NOT NULL,
   [HearingID] VARCHAR(20) PRIMARY KEY,
   [CaseNumber] VARCHAR(20),
   [CaseTypeID] VARCHAR(20),
   [CaseTitle] VARCHAR(20),
   [CourtName] VARCHAR(128),
   [Defendant] VARCHAR(128),
   [AttorneyName] VARCHAR(128),
   [TypeID] VARCHAR(20),
   [HearingFlagID] VARCHAR(20),
   [HearingDate] DATE,
   [HearingSequence] VARCHAR(20),
   [SesssionDescription] VARCHAR(20),
   [StartTime] VARCHAR(20),
   [SessionEndTime] VARCHAR(20),
   [JudgeID] VARCHAR(20),
   [DepartmentNo] BIGINT,
   [ResultCodeID] VARCHAR(20),
   [CancelledReasonCode] VARCHAR(20),
   [RescheduleReasonCode] VARCHAR(20),
   [CancelledDateTime] DATETIMEOFFSET,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_Hearing_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_Hearing_Flag FOREIGN KEY(HearingFlagID)
      REFERENCES [Activity].[HearingFlag](FlagID),
   CONSTRAINT fk_Hearing_Type FOREIGN KEY(TypeID)
      REFERENCES [Activity].[HearingType](TypeID),
   CONSTRAINT fk_Hearing_CaseType FOREIGN KEY(CaseTypeID)
      REFERENCES [Case].[CaseType](TypeID),
   CONSTRAINT fk_Hearing_ResultCode FOREIGN KEY(ResultCodeID)
      REFERENCES [Activity].[HearingResultCode](CodeID),
   CONSTRAINT fk_Hearing_Department FOREIGN KEY(DepartmentNo)
      REFERENCES [Entity].[Organization](OrganizationNo),
   CONSTRAINT fk_Hearing_Judge FOREIGN KEY(JudgeID)
      REFERENCES [Entity].[Officer](OfficerID)
);

CREATE TABLE [Activity].[HearingEvent] (
   [HearingEventID] VARCHAR(20) PRIMARY KEY,
   [HearingID] VARCHAR(20) NOT NULL,
   [EventNo] BIGINT NOT NULL,
   [LinkType] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_HearingEvent_Hearing FOREIGN KEY(HearingID)
      REFERENCES [Activity].[Hearing](HearingID),
   CONSTRAINT fk_HearingEvent_Event FOREIGN KEY(EventNo)
      REFERENCES [Case].[Event](EventNo)
);

-- -----------------------------------------------------------------------------
-- Party

CREATE TABLE [Party].[GenderCode] (
   [CodeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Party].[RaceCode] (
   [CodeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Party].[PhoneType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

-- DROP TABLE [Party].[PartyDetail]
CREATE TABLE [Party].[PartyDetail] (
   [PartyNo] BIGINT IDENTITY PRIMARY KEY,
   [PartyID] VARCHAR(20) NOT NULL,
   [GenderCodeID] VARCHAR(20),
   [RaceCodeID] VARCHAR(20),
   [Height] VARCHAR(20),
   [Weight] VARCHAR(20),
   [DateOfBirth] DATE,
   [DateOfDeath] DATE,
   [StateID] VARCHAR(20),
   [FBINumber] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_PartyDetail_Gender FOREIGN KEY(GenderCodeID)
      REFERENCES [Party].[GenderCode](CodeID),
   CONSTRAINT fk_PartyDetail_Race FOREIGN KEY(RaceCodeID)
      REFERENCES [Party].[RaceCode](CodeID)
);

CREATE TABLE [Party].[NameType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(256),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Party].[PartyName] (
   [PartyNo] BIGINT NOT NULL,
   [PartyNameID] VARCHAR(20) PRIMARY KEY,
   [TypeID] VARCHAR(20) NOT NULL,
   [GivenName] VARCHAR(30),
   [MiddleName] VARCHAR(30),
   [Surname] VARCHAR(30),
   [NameSuffix] VARCHAR(30),
   [IsPrimary] BIT,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_PartyName_Party FOREIGN KEY(PartyNo)
      REFERENCES [Party].[PartyDetail](PartyNo),
   CONSTRAINT fk_PartyName_Type FOREIGN KEY(TypeID)
      REFERENCES [Party].[NameType](TypeID)
);

CREATE TABLE [Party].[PartyPhone] (
   [PartyNo] BIGINT NOT NULL,
   [PartyPhoneID] VARCHAR(20) PRIMARY KEY,
   [TypeID] VARCHAR(20),
   [Number] VARCHAR(20),
   [IsPrimary] BIT,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_PartyPhone_Party FOREIGN KEY(PartyNo)
      REFERENCES [Party].[PartyDetail](PartyNo),
   CONSTRAINT fk_PartyPhone_Type FOREIGN KEY(TypeID)
      REFERENCES [Party].[PhoneType](TypeID)
);

CREATE TABLE [Party].[DriverLicenseType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(256),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Party].[DriverLicenseStatusCode] (
   [CodeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(256),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Party].[PartyDriversLicense] (
   [PartyNo] BIGINT NOT NULL,
   [PartyDriversLicenseID] VARCHAR(20) PRIMARY KEY,
   [TypeID] VARCHAR(20),
   [Number] VARCHAR(20),
   [StateCode] VARCHAR(20),
   [IssuedDate] DATE,
   [ExpirationDate] DATE,
   [StatusCodeID] VARCHAR(20),
   [IsUSLicense] BIT,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_PartyDriversLicense_Party FOREIGN KEY(PartyNo)
      REFERENCES [Party].[PartyDetail](PartyNo),
   CONSTRAINT fk_PartyDriversLicense_Type FOREIGN KEY(TypeID)
      REFERENCES [Party].[DriverLicenseType](TypeID),
   CONSTRAINT fk_PartyDriversLicense_Status FOREIGN KEY(StatusCodeID)
      REFERENCES [Party].[DriverLicenseStatusCode](CodeID)
);

CREATE TABLE [Party].[PartyRoleCode] (
   [CodeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(256),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

-- -----------------------------------------------------------------------------

CREATE TABLE [Case].[CaseAttorney] (
   [CaseAttorneyNo] BIGINT IDENTITY PRIMARY KEY,
   [CaseAttorneyID] VARCHAR(20) NOT NULL,
   [RoleCodeID] VARCHAR(20),
   [OfficerID] VARCHAR(20),
   [Name] VARCHAR(128),
   [IsLead] BIT,
   [IsPublicDefender] BIT,
   [IsAppinted] BIT,
   [DateAdded] DATE,
   [DateRemoved] DATE,
   [RemoveReasonCodeID] VARCHAR(20),
   [Comment] VARCHAR(1024),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_CaseAttorney_Officer FOREIGN KEY(OfficerID)
      REFERENCES [Entity].[Officer](OfficerID),
   CONSTRAINT fk_CaseAttorney_Role FOREIGN KEY(RoleCodeID)
      REFERENCES [Party].[PartyRoleCode](CodeID)
);

CREATE TABLE [Case].[CaseParty] (
   [CasePartyNo] BIGINT IDENTITY PRIMARY KEY,
   [CasePartyID] VARCHAR(20) NOT NULL,
   [CaseNo] BIGINT NOT NULL,
   [PartyNo] BIGINT NOT NULL,
   [RoleCodeID] VARCHAR(20) NOT NULL,
   [AttorneyNo] BIGINT,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_CaseParty_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_CaseParty_Party FOREIGN KEY(PartyNo)
      REFERENCES [Party].[PartyDetail](PartyNo),
   CONSTRAINT fk_CaseParty_Role FOREIGN KEY(RoleCodeID)
      REFERENCES [Party].[PartyRoleCode](CodeID),
   CONSTRAINT fk_CaseParty_Attorney FOREIGN KEY(AttorneyNo)
      REFERENCES [Case].[CaseAttorney](CaseAttorneyNo)
);

CREATE TABLE [Case].[JudgementType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Case].[JudgementEvent] (
   [JudgementEventNo] BIGINT IDENTITY PRIMARY KEY,
   [CaseJudgementID] VARCHAR(20) NOT NULL,
   [EventNo] BIGINT,
   [EventDateTime] DATETIMEOFFSET,
   [CaseNo] BIGINT NOT NULL,
   [TypeID] VARCHAR(20) NOT NULL,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_Judgement_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_Judgement_Event FOREIGN KEY(EventNo)
      REFERENCES [Case].[Event](EventNo),
   CONSTRAINT fk_Judgement_Type FOREIGN KEY(TypeID)
      REFERENCES [Case].[JudgementType](TypeID)
);

CREATE TABLE [Case].[ProtectionOrderType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Case].[ProtectionOrder] (
   [ProtectionOrderNo] BIGINT IDENTITY PRIMARY KEY,
   [CaseNo] BIGINT NOT NULL,
   [ProtectionOrderLegacyID] BIGINT,
   [OrderNumber] VARCHAR(30),
   [CourtNo] BIGINT,
   [TypeID] VARCHAR(20),
   [IssuedDate] DATE,
   [ExpirationDate] DATE,
   [RespondentPartyNo] BIGINT,
   [BradyIndicatorFlag] VARCHAR(20),
   [IsDeleted] BIT,
   [IsExpirationUnknown] BIT,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_ProtectionOrder_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_ProtectionOrder_Type FOREIGN KEY(TypeID)
      REFERENCES [Case].[ProtectionOrderType](TypeID),
   CONSTRAINT fk_ProtectionOrder_Court FOREIGN KEY(CourtNo)
      REFERENCES [Entity].[Organization](OrganizationNo),
   CONSTRAINT fk_ProtectionOrder_RespondentParty FOREIGN KEY(RespondentPartyNo)
      REFERENCES [Party].[PartyDetail](PartyNo)
);

CREATE TABLE [Case].[ProtectionOrderStatusCode] (
   [CodeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Case].[ProtectionOrderStatus] (
   [ProtectionOrderStatusNo] BIGINT IDENTITY PRIMARY KEY,
   [CaseNo] BIGINT NOT NULL,
   [ProtectionOrderNo] BIGINT NOT NULL,
   [StatusDate] DATETIMEOFFSET,
   [StatusCodeID] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_ProtectionOrderStatus_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_ProtectionOrderStatus_Type FOREIGN KEY(StatusCodeID)
      REFERENCES [Case].[ProtectionOrderStatusCode](CodeID),
   CONSTRAINT fk_ProtectionOrderStatus_Order FOREIGN KEY(ProtectionOrderNo)
      REFERENCES [Case].[ProtectionOrder](ProtectionOrderNo)
);

CREATE TABLE [Case].[CaseState] (
   [StateNo] BIGINT IDENTITY PRIMARY KEY,
   [StateID] VARCHAR(20),
   [StateDate] DATE NOT NULL,
   [StateDateID] VARCHAR(20) NOT NULL,
   
   [CaseNo] BIGINT NOT NULL,
   
   -- related immutable Event reference
   [EventNo] BIGINT,
   [EventDateTime] DATETIMEOFFSET,
   
   -- Case current state data as of StateDate
   [DispositionNo] BIGINT,
   [JudgementNo] BIGINT,
   [ChargeDispositionNo] BIGINT,
   
   [UnservedWarrantID] BIGINT,
   [FailureToAppearWarrantID] BIGINT,   -- Bench Warrant
   
   [HasDismissedJudgement] BIT,
   [HasJudgementWithoutFutureHearing] BIT,
   
   [HasFailedToPayRent] BIT,
   [HasAllChargesDisposed] BIT,
   [CaseIsSealed] BIT,
   
   -- add other conditions below

   -- diagnostics metadata
   
   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_CaseState_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_CaseState_Event FOREIGN KEY(EventNo)
      REFERENCES [Case].[Event](EventNo)
);

-- -----------------------------------------------------------------------------
-- Charge

CREATE TABLE [Charge].[ChargeDetail] (
   [ChargeNo] BIGINT IDENTITY PRIMARY KEY,
   [CaseNo] BIGINT NOT NULL,
   [ChargeID] VARCHAR(20) NOT NULL,
   [FillingDate] DATE,
   [ArrestDate] DATE,
   [ArrestTrackID] VARCHAR(20),
   [DispositionDate] DATE,
   [OfficerID] VARCHAR(20),
   [OffenseDate] DATE,
   [OffenseLocationID] VARCHAR(20),
   [ChargeNumber] VARCHAR(20),
   [ChargeDescription] VARCHAR(256),
   [DrugTypeID] VARCHAR(256),
   [IncidentZoneTypeID] VARCHAR(20),
   [JurisdictionID] VARCHAR(20),
   [OffenseID] VARCHAR(20),
   [ChargeTrackNumber] VARCHAR(20),
   [ChargeTrackSequence] VARCHAR(20),
   [ChargeMitigator] VARCHAR(20),
   [AmendedDate] DATE,
   [AmendedReason] VARCHAR(128),
   [WasChargeChanged] BIT,
   [Deleted] BIT,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_ChargeDetail_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_ChargeDetail_Offense FOREIGN KEY(OffenseID)
      REFERENCES [Code].[OffenseCode](OffenseID),
   CONSTRAINT fk_ChargeDetail_Jurisdiction FOREIGN KEY(JurisdictionID)
      REFERENCES [Code].[JurisdictionCode](CodeID)
);

CREATE TABLE [Charge].[ChargeBookingAgency] (
   [ChargeBookingAgencyID] VARCHAR(20) PRIMARY KEY,
   [CaseNo] BIGINT NOT NULL,
   [ChargeNo] BIGINT NOT NULL,
   [AgencyNo] BIGINT,
   [ArrestTime] VARCHAR(20),
   [OfficerID] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_ChargeBookingAgency_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_ChargeBookingAgency_Charge FOREIGN KEY(ChargeNo)
      REFERENCES [Charge].[ChargeDetail](ChargeNo),
   CONSTRAINT fk_ChargeBookingAgency_Agency FOREIGN KEY(AgencyNo)
      REFERENCES [Entity].[Organization](OrganizationNo),
   CONSTRAINT fk_ChargeBookingAgency_Officer FOREIGN KEY(OfficerID)
      REFERENCES [Entity].[Officer](OfficerID)
);

CREATE TABLE [Charge].[ChargeReportingAgency] (
   [ChargeReportingAgencyID] VARCHAR(20) PRIMARY KEY,
   [CaseNo] BIGINT,
   [ChargeNo] BIGINT,
   [AgencyNo] BIGINT,
   [ControlNumber] VARCHAR(20),
   [OfficerID] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_ChargeReportingAgency_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_ChargeReportingAgency_Charge FOREIGN KEY(ChargeNo)
      REFERENCES [Charge].[ChargeDetail](ChargeNo),
   CONSTRAINT fk_ChargeReportingAgency_Agency FOREIGN KEY(AgencyNo)
      REFERENCES [Entity].[Organization](OrganizationNo),
   CONSTRAINT fk_ChargeReportingAgency_Officer FOREIGN KEY(OfficerID)
      REFERENCES [Entity].[Officer](OfficerID)
);

CREATE TABLE [Charge].[ChargeAmendedReasonCode] (
   [CodeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Charge].[ChargeModifierCode] (
   [CodeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Charge].[ChargeModifier] (
   [ChargeModifierID] VARCHAR(20) PRIMARY KEY,
   [CaseNo] BIGINT,
   [ChargeNo] BIGINT,
   [ModifierID] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_ChargeModifier_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_ChargeModifier_Charge FOREIGN KEY(ChargeNo)
      REFERENCES [Charge].[ChargeDetail](ChargeNo),
   CONSTRAINT fk_ChargeModifier_Modifier FOREIGN KEY(ModifierID)
      REFERENCES [Charge].[ChargeModifierCode](CodeID)
);

CREATE TABLE [Charge].[PleaType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Charge].[PleaEvent] (
   [PleaEventNo] BIGINT IDENTITY NOT NULL,
   [PleaEventID] VARCHAR(20),
   [CaseNo] BIGINT NOT NULL,
   [PleaID] VARCHAR(20),
   [EventDate] DATE,
   [ChargeNo] BIGINT,
   [PleaType] VARCHAR(256),
   [AmendReasonCodeID] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_PleaEvent_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_PleaEvent_Charge FOREIGN KEY(ChargeNo)
      REFERENCES [Charge].[ChargeDetail](ChargeNo)
);

CREATE TABLE [Charge].[DispositionType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),
   
   [LegacyID] VARCHAR(20),
   
   [IsRetired] BIT,
   [IsDismissed] BIT,
   [IsGuilty] BIT,
   [IsAcquitted] BIT,
   [IsNolleProsequi] BIT,
   [IsMistrial] BIT,
   [IsDisposed] BIT,
   [IsPlea] BIT,
   
   [IsFinalDisposition] BIT,
   [DispositionText] VARCHAR(20),
   [RetiredDate] DATE,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Charge].[AmendReasonCode] (
   [CodeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Charge].[DispositionEvent] (
   [DispositionEventNo] BIGINT IDENTITY PRIMARY KEY,
   [DispositionEventID] VARCHAR(20) NOT NULL,
   [CaseNo] BIGINT NOT NULL,
   [ChargeNo] BIGINT NOT NULL,
   [DispositionTypeID] VARCHAR(20),
   [EventNo] BIGINT,
   [EventDate] DATE,
   [AmendReasonCodeID] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_DispositionEvent_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_DispositionEvent_Charge FOREIGN KEY(ChargeNo)
      REFERENCES [Charge].[ChargeDetail](ChargeNo),
   CONSTRAINT fk_DispositionEvent_Type FOREIGN KEY(DispositionTypeID)
      REFERENCES [Charge].[DispositionType](TypeID),
   CONSTRAINT fk_DispositionEvent_AmedReason FOREIGN KEY(AmendReasonCodeID)
      REFERENCES [Charge].[AmendReasonCode](CodeID),
   CONSTRAINT fk_DispositionEvent_Event FOREIGN KEY(EventNo)
      REFERENCES [Case].[Event](EventNo)
);

-- -----------------------------------------------------------------------------
-- Sentence

CREATE TABLE [Sentence].[SentenceType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

-- DROP TABLE [Sentence].[SentenceEvent]
CREATE TABLE [Sentence].[SentenceEvent] (
   [SentenceEventNo] BIGINT IDENTITY PRIMARY KEY,
   [CaseNo] BIGINT NOT NULL,
   [SentenceEventID] VARCHAR(20),
   [AuthorityNo] BIGINT,
   [SentenceTypeID] VARCHAR(20),
   [SentenceDate] DATE,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_SentenceEvent_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_SentenceEvent_Type FOREIGN KEY(SentenceTypeID)
      REFERENCES [Sentence].[SentenceType](TypeID),
   CONSTRAINT fk_SentenceEvent_Authority FOREIGN KEY(AuthorityNo)
      REFERENCES [Entity].[Organization](OrganizationNo)
);

CREATE TABLE [Sentence].[SentenceDetail] (
   [SentenceDetailNo] BIGINT IDENTITY PRIMARY KEY,
   [CaseNo] BIGINT NOT NULL,
   [SentenceEventID] VARCHAR(20),
   [SentenceID] VARCHAR(20),
   [ChargeNo] BIGINT,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_SentenceDetail_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_SentenceDetail_Charge FOREIGN KEY(ChargeNo)
      REFERENCES [Charge].[ChargeDetail](ChargeNo)
);

CREATE TABLE [Sentence].[ConfinementType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Sentence].[SuspendedReasonCode] (
   [CodeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Sentence].[ConfinementComponent] (
   [ConfinementComponentNo] BIGINT IDENTITY PRIMARY KEY,
   [SentenceEventID] VARCHAR(20),
   [ChargeNo] BIGINT NOT NULL,
   [SentenceNo] BIGINT,
   [ComponentID] VARCHAR(20),
   [TypeID] VARCHAR(20),
   [FacilityNo] BIGINT,
   [StartDate] DATE,
   [ImpositionOfSentenceSuspended] VARCHAR(20),
   [SentencingStayed] VARCHAR(20),
   [SuspendedYears] VARCHAR(20),
   [SuspendedMonts] VARCHAR(20),
   [SuspendedDays] VARCHAR(20),
   [SuspendedWeeks] VARCHAR(20),
   [SuspendedReasonCodeID] VARCHAR(20),
   [Term] VARCHAR(20),
   [TermTypeCodeID] VARCHAR(20),
   [YearsToLife] VARCHAR(20),
   [Life] VARCHAR(20),
   [Death] VARCHAR(20),
   [UntilAge18] VARCHAR(20),
   [UntilAge21] VARCHAR(20),
   [UntilAge25] VARCHAR(20),
   [CreditForTimeServedYears] VARCHAR(20),
   [CreditForTimeServedMonths] VARCHAR(20),
   [CreditForTimeServedDays] VARCHAR(20),
   [CreditForTimeServedWeeks] VARCHAR(20),
   [ConductCreditTypeCode] VARCHAR(20),
   [ConductCreditYears] VARCHAR(20),
   [ConductCreditMonths] VARCHAR(20),
   [ConductCreditDays] VARCHAR(20),
   [ConductCreditWeeks] VARCHAR(20),
   [TreatmentCreditYears] VARCHAR(20),
   [TreatmentCreditMonths] VARCHAR(20),
   [TreatmentCreditDays] VARCHAR(20),
   [TreatmentCreditWeeks] VARCHAR(20),
   [AdditionalCreditTypeDescription] VARCHAR(20),
   [AdditionalCreditYears] VARCHAR(20),
   [AdditionalCreditMonths] VARCHAR(20),
   [AdditionalCreditDays] VARCHAR(20),
   [AdditionalCreditWeeks] VARCHAR(20),
   [PostSentenceCreditYears] VARCHAR(20),
   [PostSentenceCreditMonths] VARCHAR(20),
   [PostSentenceCreditDays] VARCHAR(20),
   [PostSentenceCreditWeeks] VARCHAR(20),
   [MandatorySupervisedReleaseCreditYears] VARCHAR(20),
   [MandatorySupervisedReleaseCreditMonths] VARCHAR(20),
   [MandatorySupervisedReleaseCreditDays] VARCHAR(20),
   [MandatorySupervisedReleaseCreditWeeks] VARCHAR(20),
   [PayFineInLieuOfJailTime] VARCHAR(20),
   [FineAmount] MONEY,
   [FineSuspendedAmount] MONEY,
   [PayFine] VARCHAR(20),
   [PaySuspendedFine] VARCHAR(20),
   [StayedReason] VARCHAR(20),
   [LifeParoleEligible] VARCHAR(20),
   [YearsToLifeParoleEligible] VARCHAR(20),
   [FineLengthInLieuOfYears] VARCHAR(20),
   [FineLengthInLieuOfMonths] VARCHAR(20),
   [FineLengthInLieuOfDays] VARCHAR(20),
   [FineLengthInLieuOfWeeks] VARCHAR(20),
   [FineSuspendedYears] VARCHAR(20),
   [FineSuspendedMonths] VARCHAR(20),
   [FineSuspendedDays] VARCHAR(20),
   [FineSuspendedWeeks] VARCHAR(20),
   [TotalCreditSuspendedYears] VARCHAR(20),
   [TotalCreditSuspendedMonths] VARCHAR(20),
   [TotalCreditSuspendedDays] VARCHAR(20),
   [TotalCreditSuspendedWeeks] VARCHAR(20),
   [TermYearsToLife] VARCHAR(20),
   [AdultConfinementComments] VARCHAR(20),
   [InLieuOfComments] VARCHAR(256),
   [FineSuspendedAmountInLieuOf] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_ConfinementComponent_Charge FOREIGN KEY(ChargeNo)
      REFERENCES [Charge].[ChargeDetail](ChargeNo),
   CONSTRAINT fk_ConfinementComponent_Sentence FOREIGN KEY(SentenceNo)
      REFERENCES [Sentence].[SentenceDetail](SentenceDetailNo),
   CONSTRAINT fk_ConfinementComponent_Type FOREIGN KEY(TypeID)
      REFERENCES [Sentence].[ConfinementType](TypeID),
   CONSTRAINT fk_ConfinementComponent_SuspendedReasonCodeDim FOREIGN KEY(SuspendedReasonCodeID)
      REFERENCES [Sentence].[SuspendedReasonCode](CodeID),
   CONSTRAINT fk_ConfinementComponent_Organization FOREIGN KEY(FacilityNo)
      REFERENCES [Entity].[Organization](OrganizationNo)
);

CREATE TABLE [Sentence].[ProbationConditionType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Sentence].[ProbationComponent] (
   [ProbationComponentNo] BIGINT IDENTITY PRIMARY KEY,
   [ChargeNo] BIGINT,
   [SentenceNo] BIGINT,
   [ComponentID] VARCHAR(20),
   [TypeID] VARCHAR(20),
   [StartDate] DATE,
   [EndDate] DATE,
   [TermYears] VARCHAR(20),
   [TermMonths] VARCHAR(20),
   [TermDays] VARCHAR(20),
   [TermWeeks] VARCHAR(20),
   [Manual] VARCHAR(20),
   [ImpositionOfSentenceSuspended] BIT,
   [Comment] VARCHAR(256),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_ProbationComponent_Charge FOREIGN KEY(ChargeNo)
      REFERENCES [Charge].[ChargeDetail](ChargeNo),
   CONSTRAINT fk_ProbationComponent_Sentence FOREIGN KEY(SentenceNo)
      REFERENCES [Sentence].[SentenceDetail](SentenceDetailNo)
);

CREATE TABLE [Sentence].[ProbationCondition] (
   [ProbationConditionNo] BIGINT IDENTITY PRIMARY KEY,
   [ProbationComponentNo] BIGINT NOT NULL,
   [ChargeNo] BIGINT NOT NULL,
   [SentenceNo] BIGINT NOT NULL,
   [ConditionID] VARCHAR(20),
   [ComponentID] VARCHAR(20) NOT NULL,
   [TypeID] VARCHAR(20),
   [EffectiveDate] DATE,
   [StatusCodeID] VARCHAR(20),
   [StatusDate] DATE,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_ProbationCondition_Charge FOREIGN KEY(ChargeNO)
      REFERENCES [Charge].[ChargeDetail](ChargeNo),
   CONSTRAINT fk_ProbationCondition_Sentence FOREIGN KEY(SentenceNo)
      REFERENCES [Sentence].[SentenceDetail](SentenceDetailNo),
   CONSTRAINT fk_ProbationCondition_Type FOREIGN KEY(TypeID)
      REFERENCES [Sentence].[ProbationConditionType](TypeID),
   CONSTRAINT fk_ProbationCondition_Component FOREIGN KEY(ProbationComponentNo)
      REFERENCES [Sentence].[ProbationComponent](ProbationComponentNo)
);

CREATE TABLE [Sentence].[ProbationStatus] (
   [ProbationStatusNo] BIGINT IDENTITY PRIMARY KEY,
   [ProbationStatusID] VARCHAR(20),
   [ChargeNo] BIGINT NOT NULL,
   [SentenceNo] BIGINT NOT NULL,
   [ConditionNo] BIGINT NOT NULL,
   [ComponentNo] BIGINT NOT NULL,
   [StatusCodeID] VARCHAR(20),
   [StatusDate] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_ProbationStatus_Charge FOREIGN KEY(ChargeNo)
      REFERENCES [Charge].[ChargeDetail](ChargeNo),
   CONSTRAINT fk_ProbationStatus_Sentence FOREIGN KEY(SentenceNo)
      REFERENCES [Sentence].[SentenceDetail](SentenceDetailNo),
   CONSTRAINT fk_ProbationStatus_Condition FOREIGN KEY(ConditionNo)
      REFERENCES [Sentence].[ProbationCondition](ProbationConditionNo),
   CONSTRAINT fk_ProbationStatus_Component FOREIGN KEY(ComponentNo)
      REFERENCES [Sentence].[ProbationComponent](ProbationComponentNo)
);

CREATE TABLE [Sentence].[DriverLicenseConditionType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Sentence].[DriverLicenseCondition] (
   [DriverLicenseConditionNo] BIGINT IDENTITY PRIMARY KEY,
   [ChargeNo] BIGINT NOT NULL,
   [SentenceNo] BIGINT NOT NULL,
   [ConditionID] VARCHAR(20),
   [TypeID] VARCHAR(20) NOT NULL,
   [SuspensionTerm] VARCHAR(20),
   [RevocationTerm] VARCHAR(20),
   [Delay] VARCHAR(20),
   [TermYears] VARCHAR(20),
   [TermMonths] VARCHAR(20),
   [TermDays] VARCHAR(20),
   [TermWeeks] VARCHAR(20),
   [PursuantToVehicleCodeID] VARCHAR(20),
   [HabitualOffender] BIT,
   [BACDrugs] VARCHAR(20),
   [BACLevel] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_DriverLicenseCondition_Charge FOREIGN KEY(ChargeNo)
      REFERENCES [Charge].[ChargeDetail](ChargeNo),
   CONSTRAINT fk_DriverLicenseCondition_Sentence FOREIGN KEY(SentenceNo)
      REFERENCES [Sentence].[SentenceDetail](SentenceDetailNo),
   CONSTRAINT fk_DriverLicenseCondition_Type FOREIGN KEY(TypeID)
      REFERENCES [Sentence].[DriverLicenseConditionType](TypeID)
);

CREATE TABLE [Sentence].[DriverLicenseRestrictionType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Sentence].[DriverLicenseRestriction] (
   [DriverLicenseRestrictionNo] BIGINT IDENTITY PRIMARY KEY,
   [ChargeNo] BIGINT NOT NULL,
   [SentenceNo] BIGINT NOT NULL,
   [ConditionNo] BIGINT NOT NULL,
   [RestrictionID] VARCHAR(20),
   [TypeID] VARCHAR(20),
   [StartDate] DATE,
   [TermYears] VARCHAR(20),
   [TermMonths] VARCHAR(20),
   [TermDays] VARCHAR(20),
   [TermWeeks] VARCHAR(20),
   [Comment] VARCHAR(256),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_DriverLicenseRestriction_Charge FOREIGN KEY(ChargeNo)
      REFERENCES [Charge].[ChargeDetail](ChargeNo),
   CONSTRAINT fk_DriverLicenseRestriction_Sentence FOREIGN KEY(SentenceNo)
      REFERENCES [Sentence].[SentenceDetail](SentenceDetailNo),
   CONSTRAINT fk_DriverLicenseRestriction_Type FOREIGN KEY(TypeID)
      REFERENCES [Sentence].[DriverLicenseRestrictionType](TypeID),
   CONSTRAINT fk_DriverLicenseRestriction_Condition FOREIGN KEY(ConditionNo)
      REFERENCES [Sentence].[DriverLicenseCondition](DriverLicenseConditionNo),
);

CREATE TABLE [Sentence].[ProgramType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Sentence].[ProgramComponent] (
   [ProgramComponentNo] BIGINT IDENTITY PRIMARY KEY,
   [ChargeNo] BIGINT NOT NULL,
   [SentenceNo] BIGINT NOT NULL,
   [ComponentID] VARCHAR(20),
   [TypeID] VARCHAR(20) NOT NULL,
   [Ordered] BIT,
   [Recommended] BIT,
   [OrderDate] DATE,
   [CompleteBy] DATE,
   [InLieuOf] VARCHAR(20),
   [TermYears] VARCHAR(20),
   [TermMonths] VARCHAR(20),
   [TermDays] VARCHAR(20),
   [TermWeeks] VARCHAR(20),
   [Comment] VARCHAR(256),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_ProgramComponent_Charge FOREIGN KEY(ChargeNo)
      REFERENCES [Charge].[ChargeDetail](ChargeNo),
   CONSTRAINT fk_ProgramComponent_Sentence FOREIGN KEY(SentenceNo)
      REFERENCES [Sentence].[SentenceDetail](SentenceDetailNo),
   CONSTRAINT fk_ProgramComponent_Type FOREIGN KEY(TypeID)
      REFERENCES [Sentence].[ProgramType](TypeID)
);

CREATE TABLE [Sentence].[ConditionType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Sentence].[Condition] (
   [ConditionNo] BIGINT IDENTITY PRIMARY KEY,
   [ConditionID] VARCHAR(20),
   [ChargeNo] BIGINT,
   [SentenceNo] BIGINT,
   [DispComponentConditionId] VARCHAR(20),
   [TypeID] VARCHAR(20),
   [Description] VARCHAR(20),
   [EffectiveDate] DATE,
   [Amount] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_Condition_Charge FOREIGN KEY(ChargeNo)
      REFERENCES [Charge].[ChargeDetail](ChargeNo),
   CONSTRAINT fk_Condition_Sentence FOREIGN KEY(SentenceNo)
      REFERENCES [Sentence].[SentenceDetail](SentenceDetailNo),
   CONSTRAINT fk_Condition_Type FOREIGN KEY(TypeID)
      REFERENCES [Sentence].[ProgramType](TypeID)
);

CREATE TABLE [Sentence].[ConcurrentConsecutive] (
   [ConcurrentConsecutiveNo] BIGINT IDENTITY PRIMARY KEY,
   [ChargeNo] BIGINT,
   [SentenceNo] BIGINT,
   [RelatedSentenceNo] BIGINT,
   [ConcurrentOrConsecutive] VARCHAR(20),
   [ToWhatOtherSentenceID] VARCHAR(20),
   [Description] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_ConcurrentConsecutive_Charge FOREIGN KEY(ChargeNo)
      REFERENCES [Charge].[ChargeDetail](ChargeNo),
   CONSTRAINT fk_ConcurrentConsecutive_Sentence FOREIGN KEY(SentenceNo)
      REFERENCES [Sentence].[SentenceDetail](SentenceDetailNo),
   CONSTRAINT fk_ConcurrentConsecutive_RelatedSentence FOREIGN KEY(RelatedSentenceNo)
      REFERENCES [Sentence].[SentenceDetail](SentenceDetailNo)
);

-- -----------------------------------------------------------------------------
-- Warrant

CREATE TABLE [Warrant].[WarrantDetail] (
   [WarrantNo] BIGINT IDENTITY PRIMARY KEY,
   [WarrantID] VARCHAR(20),
   [WarrantNumber] VARCHAR(20),
   [CaseNo] BIGINT NOT NULL,
   [CaseNumber] VARCHAR(20),
   [BookingNumber] VARCHAR(20),
   [BailSettingType] VARCHAR(20),
   [BailAmount] MONEY,
   [ExpirationDate] DATE,
   [PartyNo] BIGINT,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_WarrantDetail_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_WarrantDetail_Party FOREIGN KEY(PartyNo)
      REFERENCES [Party].[PartyDetail](PartyNo)
);

CREATE TABLE [Warrant].[WarrantStatusCode] (
   [CodeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Warrant].[WarrantStatus] (
   [WarrantStatusNo] BIGINT IDENTITY PRIMARY KEY,
   [WarrantNo] BIGINT NOT NULL,
   [WarrantStatusID] VARCHAR(20),
   [CodeID] VARCHAR(20),
   [StatusDate] DATE,
   [Comment] VARCHAR(20),
   [WarrantLocation] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_WarrantStatus_Warrant FOREIGN KEY(WarrantNo)
      REFERENCES [Warrant].[WarrantDetail](WarrantNo),
   CONSTRAINT fk_WarrantStatus_Code FOREIGN KEY(CodeID)
      REFERENCES [Warrant].[WarrantStatusCode](CodeID)
);

CREATE TABLE [Warrant].[WarrantConditionCode] (
   [CodeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Warrant].[WarrantCondition] (
   [WarrantConditionNo] BIGINT IDENTITY PRIMARY KEY,
   [WarrantNo] BIGINT,
   [CodeID] VARCHAR(20),
   [Comment] VARCHAR(256),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_WarrantCondition_Warrant FOREIGN KEY(WarrantNo)
      REFERENCES [Warrant].[WarrantDetail](WarrantNo),
   CONSTRAINT fk_WarrantCondition_Code FOREIGN KEY(CodeID)
      REFERENCES [Warrant].[WarrantConditionCode](CodeID)
);

-- -----------------------------------------------------------------------------
-- Citation Event

CREATE TABLE [Citation].[CitationType] (
   [TypeID] VARCHAR(20) PRIMARY KEY,
   [Description] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate()
);

CREATE TABLE [Citation].[CitationEvent] (
   CitationEventNo BIGINT IDENTITY PRIMARY KEY,
   CitationLegacyNo BIGINT NOT NULL,
   CitationID VARCHAR(40),
   TypeID VARCHAR(20),
   EventNo BIGINT,
   EventDate DATE,
   EventTime VARCHAR(20),
   CourtNo BIGINT,
   CaseNo BIGINT,
   PartyNo BIGINT,
   OfficerID VARCHAR(20),

   -- Apperance Details
   -- Incident Details
   -- Officer Details
   -- Vehicle Details
   -- Vehicle Driver Details
   -- Vehicle Owner Details
   -- Employer Details

   CONSTRAINT fk_CitationEvent_Case FOREIGN KEY(CaseNo)
      REFERENCES [Case].[CaseDetail](CaseNo),
   CONSTRAINT fk_CitationEvent_Type FOREIGN KEY(TypeID)
      REFERENCES [Citation].[CitationType](TypeID),
   CONSTRAINT fk_CitationEvent_Court FOREIGN KEY(CourtNo)
      REFERENCES [Entity].[Organization](OrganizationNo),
   CONSTRAINT fk_CitationEvent_Party FOREIGN KEY(PartyNo)
      REFERENCES [Party].[PartyDetail](PartyNo),
   CONSTRAINT fk_CitationEvent_Officer FOREIGN KEY(OfficerID)
      REFERENCES [Entity].[Officer](OfficerID),
   CONSTRAINT fk_CitationEvent_Event FOREIGN KEY(EventNo)
      REFERENCES [Case].[Event](EventNo)
);
