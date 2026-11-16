CREATE DATABASE CatalogoMusica;
GO


USE CatalogoMusica;
GO






CREATE TABLE Usuario (
    IdUsuario   INT IDENTITY(1,1) NOT NULL,
    Nome        VARCHAR(100) NOT NULL,
    Email       VARCHAR(50)  NOT NULL,
    Senha       VARCHAR(255) NOT NULL,              
    TipoUsuario VARCHAR(20)  NOT NULL,
    Ativo       BIT          NOT NULL CONSTRAINT DF_Usuario_Ativo DEFAULT 1,


    CONSTRAINT PK_Usuario PRIMARY KEY (IdUsuario),
    CONSTRAINT UQ_Usuario_Email UNIQUE (Email),                                  
    CONSTRAINT CK_Usuario_Tipo CHECK (TipoUsuario IN ('Admin', 'Comum'))
);
GO


CREATE TABLE Artista (
    IdArtista INT IDENTITY(1,1) NOT NULL,
    Nome      VARCHAR(100) NOT NULL,


    CONSTRAINT PK_Artista PRIMARY KEY (IdArtista),
    CONSTRAINT UQ_Artista_Nome UNIQUE (Nome)
);
GO


CREATE TABLE Genero (
    IdGenero INT IDENTITY(1,1) NOT NULL,
    Nome     VARCHAR(50) NOT NULL,


    CONSTRAINT PK_Genero PRIMARY KEY (IdGenero),
    CONSTRAINT UQ_Genero_Nome UNIQUE (Nome)                                      
);
GO


CREATE TABLE Musica (
    IdMusica       INT IDENTITY(1,1) NOT NULL,                                   
    Titulo         VARCHAR(100) NOT NULL,                                       
    IdArtista      INT          NOT NULL,                                       
    Album          VARCHAR(100) NULL,                                           
    DataLancamento DATE         NOT NULL,                                        
    Duracao        INT          NOT NULL,                                        
    ImagemCapa     VARCHAR(MAX) NULL,


    CONSTRAINT PK_Musica PRIMARY KEY (IdMusica),
    CONSTRAINT FK_Musica_Artista FOREIGN KEY (IdArtista) REFERENCES Artista (IdArtista),
    CONSTRAINT CK_Musica_Duracao CHECK (Duracao > 0),                           
    CONSTRAINT CK_Musica_DataLancamento CHECK (DataLancamento >= '1860-01-01')   
);
GO


CREATE TABLE MusicaGenero (
    IdGenero INT NOT NULL,
    IdMusica INT NOT NULL,


    CONSTRAINT PK_MusicaGenero PRIMARY KEY (IdGenero, IdMusica),
    CONSTRAINT FK_MusicaGenero_Genero FOREIGN KEY (IdGenero) REFERENCES Genero (IdGenero),
    CONSTRAINT FK_MusicaGenero_Musica FOREIGN KEY (IdMusica) REFERENCES Musica (IdMusica)
        ON DELETE CASCADE                                                        
);
GO


CREATE TABLE Playlist (
    IdPlaylist  INT IDENTITY(1,1) NOT NULL,
    IdUsuario   INT          NOT NULL,                                           
    Nome        VARCHAR(100) NOT NULL,                                          
    Descricao   VARCHAR(255) NULL,
    DataCriacao DATETIME2    NOT NULL CONSTRAINT DF_Playlist_DataCriacao DEFAULT SYSDATETIME(),


    CONSTRAINT PK_Playlist PRIMARY KEY (IdPlaylist),
    CONSTRAINT FK_Playlist_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuario (IdUsuario),
    CONSTRAINT UQ_Playlist_Usuario_Nome UNIQUE (IdUsuario, Nome)                 
);
GO


CREATE TABLE PlaylistMusica (
    IdPlaylist INT NOT NULL,
    IdMusica   INT NOT NULL,
    Posicao    INT NOT NULL,                                                     
    DataAdicao DATETIME2 NOT NULL CONSTRAINT DF_PlaylistMusica_DataAdicao DEFAULT SYSDATETIME(),


    CONSTRAINT PK_PlaylistMusica PRIMARY KEY (IdPlaylist, IdMusica),             
    CONSTRAINT UQ_PlaylistMusica_Posicao UNIQUE (IdPlaylist, Posicao),
    CONSTRAINT CK_PlaylistMusica_Posicao CHECK (Posicao > 0),
    CONSTRAINT FK_PlaylistMusica_Playlist FOREIGN KEY (IdPlaylist) REFERENCES Playlist (IdPlaylist)
        ON DELETE CASCADE,                                                       
    CONSTRAINT FK_PlaylistMusica_Musica FOREIGN KEY (IdMusica) REFERENCES Musica (IdMusica)
        ON DELETE CASCADE                                                        
);
GO




