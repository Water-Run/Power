# Equilibrio del serbatoio con volume liquido finito

[English](TANK_PHASE_EQUILIBRIUM.md) · [简体中文](TANK_PHASE_EQUILIBRIUM.zh-CN.md) · [Français](TANK_PHASE_EQUILIBRIUM.fr.md) · [Русский](TANK_PHASE_EQUILIBRIUM.ru.md) · [日本語](TANK_PHASE_EQUILIBRIUM.ja.md) · [한국어](TANK_PHASE_EQUILIBRIUM.ko.md) · [Deutsch](TANK_PHASE_EQUILIBRIUM.de.md) · [Español](TANK_PHASE_EQUILIBRIUM.es.md) · **Italiano** · [Português](TANK_PHASE_EQUILIBRIUM.pt-BR.md)

## Contratto

Impostare `liquid_fuel_tank.parameters.headspace.phase_equilibrium` a `true` per evaporazione e condensazione del serbatoio. Il film abbinato fornisce proprietà `phase_equilibrium`; il gas tracciato condivide R, cv, potere calorifico e stechiometria. Un serbatoio bagnato dichiara temperatura iniziale comune e pressione parziale satura, uno asciutto parte al massimo a saturazione. La densità totale resta inferiore a quella liquida.

`evaporated_fuel_mass`, `condensed_fuel_mass` e `mass_flow` con segno contano il trasferimento netto per passo fisico. Leggere insieme `vapor_partial_pressure`, `saturation_pressure` corretta, inventario liquido e volume gas. La pompa aspira liquido separato; trasporto bifase, trascinamento di bolle e NPSH locale restano aperti.

## Riferimento termodinamico

Il recipiente rigido contiene liquido incomprimibile e gas ideale di pseudospecie/portante. Il volume liquido modifica quello gassoso. Il potenziale chimico liquido include `p/rho`, correggendo il riferimento `p_0(T)` a spostamento trascurabile. `reference_pressure` conserva tale significato e non diventa implicitamente la saturazione reale del fluido puro a densità finita.

[Equilibrio di fase del carburante e limiti di alimentazione](FUEL_PHASE_EQUILIBRIUM.it.md)

```text
M_f = m_l + m_v
V_g = V_tank - m_l / rho_l
p = (M_c + m_v) R T / V_g
p_v = m_v R T / V_g
ln(p_v / p_0(T)) = p / (rho_l R T)
U = (M_c + m_v) cv T + m_l (cl T + a)
```

Un flash limitato a U,V costanti conserva combustibile ed energia termica. Orifizi espliciti trasportano vapore, portante ed energia chimica; collegamenti termici scambiano con nodi finiti. Pompa/ritorno scambiano lavoro di spostamento col gas, poi l'equilibrio ridistribuisce energia interna. `hydraulic_work` conta il lavoro di pompa escludendo quello interno di fase.

## Evidenza e limiti

`equilibrium-tank-supply` verifica inversione pompa, sfiato con combustibile e parete calda finita. `equilibrium-tank-fired-cylinder` connette tale sorgente a un motore acceso. Si verificano massimo indipendente d'entropia, potenziali, derivate caloriche, raffinamento ODE, bilanci, replay, rollback e allocazioni. Asset v36 conserva il flag e legge v1-v35.

Casi accoppiati pompa/calore/sfiato mostrano il primo ordine. Entrambe le fasi sorgente restano nell'intervallo dichiarato e la densità totale sotto quella liquida con margine binary64. Densità/proprietà costanti, equilibrio istantaneo e interfaccia piana sono approssimazioni di ricerca. Miscele reali, tensione superficiale, sloshing, cinetica finita, calibrazione misurata e Unity Editor/Play/Player/IL2CPP reale restano non verificati.

[Cantera: constant-volume species](https://cantera.org/stable/cxx/da/d94/PDSS__ConstVol_8cpp_source.html) · [IUPAC: Poynting correction](https://goldbook.iupac.org/terms/view/15347) · [VALIDATION.it.md](VALIDATION.it.md)
