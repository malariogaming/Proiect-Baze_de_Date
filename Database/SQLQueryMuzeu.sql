/* =====================================================================
   Conventii:
   - fiecare tabel are cheie primara INT IDENTITY (LINQ to SQL are nevoie
     de cheie primara ca sa permita INSERT / UPDATE / DELETE);
   - valorile din CHECK-uri sunt fara diacritice, ca sa fie usor de
     comparat din C#;
   - valorile DEFAULT de aici NU sunt folosite de LINQ to SQL la insert,
     asa ca toate campurile trebuie setate si din cod (in servicii).
   ===================================================================== */

USE master;
GO
IF DB_ID('MuzeuDB') IS NOT NULL
BEGIN
    ALTER DATABASE MuzeuDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE MuzeuDB;
END
GO
CREATE DATABASE MuzeuDB;
GO
USE MuzeuDB;
GO

/* =====================================================================
   1. PERSONAL SI UTILIZATORI
   ===================================================================== */

CREATE TABLE Angajati (
    IdAngajat       INT IDENTITY(1,1) CONSTRAINT PK_Angajati PRIMARY KEY,
    Nume            NVARCHAR(50)  NOT NULL,
    Prenume         NVARCHAR(50)  NOT NULL,
    Functie         NVARCHAR(30)  NOT NULL
        CONSTRAINT CK_Angajati_Functie
        CHECK (Functie IN ('Administrator', 'Curator', 'Ghid', 'Casier', 'Restaurator')),
    Email           NVARCHAR(100) NULL,
    Telefon         NVARCHAR(20)  NULL,
    DataAngajarii   DATE          NOT NULL CONSTRAINT DF_Angajati_DataAngajarii DEFAULT (CAST(GETDATE() AS DATE)),
    Activ           BIT           NOT NULL CONSTRAINT DF_Angajati_Activ DEFAULT (1)
);
GO

CREATE TABLE Utilizatori (
    IdUtilizator        INT IDENTITY(1,1) CONSTRAINT PK_Utilizatori PRIMARY KEY,
    NumeUtilizator      NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Utilizatori_Nume UNIQUE,
    ParolaHash          NVARCHAR(64)  NOT NULL,   -- hash SHA-256 al parolei (64 caractere hex), niciodata parola in clar!
    Rol                NVARCHAR(20)  NOT NULL
        CONSTRAINT CK_Utilizatori_Rol CHECK (Rol IN ('Admin', 'Curator', 'Casier', 'Ghid')),
    IdAngajat           INT NULL CONSTRAINT FK_Utilizatori_Angajati REFERENCES Angajati(IdAngajat),
    Activ               BIT NOT NULL CONSTRAINT DF_Utilizatori_Activ DEFAULT (1),
    UltimaAutentificare DATETIME NULL
);
GO

/* =====================================================================
   2. SPATII SI EXPOZITII
   ===================================================================== */

CREATE TABLE Sali (
    IdSala            INT IDENTITY(1,1) CONSTRAINT PK_Sali PRIMARY KEY,
    Denumire          NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Sali_Denumire UNIQUE,
    Etaj              INT           NOT NULL,
    Suprafata         DECIMAL(7,2)  NULL,      -- metri patrati
    CapacitateMaxima  INT           NOT NULL CONSTRAINT CK_Sali_Capacitate CHECK (CapacitateMaxima > 0)
);
GO

CREATE TABLE Expozitii (
    IdExpozitie      INT IDENTITY(1,1) CONSTRAINT PK_Expozitii PRIMARY KEY,
    Titlu            NVARCHAR(100) NOT NULL,
    Descriere        NVARCHAR(MAX) NULL,
    Tip              NVARCHAR(20)  NOT NULL
        CONSTRAINT CK_Expozitii_Tip CHECK (Tip IN ('Permanenta', 'Temporara', 'Itineranta')),
    DataInceput      DATE          NOT NULL,
    DataSfarsit      DATE          NULL,       -- NULL = fara data de final (permanenta)
    IdSala           INT           NOT NULL CONSTRAINT FK_Expozitii_Sali REFERENCES Sali(IdSala),
    IdCurator        INT           NULL     CONSTRAINT FK_Expozitii_Angajati REFERENCES Angajati(IdAngajat),
    PretSuplimentar  DECIMAL(8,2)  NOT NULL CONSTRAINT DF_Expozitii_Pret DEFAULT (0)
        CONSTRAINT CK_Expozitii_Pret CHECK (PretSuplimentar >= 0),
    CONSTRAINT CK_Expozitii_Date CHECK (DataSfarsit IS NULL OR DataSfarsit >= DataInceput)
);
GO