CREATE TABLE FilaReproducao (
    IdFila     INT IDENTITY(1,1) NOT NULL,
    IdUsuario  INT NOT NULL,
    IdMusica   INT NOT NULL,
    Posicao    INT NOT NULL,
    DataAdicao DATETIME2 NOT NULL CONSTRAINT DF_FilaReproducao_DataAdicao DEFAULT SYSDATETIME(),


    CONSTRAINT PK_FilaReproducao PRIMARY KEY (IdFila),
    CONSTRAINT UQ_FilaReproducao_Posicao UNIQUE (IdUsuario, Posicao),
    CONSTRAINT CK_FilaReproducao_Posicao CHECK (Posicao > 0),
    CONSTRAINT FK_FilaReproducao_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuario (IdUsuario),
    CONSTRAINT FK_FilaReproducao_Musica FOREIGN KEY (IdMusica) REFERENCES Musica (IdMusica)
        ON DELETE CASCADE                                                      
);
GO






CREATE INDEX IX_Musica_Titulo    ON Musica (Titulo);
CREATE INDEX IX_Musica_IdArtista ON Musica (IdArtista);
CREATE INDEX IX_MusicaGenero_IdMusica ON MusicaGenero (IdMusica);
CREATE INDEX IX_Playlist_IdUsuario ON Playlist (IdUsuario);
CREATE INDEX IX_PlaylistMusica_IdMusica ON PlaylistMusica (IdMusica);
CREATE INDEX IX_FilaReproducao_IdMusica ON FilaReproducao (IdMusica);
GO








INSERT INTO Usuario (Nome, Email, Senha, TipoUsuario)
VALUES ('Administrador', 'admin@catalogo.com', '', 'Admin');


INSERT INTO Genero (Nome)
VALUES ('Rock'), ('Pop'), ('MPB'), ('Samba'), ('Hip Hop'),
       ('Eletrônica'), ('Sertanejo'), ('Jazz'), ('Clássica'), ('Reggae');


INSERT INTO Artista (Nome)
VALUES ('Queen'), ('Djavan');


INSERT INTO Musica (Titulo, IdArtista, Album, DataLancamento, Duracao)
VALUES ('Bohemian Rhapsody', 1, 'A Night at the Opera', '1975-10-31', 354),
       ('Oceano',            2, 'Djavan',               '1989-01-01', 235);


INSERT INTO MusicaGenero (IdGenero, IdMusica)
VALUES (1, 1), (3, 2);
GO


INSERT INTO Playlist (IdUsuario, Nome, Descricao)
VALUES (1, 'Favoritas', 'Minhas músicas preferidas');


INSERT INTO PlaylistMusica (IdPlaylist, IdMusica, Posicao)
VALUES (1, 1, 1), (1, 2, 2);


INSERT INTO FilaReproducao (IdUsuario, IdMusica, Posicao)
VALUES (1, 2, 1), (1, 1, 2);
GO


-- CONSULTA DE EXEMPLO (RF03 / RF05 / RF06)


SELECT m.IdMusica, m.Titulo, a.Nome AS Artista, m.Album, m.DataLancamento, m.Duracao,
       STRING_AGG(g.Nome, ', ') AS Generos
FROM Musica m
JOIN Artista a ON a.IdArtista = m.IdArtista
JOIN MusicaGenero mg ON mg.IdMusica = m.IdMusica
JOIN Genero g ON g.IdGenero = mg.IdGenero
GROUP BY m.IdMusica, m.Titulo, a.Nome, m.Album, m.DataLancamento, m.Duracao;
GO


-- CONSULTAS DE EXEMPLO - PLAYLIST E FILA


-- Músicas de uma playlist, em ordem (RF19)
SELECT pm.Posicao, m.Titulo, a.Nome AS Artista, m.Duracao
FROM PlaylistMusica pm
JOIN Musica m  ON m.IdMusica  = pm.IdMusica
JOIN Artista a ON a.IdArtista = m.IdArtista
WHERE pm.IdPlaylist = 1
ORDER BY pm.Posicao;
GO


-- Fila de reprodução de um usuário, em ordem (RF27)
SELECT f.Posicao, m.Titulo, a.Nome AS Artista
FROM FilaReproducao f
JOIN Musica m  ON m.IdMusica  = f.IdMusica
JOIN Artista a ON a.IdArtista = m.IdArtista
WHERE f.IdUsuario = 1
ORDER BY f.Posicao;
GO


-- Adicionar música ao final da fila (RF25)
DECLARE @IdUsuario INT = 1, @IdMusica INT = 1;
INSERT INTO FilaReproducao (IdUsuario, IdMusica, Posicao)
SELECT @IdUsuario, @IdMusica, ISNULL(MAX(Posicao), 0) + 1
FROM FilaReproducao
WHERE IdUsuario = @IdUsuario;
GO


-- Avançar para a próxima da fila (RF31 / RN21)
DECLARE @IdUsuario INT = 1;
BEGIN TRANSACTION;
    DELETE FROM FilaReproducao WHERE IdUsuario = @IdUsuario AND Posicao = 1;
    UPDATE FilaReproducao SET Posicao = Posicao - 1 WHERE IdUsuario = @IdUsuario;
COMMIT;
GO