use db_aegtecnologia;
GO


IF OBJECT_ID('dbo.POSICOES_SASCAR') IS NOT NULL
BEGIN
	DROP TABLE dbo.POSICOES_SASCAR
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE POSICOES_SASCAR (
POSICAOID BIGINT NOT NULL,
VEICULOID INT NOT NULL,
DATACADASTRO DATETIME NOT NULL,
DATAPOSICAO DATETIME NOT NULL,
LATITUDE VARCHAR (20) NOT NULL,
LONGITUDE VARCHAR (20) NOT NULL,
VELOCIDADE INT NOT NULL,
UF VARCHAR (2) NOT NULL,
CIDADE VARCHAR (50) NOT NULL,
ENDERECO VARCHAR (150) NOT NULL
CONSTRAINT PK_POSICOES_SASCAR
PRIMARY KEY (POSICAOID)
)