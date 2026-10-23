# Riferimento del volume di carburante cedevole

[English](COMPLIANT_FUEL_VOLUME.md) · [简体中文](COMPLIANT_FUEL_VOLUME.zh-CN.md) · [Français](COMPLIANT_FUEL_VOLUME.fr.md) · [Русский](COMPLIANT_FUEL_VOLUME.ru.md) · [日本語](COMPLIANT_FUEL_VOLUME.ja.md) · [한국어](COMPLIANT_FUEL_VOLUME.ko.md) · [Deutsch](COMPLIANT_FUEL_VOLUME.de.md) · [Español](COMPLIANT_FUEL_VOLUME.es.md) · **Italiano** · [Português](COMPLIANT_FUEL_VOLUME.pt-BR.md)

## Ambito costitutivo

`CompliantFuelVolume` fornisce la chiusura costitutiva della rete di carburante in equilibrio. Risolve liquido, vapore e coesistenza con accumulo elastico da massa totale e temperatura o energia. Il percorso iniettore alimentato da pompa richiede ancora l’estensione dell’accoppiamento. [Rete di trasporto del carburante in equilibrio](EQUILIBRIUM_FUEL_NETWORK.it.md)

## Termodinamica

Densità e calore specifico del liquido sono costanti; il vapore è una pseudo-specie ideale. Le fasi condividono il riferimento materiale del film esistente. Il potenziale chimico liquido include `p/rho_l`. Quindi `p_0(T)` è il riferimento a spostamento trascurabile, non la pressione di saturazione del fluido puro con densità liquida finita. La parete ha pressione esterna nulla e immagazzina una sola volta il lavoro di pressione.

`StoredEnergyJoules` include energia calorica ed elastica; l’energia chimica del potere calorifico resta un bilancio separato nel collegamento al grafo.

```text
V = V_0 + C p = m_l / rho_l + m_v R T / p
E = m_l (cl T + a) + m_v cv T + C p^2 / 2
ln(p / p_0(T)) = p / (rho_l R T)       [coexistence]
h = (U_fluid + p V) / M
dE = T dS + mu dM                    [fixed vessel parameters]
```

La coesistenza sceglie la radice del vapore a bassa densità. L’entalpia latente è `L_u + R T - p/rho_l`; la derivata della pressione soddisfa Clapeyron. A massa fissa, la capacità termica deriva energia del fluido ed elastica includendo il trasferimento di fase. La parete non porta entropia.

## API Core

`Evaluate` riceve temperatura e massa totale. `Flash` riceve massa ed energia accumulata del fluido più elasticità. `AdvanceHeat` scambia calore con un bagno finito tramite conduttanza dichiarata e durata locale in secondi, usando Euler implicito. I nomi indicano le unità SI. Le chiamate locali non sostituiscono l’orologio intero limitato di `Simulation`.

`FuelVolumeStatus` distingue ingresso non valido, limiti di temperatura, massa ed energia e fallimento del solver; gli output rifiutati sono predefiniti. `Flash` conserva l’energia richiesta e segnala l’errore calorico di ricostruzione in virgola mobile in `CaloricResidualJoules`. Le istanze non conservano stato evolutivo e le chiamate riuscite non allocano memoria gestita.

## Validità

Dichiarare volume di riferimento, cedevolezza, densità liquida e pressione massima positivi. Il limite copre tutta la curva di coesistenza e resta sotto `rho_l R T_min`. L’espansione di coesistenza alla temperatura minima deve essere almeno `128 * epsilon` del volume di riferimento, con `epsilon = 2^-52`. Esclude deformazioni irrisolte e il ramo di vapore denso fuori dal modello dichiarato; non predice un punto critico.

Rispettare l’intervallo termico materiale e `0 < M <= rho_l (V_0 + C p_max)`. Vuoto, gas portante, miscele reali, dilatazione termica, inerzia della parete, tensione superficiale e cinetica di fase finita sono esclusi. I parametri restano `unverified`. Riempimento pompa da albero, collegamento iniettore, quantità di moto e flusso critico richiedono ulteriore accoppiamento ed evidenze indipendenti.

`AdiabaticReferenceVolume` sposta lo stesso materiale immutabile tra volumi espliciti a massa ed entropia fisse. La variazione di energia include elasticità ed è il lavoro meccanico. Valutazione variabile, flash e bagno finito rispettano i limiti dichiarati. [Pompa carburante a cambio di fase azionata da albero](FUEL_CHAMBER_PUMP.it.md)

## Evidenze indipendenti

Si confrontano potenziali chimici, pendenze di Clapeyron, derivate caloriche ed entropiche e Gibbs con accumulo elastico. Una massimizzazione indipendente dell’entropia vincolata non usa il solver di saturazione. Limiti di recipiente rigido, gas ideale e scala estensiva e raffinamento RK4 del bagno finito forniscono riferimenti indipendenti. Lo scambio conserva l’energia totale e converge al primo ordine con entropia totale non decrescente. Non dimostra validità misurata del carburante né accettazione Unity.

[NIST: pure-fluid equilibrium](https://pages.nist.gov/teqp-docs/en/main/algorithms/VLE.html) · [Cantera: constant-volume thermodynamics](https://cantera.org/stable/cxx/da/d94/PDSS__ConstVol_8cpp_source.html) · [VALIDATION.md](VALIDATION.it.md)
