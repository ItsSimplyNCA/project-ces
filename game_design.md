# COGITO ERGO SUM — Game Design

## 1. Dokumentum és core vision

A Unity factory-game aktuális projektkontextusa, a rendelkezésre álló korai beszélgetés és az ebben a chatben megerősített döntések alapján. A legutóbbi döntések az irányadók. Az 1–11. fejezet a design specifikációja; a 12. fejezet külön dátumozott fejlesztési állapotjelentés, amely nem írja felül a design szabályait. Példaszámok nem végleges balance-értékek. **TBD / Future Design** pontokhoz ne találjunk ki végleges szabályokat.

Önálló 3D factory-game, mély anyagfeldolgozással, gyártási láncokkal és technológiai fejlődéssel. A workbench korlátai tudatosan ösztönzik a korai gépi automatizálást.

| Inspiráció | Szerep |
| --- | --- |
| FOUNDRY | Building feel, grid, fix egyben lerakható gépek, később in-world panelek |
| Factorio / FOUNDRY | Előre craftolt inventory-itemből építés |
| GregTech | Materials, recipes, géptierek, item/fluid pipes |
| Satisfactory | Bufferek, power/breaker szemlélet, távoli factory simulation |
| AE2 | Későbbi digital storage és valódi gépekkel működő autocrafting |
| Eden Crafters | Alakítható voxelterep, tereprendezés és bányászati szabadság |

Jelenleg single-player. Survival és multiplayer lehetséges későbbi bővítések. Nincs blokkonként épített multiblock, wall sharing vagy feszültségtierű elektromos rendszer.

## 2. Machines és recipes

A gépek fix, egyben lerakható egységek. Több gridcellát foglalhatnak, de nem casing/controller/hatch részekből épülnek. A méret utolsó értéke a magasság: 2×2×3 = három blokk magas.

- A player a machine GUI-jában választ receptet.
- Receptek adott tierekhez kötöttek, magasabb tier visszafelé kompatibilis.
- Egyszerre egy crafting folyamat, nincs párhuzamos processing.
- Indulás előtt minden input és minden outputhoz elegendő hely szükséges.
- Inputok induláskor levonódnak, folytatáskor nem vonódnak le újra.
- Outputok garantáltak, esélyalapú melléktermék nincs. Több garantált output lehet.
- Energiahiány/kézi OFF szüneteltet, progress megmarad.
- Receptváltáskor az aktív régi folyamat befejeződik, újabb régi ciklus nem indul.
- Inputfogadás azonnal az új kiválasztott receptet követi, miközben a régi folyamat még dolgozik.
- Aktív és következő ciklusra kiválasztott recept külön állapot.
- GUI ON/OFF csak craftingot állít le; input/output logisztika tovább működik.

Magasabb tier gyorsítja az alacsonyabb tierű recepteket, jelenleg változatlan MW/s-igénnyel. Emiatt a ciklus összenergiája csökken. Időszorzók későbbi balance-értékek. Energia-szorzó fejlesztői konfigurációban később változtatható, jelenleg 1.

### Bufferek

Inputba csak a kiválasztott recept szükséges item/fluid típusai kerülhetnek, kézzel és hálózatból is. Recept nélkül semmi.

Fix számú input és output slot/tartály. Itemtípusonként egy slot, egy teljes stack kapacitással. Fluid input/output tartálykapacitás géptierfüggő. Több ciklusra előre tölthető. Egy input port több típust fogadhat, külön belső slotokba/tartályokba.

Receptváltáskor régi input/output bent marad és helyet foglal. Iteminputot player kézzel veszi ki. Maradék inputfluid GUI **Empty / Flush** művelettel, megerősítés után törölhető; elvész. Outputba kizárólag a gép saját gyártása tölthet. Régi output blokkolhatja az új recept indulását.

**TBD / Future Design:** gép/recipe listák, slotszámok, tartályszámok, tierértékek, stackméretek, idő/energia/mennyiség. Korai Material + MaterialForm adatmodell javaslat, nem teljes végleges specifikáció.

## 3. Layer 0 — Universal I/O

A gép legalsó vízszintes rétege. Minden kifelé néző vízszintes face és minden lefelé néző face külön port; belső face nem port.

