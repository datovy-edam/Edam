-- dbo

CREATE TABLE [dbo].[uCode] (
   Code VARCHAR(40),
   CodeID INT PRIMARY KEY,
   Description VARCHAR(128)
);

CREATE TABLE [dbo].[ClkCaseHdr] (
   [CaseID] INT PRIMARY KEY
);

CREATE TABLE [dbo].[CaseBase] (
   [CaseID] INT PRIMARY KEY
);

CREATE TABLE [dbo].[uExtConn] (
   [ExtConnID] INT PRIMARY KEY
);

CREATE TABLE [dbo].[sTimeStdActionType] (
   [TimeStandardActionTypeKey] VARCHAR(5) NOT NULL,
   [Description] VARCHAR(60),
   CONSTRAINT pk_sTimeStdActionType PRIMARY KEY ([TimeStandardActionTypeKey])
);

CREATE TABLE [dbo].[sTimeStdDuplicateAction] (
   [TimeStandardDuplicateActionKey] VARCHAR(5) NOT NULL,
   [Description] VARCHAR(60),
   CONSTRAINT pk_sTimeStdDuplicateAction PRIMARY KEY ([TimeStandardDuplicateActionKey])
);

CREATE TABLE [dbo].[sTimeStdIcon] (
   [TimeStdIconKy] VARCHAR(20) NOT NULL,
   [Description] VARCHAR(200),
   [IconURL] VARCHAR(200),
   CONSTRAINT pk_sTimeStdIcon PRIMARY KEY ([TimeStdIconKy])
);

CREATE TABLE [dbo].[sTimeStdParentType] (
   [TimeStandardParentTypeKey] VARCHAR(5) NOT NULL,
   [Description] VARCHAR(60),
   CONSTRAINT pk_sTimeStdParentType PRIMARY KEY ([TimeStandardParentTypeKey])
);

CREATE TABLE [dbo].[uTimeStdGroup] (
   [TimeStdGroupID] INT NOT NULL,
   [DisplayOnSummary] BIT,
   [CanManuallyAdd] BIT,
   [DisplayNextDue] BIT,
   [DisplayCompleted] BIT,
   CONSTRAINT pk_uTimeStdGroup PRIMARY KEY ([TimeStdGroupID])
);

CREATE TABLE [dbo].[uTimeStd] (
   [TimeStandardID] INT NOT NULL,
   [Days] INT,
   [AvoidDays] BIT,
   [ConfirmDate] BIT,
   [CanEdit] BIT,
   [TimeStandardClosedKey] VARCHAR(5),
   [TimeStandardOpenKey] VARCHAR(5),
   [UnavailableResourceID] INT,
   [UnavailableReasonID] INT,
   [ExcludeLeap] BIT,
   [MultiCompletedActionFlag] BIT,
   [CheckPartyAgeFlag] BIT,
   [PartyMinAge] INT,
   [PartyMaxAge] INT,
   [TimeStandardGroupID] INT,
   [IsForWorkflow] BIT,
   [IncludeUnavailableDaysInCount] BIT,
   [ShowAge] BIT,
   [StartOnDay] SMALLINT,
   [HideTargetDate] BIT,
   [ShowOnlyCurrentStatusOnSummaryTab] BIT,
   CONSTRAINT pk_uTimeStd PRIMARY KEY ([TimeStandardID]),
   CONSTRAINT fk_uTimeStd_TimeStandardID
      FOREIGN KEY ([TimeStandardID])
      REFERENCES [dbo].[uCode]([CodeID]),
   CONSTRAINT fk_uTimeStd_TimeStandardClosedKey
      FOREIGN KEY ([TimeStandardClosedKey])
      REFERENCES [dbo].[sTimeStdDuplicateAction]([TimeStandardDuplicateActionKey]),
   CONSTRAINT fk_uTimeStd_TimeStandardOpenKey
      FOREIGN KEY ([TimeStandardOpenKey])
      REFERENCES [dbo].[sTimeStdDuplicateAction]([TimeStandardDuplicateActionKey]),
   CONSTRAINT fk_uTimeStd_UnavailableResourceID
      FOREIGN KEY ([UnavailableResourceID])
      REFERENCES [dbo].[uCode]([CodeID]),
   CONSTRAINT fk_uTimeStd_UnavailableReasonID
      FOREIGN KEY ([UnavailableReasonID])
      REFERENCES [dbo].[uCode]([CodeID]),
   CONSTRAINT fk_uTimeStd_TimeStandardGroupID
      FOREIGN KEY ([TimeStandardGroupID])
      REFERENCES [dbo].[uTimeStdGroup]([TimeStdGroupID]),
);

