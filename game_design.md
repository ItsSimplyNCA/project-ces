# COGITO ERGO SUM — Game Design

## 1. Dokumentum és core vision

A Unity factory-game aktuális projektkontextusa, a rendelkezésre álló korai beszélgetés és az ebben a chatben megerősített döntések alapján. A legutóbbi döntések az irányadók. Ez design, nem implementációs állapotjelentés. Példaszámok nem végleges balance-értékek. **TBD / Future Design** pontokhoz ne találjunk ki végleges szabályokat.

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

Magasabb tier gyorsítja az alacsonyabb tierű recepteket, jelenleg változatlan MW-igénnyel. Emiatt a ciklus összenergiája csökken. Időszorzók későbbi balance-értékek. Energia-szorzó fejlesztői konfigurációban később változtatható, jelenleg 1.

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

Egységes elektromosság, nincs LV/MV/HV hálózat. Tierelt földkábelek és explicit Energy Input/Output portok, légkábel félretéve. Fogyasztó Inputot, termelő Outputot igényel; Power Storage/breaker mindkettőt. Alátámasztás nem áramforrás. Teljesítmény MW, nem MW/s; tárolt energia külön mennyiség.

### Fogyasztók és generátorok

Gép csak aktív craftingkor fogyaszt. Kábelkorlát lassít: 10 MW igényű gép 5 MW-tal 50%-os, 10 s helyett 20 s ciklus. Közös kábel fogyasztói egyenlően osztoznak, nem használt rész újraosztható: 10 MW-on 10+10 igényből 5+5, 2+10 igényből 2+8.

Generátor ON működésben mindig maximumon termel/fuelt fogyaszt, igénytől függetlenül. GUI ON/OFF, OFF-ban nulla termelés/fuel, input tölthető. Kompatibilis fuel automatikusan fogadható recept nélkül, felhasználás inputslot-sorrendben. Fuelhiány után automatikus restart; kézi OFF/túlterhelés után nem.

### Power Storage

Külön machine, generatorban nincs beépítve. Tierfüggő kapacitás, saját transfer limit alapból nincs; később konfigurálhatóan bekapcsolható/állítható. Kábelkorlát és véges készlet érvényes. Nincs ON/OFF. Inputon tölt, Outputon energiát ad, egyszerre is.

Párhuzamos két Storage külön 10 MW kábelekkel közös pontra, majd 10 MW gyári kivezetés: 5–5 MW. Soros A → B → gyár esetén A 10 MW-tal tölti B-t, B saját készletéből 10 MW-ot ad a gyárnak, nettó készlete változatlan. A kiürülve B tovább ad, saját készlete fogy.

Többletből tölt, hiánynál besegít breaker trip előtt.

### Breaker és overload

**2×1×2 machine**, két blokk magas. Energy Input/Output, kézi kapcsoló, ON/OFF/TRIPPED. Nem tierfüggő, saját transfer limit nincs.

Termelés/storage ellátást meghaladó fogyasztásnál a géptől következő breaker leold, mögötti egész ág leáll. Generátor felőli rész működhet. Player helyszínen visszakapcsol; fennálló overloadnál azonnal újra trip.

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

Aktuális döntések felülírják az inspirációk és korai javaslatok szabályait. UI, adatmodell és simulation különüljön el. TBD érték ne váljon észrevétlenül végleges designná. Save/load és új lerakás eltérő életciklus. Balance-szorzók, opcionális storage limit és későbbi contention policy konfigurálhatók legyenek. Nincs fizikai itemutazás, RNG output, automatikus pipe-kapcsolódás vagy blokkonkénti multiblock. Későbbi multiplayer/survival miatt most nem szükséges ezek infrastruktúráját építeni.