| Gépméret | Oldalsó port | Alsó port | Összes |
| --- | --- | --- | --- |
| 2×2×3 | 8 | 4 | 12 |
| 3×3×4 | 12 | 9 | 21 |

Lerakáskor None. Egy face egyszerre egy mód, Configuration módban GUI-menüből:

- None
- Item Input
- Item Output
- Fluid Input
- Fluid Output
- Data
- Energy Input
- Energy Output

Egyelőre minden gépnél minden mód megjelenik, Silo/Tanknál is. Ez nem ad nem létező működési képességet. Chest nem automatizálható kivétel. Wrench nincs.

**TBD / Future Design:** géptípus szerinti portkorlátozás, nem alkalmazható módok működése, több azonos irányú energiaport kérdése, Data részletek.

## 4. Kapcsolatok és graph-based logisztika

Gép–gép item/fluid átvitelhez mindig pipe kell. Pipe-nak nincs input/extract módja: irányt a machine Output/Input portjai adják. Egy I/O face-hez egy szegmens kapcsolódik.

Mind a hat irányban kapcsolódhatnak. Egy gridcellában egy pipe/kábelszegmens. Vezetékek kerülnek, kezdetben nincs keresztező elem. Azonos típusú eltérő tierű szegmensek összekapcsolhatók.

Nincs automatikus kapcsolat. Configuration módban szegmens–szegmens és machine-port–szegmens kapcsolat is engedélyezendő. Egy művelet mindkét oldalon létrehozza; portmód és kapcsolat külön szükséges.

Ugyanoda visszahelyezett elem korábbi kapcsolati engedélye megmarad minden kapcsolódó objektumnál. Új kapcsolat nem automatikus. Géprecept/portmód reset ettől külön szabály.

### Transfer és throughput

Nincs fizikailag utazó item GameObject. Gráfon számolt forrás–cél inventory/tartály átadás, fogadóképesség és útvonalkapacitás ellenőrzésével. Item/fluid transferhez nem kell áram.

- Pipe sebessége tierfüggő, tierhez egy anyag tartozik.
- Leglassabb útvonalszakasz a bottleneck.
- Párhuzamos utak együtt használhatók; közös szakasz kapacitását nem számoljuk többször. Közös 16-os be/kivezetés és két 8 item/s ág együtt 16 item/s.
- Versengő források egyenlően osztoznak; kihasználatlan rész átadható más igénylőknek.
- Több item/fluid típus közös throughputon osztozik. Fluidok külön kezeltek, nem keverednek.
- Hossz nem csökkent sebességet. Fluidnál nincs nyomás, gravitáció vagy emelési korlát.

### Routing és termékválasztás

Default **Nearest point**: fogadóképes inputhoz legrövidebb pipe-útvonal szegmensszám szerint. Tele cél helyett következő legközelebbi. Azonos távolságnál stabil belső sorrend.

Output porton **Round Robin** választható. 8 item/s példánál első másodpercben 8 az első célnak, következőben 8 a következőnek. Ha csak 3 fér el, 3 megy át, maradék nem megy másik célhoz ugyanabban a másodpercben. Teljesen tele cél azonnal átugrandó.

Item output slotok és fluid output tartályok sorrendben bejárandók, megjegyzett pozícióval. Fogadóképes cél nélküli slot átugrandó. Pozíció mentendő, külön a célportok Round Robin állapotától.

**TBD / Future Design:** állítható contention policy, tick/adagelszámolás, Round Robin több útvonal/versengés mellett, filterek, gráfalgoritmus, eltérő méretű csere face-megfeleltetése.

## 5. Storage és Fluid Cells

Chest hagyományos slotos többtípusú kézi tároló, nem automatizálható. Silo egy itemtípus, Tank egy fluidtípus, Layer 0 portokkal és tierfüggő kapacitással.

Első beérkező anyag adja a típust. Kiürülve más típus fogadható, kivéve type lock mellett. Lock saját GUI-ban az első beérkező típusra, üresen is megmarad. Nincs előzetes típusválasztás. Silo GUI-ból kézzel tölthető itemmel, Tank Fluid Cellből fluiddal.

Fluid Cell újrahasználható item, **maximum 1 L**, egy fluidtípussal, részleges töltéssel/ürítéssel. **Nem stackelhető**, minden cella külön slot. Ürítve üres cella marad. Tank és cella között GUI-ból kétirányú transfer. Machine input cellából tölthető a receptszűrés szerint; output cellába kivehető. Csak átadható mennyiség mozog, maradék a forrásban marad.

