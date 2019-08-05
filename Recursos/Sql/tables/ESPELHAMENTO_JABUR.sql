use db_aegtecnologia;
GO


IF OBJECT_ID('dbo.ESPELHAMENTO_JABUR') IS NOT NULL
BEGIN
	DROP TABLE dbo.ESPELHAMENTO_JABUR
END
GO
/****** Object:  Table [dbo].[CRED_CESTA]    Script Date: 02/05/2019 10:25:41 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE ESPELHAMENTO_JABUR (
VEICULOID INT NOT NULL,
PLACA VARCHAR (7) NOT NULL,
ESPELHADOATE DATETIME NOT NULL
CONSTRAINT PK_ESPELHAMENTO_JABUR
PRIMARY KEY (VEICULOID)
)