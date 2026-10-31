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

CREATE SCHEMA [NODS]
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
   [OrganizationID] VARCHAR(20) PRIMARY KEY,
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

   CONSTRAINT fk_Organization_Type FOREIGN KEY (TypeID)
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
   [DivisionID] VARCHAR(20) PRIMARY KEY,
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
   [LocationID] VARCHAR(20) PRIMARY KEY,
   [TypeID] VARCHAR(20),
   [AddressLine1] VARCHAR(128),
   [AddressLine2] VARCHAR(128),
   [CityCodeID] VARCHAR(20),
   [StateCodeID] VARCHAR(20),
   [PostalCodeID] VARCHAR(20),
   [DivisionID] VARCHAR(20),

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
   CONSTRAINT fk_Location_Division FOREIGN KEY (DivisionID)
      REFERENCES [Entity].[LocationDivision](DivisionID)
);

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
   [CaseID] VARCHAR(20) PRIMARY KEY,
   [CaseNumber] VARCHAR(20) NOT NULL,
   [TypeID] VARCHAR(20),
   [CaseTitle] VARCHAR(128),
   [FiledDate] DATE,
   [JudgeID] VARCHAR(20),
   [CourtID] VARCHAR(20),

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
   CONSTRAINT fk_CaseDetail_Court FOREIGN KEY(CourtID)
      REFERENCES [Entity].[Organization](OrganizationID),
   CONSTRAINT fk_CaseDetail_Officer FOREIGN KEY(JudgeID)
      REFERENCES [Entity].[Officer](OfficerID)
);

CREATE TABLE [Case].[CaseFlag] (
   [CaseID] VARCHAR(20) NOT NULL,
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

   CONSTRAINT fk_CaseFlag_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID)
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
   [CaseID] VARCHAR(20) PRIMARY KEY,
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

   CONSTRAINT fk_CaseStatus_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_CaseStatus_StatusCode FOREIGN KEY (CodeID)
      REFERENCES [Case].[StatusCode](CodeID)
);

CREATE TABLE [Case].[CaseFiling] (
   [CaseID] VARCHAR(20) NOT NULL,
   [AuthorityID] VARCHAR(20),
   [CaseFilingID] VARCHAR(20) PRIMARY KEY,
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

   CONSTRAINT fk_CaseFilling_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_CaseFilling_Authority FOREIGN KEY(AuthorityID)
      REFERENCES [Entity].[Organization]([OrganizationID])
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
   [CaseID] VARCHAR(20) NOT NULL,
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

   CONSTRAINT fk_CaseCrossReference_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail]([CaseID]),
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
   [CaseID] VARCHAR(20) NOT NULL,
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

   CONSTRAINT fk_RelatedFilling_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
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
   [CaseID] VARCHAR(20) NOT NULL,
   [EventID] VARCHAR(20) PRIMARY KEY,
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

   CONSTRAINT fk_CaseEvent_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
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
   [CaseID] VARCHAR(20) NOT NULL,
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

   CONSTRAINT fk_BailBondRevocation_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID)
);

CREATE TABLE [Activity].[BailBondRelease] (
   [CaseID] VARCHAR(20) NOT NULL,
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

   CONSTRAINT fk_BailBondRelease_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
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
   [CaseID] VARCHAR(20),
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

   CONSTRAINT fk_ReleaseCondition_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
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
   [CaseID] VARCHAR(20) NOT NULL,
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

   CONSTRAINT fk_FinancialEvent_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
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
   [CaseID] VARCHAR(20) NOT NULL,
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

   CONSTRAINT fk_AppealEvent_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
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
   [CaseID] VARCHAR(20) NOT NULL,
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
   [DepartmentID] VARCHAR(20),
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

   CONSTRAINT fk_Hearing_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_Hearing_Flag FOREIGN KEY(HearingFlagID)
      REFERENCES [Activity].[HearingFlag](FlagID),
   CONSTRAINT fk_Hearing_Type FOREIGN KEY(TypeID)
      REFERENCES [Activity].[HearingType](TypeID),
   CONSTRAINT fk_Hearing_CaseType FOREIGN KEY(CaseTypeID)
      REFERENCES [Case].[CaseType](TypeID),
   CONSTRAINT fk_Hearing_ResultCode FOREIGN KEY(ResultCodeID)
      REFERENCES [Activity].[HearingResultCode](CodeID),
   CONSTRAINT fk_Hearing_Department FOREIGN KEY(DepartmentID)
      REFERENCES [Entity].[Organization](OrganizationID),
   CONSTRAINT fk_Hearing_Judge FOREIGN KEY(JudgeID)
      REFERENCES [Entity].[Officer](OfficerID)
);