## 6. Power

Egységes elektromosság, nincs LV/MV/HV hálózat. Tierelt földkábelek és explicit Energy Input/Output portok, légkábel félretéve. Fogyasztó Inputot, termelő Outputot igényel; Power Storage/breaker mindkettőt. Alátámasztás nem áramforrás. Tudatosan egyszerűsített játékbeli energiaszámláló: a tárolt készlet kijelzése **MW**, az input/output és a termelési/fogyasztási ráta **MW/s**. Készletváltozás: `(input − output) × deltaTime`. Ez játékbeli jelölés, nem fizikai mértékegységmodell. Az alábbi átviteli és igénypéldák MW/s értékek.

### Fogyasztók és generátorok

Gép csak aktív craftingkor fogyaszt. Kábelkorlát lassít: 10 MW/s igényű gép 5 MW/s ellátással 50%-os, 10 s helyett 20 s ciklus. Közös kábel fogyasztói egyenlően osztoznak, nem használt rész újraosztható: 10 MW/s-on 10+10 igényből 5+5, 2+10 igényből 2+8.

Generátor ON működésben mindig maximumon termel/fuelt fogyaszt, igénytől függetlenül. GUI ON/OFF, OFF-ban nulla termelés/fuel, input tölthető. Kompatibilis fuel automatikusan fogadható recept nélkül, felhasználás inputslot-sorrendben. Fuelhiány után automatikus restart; kézi OFF/túlterhelés után nem.

### Power Storage

Külön machine, generatorban nincs beépítve. Tierfüggő kapacitás, saját transfer limit nincs, és nem marad későbbi konfigurálható funkcióként sem. Import/export sebességét az útvonal kábelkapacitása korlátozza; a belső készletbe töltést a szabad hely, a készletből kisütést az elérhető mennyiség is korlátozza. Kábelkorlát és véges készlet érvényes. Nincs ON/OFF. Inputon tölt, Outputon energiát ad, egyszerre is.

Párhuzamos két Storage külön 10 MW/s kábelekkel közös pontra, majd 10 MW/s gyári kivezetés: 5–5 MW/s. Soros A → B → gyár esetén A 10 MW/s rátával táplálja B-t, B ugyanennyit továbbít a gyárnak, nettó készlete változatlan. A köztes tároló üresen és tele is továbbíthat energiát, megfelelő Input/Output bekötéssel. Az átfolyás beleszámít az input/output statisztikába, önmagában nem változtatja a belső készletet. A kiürülve B tovább ad, saját készlete fogy.

Többletből tölt, hiánynál besegít breaker trip előtt.

### Breaker és overload

**2×1×2 machine**, két blokk magas. Energy Input/Output, egyetlen kézi kapcsológomb. Túlterheléskor OFF/Open állapot és külön `Tripped: Overload` jelzés; nincs külön Reset/Restart gomb. Nem tierfüggő, saját transfer limit nincs.

Termelés/storage ellátást meghaladó fogyasztásnál a géptől következő breaker leold, mögötti egész ág leáll. Generátor felőli rész működhet. Player helyszínen ugyanazzal a kapcsológombbal visszakapcsol; ez törli a trip jelzést. Fennálló overloadnál azonnal újra trip. A generátor is egyetlen kapcsológombot használ, külön Restart nélkül.

Breaker nélkül a hálózat generátorai leállnak, egyenként kézzel újraindítandók. Elégtelen első generátor ismét leáll: előbb csökkenteni kell az igényt.

**TBD / Future Design:** hurkolt/több betáplálású topológia, effektív kábelkorlátozott igény overload-elszámolása, konkrét fuel/generátor értékek; később robbanás/kábelkiégés lehetősége, most nem implementálandó.

## 7. Building, inventory és UI

Csak grid, korai irány 1×1×1. Gépforgatás függőleges tengelyen 90°. Machine/Chest/Silo/Tank/generator/Storage/breaker teljes alapja alátámasztandó. Géptípus szerint természetes terep elfogadható vagy épített padló kell; besorolás később. Alap nem bontható ki az objektum alól. Ez felülírja az alapvesztési leállás ötletét. Pipe/kábel teljesen szabad, levegőben is.

