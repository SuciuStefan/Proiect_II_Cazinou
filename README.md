# CasinoApp (Proiect II Cazinou)

Aplicație web de tip cazinou online dezvoltată în cadrul disciplinei Ingineria Programării. Proiectul implementează o arhitectură decuplată pe straturi (N-Layer) folosind tehnologii din ecosistemul .NET.

##  Stack Tehnologic

* **Backend:** .NET 10 / ASP.NET Core Web API
* **Frontend:** Blazor Server (mod de interactivitate: `InteractiveServer`)
* **Bază de date:** SQLite
* **Acces la date:** ADO.NET (interogări SQL native) + Repository Pattern
* **Stilizare:** CSS3 Custom (Fonturi: Bebas Neue, Rajdhani)

##  Structura Soluției

Proiectul este împărțit în 5 straturi pentru a asigura separarea responsabilităților:

* **CasinoApp.DataAccess:** Conține entitățile bazei de date, clasa de configurare a conexiunii (`DbManager`), scriptul de seed/inițializare și clasele de tip Repository (`PlayerRepository`, `BetRepository`, `TransactionRepository`).
* **CasinoApp.BusinessLogic:** Gestionează regulile de calcul pentru jocuri, mize și validările intermediare.
* **CasinoApp.API:** Expune endpoint-urile REST folosite pentru comunicarea dintre baza de date și interfața grafică.
* **CasinoApp.Web:** Serverul Blazor principal care gestionează layout-ul și paginile aplicației.
* **CasinoApp.Web.Client:** Componentele interactive randate pe partea de client.

##  Instalare și Rulare Locală

Pentru ca aplicația să funcționeze corect, este necesară pornirea simultană a API-ului și a serverului Web.

### 1. Clonarea repository-ului
```bash
git clone [https://github.com/SuciuStefan/Proiect_II_Cazinou.git](https://github.com/SuciuStefan/Proiect_II_Cazinou.git)
cd Proiect_II_Cazinou
```

### 2. Configurarea pornirii multiple în Visual Studio
1. Deschide soluția în Visual Studio.
2. Click dreapta pe **Solution 'CasinoApp'** în Solution Explorer și selectează **Properties**.
3. Mergi la **Startup Project** și bifează **Multiple startup projects**.
4. Setează acțiunea pe **Start** pentru următoarele două proiecte:
   * `CasinoApp.API`
   * `CasinoApp.Web`
5. Apasă **Apply**, apoi rulează soluția (apasă **F5** sau butonul *Start*).

### 3. Inițializarea bazei de date
Aplicația folosește o clasă de tip `DatabaseInitializer`. La prima rulare a proiectului, dacă fișierul `casino.db` nu este detectat în folderul de pornire, sistemul va genera automat fișierul SQLite și structura tabelelor aferente.