/* =====================================================================
   3. ARTEFACTE
   ===================================================================== */

CREATE TABLE Categorii (
    IdCategorie  INT IDENTITY(1,1) CONSTRAINT PK_Categorii PRIMARY KEY,
    Denumire     NVARCHAR(50) NOT NULL CONSTRAINT UQ_Categorii_Denumire UNIQUE
);
GO

CREATE TABLE Artefacte (
    IdArtefact       INT IDENTITY(1,1) CONSTRAINT PK_Artefacte PRIMARY KEY,
    CodInventar      NVARCHAR(20)  NOT NULL CONSTRAINT UQ_Artefacte_CodInventar UNIQUE,
    Denumire         NVARCHAR(100) NOT NULL,
    Descriere        NVARCHAR(MAX) NULL,
    Autor            NVARCHAR(100) NULL,
    Perioada         NVARCHAR(50)  NULL,       -- text liber: "sec. XV", "Neolitic"
    AnAproximativ    INT           NULL,       -- negativ = i.Hr.; util la sortare
    Material         NVARCHAR(100) NULL,
    Dimensiuni       NVARCHAR(50)  NULL,
    Provenienta      NVARCHAR(150) NULL,
    DataAchizitie    DATE          NULL,
    ValoareEstimata  DECIMAL(14,2) NULL,
    StareConservare  NVARCHAR(20)  NOT NULL CONSTRAINT DF_Artefacte_Stare DEFAULT ('Buna')
        CONSTRAINT CK_Artefacte_Stare
        CHECK (StareConservare IN ('Foarte buna', 'Buna', 'Satisfacatoare', 'Deteriorata')),
    IdCategorie      INT NOT NULL CONSTRAINT FK_Artefacte_Categorii REFERENCES Categorii(IdCategorie),
    IdExpozitie      INT NULL     CONSTRAINT FK_Artefacte_Expozitii REFERENCES Expozitii(IdExpozitie)
                                  -- NULL = artefactul se afla in depozit
);
GO

CREATE TABLE Restaurari (
    IdRestaurare   INT IDENTITY(1,1) CONSTRAINT PK_Restaurari PRIMARY KEY,
    IdArtefact     INT NOT NULL CONSTRAINT FK_Restaurari_Artefacte REFERENCES Artefacte(IdArtefact),
    IdRestaurator  INT NULL     CONSTRAINT FK_Restaurari_Angajati REFERENCES Angajati(IdAngajat),
    DataInceput    DATE NOT NULL,
    DataSfarsit    DATE NULL,                 -- NULL = restaurare in curs
    Descriere      NVARCHAR(500) NOT NULL,
    Cost           DECIMAL(10,2) NULL,
    CONSTRAINT CK_Restaurari_Date CHECK (DataSfarsit IS NULL OR DataSfarsit >= DataInceput)
);
GO

-- Istoricul mutarilor; se completeaza AUTOMAT de trigger-ul de mai jos.
-- Doua FK catre Expozitii -> in designer-ul LINQ to SQL se redenumesc
-- asocierile (ex. ExpozitieVeche / ExpozitieNoua).
CREATE TABLE IstoricLocatii (
    IdMutare          INT IDENTITY(1,1) CONSTRAINT PK_IstoricLocatii PRIMARY KEY,
    IdArtefact        INT NOT NULL CONSTRAINT FK_IstoricLocatii_Artefacte REFERENCES Artefacte(IdArtefact),
    IdExpozitieVeche  INT NULL     CONSTRAINT FK_IstoricLocatii_ExpVeche REFERENCES Expozitii(IdExpozitie),
    IdExpozitieNoua   INT NULL     CONSTRAINT FK_IstoricLocatii_ExpNoua  REFERENCES Expozitii(IdExpozitie),
    DataMutare        DATETIME NOT NULL CONSTRAINT DF_IstoricLocatii_Data DEFAULT (GETDATE())
);
GO