Alsó portokhoz alátámasztó átvezetőblokk később; addig hozzáférésük teljes alátámasztás mellett megoldatlan korlát.

Minden machine/tároló/pipe/kábel előre craftolt item, inventoryból lerakva. Tiercsere előtt lebontás kell, nincs helyben upgrade. Kezdetben pipe/kábel egyenként; húzásos építés később.

Lebontáskor **maga az item** jár vissza, nem alapanyagok. Belső tartalom és progress elvész. Inventoryban el nem férő visszaadott item elvész. Minden veszteségről előzetes megerősítés kell. Nincs átmeneti chest.

Új lerakáskor recept/portmód reset; kapcsolati engedély helyben cserekor megmarad. Save/load nem reset.

Deconstruct mód rámutatás/nyomva tartás; Configuration mód port/kapcsolat GUI-menüvel. Wrench nincs. Később in-world panelek. Config copy-paste kívánt, részletei később.

Inventory slot/stack alapú, nincs súlykorlát, fejlesztéssel növelhető slotszám.

### Workbench és Lifepod

- Nincs kézi crafting; workbench és később machine-ek gyártanak.
- Player beépített workbenchös Lifepod mellett kezd.
- Önálló workbench meglévő workbenchben craftolható.
- Alapanyag player inventoryból, termék oda.
- Nem fér el: nem indul/nincs inputlevonás, **Inventory Full**.
- Áram nem kell; recipe idejét végig kell várni nyitott GUI-val.
- Egy folyamat, nincs queue/párhuzamos crafting.
- Player inventory crafting közben zárolt.
- GUI bezárás: cancel, alapanyag visszajár, nincs termék; opcionális **Crafting Cancelled**.
- Lifepod lebontható, visszatérítése később eldöntendő kivétel.

**TBD / Future Design:** kezdőslotszám/stackek, hotbar, minimalista kezdőfelszerelés, alapanyag-felhasználással felszabaduló hely workbench-ellenőrzése, building UX finer details.

## 8. Digital Era

AE2 ihlette digitális storage és valódi machine-eket használó autocrafting irány. Korai ötletek: controller, modules, terminal, data cables, storage interfaces, import/export, production patterns/planner. Nem végleges lista/balance. V1/v2 határok game balance során dőlnek el. Chest automatizálhatatlansága külön döntés nélkül nem oldható fel.

### Opcionális programozható gyárvezérlés

Elfogadott opcionális irány (2026-11-03): ComputerCraft ihlette programozhatóság az egyedi, AE2-alapú Digital Era rendszerbe integrálva. Nem különálló ComputerCraft-másolat vagy annak teljes funkciókészlete: a meglévő digitális hálózat, terminálok és autocrafting képességeire épülő vezérlési réteg.

A programozás opcionális: a fő fejlődési út és az alapvető automatizálás programírás nélkül is használható marad. Lehetséges felhasználások a hálózati készletek és gépállapotok lekérdezése, saját monitoring/kijelzések, feltételes gyártási kérések és gépvezérlés. Ezek tervezési lehetőségek, nem végleges API- vagy eszközlista.

Az integráció a valódi gépekkel végzett autocraftingra épül; az anyagmozgatás, recept-, energia- és útvonalkorlátok továbbra is érvényesek. A Data portok és a digitális hálózat pontos kapcsolata még tervezendő. Programozható robotok vagy a ComputerCraft más funkciói e döntéssel nem kerülnek automatikusan a scope-ba.

**TBD / Future Design:** programozási felület és nyelv, elérhető adatok/parancsok, hozzáférés és unlock, controller/module/terminal szerepek, autocrafting-integráció, események és ütemezés, futási erőforráskorlátok, hibakeresés, programok és futási állapot mentése; Digital Era v1/v2 besorolás.

**TBD / Future Design:** detailed Digital Era/autocrafting specifics, power, memory/bandwidth/channel, scheduling, stock keeping és monitoring.

## 9. Save és performance

Adatvezérelt save, stabil objektum-ID-k, nem GameObject/scene serialization. Mentendő:

