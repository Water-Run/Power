# Rail di combustibile liquido alimentato da pompa

[English](PUMP_FED_FUEL.md) · [简体中文](PUMP_FED_FUEL.zh-CN.md) · [Français](PUMP_FED_FUEL.fr.md) · [Русский](PUMP_FED_FUEL.ru.md) · [日本語](PUMP_FED_FUEL.ja.md) · [한국어](PUMP_FED_FUEL.ko.md) · [Deutsch](PUMP_FED_FUEL.de.md) · [Español](PUMP_FED_FUEL.es.md) · **Italiano** · [Português](PUMP_FED_FUEL.pt-BR.md)

## Contratto

`liquid_rail_feed` associa un iniettore liquido a una pompa volumetrica esistente e a un confine esplicito di materia/calore. Il nodo di uscita idraulica deve corrispondere alla cedevolezza e alla pressione assoluta iniziale del rail. Pompa e iniettore possiedono questo nodo; altri percorsi fluidi non contabilizzati sono rifiutati.

L'energia di pressione è memorizzata una volta nel nodo idraulico. Portata e reazione d'albero seguono la soluzione accoppiata conservativa. Il combustibile entrante porta energia termica e chimica; lo stoccaggio termico del rail miscela la temperatura. Il flusso inverso con segno restituisce combustibile alla temperatura attuale del rail. Scarico, riscaldamento parete, vapore e combustione prescritta rimangono separati.

`pump-fed-liquid-cylinder` e `pump-fed-needle-cylinder` conservano iniezione fisica e movimento opzionale dell'ago. I KPI di pressione usano un limite dichiarato della sola pompa con unità esplicite. Leggere `total_fuel_delivered`, `reservoir_enthalpy` e `fuel_energy_in` sull'ID 1511; la pompa ID 1510 espone il lavoro reale albero-fluido.

## Evidenze e limiti

v36 conserva collegamenti e temperatura sorgente e legge v1-v35. Scambio analitico albero/pressione, raffinamento ODE simultaneo indipendente, miscelazione termica, bilanci massa/combustibile/energia/volume, ritorno e rollback completo hanno verifiche separate.

Serbatoio rigido miscelato, liquido incomprimibile e gas ideale. Sloshing/forma idrostatica, termodinamica delle miscele reali, cavitazione, mappe misurate pompa/valvola, calibrazione OEM e Unity Editor/Play/Player/IL2CPP reale restano aperti. Parametri `unverified`.

## Serbatoio finito di combustibile liquido

`liquid_fuel_tank` conserva massa liquida finita ed energia termica con densità, riferimento termico del film e potere calorifico dell'iniettore associato. L'alimentazione lo seleziona con `tank_component` e omette `supply_temperature`. Ogni serbatoio appartiene a un'alimentazione compatibile.

Energia termica e chimica del serbatoio entrano nello stoccaggio totale. Il trasferimento interno non aggiunge massa o energia chimica esterna. La pressione prescritta all'ingresso mantiene il confine di lavoro di pressione. Aspirazione/scarico gas possono ancora trasportare energia chimica.

Serbatoio rigido miscelato, liquido incomprimibile e gas ideale. Sloshing/forma idrostatica, termodinamica delle miscele reali, cavitazione, mappe misurate pompa/valvola, calibrazione OEM e Unity Editor/Play/Player/IL2CPP reale restano aperti. Parametri `unverified`.

[LIQUID_FUEL_TANK.it.md](LIQUID_FUEL_TANK.it.md)

## Ritorno di scarico carburante tracciato

`liquid_rail_return` collega un'alimentazione a un `hydraulic_relief` unidirezionale esclusivo. La valvola collega il rail alla stessa pressione d'ingresso prescritta della pompa. Registrare ogni percorso; porte incompatibili, proprietà duplicate e percorsi non tracciati sono rifiutati.

`fluid_heat_fraction` sceglie esplicitamente la quota [0,1] delle perdite portata dal carburante di ritorno. Il resto segue il percorso termico dichiarato. Miscelazione simultanea rail/serbatoio conserva massa, chimica, lavoro di pressione e calore. Il ritorno a sorgente esterna porta massa/energia oltre confine.

[LIQUID_FUEL_RETURN.it.md](LIQUID_FUEL_RETURN.it.md)

## Geometria del serbatoio e spazio gassoso finito

`liquid_fuel_tank.parameters.headspace` dichiara `capacity` in `m3` o `l` e `gas_node`. Il gas omette `storage`: volume `capacity - liquid_mass / density`, un proprietario e volume positivo. Pompa e ritorno usano pressione prescritta nulla: il gas finito determina la pressione d'ingresso.

Il solver accoppiato scambia lavoro di pressione fra albero, rail e gas senza fonte esterna. Orifizi gassosi e collegamenti termici forniscono sfiato/calore espliciti. Leggere `pressure`, `fill_fraction`, `hydraulic_work` cumulativo con segno e massa, energia, volume del gas. Dose, liquido, evaporazione e combustione restano separati.

[Geometria del serbatoio e spazio gassoso finito](TANK_HEADSPACE.it.md)

## Equilibrio di fase del carburante e limiti di alimentazione

L'alimentazione predefinita respinge liquido sotto saturazione. Il serbatoio finito opzionale risolve il vapore sorgente con potenziali corretti. Rail e aspirazione trasportano ancora liquido separato; bolle e flusso cavitante richiedono altra fisica.

Il confronto ammette solo arrotondamento binary64: `64 * epsilon * max(p, p_sat)`, con `epsilon = 2^-52`. Il margine riportato non viene limitato. È un controllo di saturazione globale; perdite locali e NPSH della pompa richiedono un modello di flusso risolto. Le verifiche indipendenti richiedono inoltre entropia totale non decrescente nel flash adiabatico e nello scambio con parete finita.

[Equilibrio di fase del carburante e limiti di alimentazione](FUEL_PHASE_EQUILIBRIUM.it.md)

## Equilibrio del serbatoio con volume liquido finito

Impostare `liquid_fuel_tank.parameters.headspace.phase_equilibrium` a `true` per evaporazione e condensazione del serbatoio. Il film abbinato fornisce proprietà `phase_equilibrium`; il gas tracciato condivide R, cv, potere calorifico e stechiometria. Un serbatoio bagnato dichiara temperatura iniziale comune e pressione parziale satura, uno asciutto parte al massimo a saturazione. La densità totale resta inferiore a quella liquida.

`evaporated_fuel_mass`, `condensed_fuel_mass` e `mass_flow` con segno contano il trasferimento netto per passo fisico. Leggere insieme `vapor_partial_pressure`, `saturation_pressure` corretta, inventario liquido e volume gas. La pompa aspira liquido separato; trasporto bifase, trascinamento di bolle e NPSH locale restano aperti.

[Equilibrio del serbatoio con volume liquido finito](TANK_PHASE_EQUILIBRIUM.it.md)

Il trasporto compone ora con `fuel_chamber` azionato da albero e `fuel_check_valve` ideale. Iniettore, inerzia/flusso critico e miscele reali misurate restano separati. I parametri sono `unverified`. [Pompa carburante a cambio di fase azionata da albero](FUEL_CHAMBER_PUMP.it.md)
