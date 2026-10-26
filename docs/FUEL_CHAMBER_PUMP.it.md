# Pompa carburante a cambio di fase azionata da albero

[English](FUEL_CHAMBER_PUMP.md) · [简体中文](FUEL_CHAMBER_PUMP.zh-CN.md) · [Français](FUEL_CHAMBER_PUMP.fr.md) · [Русский](FUEL_CHAMBER_PUMP.ru.md) · [日本語](FUEL_CHAMBER_PUMP.ja.md) · [한국어](FUEL_CHAMBER_PUMP.ko.md) · [Deutsch](FUEL_CHAMBER_PUMP.de.md) · [Español](FUEL_CHAMBER_PUMP.es.md) · **Italiano** · [Português](FUEL_CHAMBER_PUMP.pt-BR.md)

## Contratto fisico

`fuel_chamber` accoppia un albero reale a una camera carburante finita. `fuel_check_valve` di ingresso e uscita forniscono riempimento ed erogazione per pressione. Liquido, vapore ed energia elastica usano la stessa chiusura. Con ingresso insufficiente la camera può evaporare in aspirazione; l’erogazione segue inventario e pressione calcolati.

`node_a` è rotazionale, `node_b` un nodo carburante con un solo proprietario geometrico. Dichiarare `minimum_reference_volume`, `swept_volume` in `m3` o `l`, `phase` in `rad` o `deg`. Il volume iniziale coincide con la geometria entro arrotondamento binary64. Il meccanismo sinusoidale non aggiunge inerzia di pistone/camma; l’inerzia d’albero è esplicita.

```text
V0(theta) = Vmin + Vs sin^2((theta - phase) / 2)
V = V0(theta) + C p
E = U_fluid + C p^2 / 2
dE = T dS + mu dM - p dV0
torque_pressure = -delta_E_adiabatic / delta_theta
```

## Lavoro e porte

Il sottopasso meccanico chiuso conserva massa ed entropia. L’energia include fluido e parete elastica, quindi il lavoro d’albero usa il volume geometrico di riferimento. La stessa variazione accettata determina la coppia nel solver meccanico con ingranaggi, convertitori, idraulica e intervalli speculativi della frizione. L’espansione spinge l’albero; `hydraulic_work` positivo entra nella camera. Nessuna fonte esterna aggiuntiva di pressione.

`fuel_check_valve` usa conduttanza esplicita in `m3_s_pa` e apertura frazionaria opzionale. Il fluido donatore omogeneo passa solo A→B quando A ha pressione maggiore. È una valvola ideale senza massa e con pressione di apertura nulla. Restano i contratti di entalpia conservativa e calore di parete finita della rete.

## Uso ed evidenze

Richiedere `equilibrium-fuel-pump` o `electric-equilibrium-fuel-pump` tramite MCP o eseguire JSON in CLI. Leggere massa, temperatura, pressione, vapore ed energia insieme a erogazione valvole e velocità dell’albero. `volume` del componente è geometrico; `volume` del nodo include dilatazione della parete. `torque`, `hydraulic_power`, `hydraulic_work` espongono lo scambio interno.

Pressione/lavoro liquidi, adiabatiche di vapore ideale e inversione indipendente dell’entropia bifase verificano la chiusura. Il moto liquido chiuso raffina al secondo ordine; riempimento/erogazione con fase al primo contro RK4 invertito indipendentemente. Ingresso insufficiente e pieno, motore elettrico, ingranaggi/idraulica/frizione, replay, rollback e allocazioni coprono l’integrazione. Asset v37 salva geometria e legge v1-v36. Unity Editor/Play/Player/IL2CPP effettivo resta non verificato.

## Lavoro rimanente

I parametri restano `unverified`. Limiti di temperatura, pressione e volume rappresentabile valgono per tutto il ciclo. Si assumono liquido incomprimibile, vapore ideale pseudo-specie, equilibrio istantaneo a temperatura comune e pressione esterna del carter nulla. Inerzia locale/NPSH, flusso critico, ritardo di nucleazione, inerzia/isteresi valvole, miscele reali e calibrazione misurata restano aperti.

[Modelica: swept volume](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Fluid.Machines.SweptVolume.html) · [Cantera: reactor walls](https://cantera.org/dev/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.it.md)

La pompa del dominio carburante alimenta ora ago fisico e scarico a titolo congelato in un ricevitore acceso con volume liquido finito. Gli inventari comprendono cambio fase, lavoro di spostamento, energia cinetica del getto e impulso sulla parete ferma. [Scarico carburante a titolo congelato e ricevitori finiti](FUEL_DISCHARGE.it.md)
