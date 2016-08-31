IF OBJECT_ID('UnitTesting.FactoryMock', 'P') IS NOT NULL
  DROP PROCEDURE UnitTesting.FactoryMock
GO

CREATE PROCEDURE FactoryMock
	@ProcedureName VARCHAR(128),
	@MockType CHAR(10) = 'N'
AS

--=========================================================================================================
-- Declare section
--=========================================================================================================
DECLARE @AlterSQL		  VARCHAR(650),
	    @SchemeSP         VARCHAR(50),
		@ObjectIdSP       BIGINT,
        @ListParameterSQL VARCHAR(500),
	    @Parameters		  VARCHAR(600),
		@DeclareSQL       VARCHAR(1000),
		@InsertCall		  VARCHAR(500),
		@InsertArguments  VARCHAR(5000),
		@SqlString		  NVARCHAR(4000),
		@BodySQL          NVARCHAR(3500),
		@IdSPCall         INT,
		@ParametersSQL    VARCHAR(4000)='',
		@CountParameters  INT

DECLARE @ListParameters AS TABLE
( ParameterID INT IDENTITY,
  ParameterName VARCHAR(60)
);

DECLARE @ParametersEntered AS TABLE
(
  ParameterName  VARCHAR(60),
  ParameterValue sql_variant
);

--==========================================================================================================
--Verifying if exists the Stored Procedure to make the structure and getting the scheme of the SP
--was necessary add logic to manage the use of schemes
--==========================================================================================================
IF (SELECT CHARINDEX('.',@ProcedureName,1))>0
BEGIN
  DECLARE @LenProcedureName INT=LEN(@ProcedureName)
  DECLARE @PositionPattern INT = (CHARINDEX('.',@ProcedureName,1))
  DECLARE @SchemeFromSP VARCHAR(80)


  SELECT @SchemeFromSP=SUBSTRING(@ProcedureName,1,@PositionPattern-1);
  SELECT @SchemeFromSP=REPLACE((REPLACE(@SchemeFromSP ,']', '')) ,'[', '');

  SELECT @ProcedureName=SUBSTRING(@ProcedureName,@PositionPattern+1,@LenProcedureName-@PositionPattern+1);  
  SELECT @ProcedureName=REPLACE((REPLACE(@ProcedureName ,']', '')) ,'[', '');

END


IF NOT EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = @ProcedureName)
BEGIN
  PRINT '*******ERROR - SP DOES NOT EXISTS ********'
END

SELECT @SchemeSP=schema_Name(Ob.schema_id),
	   @ObjectIdSP=object_id
FROM sys.objects  Ob
INNER JOIN sys.schemas Sch
ON Sch.[schema_id]=Ob.[schema_id]
WHERE [type_desc] IN ('SQL_STORED_PROCEDURE')
AND Ob.[name] NOT LIKE 'sp_%'
AND Ob.[name] = @ProcedureName
AND Sch.name = COALESCE(@SchemeFromSP,Sch.name)

SET @AlterSQL = 'ALTER PROCEDURE ' + @SchemeSP + '.' + @ProcedureName + '  ';

--==========================================================================================================
--Verifying if a temporary table exists and else create
--==========================================================================================================
IF OBJECT_ID('tempdb..##SPCall') IS NULL
BEGIN
CREATE TABLE ##SPCall
(
    CallID        INT IDENTITY   NOT NULL,
    SP_Name       VARCHAR(120)    NOT NULL,
);
END

--==========================================================================================================
--Verifying if a temporary table exists and else create
--==========================================================================================================
IF OBJECT_ID('tempdb..##ArgumentsCall') IS NULL
BEGIN
CREATE TABLE ##ArgumentsCall
(
    CallID				INT            NOT NULL,
	ArgumentName		VARCHAR(75)    NOT NULL,
    ArgumentValue       sql_variant    NULL,
);
END

---==============================================================================================================================
--- Verifying the parameters of the SP, if it has at least parameter we get the 
--- name and datatype of this
--===============================================================================================================================
IF EXISTS(
 SELECT *
 FROM sysobjects t1
 INNER JOIN syscolumns t2 on t1.[id]=t2.[id]
 INNER JOIN systypes t3 on t2.xtype=t3.xtype
 where t1.[name]=@ProcedureName)
	BEGIN
	 SELECT @Parameters=SUBSTRING(definition,CHARINDEX('@',definition),CHARINDEX(CHAR(10)+'AS',definition)-CHARINDEX('@',definition))
	 FROM sys.sql_modules WHERE OBJECT_ID=@ObjectIdSP;
	END

