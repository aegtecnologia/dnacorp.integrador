use db_aegtecnologia;
GO


IF OBJECT_ID('dbo.POSICOES_OMNILINK') IS NOT NULL
BEGIN
	DROP TABLE dbo.POSICOES_OMNILINK
END
GO
/****** Object:  Table [dbo].[CRED_CESTA]    Script Date: 02/05/2019 10:25:41 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE POSICOES_OMNILINK (
POSICAOID INT NOT NULL,
TERMINALID INT NOT NULL,
DATACADASTRO DATETIME NOT NULL,
DATAPOSICAO DATETIME NOT NULL,
LATITUDE VARCHAR (20) NOT NULL,
LONGITUDE VARCHAR (20) NOT NULL,
ENDERECO VARCHAR (150) NOT NULL,
VELOCIDADE INT NOT NULL
CONSTRAINT PK_POSICOES_OMNILINK
PRIMARY KEY (POSICAOID)
)