CREATE TABLE [dbo].[TimeStandardTrackHistory] (
   [TimeStandardTrackHistoryId] INT IDENTITY NOT NULL,
   [CaseID] INT NOT NULL,
   [TimeStandardTrackId] INT,
   [IsTimeTrackTurnedOff] BIT,
   [Cmmnt] VARCHAR(MAX),
   [UserIDCreate] INT,
   [TimestampCreate] DATETIME,
   [UserIDChange] INT,
   [TimestampChange] DATETIME,
   CONSTRAINT pk_TimeStandardTrackHistory PRIMARY KEY ([TimeStandardTrackHistoryId]),
   CONSTRAINT fk_TimeStandardTrackHistory_CaseID
      FOREIGN KEY ([CaseID])
      REFERENCES [dbo].[ClkCaseHdr]([CaseID]),
   CONSTRAINT fk_TimeStandardTrackHistory_TimeStandardTrackId
      FOREIGN KEY ([TimeStandardTrackId])
      REFERENCES [dbo].[uCode]([CodeID]),
);

CREATE TABLE [dbo].[TimeStdInst] (
   [TimeStandardInstID] INT IDENTITY NOT NULL,
   [CaseID] INT,
   [TimeStandardID] INT,
   [TimeStandardTrackID] INT,
   [DtTarget] DATETIME,
   [DtComplete] DATETIME,
   [Description] VARCHAR(60),
   [Cmmnt] VARCHAR(MAX),
   [Days] INT,
   [AvoidDays] BIT,
   [UnavailableResourceID] INT,
   [UnavailableReasonID] INT,
   [ExcludeLeap] BIT,
   [ConfirmDate] BIT,
   [TimeStandardClosedKey] VARCHAR(5),
   [TimeStandardOpenKey] VARCHAR(5),
   [MultiCompletedActionFlag] BIT,
   [CheckPartyAgeFlag] BIT,
   [PartyMinAge] INT,
   [PartyMaxAge] INT,
   [Deleted] BIT,
   [Manual] BIT,
   [UserIDCreate] INT,
   [TimestampCreate] DATETIME,
   [UserIDChange] INT,
   [TimestampChange] DATETIME,
   [DoNotRecur] BIT,
   [Party] INT,
   [IncludeUnavailableDaysInCount] BIT,
   [ChargeID] INT,
   [IsUserModifiedTarget] BIT,
   [IsUserModifiedCompleted] BIT,
   [StartOnDay] SMALLINT,
   CONSTRAINT pk_TimeStdInst PRIMARY KEY ([TimeStandardInstID]),
   CONSTRAINT fk_TimeStdInst_CaseID
      FOREIGN KEY ([CaseID])
      REFERENCES [dbo].[CaseBase]([CaseID]),
   CONSTRAINT fk_TimeStdInst_TimeStandardID
      FOREIGN KEY ([TimeStandardID])
      REFERENCES [dbo].[uTimeStd]([TimeStandardID]),
   CONSTRAINT fk_TimeStdInst_TimeStandardTrackID
      FOREIGN KEY ([TimeStandardTrackID])
      REFERENCES [dbo].[uCode]([CodeID]),
   CONSTRAINT fk_TimeStdInst_UnavailableResourceID
      FOREIGN KEY ([UnavailableResourceID])
      REFERENCES [dbo].[uCode]([CodeID]),
   CONSTRAINT fk_TimeStdInst_UnavailableReasonID
      FOREIGN KEY ([UnavailableReasonID])
      REFERENCES [dbo].[uCode]([CodeID]),
   CONSTRAINT fk_TimeStdInst_TimeStandardClosedKey
      FOREIGN KEY ([TimeStandardClosedKey])
      REFERENCES [dbo].[sTimeStdDuplicateAction]([TimeStandardDuplicateActionKey]),
   CONSTRAINT fk_TimeStdInst_TimeStandardOpenKey
      FOREIGN KEY ([TimeStandardOpenKey])
      REFERENCES [dbo].[sTimeStdDuplicateAction]([TimeStandardDuplicateActionKey]),
);

CREATE TABLE [dbo].[uTimeStdStatus] (
   [TimeStdStatusID] INT NOT NULL,
   [TimeStdIconKy] VARCHAR(20),
   [IsConsideredOverdue] BIT,
   [HideTargetDate] BIT,
   [ShowCausingCode] BIT,
   CONSTRAINT pk_uTimeStdStatus PRIMARY KEY ([TimeStdStatusID]),
   CONSTRAINT fk_uTimeStdStatus_TimeStdIconKy
      FOREIGN KEY ([TimeStdIconKy])
      REFERENCES [dbo].[sTimeStdIcon]([TimeStdIconKy]),
);