/* =====================================================================
   4. PROGRAMUL MUZEULUI
   ===================================================================== */

-- 1 = Luni ... 7 = Duminica
-- (in C#, DayOfWeek.Sunday = 0 -> se face conversia in cod)
CREATE TABLE ProgramSaptamanal (
    ZiSaptamana    TINYINT NOT NULL CONSTRAINT PK_ProgramSaptamanal PRIMARY KEY
        CONSTRAINT CK_Program_Zi CHECK (ZiSaptamana BETWEEN 1 AND 7),
    OraDeschidere  TIME(0) NULL,
    OraInchidere   TIME(0) NULL,
    Inchis         BIT NOT NULL CONSTRAINT DF_Program_Inchis DEFAULT (0),
    CONSTRAINT CK_Program_Ore CHECK (
        Inchis = 1 OR (OraDeschidere IS NOT NULL AND OraInchidere IS NOT NULL AND OraInchidere > OraDeschidere))
);
GO

-- Zile speciale: sarbatori, inchideri, program prelungit (Noaptea Muzeelor) etc.
CREATE TABLE ExceptiiProgram (
    IdExceptie     INT IDENTITY(1,1) CONSTRAINT PK_ExceptiiProgram PRIMARY KEY,
    Data           DATE NOT NULL CONSTRAINT UQ_ExceptiiProgram_Data UNIQUE,
    OraDeschidere  TIME(0) NULL,
    OraInchidere   TIME(0) NULL,
    Inchis         BIT NOT NULL CONSTRAINT DF_Exceptii_Inchis DEFAULT (1),
    Motiv          NVARCHAR(100) NOT NULL,
    CONSTRAINT CK_Exceptii_Ore CHECK (
        Inchis = 1 OR (OraDeschidere IS NOT NULL AND OraInchidere IS NOT NULL AND OraInchidere > OraDeschidere))
);
GO

/* =====================================================================
   5. VIZITATORI, BILETE, VIZITE, TURURI
   ===================================================================== */

CREATE TABLE Vizitatori (
    IdVizitator       INT IDENTITY(1,1) CONSTRAINT PK_Vizitatori PRIMARY KEY,
    Nume              NVARCHAR(50)  NOT NULL,
    Prenume           NVARCHAR(50)  NOT NULL,
    Email             NVARCHAR(100) NULL,
    Telefon           NVARCHAR(20)  NULL,
    DataInregistrare  DATETIME NOT NULL CONSTRAINT DF_Vizitatori_Data DEFAULT (GETDATE())
);
GO

CREATE TABLE TipuriBilet (
    IdTipBilet  INT IDENTITY(1,1) CONSTRAINT PK_TipuriBilet PRIMARY KEY,
    Denumire    NVARCHAR(50) NOT NULL CONSTRAINT UQ_TipuriBilet_Denumire UNIQUE,
    Pret        DECIMAL(8,2) NOT NULL CONSTRAINT CK_TipuriBilet_Pret CHECK (Pret >= 0),
    Activ       BIT NOT NULL CONSTRAINT DF_TipuriBilet_Activ DEFAULT (1)
);
GO

CREATE TABLE TururiGhidate (
    IdTur                INT IDENTITY(1,1) CONSTRAINT PK_TururiGhidate PRIMARY KEY,
    IdExpozitie          INT NOT NULL CONSTRAINT FK_Tururi_Expozitii REFERENCES Expozitii(IdExpozitie),
    IdGhid               INT NOT NULL CONSTRAINT FK_Tururi_Angajati  REFERENCES Angajati(IdAngajat),
    DataOraInceput       DATETIME NOT NULL,
    DurataMinute         INT NOT NULL CONSTRAINT CK_Tururi_Durata CHECK (DurataMinute BETWEEN 15 AND 240),
    Limba                NVARCHAR(30) NOT NULL CONSTRAINT DF_Tururi_Limba DEFAULT ('Romana'),
    NrMaximParticipanti  INT NOT NULL CONSTRAINT CK_Tururi_NrMax CHECK (NrMaximParticipanti > 0),
    PretSuplimentar      DECIMAL(8,2) NOT NULL CONSTRAINT DF_Tururi_Pret DEFAULT (0)
);
GO

-- O vizita = o rezervare / o vanzare la casa. Poate contine mai multe bilete.
CREATE TABLE Vizite (
    IdVizita     INT IDENTITY(1,1) CONSTRAINT PK_Vizite PRIMARY KEY,
    IdVizitator  INT NULL CONSTRAINT FK_Vizite_Vizitatori REFERENCES Vizitatori(IdVizitator),
                 -- NULL = vizitator anonim (bilet cumparat direct la casa)
    DataVizita   DATETIME NOT NULL,
    IdTur        INT NULL CONSTRAINT FK_Vizite_Tururi   REFERENCES TururiGhidate(IdTur),
    IdCasier     INT NULL CONSTRAINT FK_Vizite_Angajati REFERENCES Angajati(IdAngajat),
    Status       NVARCHAR(20) NOT NULL CONSTRAINT DF_Vizite_Status DEFAULT ('Rezervata')
        CONSTRAINT CK_Vizite_Status CHECK (Status IN ('Rezervata', 'Confirmata', 'Efectuata', 'Anulata')),
    DataCreare   DATETIME NOT NULL CONSTRAINT DF_Vizite_DataCreare DEFAULT (GETDATE()),
    Observatii   NVARCHAR(200) NULL
);
GO

CREATE TABLE Bilete (
    IdBilet      INT IDENTITY(1,1) CONSTRAINT PK_Bilete PRIMARY KEY,
    IdVizita     INT NOT NULL CONSTRAINT FK_Bilete_Vizite      REFERENCES Vizite(IdVizita),
    IdTipBilet   INT NOT NULL CONSTRAINT FK_Bilete_TipuriBilet REFERENCES TipuriBilet(IdTipBilet),
    IdExpozitie  INT NULL     CONSTRAINT FK_Bilete_Expozitii   REFERENCES Expozitii(IdExpozitie),
                 -- NULL = acces general; altfel bilet pentru o expozitie temporara
    PretAchitat  DECIMAL(8,2) NOT NULL CONSTRAINT CK_Bilete_Pret CHECK (PretAchitat >= 0)
                 -- se salveaza pretul de la momentul vanzarii (preturile se pot schimba)
);
GO

CREATE TABLE Feedback (
    IdFeedback    INT IDENTITY(1,1) CONSTRAINT PK_Feedback PRIMARY KEY,
    IdVizita      INT NOT NULL CONSTRAINT FK_Feedback_Vizite    REFERENCES Vizite(IdVizita),
    IdExpozitie   INT NULL     CONSTRAINT FK_Feedback_Expozitii REFERENCES Expozitii(IdExpozitie),
    Nota          TINYINT NOT NULL CONSTRAINT CK_Feedback_Nota CHECK (Nota BETWEEN 1 AND 5),
    Comentariu    NVARCHAR(500) NULL,
    DataFeedback  DATETIME NOT NULL CONSTRAINT DF_Feedback_Data DEFAULT (GETDATE())
);
GO

/* =====================================================================
   6. TRIGGER: logheaza automat mutarea unui artefact intre expozitii
   SET NOCOUNT ON este obligatoriu: altfel LINQ to SQL primeste un numar
   gresit de randuri afectate si arunca ChangeConflictException.
   ===================================================================== */

CREATE TRIGGER trg_Artefacte_IstoricLocatii
ON Artefacte
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(IdExpozitie)
        INSERT INTO IstoricLocatii (IdArtefact, IdExpozitieVeche, IdExpozitieNoua, DataMutare)
        SELECT i.IdArtefact, d.IdExpozitie, i.IdExpozitie, GETDATE()
        FROM inserted i
        JOIN deleted d ON d.IdArtefact = i.IdArtefact
        WHERE ISNULL(i.IdExpozitie, -1) <> ISNULL(d.IdExpozitie, -1);
END
GO

/* =====================================================================
   7. VIEW-URI SI PROCEDURI STOCATE (pentru rapoarte)
   In LINQ to SQL: view-urile se trag in designer ca tabelele (read-only),
   procedurile se trag in panoul din dreapta si devin metode pe DataContext.
   ===================================================================== */

CREATE VIEW vw_StatisticiExpozitii
AS
SELECT
    e.IdExpozitie,
    e.Titlu,
    e.Tip,
    s.Denumire AS Sala,
    (SELECT COUNT(*) FROM Artefacte a WHERE a.IdExpozitie = e.IdExpozitie) AS NrArtefacte,
    (SELECT COUNT(*)
       FROM Bilete b JOIN Vizite v ON v.IdVizita = b.IdVizita
      WHERE b.IdExpozitie = e.IdExpozitie AND v.Status <> 'Anulata') AS NrBileteVandute,
    (SELECT AVG(CAST(f.Nota AS DECIMAL(4,2)))
       FROM Feedback f WHERE f.IdExpozitie = e.IdExpozitie) AS NotaMedie
FROM Expozitii e
JOIN Sali s ON s.IdSala = e.IdSala;
GO

CREATE VIEW vw_ArtefacteDetaliat
AS
SELECT
    a.IdArtefact, a.CodInventar, a.Denumire, a.Autor, a.Perioada, a.StareConservare,
    c.Denumire AS Categorie,
    ISNULL(e.Titlu, N'Depozit') AS Locatie,
    CAST(CASE WHEN EXISTS (SELECT 1 FROM Restaurari r
                           WHERE r.IdArtefact = a.IdArtefact AND r.DataSfarsit IS NULL)
              THEN 1 ELSE 0 END AS BIT) AS InRestaurare
FROM Artefacte a
JOIN Categorii c      ON c.IdCategorie = a.IdCategorie
LEFT JOIN Expozitii e ON e.IdExpozitie = a.IdExpozitie;
GO

CREATE PROCEDURE sp_VenituriPerioada
    @DataStart   DATE,
    @DataSfarsit DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        CAST(v.DataVizita AS DATE) AS Zi,
        COUNT(b.IdBilet)           AS NrBilete,
        SUM(b.PretAchitat)         AS Venit
    FROM Vizite v
    JOIN Bilete b ON b.IdVizita = v.IdVizita
    WHERE v.Status <> 'Anulata'
      AND CAST(v.DataVizita AS DATE) BETWEEN @DataStart AND @DataSfarsit
    GROUP BY CAST(v.DataVizita AS DATE)
    ORDER BY Zi;
END
GO

-- Returneaza 1 daca muzeul e deschis la data/ora data (tine cont de exceptii)
CREATE FUNCTION fn_EsteDeschis (@Moment DATETIME)
RETURNS BIT
AS
BEGIN
    DECLARE @Zi DATE = CAST(@Moment AS DATE);
    DECLARE @Ora TIME(0) = CAST(@Moment AS TIME(0));
    DECLARE @Rezultat BIT = 0;

    IF EXISTS (SELECT 1 FROM ExceptiiProgram WHERE Data = @Zi)
        SELECT @Rezultat = CASE WHEN Inchis = 0 AND @Ora >= OraDeschidere AND @Ora < OraInchidere
                                THEN 1 ELSE 0 END
        FROM ExceptiiProgram WHERE Data = @Zi;
    ELSE
        -- (DATEPART(WEEKDAY) depinde de DATEFIRST, de aceea calculam independent: 1 = Luni)
        SELECT @Rezultat = CASE WHEN Inchis = 0 AND @Ora >= OraDeschidere AND @Ora < OraInchidere
                                THEN 1 ELSE 0 END
        FROM ProgramSaptamanal
        WHERE ZiSaptamana = (DATEDIFF(DAY, '19000101', @Zi) % 7) + 1;   -- 1900-01-01 a fost Luni

    RETURN @Rezultat;
END
GO

/* =====================================================================
   8. DATE DE TEST
   ===================================================================== */

INSERT INTO Angajati (Nume, Prenume, Functie, Email, Telefon, DataAngajarii, Activ) VALUES
(N'Popescu',   N'Andrei',  'Administrator', N'andrei.popescu@muzeu.ro', N'0721000001', '2018-03-01', 1),
(N'Ionescu',   N'Maria',   'Curator',       N'maria.ionescu@muzeu.ro',  N'0721000002', '2019-09-15', 1),
(N'Dumitru',   N'Elena',   'Ghid',          N'elena.dumitru@muzeu.ro',  N'0721000003', '2021-05-10', 1),
(N'Stan',      N'Mihai',   'Casier',        N'mihai.stan@muzeu.ro',     N'0721000004', '2022-01-20', 1),
(N'Georgescu', N'Ioana',   'Restaurator',   N'ioana.georgescu@muzeu.ro',N'0721000005', '2020-11-02', 1);

INSERT INTO Sali (Denumire, Etaj, Suprafata, CapacitateMaxima) VALUES
(N'Sala Mare',          0, 450.00, 150),
(N'Sala de Arta Veche', 1, 220.50,  60),
(N'Sala Temporara',     1, 180.00,  50);

INSERT INTO Expozitii (Titlu, Descriere, Tip, DataInceput, DataSfarsit, IdSala, IdCurator, PretSuplimentar) VALUES
(N'Istoria Daciei',              N'Obiecte din perioada daco-romana.',          'Permanenta', '2015-01-01', NULL,         1, 2,  0),
(N'Pictura romaneasca sec. XIX', N'Lucrari ale maestrilor romani din sec. XIX.', 'Permanenta', '2017-06-01', NULL,         2, 2,  0),
(N'Ceramica de Cucuteni',        N'Expozitie temporara de ceramica neolitica.',  'Temporara',  '2026-10-01', '2027-01-31', 3, 2, 15);

INSERT INTO Categorii (Denumire) VALUES
(N'Arheologie'), (N'Pictura'), (N'Sculptura'), (N'Ceramica'), (N'Numismatica');

INSERT INTO Artefacte (CodInventar, Denumire, Descriere, Autor, Perioada, AnAproximativ, Material, Dimensiuni,
                       Provenienta, DataAchizitie, ValoareEstimata, StareConservare, IdCategorie, IdExpozitie) VALUES
('INV-0001', N'Bratara dacica spiralata', N'Bratara din aur cu capete de animale.', NULL, N'sec. I i.Hr.', -50, N'Aur', N'12 cm', N'Sarmizegetusa Regia', '2011-04-12', 250000, 'Foarte buna', 1, 1),
('INV-0002', N'Coif de lupta',            N'Coif ceremonial.',                       NULL, N'sec. IV i.Hr.', -400, N'Argint', N'25 x 20 cm', N'Descoperire arheologica', '2009-07-01', 400000, 'Buna', 1, 1),
('INV-0003', N'Moneda Koson',             N'Moneda de aur tip Koson.',               NULL, N'sec. I i.Hr.', -44, N'Aur', N'2 cm', N'Donatie', '2016-02-20', 8000, 'Buna', 5, 1),
('INV-0004', N'Car cu boi',               N'Ulei pe panza.',                         N'Nicolae Grigorescu', N'sec. XIX', 1899, N'Ulei pe panza', N'60 x 90 cm', N'Achizitie licitatie', '2014-10-05', 900000, 'Buna', 2, 2),
('INV-0005', N'Portret de femeie',        N'Ulei pe panza.',                         N'Theodor Aman', N'sec. XIX', 1870, N'Ulei pe panza', N'50 x 40 cm', N'Donatie', '2012-03-14', 300000, 'Satisfacatoare', 2, 2),
('INV-0006', N'Vas pictat Cucuteni A',    N'Vas ceramic cu decor spiralat.',         NULL, N'Neolitic', -4200, N'Ceramica', N'35 cm', N'Sit Cucuteni', '2005-08-30', 120000, 'Buna', 4, 3),
('INV-0007', N'Figurina antropomorfa',    N'Figurina feminina stilizata.',           NULL, N'Neolitic', -4000, N'Lut ars', N'11 cm', N'Sit Cucuteni', '2005-08-30', 60000, 'Deteriorata', 4, NULL),
('INV-0008', N'Bust de marmura',          N'Bust roman.',                            NULL, N'sec. II d.Hr.', 150, N'Marmura', N'45 cm', N'Tomis', '2010-05-18', 200000, 'Buna', 3, NULL);

INSERT INTO Restaurari (IdArtefact, IdRestaurator, DataInceput, DataSfarsit, Descriere, Cost) VALUES
(5, 5, '2025-02-01', '2025-04-15', N'Curatare si consolidare strat pictural.', 12000),
(7, 5, '2026-10-20', NULL,         N'Lipire fragmente si consolidare.',          NULL);

-- 1 = Luni (inchis), 2..7 = Marti..Duminica
INSERT INTO ProgramSaptamanal (ZiSaptamana, OraDeschidere, OraInchidere, Inchis) VALUES
(1, NULL,    NULL,    1),
(2, '10:00', '18:00', 0),
(3, '10:00', '18:00', 0),
(4, '10:00', '20:00', 0),
(5, '10:00', '18:00', 0),
(6, '10:00', '19:00', 0),
(7, '10:00', '17:00', 0);

INSERT INTO ExceptiiProgram (Data, OraDeschidere, OraInchidere, Inchis, Motiv) VALUES
('2026-12-01', NULL,    NULL,    1, N'Ziua Nationala'),
('2026-12-25', NULL,    NULL,    1, N'Craciun'),
('2027-05-15', '18:00', '23:59', 0, N'Noaptea Muzeelor');

INSERT INTO TipuriBilet (Denumire, Pret, Activ) VALUES
(N'Adult',     30, 1),
(N'Elev/Student', 10, 1),
(N'Pensionar', 15, 1),
(N'Copil sub 7 ani', 0, 1);

INSERT INTO Vizitatori (Nume, Prenume, Email, Telefon, DataInregistrare) VALUES
(N'Marin',   N'Ana',    N'ana.marin@email.ro',   N'0740000001', '2026-10-02'),
(N'Radu',    N'Vlad',   N'vlad.radu@email.ro',   N'0740000002', '2026-10-15'),
(N'Constantinescu', N'Irina', NULL,             N'0740000003', '2026-11-01');

INSERT INTO TururiGhidate (IdExpozitie, IdGhid, DataOraInceput, DurataMinute, Limba, NrMaximParticipanti, PretSuplimentar) VALUES
(1, 3, '2026-11-14 11:00', 60, 'Romana',  20, 20),
(3, 3, '2026-11-15 14:00', 45, 'Engleza', 15, 25);

INSERT INTO Vizite (IdVizitator, DataVizita, IdTur, IdCasier, Status, DataCreare, Observatii) VALUES
(1,    '2026-10-05 12:30', NULL, 4, 'Efectuata', '2026-10-05 12:25', NULL),
(NULL, '2026-10-05 15:10', NULL, 4, 'Efectuata', '2026-10-05 15:05', N'Vanzare la casa'),
(2,    '2026-11-14 11:00', 1,    NULL, 'Rezervata', '2026-11-08 09:00', N'Rezervare online'),
(3,    '2026-11-03 10:30', NULL, 4, 'Anulata',   '2026-11-01 18:00', N'Anulata de vizitator');

INSERT INTO Bilete (IdVizita, IdTipBilet, IdExpozitie, PretAchitat) VALUES
(1, 1, NULL, 30),
(1, 1, 3,    45),   -- adult + supliment expozitie temporara
(2, 2, NULL, 10),
(2, 4, NULL,  0),
(3, 1, NULL, 50),   -- adult + tur ghidat
(3, 2, NULL, 30),
(4, 3, NULL, 15);

INSERT INTO Feedback (IdVizita, IdExpozitie, Nota, Comentariu, DataFeedback) VALUES
(1, 1, 5, N'Foarte frumos organizata!', '2026-10-05 16:00'),
(1, 3, 4, N'Ceramica superba, ar fi utile mai multe explicatii.', '2026-10-05 16:05');

-- Test trigger: mutam figurina din depozit in expozitia temporara (apare in IstoricLocatii)
UPDATE Artefacte SET IdExpozitie = 1 WHERE CodInventar = 'INV-0008';
GO

SELECT * FROM IstoricLocatii

-- Verificari rapide
SELECT * FROM vw_StatisticiExpozitii;
SELECT * FROM vw_ArtefacteDetaliat;
SELECT * FROM IstoricLocatii;
EXEC sp_VenituriPerioada '2026-10-01', '2026-11-30';
SELECT dbo.fn_EsteDeschis('2026-11-17 12:00') AS DeschisMarti,   -- 1
       dbo.fn_EsteDeschis('2026-11-16 12:00') AS DeschisLuni,    -- 0
       dbo.fn_EsteDeschis('2026-12-01 12:00') AS Deschis1Dec;    -- 0
GO