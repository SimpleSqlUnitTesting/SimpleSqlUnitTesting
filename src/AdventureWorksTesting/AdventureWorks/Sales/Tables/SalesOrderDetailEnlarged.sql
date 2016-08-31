CREATE TABLE [Sales].[SalesOrderDetailEnlarged] (
    [SalesOrderID]          INT              NOT NULL,
    [SalesOrderDetailID]    INT              IDENTITY (1, 1) NOT NULL,
    [CarrierTrackingNumber] NVARCHAR (25)    NULL,
    [OrderQty]              SMALLINT         NOT NULL,
    [ProductID]             INT              NOT NULL,
    [SpecialOfferID]        INT              NOT NULL,
    [UnitPrice]             MONEY            NOT NULL,
    [UnitPriceDiscount]     MONEY            NOT NULL,
    [LineTotal]             AS               (isnull(([UnitPrice]*((1.0)-[UnitPriceDiscount]))*[OrderQty],(0.0))),
    [rowguid]               UNIQUEIDENTIFIER ROWGUIDCOL NOT NULL,
    [ModifiedDate]          DATETIME         NOT NULL,
    CONSTRAINT [PK_SalesOrderDetailEnlarged_SalesOrderID_SalesOrderDetailID] PRIMARY KEY CLUSTERED ([SalesOrderID] ASC, [SalesOrderDetailID] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [AK_SalesOrderDetailEnlarged_rowguid]
    ON [Sales].[SalesOrderDetailEnlarged]([rowguid] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_SalesOrderDetailEnlarged_ProductID]
    ON [Sales].[SalesOrderDetailEnlarged]([ProductID] ASC);