CREATE TABLE [dbo].[TimeStdInstAction] (
   [TimeStandardActionID] INT IDENTITY NOT NULL,
   [TimeStandardInstID] INT,
   [TimeStandardActionTypeKey] VARCHAR(5),
   [TimeStandardParentTypeKey] VARCHAR(5),
   [EventCategoryKey] VARCHAR(5),
   [CodeID] INT,
   [CodeIDCacheTable] VARCHAR(10),
   [StatusCodeID] INT,
   [TimeStandardTriggerDateType] VARCHAR(5),
   [fTriggeringAction] BIT,
   [CreatePerParty] BIT,
   [CreatePerCharge] BIT,
   [RequiredOccurrences] INT,
   [CurrentOccurrences] INT,
   [CloseOnlyIfRelatedToCauseTrigger] BIT,
   [TimeStandardClosedOverrideKey] VARCHAR(5),
   [TimeStandardOpenOverrideKey] VARCHAR(5),
   [RequireAllChargesDisposedForClose] BIT,
   [CloseAllParties] BIT,
   CONSTRAINT pk_TimeStdInstAction PRIMARY KEY ([TimeStandardActionID]),
   CONSTRAINT fk_TimeStdInstAction_TimeStandardInstID
      FOREIGN KEY ([TimeStandardInstID])
      REFERENCES [dbo].[TimeStdInst]([TimeStandardInstID]),
   CONSTRAINT fk_TimeStdInstAction_TimeStandardActionTypeKey
      FOREIGN KEY ([TimeStandardActionTypeKey])
      REFERENCES [dbo].[sTimeStdActionType]([TimeStandardActionTypeKey]),
   CONSTRAINT fk_TimeStdInstAction_TimeStandardParentTypeKey
      FOREIGN KEY ([TimeStandardParentTypeKey])
      REFERENCES [dbo].[sTimeStdParentType]([TimeStandardParentTypeKey]),
   CONSTRAINT fk_TimeStdInstAction_StatusCodeID
      FOREIGN KEY ([StatusCodeID])
      REFERENCES [dbo].[uTimeStdStatus]([TimeStdStatusID]),
);

CREATE TABLE [dbo].[TimeStdInstActionGroup] (
   [GroupID] INT IDENTITY NOT NULL,
   [TimeStdInstID] INT,
   CONSTRAINT pk_TimeStdInstActionGroup PRIMARY KEY ([GroupID]),
   CONSTRAINT fk_TimeStdInstActionGroup_TimeStdInstID
      FOREIGN KEY ([TimeStdInstID])
      REFERENCES [dbo].[TimeStdInst]([TimeStandardInstID]),
);

CREATE TABLE [dbo].[TimeStdInstActionTrigger] (
   [TimeStandardActionID] INT NOT NULL,
   [TriggeringParentTypeKey] VARCHAR(5) NOT NULL,
   [TriggeringParentID] INT NOT NULL,
   CONSTRAINT pk_TimeStdInstActionTrigger PRIMARY KEY ([TimeStandardActionID],[TriggeringParentTypeKey],[TriggeringParentID]),
   CONSTRAINT fk_TimeStdInstActionTrigger_TimeStandardActionID
      FOREIGN KEY ([TimeStandardActionID])
      REFERENCES [dbo].[TimeStdInstAction]([TimeStandardActionID]),
   CONSTRAINT fk_TimeStdInstActionTrigger_TriggeringParentTypeKey
      FOREIGN KEY ([TriggeringParentTypeKey])
      REFERENCES [dbo].[sTimeStdParentType]([TimeStandardParentTypeKey]),
);

CREATE TABLE [dbo].[TimeStdInstStatus] (
   [TimeStandardInstStatusID] INT IDENTITY NOT NULL,
   [TimeStandardInstID] INT,
   [TimeStdStatusCodeID] INT,
   [Date] DATETIME,
   [TriggeringParentTypeKey] VARCHAR(5),
   [TriggeringParentID] INT,
   [TimeStandardActionTypeKey] VARCHAR(5),
   [Manual] BIT,
   CONSTRAINT pk_TimeStdInstStatus PRIMARY KEY ([TimeStandardInstStatusID]),
   CONSTRAINT fk_TimeStdInstStatus_TimeStandardInstID
      FOREIGN KEY ([TimeStandardInstID])
      REFERENCES [dbo].[TimeStdInst]([TimeStandardInstID]),
   CONSTRAINT fk_TimeStdInstStatus_TimeStdStatusCodeID
      FOREIGN KEY ([TimeStdStatusCodeID])
      REFERENCES [dbo].[uTimeStdStatus]([TimeStdStatusID]),
   CONSTRAINT fk_TimeStdInstStatus_TimeStandardActionTypeKey
      FOREIGN KEY ([TimeStandardActionTypeKey])
      REFERENCES [dbo].[sTimeStdActionType]([TimeStandardActionTypeKey]),
);