INSERT INTO @ListParameters(ParameterName)
SELECT PARAMETER_NAME
FROM information_schema.parameters
WHERE specific_name=@ProcedureName
ORDER BY ORDINAL_POSITION ASC;

SET @CountParameters = @@ROWCOUNT;

IF @CountParameters > 0
BEGIN
-- Logic in case of normal mock
IF @MockType='N'
BEGIN
 --==========================================================================================================
 --Starting the block of dynamic SQL for rebuild the specific SP
 --==========================================================================================================
 SET @DeclareSQL='SET NOCOUNT ON; DECLARE @IdSPCall INT; DECLARE @ProcedureName VARCHAR(128) = ''' + @ProcedureName + ''';';
 SET @DeclareSQL=@DeclareSQL + '  DECLARE @ListParameters AS TABLE
								 ( ParameterID INT IDENTITY,
								  ParameterMode VARCHAR(30),
								  ParameterName VARCHAR(80),
								  ParameterDataType VARCHAR(35)
								 );

								DECLARE @ParametersEntered AS TABLE
								(
								 ParameterName  VARCHAR(80),
								 ParameterValue sql_variant
								); '
						

SET @ListParameterSQL='INSERT INTO @ListParameters(ParameterMode,ParameterName,ParameterDataType)
					   SELECT PARAMETER_MODE,PARAMETER_NAME,DATA_TYPE
					   FROM information_schema.parameters
					   WHERE specific_name=@ProcedureName
					   ORDER BY ORDINAL_POSITION ASC;';

 SET @InsertCall=' INSERT INTO ##SPCall VALUES ( ''' + @ProcedureName + '''); SELECT @IdSPCall=SCOPE_IDENTITY(); ';

--==========================================================================================================
-- We create a cursor for create the dynamic code which will insert one by one in the table ##ArgumentsCall
--==========================================================================================================

 DECLARE @cursor_parameter VARCHAR(35),
		 @cursor_value     sql_variant,
		 @cursor_identity  tinyint =1;
 
 DECLARE parameters_cursor CURSOR FOR 
 SELECT  ParameterName
 FROM @ListParameters
 ORDER BY ParameterID;

OPEN parameters_cursor

FETCH NEXT FROM parameters_cursor 
INTO @cursor_parameter

WHILE @@FETCH_STATUS = 0
BEGIN
 
  SET @ParametersSQL = @ParametersSQL + 'INSERT INTO @ParametersEntered ' +
											'SELECT ParameterName, ' + @cursor_parameter + ' ' +
											'FROM @ListParameters WHERE ParameterID=' + CAST(@cursor_identity AS CHAR(2))+ ';'
  
  SET @cursor_identity=@cursor_identity + 1;
  
 -- Get the next rows
    FETCH NEXT FROM parameters_cursor
    INTO @cursor_parameter
END 
CLOSE parameters_cursor;
DEALLOCATE parameters_cursor;


SET @InsertArguments=' INSERT INTO ##ArgumentsCall(CallID,ArgumentName,ArgumentValue)' +
					 ' SELECT @IdSPCall,' +
					 ' ParameterName,'    +
					 ' Value=(SELECT CAST(ParameterValue AS CHAR(100)) FROM  @ParametersEntered WHERE ParameterName=LP.ParameterName)' +
					 ' FROM @ListParameters LP;'

 SELECT @BodySQL=@DeclareSQL + @ListParameterSQL + @InsertCall+ @ParametersSQL + @InsertArguments;

END

IF @MockType='E'
SET @BodySQL='RAISERROR( ''Mock Error in SP'', -- Message text
                16, -- severity (Level 11-16 These messages indicate errors that can be corrected by the user)
                 1  -- state
    );';
END

IF @CountParameters = 0
BEGIN
 SET @Parameters=' ';
 SET @BodySQL='SELECT 1';
END

--Building dinamically the SP mocked
 SET @SqlString = @AlterSQL + @Parameters + '  AS ' + @BodySQL;
 EXECUTE sp_executesql @SqlString
 GO