- World/player/progression, inventoryk, Fluid Cell típus/mennyiség.
- Objektum típus/tier/pozíció/forgatás.
- Aktív/kiválasztott recipe, progress, input/output/fluid készlet.
- Port/kapcsolat, megőrzött cserekapcsolatok, bejárási/Round Robin pozíció.
- Silo/Tank típus/amount/lock.
- Kapcsolók, generator leállási ok, breaker állapot, Power Storage energia.

Load ugyanonnan folytat, nem resetel és nem von le újra inputot. Graph/routes/cache loadkor újraépül. Kézi mentés és állítható időközű autosave.

**Factory Simulation ≠ Rendering:** távoli nem renderelt gyár tovább termel/szállít/energiát kezel. Távoli animáció/rendering/szükségtelen collision kikapcsolható. Central simulation ticks, nem sok nehéz per-machine Update. Cache és dirty rebuild topológiai változáskor. 5–10 Hz csak korai példa.

Csak futó játékban termel, nincs offline progress. Szünetben teljes játék/termelés/transfer/power megáll, nincs valós idejű catch-up.

**TBD / Future Design:** save format/version/migration/backups, autosave alapértékek, tick/performance célok, chunk/area rendszer, aktív workbench mentés/kilépés kezelése.

### Voxelvilág — Eden Crafters által inspirált irány

A választott világdesign-irány alakítható voxelterep: ásás, feltöltés és egyengetés. A terepalakítás a bányászatot, a gyárterület előkészítését és a föld alatti építkezést szolgálja. Az Eden Crafters a játékmenet referenciája, nem ismert belső implementáció vagy teljes feature-lista átvételének előírása.

- A voxelterep és az építési rács külön rendszer. Gépek, csövek, kábelek és alapzatok továbbra is a building gridhez igazodnak.
- A voxeladatból összefüggő terepfelület készülhet; a voxelalap nem jelent kötelező Minecraft-szerű kockás megjelenést. A végleges vizuális stílus és meshképzési algoritmus TBD.
- Nyersanyagirány: a terepben elhelyezett, kitermelhető érctelepek. Eloszlásuk, kimerülésük és a gépi bányászat működése TBD.
- A 7. fejezet alátámasztási szabálya érvényes: lerakott objektum szükséges alátámasztása terepalakítással sem távolítható el. Ennek pontos térbeli ellenőrzése TBD. Talajomlás és fizikai épületösszeomlás nem része az első prototípusnak.

Javasolt technikai alap: térbeli chunkokra bontott terepadatok, chunkonként közös mesh és ütközés, külön GameObject nélkül minden voxelhez. Terepalakítás csak az érintett chunkokat és szükség esetén határszomszédaikat jelöli újraépítendőnek. A chunkhatárok folytonosságát és az ütközésfrissítés költségét ellenőrizni kell.

A terep betöltése/megjelenítése különül el a globális gyárszimulációtól. Távoli, nem renderelt területen a factory tovább működik. Mentési irány: reprodukálható alapvilág mellett a terepmódosítások és a kitermelt nyersanyagállapot tárolása; generátorverzió vagy tárolt alapadat szükséges ahhoz, hogy későbbi változtatás ne írja át a mentett világot. A pontos formátum TBD.

Első prototípus: kis, véges dombos tesztterület; ásás, feltöltés, egyengetés; egy érctípus; alapzat és gépelhelyezés az alátámasztási szabállyal; mentés és visszatöltés. Ez igazolja a terep és a meglévő building/factory rendszerek együttműködését a nagy világ előtt.

**TBD / Future Design:** voxelméret, chunkméret, világméret és határok, véges/végtelen világ, kézi/procedurális generálás, seed és generátorverzió, biome-ok és barlangok, terepfelület algoritmusa, látótávolság/LOD, terepalakító eszközök és anyagköltség, érctelepek és gépi bányászat, alátámasztás térbeli ellenőrzése, tömörítés és mentés, teljesítménycélok. Az Eden Crafters bolygó-terraformálási/progression rendszere ettől külön, jelenleg nincs elfogadva.

## 10. Tudatosan nyitott irányok

