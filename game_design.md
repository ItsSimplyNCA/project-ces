# COGITO ERGO SUM — Game Design

## 1. Dokumentum és core vision

A Unity factory-game aktuális projektkontextusa, a rendelkezésre álló korai beszélgetés és az ebben a chatben megerősített döntések alapján. A legutóbbi döntések az irányadók. Az 1–11. fejezet a design specifikációja; a 12. fejezet külön dátumozott fejlesztési állapotjelentés, amely nem írja felül a design szabályait. Példaszámok nem végleges balance-értékek. **TBD / Future Design** pontokhoz ne találjunk ki végleges szabályokat.

Önálló 3D survival/factory-game, mély anyagfeldolgozással, gyártási láncokkal és technológiai fejlődéssel. A workbench korlátai tudatosan ösztönzik a korai gépi automatizálást.

Elfogadott alapirány (2026-11-05): a korai játék survivalhangsúlyos. A technológia és az automatizálás előrehaladtával a túlélési feladatok terhe fokozatosan csökken, és több figyelem jut a gyárépítésre és a bolygó átalakítására. A terraformálás a játék egyik fő, hosszú távú célja; az ipar biztosítja hozzá az eszközöket. A konkrét szükségletek, automatizálási lépcsők és terraformálási mechanikák még TBD. Ez designirány, nem meglévő implementáció.

### Vizuális irány

Elfogadott vizuális cél (2026-11-05): cel-shaded megjelenés, Borderlands ihlette ipari, képregényes karakterrel. A kontúrok, árnyéklépcsők, anyagok és paletta pontos szabályai, valamint a shadermegoldás TBD. A `ConceptArt/2026-11-05/` képei vizuális próbák; a száraz, sziklás bolygókép, az ábrázolt gépek és a képeken megjelenő szállítószalagok nem új gameplay-döntések.

| Inspiráció | Szerep |
| --- | --- |
| FOUNDRY | Building feel, grid, fix egyben lerakható gépek, később in-world panelek |
| Factorio / FOUNDRY | Előre craftolt inventory-itemből építés |
| GregTech | Materials, recipes, géptierek, item/fluid pipes |
| Satisfactory | Bufferek, power/breaker szemlélet, távoli factory simulation |
| AE2 | Későbbi digital storage és valódi gépekkel működő autocrafting |
| Eden Crafters | Alakítható voxelterep, tereprendezés és bányászati szabadság |

Jelenleg single-player. A survival az elfogadott játékirány része, részletszabályai és implementációja még tervezendők. Multiplayer lehetséges későbbi bővítés. Nincs blokkonként épített multiblock, wall sharing vagy feszültségtierű elektromos rendszer.

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
- A voxeladatból összefüggő terepfelület készülhet; a voxelalap nem jelent kötelező Minecraft-szerű kockás megjelenést. A cel-shaded vizuális irány az 1. fejezet szerint elfogadott; a terep konkrét megjelenítése és meshképzési algoritmusa TBD.
- Nyersanyagirány: a terepben elhelyezett, kitermelhető érctelepek. Eloszlásuk, kimerülésük és a gépi bányászat működése TBD.
- A 7. fejezet alátámasztási szabálya érvényes: lerakott objektum szükséges alátámasztása terepalakítással sem távolítható el. Ennek pontos térbeli ellenőrzése TBD. Talajomlás és fizikai épületösszeomlás nem része az első prototípusnak.

Javasolt technikai alap: térbeli chunkokra bontott terepadatok, chunkonként közös mesh és ütközés, külön GameObject nélkül minden voxelhez. Terepalakítás csak az érintett chunkokat és szükség esetén határszomszédaikat jelöli újraépítendőnek. A chunkhatárok folytonosságát és az ütközésfrissítés költségét ellenőrizni kell.

A terep betöltése/megjelenítése különül el a globális gyárszimulációtól. Távoli, nem renderelt területen a factory tovább működik. Mentési irány: reprodukálható alapvilág mellett a terepmódosítások és a kitermelt nyersanyagállapot tárolása; generátorverzió vagy tárolt alapadat szükséges ahhoz, hogy későbbi változtatás ne írja át a mentett világot. A pontos formátum TBD.

Első prototípus: kis, véges dombos tesztterület; ásás, feltöltés, egyengetés; egy érctípus; alapzat és gépelhelyezés az alátámasztási szabállyal; mentés és visszatöltés. Ez igazolja a terep és a meglévő building/factory rendszerek együttműködését a nagy világ előtt.

