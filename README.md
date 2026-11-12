SETUP PROIECT MUZEU - pași pentru partea de frontend (WPF)
=========================================================

Ce trebuie să ai instalat înainte
---------------------------------
- Visual Studio (2022 sau mai nou)
- SQL Server Express, cu numele de instanță implicit: SQLEXPRESS
- SQL Server Management Studio (SSMS)

Dacă nu ai SQL Server Express, îl descarci de pe site-ul Microsoft (caută "SQL Server Express download").
La instalare alegi varianta Basic și lași numele instanței SQLEXPRESS.


Pasul 0: Componentele din Visual Studio (o singură dată)
--------------------------------------------------------
Deschide Visual Studio Installer → Modify:
- În tab-ul Workloads, bifezi .NET desktop development.
- În tab-ul Individual components, cauți și bifezi LINQ to SQL tools și .NET Framework 4.8 targeting pack.
Apeși Modify și aștepți să se instaleze.


Pasul 1: Descarci proiectul de pe GitHub
----------------------------------------
1. Acceptă invitația de Collaborator pe care ți-am trimis-o pe GitHub (vine pe mail).
2. În Visual Studio, pe ecranul de start, alegi Clone a repository.
3. La Repository location pui: <LINK-UL REPOSITORY-ULUI>
4. Alegi un folder pe calculatorul tău și apeși Clone.
5. Dacă nu se deschide automat, deschizi fișierul MuzeuApp.sln.


Pasul 2: Creezi baza de date în SSMS
------------------------------------
1. Deschide SSMS. În fereastra Connect to Server completezi:
   - Server type: Database Engine
   - Server name: .\SQLEXPRESS
   - Authentication: Windows Authentication
   - Dacă ai câmpul Encryption (în SSMS 20 și mai nou), bifezi Trust server certificate.
2. Apeși Connect.
3. File → Open → File și deschizi scriptul Database\SQLQueryMuzeu.sql din folderul proiectului.
4. Apeși Execute (sau F5).

Ca să verifici că a mers:
- În tab-ul Messages nu ar trebui să apară text roșu.
- În tab-ul Results ar trebui să vezi 5 tabele de rezultate. Ultimul trebuie să arate 1, 0, 0
  (marți deschis, luni închis, 1 decembrie închis).
- În stânga, click dreapta pe Databases → Refresh. Acolo trebuie să apară MuzeuDB.

Dacă SSMS nu se poate conecta la .\SQLEXPRESS:
- Apasă Win+R, scrie services.msc și caută serviciul "SQL Server (SQLEXPRESS)".
  Dacă e oprit, click dreapta → Start.
- Dacă serviciul nu există deloc, înseamnă că SQL Server Express nu e instalat (vezi începutul).


Pasul 3: Verifici că backend-ul merge la tine
---------------------------------------------
1. În Solution Explorer, click dreapta pe Muzeu.ConsoleTest → Set as Startup Project.
2. Apeși F5. Ar trebui să vezi:
   - cele 3 expoziții (Istoria Daciei, Pictura romaneasca sec. XIX, Ceramica de Cucuteni)
   - Numar artefacte: 8
   - veniturile pe octombrie-noiembrie
   - "Deschis marti 17.11.2026 la 12:00? True"
Dacă apare asta, totul e configurat corect.


Pasul 4: Creezi proiectul WPF
-----------------------------
1. Click dreapta pe Solution 'MuzeuApp' → Add → New Project → WPF App (.NET Framework).
   Atenție să fie varianta cu (.NET Framework), nu WPF App simplu.
2. Name: Muzeu.WPF, Framework: .NET Framework 4.8.
3. Click dreapta pe References (sub Muzeu.WPF) → Add Reference:
   - la Projects bifezi Muzeu.Backend
   - la Assemblies bifezi System.Data.Linq
4. Click dreapta pe Muzeu.WPF → Set as Startup Project.
5. Deschide App.config din Muzeu.WPF și adaugi blocul <connectionStrings>, astfel încât fișierul să arate așa:

<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <connectionStrings>
    <add name="MuzeuDB"
         connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=MuzeuDB;Integrated Security=True"
         providerName="System.Data.SqlClient" />
  </connectionStrings>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" />
  </startup>
</configuration>

Numele trebuie să fie exact "MuzeuDB".


Pasul 5 (opțional): Vezi baza din Visual Studio
-----------------------------------------------
Nu e necesar ca să meargă aplicația, dar e util ca să te uiți la date fără să deschizi SSMS.
1. View → Server Explorer.
2. Click dreapta pe Data Connections → Add Connection.
3. La Data source trebuie să fie Microsoft SQL Server (SqlClient). Dacă scrie altceva, apeși Change și îl alegi.
4. Server name: .\SQLEXPRESS, Authentication: Windows Authentication
   (dacă apare opțiunea Trust server certificate, o bifezi).
5. Select or enter a database name: MuzeuDB → Test Connection → OK.


Reguli pentru tot proiectul
---------------------------
- DataContext-ul se creează mereu prin DbFactory.Create(), niciodată cu new MuzeuDBDataContext() fără parametri.
  Constructorul fără parametri folosește connection string-ul salvat de designer pe calculatorul meu,
  care la tine s-ar putea să nu meargă.
- Nu modifica fișierul MuzeuDB.dbml și nici scriptul SQL. Dacă ai nevoie de o coloană sau de un tabel nou,
  spune-mi și le modific eu în ambele locuri, ca să rămână sincronizate.
- Lucrează pe branch-ul frontend, nu direct pe main.