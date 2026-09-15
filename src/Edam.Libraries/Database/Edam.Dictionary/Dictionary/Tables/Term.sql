CREATE TABLE [Dictionary].[Term] (
    [KeyID]        NVARCHAR (40)      NOT NULL,
    [LexiconID]    NVARCHAR (128)     NOT NULL,
    [Category]     NVARCHAR (128)     NOT NULL,
    [Context]      NVARCHAR (128)     NULL,
    [Term]         NVARCHAR (128)     NOT NULL,
    [OriginalTerm] NVARCHAR (128)     NOT NULL,
    [Soundex]      NVARCHAR (4)       NOT NULL,
    [Confidence]   DECIMAL (18, 2)    NULL,
    [Definition]   NVARCHAR (MAX)     NOT NULL,
    [Status]       INT                NULL,
    [DataOwnerID]  VARCHAR(20)        NULL,
    [DataSourceID] VARCHAR(20)        NULL,
    [CreatedDate]  DATETIMEOFFSET (7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [UpdatedDate]  DATETIMEOFFSET (7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT [PK_Term] PRIMARY KEY CLUSTERED ([KeyID] ASC)
);