**TBD / Future Design:** voxelméret, chunkméret, világméret és határok, véges/végtelen világ, kézi/procedurális generálás, seed és generátorverzió, biome-ok és barlangok, terepfelület algoritmusa, látótávolság/LOD, terepalakító eszközök és anyagköltség, érctelepek és gépi bányászat, alátámasztás térbeli ellenőrzése, tömörítés és mentés, teljesítménycélok. A hosszú távú terraformálási cél elfogadott; konkrét környezeti változói és progressionje nem véglegesek. Az Eden Crafters teljes bolygó-terraformálási rendszerének átvétele nincs elfogadva.

### Folyamatban lévő bolygó- és progression-tervezés

A 2026-11-05-én behozott „PROJECT CONTEXT – Terraforming Automation Game” összefoglaló ötletbörze-forrás, amelyet a felhasználóval pontonként újraértékelünk. A forrás saját „Confirmed” és „véglegesített” címkéi nem jelentenek jelenlegi jóváhagyást, a benne szereplő utasítások sem írják felül az elfogadott designt.

A következő átbeszélendő pont a bolygó alapkaraktere. A sziklás/kietlen felszín, ritka felszíni víz, légköri összetétel és toxicitás, valamint őshonos élet még nincs elfogadva. A survival konkrét szükségletei, a korszakok és a Stone Age 5–10 órás hossza, mechanikai meghajtás, kézi crafting, queue, új munkaállomások és szerszámok szintén nem elfogadottak. A jelenlegi crafting-, gép- és logisztikai szabályok külön döntésig érvényben maradnak.

## 10. Tudatosan nyitott irányok

| Terület | Státusz |
| --- | --- |
| World/resource generation | Eden Crafters ihlette alakítható voxelterep; generálás és nyersanyagszabályok TBD / Future Design |
| Detailed questline/unlock | Guidebook questline, lehetséges itemleadásos Milestones, még nincs végleges döntés |
| Terraforming / Endgame | Terraformálás mint fő hosszú távú cél elfogadva; konkrét rendszer és végcél TBD / Future Design |
| Building UX finer details | In-world panels, copy-paste, húzásos építés, átvezetőblokk később |
| Digital Era v1/v2/autocrafting | Későbbi game balance/specifikáció |
| Survival | Survivalhangsúlyos early game, technológiával és automatizálással csökkenő teher; konkrét rendszerek TBD |
| Multiplayer | Jövőbeli lehetőség; jelenleg single-player |
| Balance | Tierváltozatok, anyagok, receptek, kapacitások, sebességek később |

## 11. Codex implementációs alapelvek

Aktuális döntések felülírják az inspirációk és korai javaslatok szabályait. UI, adatmodell és simulation különüljön el. TBD érték ne váljon észrevétlenül végleges designná. Save/load és új lerakás eltérő életciklus. Balance-szorzók és későbbi contention policy konfigurálhatók legyenek. Power Storage saját transfer limitet ne vezessünk be. Nincs fizikai itemutazás, RNG output, automatikus pipe-kapcsolódás vagy blokkonkénti multiblock. A multiplayer továbbra is jövőbeli lehetőség; a survival elfogadott irány, de részletspecifikáció nélkül ne vezessünk be önkényesen szükségleteket vagy büntetéseket.

## 12. Fejlesztési állapot és hátralévő munka — 2026-11-05

### Forrás és ellenőrzési korlát

Napzáró egyeztetés Coding Codyval, a jelenlegi repó és saját fejlesztési előzményei alapján. Forráschat: `01a0fc74-a92f-7691-a13f-e799369cc6af`, aktuális összefoglaló turn: `01a1ac2c-5c92-7362-820d-4a4bb45fda52`. Ellenőrzött commit: `e31ea7a` — Add factory component save data, validation and restore support. Projekt: `H:/git-repo-for-unity/project-ces`, Unity 6000.3.11f1, URP, új Input System. Ez a fejezet a november 3-i `edd9eca` állapotjelentést frissíti; a korábbi állapot Gitből visszakereshető.

Fájlalapú állapotfelmérés: Cody nem indított Unity Play módot vagy buildet, és nem futtatott teljes tesztkészletet. A kódban meglévő működés nem jelent runtime-validációt. Az Obsidian vault 2026-10-12-i auditját és feladatstátuszait ez a napzárás nem frissítette. A designelfogadás nem implementációs státusz.

### Mai design-döntések és tervezési folytatás