CREATE TABLE [dbo].[xTimeStandardActionHearRslt] (
   [TimeStandardActionID] INT NOT NULL,
   [HearingResultTypeID] INT NOT NULL,
   CONSTRAINT pk_xTimeStandardActionHearRslt PRIMARY KEY ([TimeStandardActionID],[HearingResultTypeID]),
   CONSTRAINT fk_xTimeStandardActionHearRslt_TimeStandardActionID
      FOREIGN KEY ([TimeStandardActionID])
      REFERENCES [dbo].[TimeStdInstAction]([TimeStandardActionID]),
   CONSTRAINT fk_xTimeStandardActionHearRslt_HearingResultTypeID
      FOREIGN KEY ([HearingResultTypeID])
      REFERENCES [dbo].[uCode]([CodeID]),
);

CREATE TABLE [dbo].[xTimeStandardActionWarrantType] (
   [TimeStandardActionID] INT NOT NULL,
   [WarrantTypeID] INT NOT NULL,
   CONSTRAINT pk_xTimeStandardActionWarrantType PRIMARY KEY ([TimeStandardActionID],[WarrantTypeID]),
   CONSTRAINT fk_xTimeStandardActionWarrantType_TimeStandardActionID
      FOREIGN KEY ([TimeStandardActionID])
      REFERENCES [dbo].[TimeStdInstAction]([TimeStandardActionID]),
   CONSTRAINT fk_xTimeStandardActionWarrantType_WarrantTypeID
      FOREIGN KEY ([WarrantTypeID])
      REFERENCES [dbo].[uCode]([CodeID]),
);

CREATE TABLE [dbo].[xTimeStdInstActionGroupTimeStdInstAction] (
   [GroupID] INT NOT NULL,
   [ActionID] INT NOT NULL,
   CONSTRAINT pk_xTimeStdInstActionGroupTimeStdInstAction PRIMARY KEY ([GroupID],[ActionID]),
   CONSTRAINT fk_xTimeStdInstActionGroupTimeStdInstAction_GroupID
      FOREIGN KEY ([GroupID])
      REFERENCES [dbo].[TimeStdInstActionGroup]([GroupID]),
   CONSTRAINT fk_xTimeStdInstActionGroupTimeStdInstAction_ActionID
      FOREIGN KEY ([ActionID])
      REFERENCES [dbo].[TimeStdInstAction]([TimeStandardActionID]),
);

CREATE TABLE [dbo].[xuTimeStdAction] (
   [TimeStandardActionID] INT IDENTITY NOT NULL,
   [TimeStandardID] INT,
   [ParentTypeKey] VARCHAR(5),
   [CodeID] INT,
   [CodeIDCacheTable] VARCHAR(10),
   [EventCategoryKey] VARCHAR(5),
   [StatusCodeID] INT,
   [TimeStandardTriggerDateType] VARCHAR(5),
   [ActionTypeKey] VARCHAR(5),
   [CreatePerParty] BIT,
   [CreatePerCharge] BIT,
   [RequiredOccurrences] INT,
   [CloseOnlyIfRelatedToCauseTrigger] BIT,
   [TimeStandardClosedOverrideKey] VARCHAR(5),
   [TimeStandardOpenOverrideKey] VARCHAR(5),
   [RequireAllChargesDisposedForClose] BIT,
   [CloseAllParties] BIT,
   CONSTRAINT pk_xuTimeStdAction PRIMARY KEY ([TimeStandardActionID]),
   CONSTRAINT fk_xuTimeStdAction_TimeStandardID
      FOREIGN KEY ([TimeStandardID])
      REFERENCES [dbo].[uTimeStd]([TimeStandardID]),
   CONSTRAINT fk_xuTimeStdAction_ParentTypeKey
      FOREIGN KEY ([ParentTypeKey])
      REFERENCES [dbo].[sTimeStdParentType]([TimeStandardParentTypeKey]),
   CONSTRAINT fk_xuTimeStdAction_StatusCodeID
      FOREIGN KEY ([StatusCodeID])
      REFERENCES [dbo].[uTimeStdStatus]([TimeStdStatusID]),
   CONSTRAINT fk_xuTimeStdAction_ActionTypeKey
      FOREIGN KEY ([ActionTypeKey])
      REFERENCES [dbo].[sTimeStdActionType]([TimeStandardActionTypeKey]),
);