| Terület | Státusz |
| --- | --- |
| World/resource generation | Eden Crafters ihlette alakítható voxelterep; generálás és nyersanyagszabályok TBD / Future Design |
| Detailed questline/unlock | Guidebook questline, lehetséges itemleadásos Milestones, még nincs végleges döntés |
| Endgame | TBD / Future Design |
| Building UX finer details | In-world panels, copy-paste, húzásos építés, átvezetőblokk később |
| Digital Era v1/v2/autocrafting | Későbbi game balance/specifikáció |
| Survival/multiplayer | Jövőbeli lehetőség, most single-player factory core |
| Balance | Tierváltozatok, anyagok, receptek, kapacitások, sebességek később |

## 11. Codex implementációs alapelvek

Aktuális döntések felülírják az inspirációk és korai javaslatok szabályait. UI, adatmodell és simulation különüljön el. TBD érték ne váljon észrevétlenül végleges designná. Save/load és új lerakás eltérő életciklus. Balance-szorzók és későbbi contention policy konfigurálhatók legyenek. Power Storage saját transfer limitet ne vezessünk be. Nincs fizikai itemutazás, RNG output, automatikus pipe-kapcsolódás vagy blokkonkénti multiblock. Későbbi multiplayer/survival miatt most nem szükséges ezek infrastruktúráját építeni.

## 12. Fejlesztési állapot és hátralévő munka — 2026-11-03

### Forrás és ellenőrzési korlát

Coding Cody összefoglalója a Designer Danyval történt egyeztetéshez, a jelenlegi repó és saját fejlesztési előzményei alapján. Forráschat: `01a0fc74-a92f-7691-a13f-e799369cc6af`, első összefoglaló turn: `01a1a08d-ad3c-7821-a140-bcb9b96952a7`, nap végi frissítés: `01a1a1e1-ad2c-7af1-887c-48555ddccab0`. Ellenőrzött commit: `edd9eca` (Add player save/load and world layout serialization). Projekt: `H:/git-repo-for-unity/project-ces`, Unity 6000.3.11f1, URP, új Input System.

Ez fájlalapú állapotfelmérés: Cody most nem indított Play módot és nem futtatott teljes tesztkészletet. A kódban meglévő működés nem jelent teljes runtime-validációt. Az Obsidian vault 2026-10-12-i auditját és feladatstátuszait ez az egyeztetés nem frissítette.

A 6. fejezetbe átvezetett energia-döntések Cody által jelzett korábbi kifejezett felhasználói döntések. Az alábbi implementációs eltérések önmagukban nem új design-döntések.

### Kódban meglévő prototípusrendszerek

- Player: mozgás, kameranézés, ugrás, célkereszt.
- Building: gridfoglalás, fix többcellás prefabok, forgatás, előnézet, inventoryból lerakás, nyomva tartott bontás és veszteségmegerősítések, teljes gridméretű kijelölés.
- Inventory: slotok, stackek, kezdőitemek, egyedi Fluid Cell-tartalom és állapotmegőrző átviteli metódusok.
- Machine: aktív/kiválasztott recept, egy folyamat, induláskori inputlevonás, outputhely-ellenőrzés, progressmegőrzés, ON/OFF, tierkompatibilitás és gyorsítás; fluidbufferek és cellás kezelés.
- Universal I/O: Layer 0 portok, portmódok, külön kapcsolati engedélyek.
- Item/fluid logisztika: gráfos átvitel, közös szakaszkapacitás, alternatív utak, Nearest/Round Robin és outputbejárási kurzorok; pipe/kábel kapcsolati vizualizáció.
- Silo/Tank: egy anyagtípus, kapacitás, type lock, GUI, kézi és hálózati kezelés.
- Workbench: időzített folyamat player inventoryból, cancelkor visszaadás, GUI-bezáráskor megszakítás; a teljes zárolás és bontásvédelem még auditálandó.
- Energia: tesztgenerátor, kábelkapacitások, részleges ellátás és lassulás, breaker/túlterhelési prototípus, Power Storage töltés/kisütés/tárolóközi átvitel/átfolyás és statisztika.
- Pause: Escape/Resume, timeScale-alapú szünet. Energiaoldalon komponens-, hálózat-, végpont- és útvonalcache topológiaváltozási érvénytelenítéssel.

Az item/fluid hálózatok jelenleg másodperces központi tickekkel futnak. A fluidelosztás 0.25 L-es, az energiaelosztás 0.25-ös adagokat használ. Ezek implementációs paraméterek, nem végleges balance-döntések. A gépek és a Workbench saját Update-ben dolgoznak, az energiaelosztás képkockánként fut; a 9. fejezet központi szimulációs célja még nem teljesült.