- Survivalhangsúlyos early game, technológiai fejlődéssel és automatizálással fokozatosan csökkenő survivalteher.
- A terraformálás fő hosszú távú cél; az ipar biztosítja hozzá az eszközöket.
- Cel-shaded, Borderlands ihlette vizuális irány; három concept art készült. A konkrét shader, paletta és bolygókép még nincs véglegesítve.
- Az áthozott terraformálási összefoglalót pontonként vizsgáljuk újra. Következő téma: a bolygó alapkaraktere. A forrás további állításai, beleértve saját „véglegesített” pontjait, külön jóváhagyásig ötletek maradnak. A jelenlegi crafting és logisztika nincs felülírva.

Cody fejlesztési chatjében a november 3-i egyeztetés óta nem született új kifejezett gameplay-design döntés; a munka a meglévő mentési irányt valósította meg.

### Kódban meglévő prototípusrendszerek

A korábbi alapok továbbra is jelen vannak: player mozgás/nézés/ugrás; gridalapú építés és bontás veszteségmegerősítéssel; inventory és Fluid Cell-adatok; gépi item/fluid processing külön aktív és kiválasztott recepttel; Universal I/O; gráfos item/fluid logisztika Nearest/Round Robin móddal; Silo/Tank; Workbench; tesztgenerátor, kábelkorlátok, breaker és Power Storage; pause és energiaoldali cache.

Az item/fluid hálózatok másodperces központi tickeket használnak; a fluidelosztás 0.25 L-es, az energiaelosztás 0.25-ös adagokat használ. Ezek implementációs paraméterek. A gépek és a Workbench saját Update-ben dolgoznak, az energiaelosztás képkockánként fut. A rendereléstől/chunkunloadtól független teljes adatalapú gyárszimuláció még hiányzik.

### Mentés: komponensszintű támogatás

**A kódban meglévő capture/validálás/restore:**

- Inventory és player: üres slotok, Fluid Cell-típus/mennyiség, pozíció, testforgatás, kamerapitch. A korábbi `fluidAmount` kihagyás javítva. A player restore kezeli a CharacterControllert és nullázza a függőleges sebességet; a player/inventory restore jelenleg az aktuálissal azonos slotszámot követel.
- Külön inventory/player debug JSON-fájlok, verzióellenőrzéssel, `.tmp` írással és meglévő mentésnél `.bak` backuppal. A backup nem jelent automatikus helyreállítást.
- Machine: kiválasztott/aktív recept, ON/OFF, előrehaladás, item/fluid bufferek és Fluid Cell-tartalom. Az aktív recept outputhelyét szimulált tárolókon ellenőrzi; ismert validálási hiba lent.
- Silo: itemtípus, mennyiség, lock és egyedi Fluid Cellek FIFO-sorrendje. Tank: fluidtípus, mennyiség és lock; üresen zárolt típus megőrizhető.
- PowerGenerator/PowerBreaker: kapcsolóállapot és túlterhelésjelzés; tripped állapot nem párosulhat bekapcsolt/zárt állapottal. PowerStorage: készlet és kapacitásvalidálás; pillanatnyi forgalom újraszámítandó.
- UniversalPort/UniversalIO: portazonosság, mód, kapcsolati engedély, routingmód és kurzorok. Párosítás helyi koordináta és face alapján, nem listasorrend szerint; ismert enumvalidálási hiba lent. Az item-routing outputkurzora az aktuális slotszámhoz igazodik, nulla slotot és nagy mentett értéket is kezel.
- Workbench: aktív recept, előrehaladás, már levont/visszaadásra váró alapanyagok; az aktív recept szükségletének összevetése a mentett tartalommal. A restore player inventoryt kap és nem von le ismét inputot. A teljes betöltés GUI/cancel viselkedése még tisztázandó.
- World snapshot: cellaméret, épülettípus-ID, gridorigin, forgatás, pipe/kábel flagjei, portok és az épületek fenti komponensadatai; a GridWorld rögzíti és validálja őket. A validálás kiterjed a definíciókra, méretekre, koordinátákra, átfedésekre és komponensadatokra. Ez még nem teljes világ-restore.

Források: `Assets/Scripts/Saving/` adat- és debugosztályai; `Assets/Scripts/Building/GridWorld.cs`; a Machine, Silo, Tank, Workbench, power- és portkomponensek capture/validálás/restore metódusai.

A MachineSaveDebug, StorageSaveDebug, PowerSaveDebug és PortSaveDebug mind szerepel a mentett SaveDebug objektumon. A katalógus/UI-referenciák beállítva; a futás közben lerakott épületek célmezői teszteléskor töltendők. Az átfedési negatív teszt teljes épületadatot másol, így nem hiányzó portadatok miatt utasítja el a példányt. Hibás gép-előrehaladásra is van negatív tesztművelet. Ezek megléte nem igazolja a sikeres lefutást.

### Runtime-bizonyíték és ismert hibák