CREATE TABLE [dbo].[xuTimeStdActionGroup] (
   [GroupID] INT IDENTITY NOT NULL,
   [uTimeStdCodeID] INT,
   CONSTRAINT pk_xuTimeStdActionGroup PRIMARY KEY ([GroupID]),
   CONSTRAINT fk_xuTimeStdActionGroup_uTimeStdCodeID
      FOREIGN KEY ([uTimeStdCodeID])
      REFERENCES [dbo].[uTimeStd]([TimeStandardID]),
);

CREATE TABLE [dbo].[xuTimeStdActionuExtConn] (
   [TimeStandardActionID] INT NOT NULL,
   [TimeStandardID] INT,
   [ExtConnID] INT NOT NULL,
   CONSTRAINT pk_xuTimeStdActionuExtConn PRIMARY KEY ([TimeStandardActionID],[ExtConnID]),
   CONSTRAINT fk_xuTimeStdActionuExtConn_TimeStandardActionID
      FOREIGN KEY ([TimeStandardActionID])
      REFERENCES [dbo].[xuTimeStdAction]([TimeStandardActionID]),
   CONSTRAINT fk_xuTimeStdActionuExtConn_TimeStandardID
      FOREIGN KEY ([TimeStandardID])
      REFERENCES [dbo].[uTimeStd]([TimeStandardID]),
   CONSTRAINT fk_xuTimeStdActionuExtConn_ExtConnID
      FOREIGN KEY ([ExtConnID])
      REFERENCES [dbo].[uExtConn]([ExtConnID]),
);

CREATE TABLE [dbo].[xuTimeStdActionuHearRslt] (
   [TimeStandardActionID] INT NOT NULL,
   [TimeStandardID] INT,
   [HearingResultID] INT NOT NULL,
   CONSTRAINT pk_xuTimeStdActionuHearRslt PRIMARY KEY ([TimeStandardActionID],[HearingResultID]),
   CONSTRAINT fk_xuTimeStdActionuHearRslt_TimeStandardActionID
      FOREIGN KEY ([TimeStandardActionID])
      REFERENCES [dbo].[xuTimeStdAction]([TimeStandardActionID]),
   CONSTRAINT fk_xuTimeStdActionuHearRslt_TimeStandardID
      FOREIGN KEY ([TimeStandardID])
      REFERENCES [dbo].[uTimeStd]([TimeStandardID]),
   CONSTRAINT fk_xuTimeStdActionuHearRslt_HearingResultID
      FOREIGN KEY ([HearingResultID])
      REFERENCES [dbo].[uCode]([CodeID]),
);

CREATE TABLE [dbo].[xuTimeStdActionuWarrantType] (
   [TimeStandardActionID] INT NOT NULL,
   [TimeStandardID] INT,
   [WarrantTypeID] INT NOT NULL,
   CONSTRAINT pk_xuTimeStdActionuWarrantType PRIMARY KEY ([TimeStandardActionID],[WarrantTypeID]),
   CONSTRAINT fk_xuTimeStdActionuWarrantType_TimeStandardActionID
      FOREIGN KEY ([TimeStandardActionID])
      REFERENCES [dbo].[xuTimeStdAction]([TimeStandardActionID]),
   CONSTRAINT fk_xuTimeStdActionuWarrantType_TimeStandardID
      FOREIGN KEY ([TimeStandardID])
      REFERENCES [dbo].[uTimeStd]([TimeStandardID]),
   CONSTRAINT fk_xuTimeStdActionuWarrantType_WarrantTypeID
      FOREIGN KEY ([WarrantTypeID])
      REFERENCES [dbo].[uCode]([CodeID]),
);

CREATE TABLE [dbo].[xuTimeStdTrackuTimeStd] (
   [TimeStandardTrackID] INT NOT NULL,
   [TimeStandardID] INT NOT NULL,
   CONSTRAINT pk_xuTimeStdTrackuTimeStd PRIMARY KEY ([TimeStandardTrackID],[TimeStandardID]),
   CONSTRAINT fk_xuTimeStdTrackuTimeStd_TimeStandardTrackID
      FOREIGN KEY ([TimeStandardTrackID])
      REFERENCES [dbo].[uCode]([CodeID]),
   CONSTRAINT fk_xuTimeStdTrackuTimeStd_TimeStandardID
      FOREIGN KEY ([TimeStandardID])
      REFERENCES [dbo].[uTimeStd]([TimeStandardID]),
);