CREATE TABLE [Activity].[HearingEvent] (
   [HearingEventID] VARCHAR(20) PRIMARY KEY,
   [HearingID] VARCHAR(20) NOT NULL,
   [EventID] VARCHAR(20) NOT NULL,
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
   CONSTRAINT fk_HearingEvent_Event FOREIGN KEY(EventID)
      REFERENCES [Case].[Event](EventID)
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

CREATE TABLE [Party].[PartyDetail] (
   [PartyID] VARCHAR(20) PRIMARY KEY,
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
   [PartyID] VARCHAR(20) NOT NULL,
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

   CONSTRAINT fk_PartyName_Party FOREIGN KEY(PartyID)
      REFERENCES [Party].[PartyDetail](PartyID),
   CONSTRAINT fk_PartyName_Type FOREIGN KEY(TypeID)
      REFERENCES [Party].[NameType](TypeID)
);

CREATE TABLE [Party].[PartyPhone] (
   [PartyID] VARCHAR(20) NOT NULL,
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

   CONSTRAINT fk_PartyPhone_Party FOREIGN KEY(PartyID)
      REFERENCES [Party].[PartyDetail](PartyID),
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
   [PartyID] VARCHAR(20) NOT NULL,
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

   CONSTRAINT fk_PartyDriversLicense_Party FOREIGN KEY(PartyID)
      REFERENCES [Party].[PartyDetail](PartyID),
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
   [CaseAttorneyID] VARCHAR(20) PRIMARY KEY,
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
   [CasePartyID] VARCHAR(20) PRIMARY KEY,
   [CaseID] VARCHAR(20) NOT NULL,
   [PartyID] VARCHAR(20) NOT NULL,
   [RoleCodeID] VARCHAR(20) NOT NULL,
   [AttorneyID] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_CaseParty_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_CaseParty_Party FOREIGN KEY(PartyID)
      REFERENCES [Party].[PartyDetail](PartyID),
   CONSTRAINT fk_CaseParty_Role FOREIGN KEY(RoleCodeID)
      REFERENCES [Party].[PartyRoleCode](CodeID),
   CONSTRAINT fk_CaseParty_Attorney FOREIGN KEY(AttorneyID)
      REFERENCES [Case].[CaseAttorney](CaseAttorneyID)
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
   [CaseJudgementID] VARCHAR(20) PRIMARY KEY,
   [EventID] VARCHAR(20),
   [EventDateTime] DATETIMEOFFSET,
   [CaseID] VARCHAR(20) NOT NULL,
   [TypeID] VARCHAR(20) NOT NULL,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_Judgement_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_Judgement_Event FOREIGN KEY(EventID)
      REFERENCES [Case].[Event](EventID),
   CONSTRAINT fk_Judgement_Type FOREIGN KEY(TypeID)
      REFERENCES [Case].[JudgementType](TypeID)
);

CREATE TABLE [Case].[CaseState] (
   [StateID] VARCHAR(20) PRIMARY KEY,
   [StateDate] DATE NOT NULL,
   [StateDateID] VARCHAR(20) NOT NULL,
   
   [CaseID] VARCHAR(20) NOT NULL,
   
   -- related immutable Event reference
   [EventID] VARCHAR(20),
   [EventDateTime] DATETIMEOFFSET,
   
   -- Case current state data as of StateDate
   [DispositionID] VARCHAR(20),
   [JudgementID] VARCHAR(20),
   [ChargeDispositionID] VARCHAR(20),
   
   [UnservedWarrantID] VARCHAR(20),
   [FailureToAppearWarrantID] VARCHAR(20),   -- Bench Warrant
   
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

   CONSTRAINT fk_CaseState_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_CaseState_Event FOREIGN KEY(EventID)
      REFERENCES [Case].[Event](EventID)
);

-- -----------------------------------------------------------------------------
-- Charge

CREATE TABLE [Charge].[ChargeDetail] (
   [CaseID] VARCHAR(20) NOT NULL,
   [ChargeID] VARCHAR(20) PRIMARY KEY,
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

   CONSTRAINT fk_ChargeDetail_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_ChargeDetail_Offense FOREIGN KEY(OffenseID)
      REFERENCES [Code].[OffenseCode](OffenseID),
   CONSTRAINT fk_ChargeDetail_Jurisdiction FOREIGN KEY(JurisdictionID)
      REFERENCES [Code].[JurisdictionCode](CodeID)
);

CREATE TABLE [Charge].[ChargeBookingAgency] (
   [ChargeBookingAgencyID] VARCHAR(20) PRIMARY KEY,
   [CaseID] VARCHAR(20) NOT NULL,
   [ChargeID] VARCHAR(20) NOT NULL,
   [AgencyID] VARCHAR(20),
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

   CONSTRAINT fk_ChargeBookingAgency_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_ChargeBookingAgency_Charge FOREIGN KEY(ChargeID)
      REFERENCES [Charge].[ChargeDetail](ChargeID),
   CONSTRAINT fk_ChargeBookingAgency_Agency FOREIGN KEY(AgencyID)
      REFERENCES [Entity].[Organization](OrganizationID),
   CONSTRAINT fk_ChargeBookingAgency_Officer FOREIGN KEY(OfficerID)
      REFERENCES [Entity].[Officer](OfficerID)
);

CREATE TABLE [Charge].[ChargeReportingAgency] (
   [ChargeReportingAgencyID] VARCHAR(20) PRIMARY KEY,
   [CaseID] VARCHAR(20),
   [ChargeID] VARCHAR(20),
   [AgencyID] VARCHAR(20),
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

   CONSTRAINT fk_ChargeReportingAgency_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_ChargeReportingAgency_Charge FOREIGN KEY(ChargeID)
      REFERENCES [Charge].[ChargeDetail](ChargeID),
   CONSTRAINT fk_ChargeReportingAgency_Agency FOREIGN KEY(AgencyID)
      REFERENCES [Entity].[Organization](OrganizationID),
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
   [CaseID] VARCHAR(20),
   [ChargeID] VARCHAR(20),
   [ModifierID] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_ChargeModifier_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_ChargeModifier_Charge FOREIGN KEY(ChargeID)
      REFERENCES [Charge].[ChargeDetail](ChargeID),
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
   [PleaEventID] VARCHAR(20) PRIMARY KEY,
   [CaseID] VARCHAR(20),
   [PleaID] VARCHAR(20),
   [EventDate] DATE,
   [ChargeID] VARCHAR(20),
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

   CONSTRAINT fk_PleaEvent_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID)
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
   [DispositionEventID] VARCHAR(20) PRIMARY KEY,
   [CaseID] VARCHAR(20),
   [DispositionTypeID] VARCHAR(20),
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

   CONSTRAINT fk_DispositionEvent_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_DispositionEvent_Type FOREIGN KEY(DispositionTypeID)
      REFERENCES [Charge].[DispositionType](TypeID),
   CONSTRAINT fk_DispositionEvent_AmedReason FOREIGN KEY(AmendReasonCodeID)
      REFERENCES [Charge].[AmendReasonCode](CodeID)
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

CREATE TABLE [Sentence].[SentenceEvent] (
   [CaseID] VARCHAR(20),
   [SentenceEventID] VARCHAR(20) PRIMARY KEY,
   [AuthorityID] VARCHAR(20),
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

   CONSTRAINT fk_SentenceEvent_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_SentenceEvent_Type FOREIGN KEY(SentenceTypeID)
      REFERENCES [Sentence].[SentenceType](TypeID),
   CONSTRAINT fk_SentenceEvent_Authority FOREIGN KEY(AuthorityID)
      REFERENCES [Entity].[Organization](OrganizationID)
);

CREATE TABLE [Sentence].[SentenceDetail] (
   [CaseID] VARCHAR(20),
   [SentenceEventID] VARCHAR(20),
   [SentenceID] VARCHAR(20) PRIMARY KEY,
   [ChargeID] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_SentenceDetail_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_SentenceDetail_Charge FOREIGN KEY(ChargeID)
      REFERENCES [Charge].[ChargeDetail](ChargeID)
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
   [SentenceEventID] VARCHAR(20),
   [ChargeID] VARCHAR(20),
   [SentenceID] VARCHAR(20),
   [ComponentID] VARCHAR(20) PRIMARY KEY,
   [TypeID] VARCHAR(20),
   [FacilityID] VARCHAR(20),
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

   CONSTRAINT fk_ConfinementComponent_Charge FOREIGN KEY(ChargeID)
      REFERENCES [Charge].[ChargeDetail](ChargeID),
   CONSTRAINT fk_ConfinementComponent_Sentence FOREIGN KEY(SentenceID)
      REFERENCES [Sentence].[SentenceDetail](SentenceID),
   CONSTRAINT fk_ConfinementComponent_Type FOREIGN KEY(TypeID)
      REFERENCES [Sentence].[ConfinementType](TypeID),
   CONSTRAINT fk_ConfinementComponent_SuspendedReasonCode FOREIGN KEY(SuspendedReasonCodeID)
      REFERENCES [Sentence].[SuspendedReasonCode](CodeID),
   CONSTRAINT fk_ConfinementComponent_Organization FOREIGN KEY(FacilityID)
      REFERENCES [Entity].[Organization](OrganizationID)
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
   [ChargeID] VARCHAR(20),
   [SentenceID] VARCHAR(20),
   [ComponentID] VARCHAR(20) PRIMARY KEY,
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

   CONSTRAINT fk_ProbationComponent_Charge FOREIGN KEY(ChargeID)
      REFERENCES [Charge].[ChargeDetail](ChargeID),
   CONSTRAINT fk_ProbationComponent_Sentence FOREIGN KEY(SentenceID)
      REFERENCES [Sentence].[SentenceDetail](SentenceID)
);

CREATE TABLE [Sentence].[ProbationCondition] (
   [ChargeID] VARCHAR(20) NOT NULL,
   [SentenceID] VARCHAR(20) NOT NULL,
   [ConditionID] VARCHAR(20) PRIMARY KEY,
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

   CONSTRAINT fk_ProbationCondition_Charge FOREIGN KEY(ChargeID)
      REFERENCES [Charge].[ChargeDetail](ChargeID),
   CONSTRAINT fk_ProbationCondition_Sentence FOREIGN KEY(SentenceID)
      REFERENCES [Sentence].[SentenceDetail](SentenceID),
   CONSTRAINT fk_ProbationCondition_Type FOREIGN KEY(TypeID)
      REFERENCES [Sentence].[ProbationConditionType](TypeID),
   CONSTRAINT fk_ProbationCondition_Component FOREIGN KEY(ComponentID)
      REFERENCES [Sentence].[ProbationComponent](ComponentID)
);

CREATE TABLE [Sentence].[ProbationStatus] (
   [ProbationStatusID] VARCHAR(20) PRIMARY KEY,
   [ChargeID] VARCHAR(20) NOT NULL,
   [SentenceID] VARCHAR(20) NOT NULL,
   [ConditionID] VARCHAR(20) NOT NULL,
   [ComponentID] VARCHAR(20) NOT NULL,
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

   CONSTRAINT fk_ProbationStatus_Charge FOREIGN KEY(ChargeID)
      REFERENCES [Charge].[ChargeDetail](ChargeID),
   CONSTRAINT fk_ProbationStatus_Sentence FOREIGN KEY(SentenceID)
      REFERENCES [Sentence].[SentenceDetail](SentenceID),
   CONSTRAINT fk_ProbationStatus_Condition FOREIGN KEY(ConditionID)
      REFERENCES [Sentence].[ProbationCondition](ConditionID),
   CONSTRAINT fk_ProbationStatus_Component FOREIGN KEY(ComponentID)
      REFERENCES [Sentence].[ProbationComponent](ComponentID)
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
   [ChargeID] VARCHAR(20) NOT NULL,
   [SentenceID] VARCHAR(20) NOT NULL,
   [ConditionID] VARCHAR(20) PRIMARY KEY,
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

   CONSTRAINT fk_DriverLicenseCondition_Charge FOREIGN KEY(ChargeID)
      REFERENCES [Charge].[ChargeDetail](ChargeID),
   CONSTRAINT fk_DriverLicenseCondition_Sentence FOREIGN KEY(SentenceID)
      REFERENCES [Sentence].[SentenceDetail](SentenceID),
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
   [ChargeID] VARCHAR(20) NOT NULL,
   [SentenceID] VARCHAR(20) NOT NULL,
   [ConditionID] VARCHAR(20) NOT NULL,
   [RestrictionID] VARCHAR(20) PRIMARY KEY,
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

   CONSTRAINT fk_DriverLicenseRestriction_Charge FOREIGN KEY(ChargeID)
      REFERENCES [Charge].[ChargeDetail](ChargeID),
   CONSTRAINT fk_DriverLicenseRestriction_Sentence FOREIGN KEY(SentenceID)
      REFERENCES [Sentence].[SentenceDetail](SentenceID),
   CONSTRAINT fk_DriverLicenseRestriction_Type FOREIGN KEY(TypeID)
      REFERENCES [Sentence].[DriverLicenseRestrictionType](TypeID),
   CONSTRAINT fk_DriverLicenseRestriction_Condition FOREIGN KEY(ConditionID)
      REFERENCES [Sentence].[DriverLicenseCondition](ConditionID),
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
   [ChargeID] VARCHAR(20) NOT NULL,
   [SentenceID] VARCHAR(20) NOT NULL,
   [ComponentID] VARCHAR(20) PRIMARY KEY,
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

   CONSTRAINT fk_ProgramComponent_Charge FOREIGN KEY(ChargeID)
      REFERENCES [Charge].[ChargeDetail](ChargeID),
   CONSTRAINT fk_ProgramComponent_Sentence FOREIGN KEY(SentenceID)
      REFERENCES [Sentence].[SentenceDetail](SentenceID),
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
   [SentenceEventID] VARCHAR(20),
   [ChargeID] VARCHAR(20),
   [SentenceID] VARCHAR(20),
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

   CONSTRAINT fk_Condition_Charge FOREIGN KEY(ChargeID)
      REFERENCES [Charge].[ChargeDetail](ChargeID),
   CONSTRAINT fk_Condition_Sentence FOREIGN KEY(SentenceID)
      REFERENCES [Sentence].[SentenceDetail](SentenceID),
   CONSTRAINT fk_Condition_Type FOREIGN KEY(TypeID)
      REFERENCES [Sentence].[ProgramType](TypeID)
);

CREATE TABLE [Sentence].[ConcurrentConsecutive] (
   [ChargeID] VARCHAR(20),
   [SentenceID] VARCHAR(20),
   [RelatedSentenceID] VARCHAR(20),
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

   CONSTRAINT fk_ConcurrentConsecutive_Charge FOREIGN KEY(ChargeID)
      REFERENCES [Charge].[ChargeDetail](ChargeID),
   CONSTRAINT fk_ConcurrentConsecutive_Sentence FOREIGN KEY(SentenceID)
      REFERENCES [Sentence].[SentenceDetail](SentenceID),
   CONSTRAINT fk_ConcurrentConsecutive_RelatedSentence FOREIGN KEY(RelatedSentenceID)
      REFERENCES [Sentence].[SentenceDetail](SentenceID)
);

-- -----------------------------------------------------------------------------
-- Warrant

CREATE TABLE [Warrant].[WarrantDetail] (
   [WarrantID] VARCHAR(20) PRIMARY KEY,
   [WarrantNumber] VARCHAR(20),
   [CaseID] VARCHAR(20),
   [CaseNumber] VARCHAR(20),
   [BookingNumber] VARCHAR(20),
   [BailSettingType] VARCHAR(20),
   [BailAmount] MONEY,
   [ExpirationDate] DATE,
   [PartyID] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_WarrantDetail_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_WarrantDetail_Party FOREIGN KEY(PartyID)
      REFERENCES [Party].[PartyDetail](PartyID)
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
   [WarrantID] VARCHAR(20) NOT NULL,
   [WarrantStatusID] VARCHAR(20) PRIMARY KEY,
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

   CONSTRAINT fk_WarrantStatus_Warrant FOREIGN KEY(WarrantID)
      REFERENCES [Warrant].[WarrantDetail](WarrantID),
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
   [WarrantID] VARCHAR(20),
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

   CONSTRAINT fk_WarrantCondition_Warrant FOREIGN KEY(WarrantID)
      REFERENCES [Warrant].[WarrantDetail](WarrantID),
   CONSTRAINT fk_WarrantCondition_Code FOREIGN KEY(CodeID)
      REFERENCES [Warrant].[WarrantConditionCode](CodeID)
);

-- -----------------------------------------------------------------------------
-- NODS

CREATE TABLE [NODS].[EventParticipant] (
   [EventParticipantID] VARCHAR(20) PRIMARY KEY,
   [EventID] VARCHAR(20) NOT NULL,
   [ParticipantType] VARCHAR(256) NOT NULL,
   [InterpreterLanguage] VARCHAR(20),
   [PartyID] VARCHAR(20) NOT NULL,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_EventParticipant_Event FOREIGN KEY(EventID)
      REFERENCES [Case].[Event](EventID),
   CONSTRAINT fk_EventParticipant_Party FOREIGN KEY(PartyID)
      REFERENCES [Party].[PartyDetail](PartyID)
);

CREATE TABLE [NODS].[MannerOfDispositionType] (
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

CREATE TABLE [NODS].[DispositionCategoryCode] (
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

CREATE TABLE [NODS].[JudgementType] (
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

CREATE TABLE [NODS].[CaseDispositionEvent] (
   [CaseID] VARCHAR(20) NOT NULL,
   [CaseDispositionEventID] VARCHAR(20) PRIMARY KEY,
   [EventDate] DATE,
   [EventTime] VARCHAR(20),
   [AuthorityID] VARCHAR(20),
   [OverallOutcome] VARCHAR(20),
   [MannerOfDispositionTypeID] VARCHAR(20),
   [IsPreAdjudicationDiversion] BIT,
   [PreAdjudicationDiversionProgramID] VARCHAR(20),
   [PostAdjudicationDiversionProgramID] VARCHAR(20),
   [DispositionTypeID] VARCHAR(20),
   [PleaTypeID] VARCHAR(20),
   [IsDirectedVerdic] BIT,
   [CaseClosedDate] DATE,
   [PrevailingPartyID] VARCHAR(20),
   [DispositionCategoryCodeID] VARCHAR(20),
   [JudgementTypeID] VARCHAR(20),
   [TotalMonetaryDamages] MONEY,
   [TotalCosts] MONEY,
   [HasNonMonetaryRelief] BIT,
   [DispositionDetail] VARCHAR(256),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_CaseDispositionEvent_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_CaseDispositionEvent_Authority FOREIGN KEY(AuthorityID)
      REFERENCES [Entity].[Organization](OrganizationID),
   CONSTRAINT fk_CaseDispositionEvent_Disposition FOREIGN KEY(MannerOfDispositionTypeID)
      REFERENCES [NODS].[MannerOfDispositionType](TypeID),
   CONSTRAINT fk_CaseDispositionEvent_Plea FOREIGN KEY(PleaTypeID)
      REFERENCES [Charge].[PleaType](TypeID),
   CONSTRAINT fk_CaseDispositionEvent_Party FOREIGN KEY(PrevailingPartyID)
      REFERENCES [Party].[PartyDetail](PartyID),
   CONSTRAINT fk_CaseDispositionEvent_Judgement FOREIGN KEY(JudgementTypeID)
      REFERENCES [NODS].[JudgementType](TypeID)
);

CREATE TABLE [NODS].[OutcomeType] (
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

CREATE TABLE [NODS].[CaseEventOutcome] (
   [CaseEventOutcomeID] VARCHAR(20) PRIMARY KEY,
   [CaseID] VARCHAR(20),
   [EventID] VARCHAR(20),
   [TypeID] VARCHAR(20),
   [Reason] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_CaseEventOutcome_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_CaseEventOutcome_Event FOREIGN KEY(EventID)
      REFERENCES [Case].[Event](EventID),
   CONSTRAINT fk_CaseEventOutcome_Party FOREIGN KEY(TypeID)
      REFERENCES [NODS].[OutcomeType](TypeID)
);

CREATE TABLE [NODS].[CaseFillingAppliesToParty] (
   [CaseID] VARCHAR(20) NOT NULL,
   [PartyID] VARCHAR(20) NOT NULL,
   [CaseFillingAppliesToPartyID] VARCHAR(20) PRIMARY KEY,
   [CaseFillingPartyID] VARCHAR(20),
   [CaseFilingID] VARCHAR(20),
   [FillingCaseID] VARCHAR(20),
   [PartyCaseID] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_CaseFillingAppliesToParty_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID)
);

CREATE TABLE [NODS].[ReferralType] (
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

CREATE TABLE [NODS].[FacilitationType] (
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

CREATE TABLE [NODS].[ResolutionType] (
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

CREATE TABLE [NODS].[DisputeResolution] (
   [CaseID] VARCHAR(20) NOT NULL,
   [DisputeResolutionID] VARCHAR(20) NOT NULL,
   [ReferralDate] DATE,
   [ReferralTypeID] VARCHAR(20),
   [IsCourtOrdereed] BIT,
   [IsNeutralInvolved] BIT,
   [ReportDate] DATE,
   [ReferralToTrial] BIT,
   [FacilitationTypeID] VARCHAR(20),
   [ResolutionTypeID] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_DisputeResolution_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_DisputeResolution_Referral FOREIGN KEY(ReferralTypeID)
      REFERENCES [NODS].[ReferralType](TypeID),
   CONSTRAINT fk_CaseEventOutcome_Facilitation FOREIGN KEY(FacilitationTypeID)
      REFERENCES [NODS].[FacilitationType](TypeID),
   CONSTRAINT fk_CaseEventOutcome_Resolution FOREIGN KEY(ResolutionTypeID)
      REFERENCES [NODS].[ResolutionType](TypeID)
);

CREATE TABLE [NODS].[ReferalSourceType] (
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

CREATE TABLE [NODS].[IntakeDecisionType] (
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

CREATE TABLE [NODS].[Intake] (
   [CaseID] VARCHAR(20),
   [IntakeID] VARCHAR(20) PRIMARY KEY,
   [ReferalDate] DATE,
   [ReferalSourceTypeID] VARCHAR(20),
   [ReferralPersonAgencyID] VARCHAR(20),
   [IntakeDecisionDate] DATE,
   [IntakeDecisionTypeID] VARCHAR(20),
   [LivingArragementAtIntake] BIT,
   [PostalCodeIntake] VARCHAR(20),
   [WasDetained] BIT,
   [IsDiversionEligible] BIT,
   [WasAssessedAtIntake] BIT,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_Intake_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_Intake_Referral FOREIGN KEY(ReferalSourceTypeID)
      REFERENCES [NODS].[ReferalSourceType](TypeID),
   CONSTRAINT fk_Intake_IntakeDecision_Type FOREIGN KEY(IntakeDecisionTypeID)
      REFERENCES [NODS].[IntakeDecisionType](TypeID),
   CONSTRAINT fk_Intake_IntakeDecision_Agency FOREIGN KEY(ReferalSourceTypeID)
      REFERENCES [Entity].[Organization](OrganizationID)
);

CREATE TABLE [NODS].[ServiceType] (
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

CREATE TABLE [NODS].[TypeOfService] (
   [CaseID] VARCHAR(20) NOT NULL,
   [PersonID] VARCHAR(20) NOT NULL,
   [ServiceID] VARCHAR(20) PRIMARY KEY,
   [ServiceTypeID] VARCHAR(20),
   [ParticipantCourtCaseID] VARCHAR(20),
   [ParticipantID] VARCHAR(20),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_TypeOfService_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_TypeOfService_Person FOREIGN KEY(PersonID)
      REFERENCES [Party].[PartyDetail](PartyID),
   CONSTRAINT fk_TypeOfService_Service_Type FOREIGN KEY(ServiceTypeID)
      REFERENCES [NODS].[ServiceType](TypeID),
   CONSTRAINT fk_TypeOfService_CourtCase FOREIGN KEY(ParticipantCourtCaseID)
      REFERENCES [Case].[CaseDetail](CaseID)
);

CREATE TABLE [NODS].[RiskAssessment] (
   [CaseID] VARCHAR(20) NOT NULL,
   [RiskAssessmentID] VARCHAR(20) PRIMARY KEY,
   [NameInstrument] VARCHAR(128),
   [Score] DECIMAL(12,2),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_RiskAssessment_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID)
);

CREATE TABLE [NODS].[ViolationType] (
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

CREATE TABLE [NODS].[SentenceViolation] (
   [SentenceID] VARCHAR(20) NOT NULL,
   [SentenceViolationID] VARCHAR(20) PRIMARY KEY,
   [ViolationTypeID] VARCHAR(20),
   [ViolationOutcome] VARCHAR(128),

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_SentenceViolation_Sentence FOREIGN KEY(SentenceID)
      REFERENCES [Sentence].[SentenceDetail](SentenceID),
   CONSTRAINT fk_SentenceViolation_Violation_Type FOREIGN KEY(ViolationTypeID)
      REFERENCES [NODS].[ViolationType](TypeID)
);

CREATE TABLE [NODS].[SuspensionTransmission] (
   [CaseID] VARCHAR(20) NOT NULL,
   [SuspensionTransmissionID] VARCHAR(20) PRIMARY KEY,
   [SuspensionTransmittedDate] DATE,
   [TransmissionAgencyID] VARCHAR(20),
   [TransmissionDate] DATE,
   [LengthOfSuspension] INTEGER,

   [SessionID] VARCHAR(40),
   [TenantID] VARCHAR(20),
   [DataOwnerID] VARCHAR(20),
   [DataSourceID] VARCHAR(20),
   [CreatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [UpdatedDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),
   [RecordStatusCodeID] CHAR(1) NOT NULL DEFAULT 'A',
   [RecordStatusDateTime] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(),

   CONSTRAINT fk_SuspensionTransmission_Case FOREIGN KEY(CaseID)
      REFERENCES [Case].[CaseDetail](CaseID),
   CONSTRAINT fk_SuspensionTransmission_Agency FOREIGN KEY(TransmissionAgencyID)
      REFERENCES [Entity].[Organization](OrganizationID)
);

