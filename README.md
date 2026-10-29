# Golf Seasons - Unity AI & Gameplay Project

<details open>
<summary>English</summary>

**Golf Seasons** is a 3D mini-golf game developed in Unity that integrates a complex physics system, database-driven progression (SQLite), and an advanced Artificial Intelligence system using **Unity ML-Agents**.

Beyond the classic gameplay experience, this project utilizes AI agents as an **Automated Quality Assurance (QA)** system, capable of discovering physics bugs, progression errors (softlocks), and logging these issues into databases for further debugging.

### Main Features

#### Gameplay & Physics

* **Custom Physics System:** Advanced control over ball friction and behavior (`Bila.cs`) based on the surface type (Sand, Ice, Puddles, Tubes).
* **Seasons and Courses:** The game is divided into 4 thematic seasons (Spring, Summer, Autumn, Winter), each with specific levels and conditions (Par Strokes, Par Time).
* **Flexible Cameras:** Support for a dedicated player camera (`CameraRig.cs`) and a free/spectator camera for monitoring AI training sessions (`CameraAntrenament.cs`).

#### Artificial Intelligence (Unity ML-Agents)

* **Intelligent Agent (`AgentG.cs`):** An agent capable of learning to play levels using Reinforcement Learning algorithms.
* **Automated Bug Detection:** The agent doesn't just play; it validates levels. It can automatically detect:
  * *Tunneling* (the ball passing through walls at high speeds).
  * *Out of Bounds* (falling out of the map boundaries).
  * *Softlocks* (the ball getting stuck with no way to progress).
  * *UI/DB Desyncs* and irregularities in the hole progression order.
* **Dedicated Training Menu:** The game includes a UI control panel (`MeniuAntrenare.cs`) that automatically downloads Python, configures the virtual environment (`venv`), installs necessary PyTorch/ML-Agents packages (including CUDA GPU support), and manages the `.bat` background processes.

#### Databases (SQLite)

* **Progress and Scoreboard:** Player and AI progress, time records, and stroke counts are saved in a SQLite database (`db_golfseasons.db`).
* **AI Log System (`LoguriAI.cs`):** Any bug discovered by the AI during dozens of concurrently running instances is logged in `.csv` files, which are later imported into a separate database (`LoguriAI.db`) using an external C# script.

### Technologies Used

* **Game Engine:** Unity 3D
* **Language:** C#
* **Machine Learning:** Unity ML-Agents (PyTorch, Python)
* **Databases:** Mono.Data.Sqlite
* **UI:** Unity UI Toolkit (`UIDocument`) and TextMeshPro

### How to Run AI Training

1. Open the game and navigate to the **Training Menu**.
2. Choose whether you want the automatic installation of dependencies (Recommended for the first run).
3. Select the target architecture (CPU / GTX & RTX 2000-4000 series GPU / RTX 5000+ Series GPU).
4. Enter a `Run ID` and the number of concurrent instances (e.g., 10 instances).
5. The game will run a custom Batch script that starts the Python ML-Agents server and launches headless instances (without graphics, for maximum performance).

</details>

<details>
<summary>Romana</summary>

**Golf Seasons** este un joc 3D de mini-golf dezvoltat in Unity, care integreaza un sistem complex de fizica, progresie bazata pe baze de date (SQLite) si un sistem avansat de Inteligenta Artificiala folosind **Unity ML-Agents**.

Dincolo de experienta clasica de gameplay, acest proiect utilizeaza agenti AI ca un sistem de **Automated Quality Assurance (QA)**, capabil sa descopere bug-uri de fizica, erori de progresie (softlocks) si sa le inregistreze in baze de date pentru depanare ulterioara.

### Functionalitati Principale

#### Gameplay & Fizica

* **Sistem de Fizica Custom:** Control avansat asupra frecarii si comportamentului bilei (`Bila.cs`) in functie de tipul suprafetei (Nisip, Gheata, Balti, Tuburi).
* **Sezoane si Trasee:** Jocul este impartit in 4 sezoane tematice (Primavara, Vara, Toamna, Iarna), fiecare cu nivele si conditii specifice (Par Lovituri, Par Timp).
* **Camere Flexibile:** Suport pentru o camera dedicata jucatorului (`CameraRig.cs`) si o camera libera/spectator pentru monitorizarea sesiunilor de antrenament AI (`CameraAntrenament.cs`).

#### Inteligenta Artificiala (Unity ML-Agents)

* **Agent Inteligent (`AgentG.cs`):** Agent capabil sa invete sa parcurga traseele folosind algoritmi de Reinforcement Learning.
* **Detectie Automata de Bug-uri:** Agentul nu doar joaca, ci si valideaza nivelurile. Poate detecta automat:
  * *Tunneling* (trecerea bilei prin pereti la viteze mari).
  * *Out of Bounds* (caderea in afara limitelor hartii).
  * *Softlocks* (blocarea bilei fara posibilitate de progres).
  * *Desincronizari UI/DB* si incalcari ale ordinii traseelor.
* **Meniu Dedicat pentru Antrenament:** Jocul include un panou de control UI (`MeniuAntrenare.cs`) care descarca automat Python, configureaza mediul virtual (`venv`), instaleaza pachetele necesare PyTorch/ML-Agents (inclusiv suport GPU CUDA) si gestioneaza procesele de fundal `.bat`.

#### Baze de Date (SQLite)

* **Progres si Scoreboard:** Progresul jucatorului si al inteligentei artificiale, recordurile de timp si lovituri sunt salvate intr-o baza de date SQLite (`db_golfseasons.db`).
* **Sistem de Loguri AI (`LoguriAI.cs`):** Orice bug descoperit de AI in timpul rularii zecilor de instante in paralel este inregistrat in fisiere `.csv`, care ulterior sunt importate intr-o baza de date separata (`LoguriAI.db`) folosind un script C# extern.

### Tehnologii Utilizate

* **Motor Grafic:** Unity 3D
* **Limbaj:** C#
* **Machine Learning:** Unity ML-Agents (PyTorch, Python)
* **Baze de Date:** Mono.Data.Sqlite
* **Interfata (UI):** Unity UI Toolkit (`UIDocument`) si TextMeshPro

### Cum se ruleaza Antrenamentul AI

1. Deschideti jocul si navigati la meniul de **Antrenare**.
2. Alegeti daca doriti instalarea automata a dependentelor (Recomandat la prima rulare).
3. Selectati arhitectura dorita (CPU / GPU seria GTX & RTX 2000-4000 / GPU seria RTX 5000+).
4. Introduceti un `Run ID` si numarul de instante paralele (ex. 10 instante).
5. Jocul va rula un script Batch care porneste serverul ML-Agents si lanseaza instantele "headless" (fara grafica, pentru performanta maxima).

</details>
