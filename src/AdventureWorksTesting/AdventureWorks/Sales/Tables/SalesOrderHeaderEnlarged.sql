CREATE TABLE [Sales].[SalesOrderHeaderEnlarged] (
    [SalesOrderID]           INT                   IDENTITY (1, 1) NOT FOR REPLICATION NOT NULL,
    [RevisionNumber]         TINYINT               NOT NULL,
    [OrderDate]              DATETIME              NOT NULL,
    [DueDate]                DATETIME              NOT NULL,
    [ShipDate]               DATETIME              NULL,
    [Status]                 TINYINT               NOT NULL,
    [OnlineOrderFlag]        [dbo].[Flag]          NOT NULL,
    [SalesOrderNumber]       AS                    (isnull(N'SO'+CONVERT([nvarchar](23),[SalesOrderID],(0)),N'*** ERROR ***')),
    [PurchaseOrderNumber]    [dbo].[OrderNumber]   NULL,
    [AccountNumber]          [dbo].[AccountNumber] NULL,
    [CustomerID]             INT                   NOT NULL,
    [SalesPersonID]          INT                   NULL,
    [TerritoryID]            INT                   NULL,
    [BillToAddressID]        INT                   NOT NULL,
    [ShipToAddressID]        INT                   NOT NULL,
    [ShipMethodID]           INT                   NOT NULL,
    [CreditCardID]           INT                   NULL,
    [CreditCardApprovalCode] VARCHAR (15)          NULL,
    [CurrencyRateID]         INT                   NULL,
    [SubTotal]               MONEY                 NOT NULL,
    [TaxAmt]                 MONEY                 NOT NULL,
    [Freight]                MONEY                 NOT NULL,
    [TotalDue]               AS                    (isnull(([SubTotal]+[TaxAmt])+[Freight],(0))),
    [Comment]                NVARCHAR (128)        NULL,
    [rowguid]                UNIQUEIDENTIFIER      ROWGUIDCOL NOT NULL,
    [ModifiedDate]           DATETIME              NOT NULL,
    CONSTRAINT [PK_SalesOrderHeaderEnlarged_SalesOrderID] PRIMARY KEY CLUSTERED ([SalesOrderID] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [AK_SalesOrderHeaderEnlarged_rowguid]
    ON [Sales].[SalesOrderHeaderEnlarged]([rowguid] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [AK_SalesOrderHeaderEnlarged_SalesOrderNumber]
    ON [Sales].[SalesOrderHeaderEnlarged]([SalesOrderNumber] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_SalesOrderHeaderEnlarged_CustomerID]
    ON [Sales].[SalesOrderHeaderEnlarged]([CustomerID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_SalesOrderHeaderEnlarged_SalesPersonID]
    ON [Sales].[SalesOrderHeaderEnlarged]([SalesPersonID] ASC);