### Korábbi runtime-visszajelzések

Cody előzményeiben kifejezetten visszaigazolt több inventory-, gép-GUI- és craftingjavítás, a pipe-kapcsolatok vizualizációja és a machine fluid output → pipe → Tank Steam-átvitel javítása. A helyreállított projekt elindulását is visszajelezte a felhasználó. Ezek nem teljes regressziós ellenőrzések.

Az energia-, breaker-, soros/párhuzamos tároló-, pause- és cache-edge case-ek teljes igazolása még hátravan; az egyszerű „ok” válasz nem elegendő minden teszteset sikerének megállapításához.

### Mentés: nap végi állapot — 2026-11-03

A mai munka a meglévő mentési irány implementációját vitte előre; Cody chatjében az előző egyeztetés óta nem született új gameplay-design döntés. Az opcionális, ComputerCraft ihlette integrált Digital Era programozhatóság a 8. fejezetben rögzített jövőbeli irány, implementációja nincs.

**Kódban elkészült:**

- Típusazonosítók, definíciókatalógus, lookup és ID-ellenőrzés. A korábbi auditban 18 item-, 2 fluid-, 5 recipe- és 14 buildingreferencia szerepelt.
- Inventory capture/validált restore üres slotokkal és Fluid Cell-tartalommal; a korábban kimaradt `fluidAmount` mentése javítva. A restore csak teljes siker után cseréli le az inventoryt, jelenleg az aktuálissal azonos slotszámot követel.
- Memóriabeli inventory JSON-pillanatkép és restore; külön `inventory-debug.json` fájlos mentés/betöltés, verzióellenőrzés, `.tmp` írás és meglévő mentésnél `.bak` backup.
- Playerpozíció, testforgatás, kamerapitch és inventory capture/restore; véges számok és quaternion validálása, CharacterController ideiglenes kikapcsolása, pitch visszaállítása és függőleges sebesség nullázása. Külön `player-debug.json` fájlos mentési/betöltési debug útvonal.
- Világelrendezés capture: cellaméret, épülettípus-ID, gridorigin és forgatás, többcellás épületenként egy bejegyzés, koordináták szerint rendezve.
- Pipe/kábel kapcsolati flagjeinek és a portok helyi cellájának, face-ének, módjának, kapcsolati engedélyének és routingkurzorainak capture-je.
- Világadat-validálás: cellaméret, ID/prefab, pozitív épületméret, forgatás, koordinátatartomány, átfedések; kapcsolati bitek, várható Layer 0 portok, hiányzó/ismétlődő portok, enumértékek és nemnegatív routingkurzorok.
- Nem destruktív világdebug: capture, JSON-visszaolvasás, validálás és szándékosan átfedő épület elutasítását vizsgáló tesztművelet. A mentési debugkomponensek a FactoryPrototype jelenetben szerepelnek, vizsgált referenciáik be vannak kötve.

Forrásfájlok a `Assets/Scripts/Saving/` könyvtárban: `GameSaveData.cs`, `GameDefinitionCatalog.cs`, `InventorySaveDebug.cs`, `InventoryFileSaveDebug.cs`, `PlayerSaveHandler.cs`, `PlayerFileSaveDebug.cs`, `WorldSaveData.cs`, `WorldLayoutSaveDebug.cs`; továbbá `Assets/Scripts/Inventory/PlayerInventory.cs` és `Assets/Scripts/Building/GridWorld.cs`.

**Runtime-bizonyíték:** Cody megtalálta a november 3-án módosított, érvényes, `version = 1` értékű `inventory-debug.json` fájlt. Ez a fájlírás megtörténtét igazolja, a sikeres restore-t önmagában nem. A jelenlegi projektbeállításokból származtatott mentési helyen `player-debug.json` fájlt nem talált. A player fájlos round-trip és a világvalidátor negatív tesztjeinek eredményére nincs részletes felhasználói visszaigazolás. Cody most nem indított Play módot, teljes fordítást vagy tesztkészletet.

**Teljes játék-/világmentés és visszatöltés még nincs.** Hátravan:

- Player fájlos round-trip, inventory-visszatöltés és világvalidátor negatív tesztek egyértelmű runtime-ellenőrzése.
- A `WorldSaveData` bekötése a jelenleg csak playeradatot tartalmazó `GameSaveData` mellé; világfájl-mentés és épületeket rekonstruáló visszatöltés.
- Stabil objektumpéldány-ID-k és teljes világállapot; gépek kiválasztott/aktív receptje, progress, item/fluid bufferek.
- Portok, kapcsolatok és routingkurzorok tényleges restore-ja; a helyben csere után megőrzött kapcsolati memória mentése.
- Silo/Tank tartalom és lock; kapcsolók, generátor/breaker leoldási okok, Power Storage készlet.
- Biztonságos teljes betöltési tranzakció, graph/cache újraépítés, normál Save/Load felület és autosave; migráció és automatikus backup-helyreállítás.

**Validálási és formátumkorlátok:** a világvalidátor még nem ellenőrzi a terepi alátámasztást. Engedélyezett kapcsolathoz nem követel jelenlegi szomszédot: az ideiglenesen szomszéd nélküli megjegyzett engedély elfogadható. A routingkurzoroknál csak a nemnegativitás ellenőrzött, a restore kompatibilitáskezelése hátravan. A backup létrehozása nem jelent automatikus helyreállítást. A debugformátumok és verzióellenőrzések nem zárják le a végleges save format/migráció designját.

### Design–implementáció eltérések és ellenőrzendő pontok

- Itemtípusonként egy inputslot a design; a kód azonos itemet több slotba is tölthet.
- Megerősítéses inputfluid Flush nincs azonosítva; a kód cellába visszavételt támogat.
- A generátor tüzelőanyag nélküli tesztforrás. Fuelinventory, automatikus fuelbefogadás/égetés és fuelhiány utáni restart hiányzik.
- A Breaker building-definíció/prefab Unity XYZ mérete `(1,2,1)`. A design 2×1×2 mérete a saját, magasságot utolsó helyre tevő konvenciójával sem ezt jelenti. A kívánt méret tisztázandó; a design nincs a prefabhoz igazítva.
- A `GridWorld.CanRemoveBuilding()` létezik, de a vizsgált bontási útvonal nem hívja: az alátámasztás védelme nem tekinthető késznek.
- Workbench tartalomjelzése nincs bekötve az `IDeconstructionContents` interfészbe; bontási figyelmeztetése hiányos. Teljes inventoryzárolás nem látható minden mutáló API-n.
- Hurkolt/elágazó/több betáplálású energiahálózatokon a túlterhelési védelem kiválasztása bejárási sorrendtől függhet. A cache nem bizonyít maximális áramlást vagy teljes teljesítményoptimalizálást.
- A globális szimuláció aktív, lerakott GameObjectekre támaszkodik. Chunkunloadtól és rendereléstől teljesen független adatmodell még nincs.

### Hátralévő rendszerek és javasolt sorrend

Közvetlen javasolt folytatás: player fájlos round-trip és világvalidátor negatív tesztek runtime-ellenőrzése → épületek belső állapotának capture/validálása és kapcsolati restore API → teljes világfájl és biztonságos visszatöltés. A jelenlegi világ lecserélését csak a belső állapotok megőrzésével kössük be, hogy a betöltés ne hozzon létre kiürített, resetelt gyárat. Ezzel együtt rendezendők a fenti designeltérések és az energia/pause/logisztika célzott runtime-tesztjei. Ez javasolt fejlesztési sorrend, nem új felhasználói prioritásdöntés.

További core munka: Chest, Lifepod és kezdő játékmenet; nyersanyagbeszerzés/bányászat; voxelterep és terepalakítás; világ- és nyersanyaggenerálás; progression/guidebook/questline/milestone-ok; anyag-, recept- és gépkínálat, balansz. A voxelvilág jelenleg designirány, implementációját Cody nem találta a scriptállományban.

Későbbi vagy nyitott scope: Digital Era/autocrafting, in-world panelek, inventoryfejlesztések és teljes drag/drop UX, config copy-paste, húzásos/tömeges építés. Survival és multiplayer továbbra is jövőbeli lehetőség; hiányuk nem a jelenlegi single-player core hibája. A részletszabályok az érintett fejezetek TBD pontjai szerint maradnak nyitva.