Korábbi felhasználói visszaigazolás van több inventory-/gép-GUI-/craftingjavításról, a pipe-vizualizációról és a machine fluid output → pipe → Tank Steam-átvitelről. Ezek nem teljes regressziós ellenőrzések. Az új mentési lépésekhez nincs részletes tesztlog vagy eredménybeszámoló; az egyszerű „ok” nem igazolja minden eset sikerét.

Cody szerint az `inventory-debug.json` továbbra is létezik november 3-i időbélyeggel; a jelenlegi persistentDataPath alatt `player-debug.json` nincs. Az inventoryfájl megléte a fájlírást támasztja alá, a sikeres restore-t és az új komponensek runtime-roundtripjét nem.

**Közvetlenül javítandó mentési hibák:**

- `UniversalPort.cs`: a routingvalidálás `Enum.IsDefined(typeof(ItemRoutingMode), data.mode)` hívása helyett a `data.routingMode` ellenőrzése szükséges. Az eltérő enumtípus kivételt okozhat; a portvalidálás/restore nem tekinthető készre igazoltnak.
- `Machine.cs`: a craftingvalidálásból hiányzik a `craftingElapsed >= duration` ellenőrzés. A befejezési határt elérő mentett előrehaladást jelenleg nem utasítja el.

**További ismert eltérések és korlátok:**

- SiloT2 prefab még T1 tárolódefinícióra mutat, így T1-kapacitással működik.
- Workbench HasContents mellett továbbra sem implementálja az IDeconstructionContents interfészt; a bontási figyelmeztetés hiányos. Teljes inventoryzárolás korábbi audit szerint nem látható minden mutáló API-n.
- A buildbeállításban még SampleScene szerepel. A Workbench aktív állapotának teljes betöltése és a GUI-bezárásos cancel összehangolandó.
- Itemtípusonként egy inputslot a design, a kód azonos itemet több slotba tölthet. Megerősítéses inputfluid Flush korábban nem volt azonosítva; cellába visszavétel van.
- Generátor továbbra is fuel nélküli tesztforrás; fuelkezelés és fuelhiány utáni restart hiányzik.
- Breaker Unity XYZ mérete `(1,2,1)`, eltér a design 2×1×2 méretétől; külön tisztázandó.
- A korábbi bontási auditban a GridWorld.CanRemoveBuilding() nem volt bekötve a vizsgált útvonalba. Az alátámasztás védelme nem tekinthető késznek; a világvalidátor sem ellenőrzi a terepi alátámasztást.
- A megjegyzett kapcsolati engedélyhez szándékosan nem szükséges jelenlegi szomszéd. A lebontott objektumok külön runtime kapcsolati emlékezete még nincs perzisztálva.
- Hurkolt/több betáplálású energiahálózatok overload-kezelése és energia/pause/cache edge case-ek továbbra is ellenőrzendők. A cache nem bizonyít maximális áramlást vagy teljes optimalizálást.

### Hátralévő mentési munka és következő lépések

**Teljes játék-/világmentés továbbra sincs.** A GameSaveData csak verziót és playeradatot tartalmaz, WorldSaveData nincs hozzákapcsolva. Nincs világfájl-írás/olvasás, teljes épületrekonstrukció vagy egyetlen összehangolt player–világ restore. Stabil objektumpéldány-ID-k, cserekapcsolat-memória mentése, teljes betöltési tranzakció és graph/cache újraépítés is rendezendő. Autosave, normál mentési felület, migráció és automatikus backup-helyreállítás hiányzik. A debugformátum nem végleges mentésdesign.

Javasolt sorrend: két validálási hiba javítása és célzott ellenőrzése → komponensszintű roundtripek → összehangolt player–világ adat és teljes előzetes validálás → épületek és belső állapotuk visszaépítése → világfájl és felhasználói mentési folyamat. A világ lecserélése nem eredményezhet kiürített, resetelt gyárat. Ez fejlesztési javaslat, nem új prioritásdöntés.

További tervezendő/fejlesztendő területek: Chest, Lifepod és kezdő játékmenet; nyersanyagbeszerzés, bányászat és voxelterep; bolygó és világ/resource generation; survival/progression és terraformálás; guidebook/questline/milestone-ok; anyag-, recept- és gépkínálat, balansz. Ezekre a mai designelfogadás nem jelent implementációt.

Későbbi/nyitott scope: Digital Era és opcionális programozhatóság, in-world panelek, inventoryfejlesztések és drag/drop UX, config copy-paste, húzásos építés. Multiplayer továbbra is jövőbeli lehetőség. A részletszabályok az érintett fejezetek TBD pontjai szerint nyitottak.
