# Rail di combustibile liquido alimentato da pompa

[English](PUMP_FED_FUEL.md) · [简体中文](PUMP_FED_FUEL.zh-CN.md) · [Français](PUMP_FED_FUEL.fr.md) · [Русский](PUMP_FED_FUEL.ru.md) · [日本語](PUMP_FED_FUEL.ja.md) · [한국어](PUMP_FED_FUEL.ko.md) · [Deutsch](PUMP_FED_FUEL.de.md) · [Español](PUMP_FED_FUEL.es.md) · **Italiano** · [Português](PUMP_FED_FUEL.pt-BR.md)

## Contratto

`liquid_rail_feed` associa un iniettore liquido a una pompa volumetrica esistente e a un confine esplicito di materia/calore. Il nodo di uscita idraulica deve corrispondere alla cedevolezza e alla pressione assoluta iniziale del rail. Pompa e iniettore possiedono questo nodo; altri percorsi fluidi non contabilizzati sono rifiutati.

L'energia di pressione è memorizzata una volta nel nodo idraulico. Portata e reazione d'albero seguono la soluzione accoppiata conservativa. Il combustibile entrante porta energia termica e chimica; lo stoccaggio termico del rail miscela la temperatura. Il flusso inverso con segno restituisce combustibile alla temperatura attuale del rail. Scarico, riscaldamento parete, vapore e combustione prescritta rimangono separati.

`pump-fed-liquid-cylinder` e `pump-fed-needle-cylinder` conservano iniezione fisica e movimento opzionale dell'ago. I KPI di pressione usano un limite dichiarato della sola pompa con unità esplicite. Leggere `total_fuel_delivered`, `reservoir_enthalpy` e `fuel_energy_in` sull'ID 1511; la pompa ID 1510 espone il lavoro reale albero-fluido.

## Evidenze e limiti

v26 conserva collegamenti e temperatura sorgente e legge v1-v25. Scambio analitico albero/pressione, raffinamento ODE simultaneo indipendente, miscelazione termica, bilanci massa/combustibile/energia/volume, ritorno e rollback completo hanno verifiche separate.

La sorgente è un confine esterno esplicito, non un serbatoio finito modellato. Svuotamento, efficienza/regolazione pompa, perdite tubazioni, cavitazione, proprietà dipendenti dalla pressione e spray a volume finito restano aperti. Parametri `unverified`; non si stabilisce calibrazione OEM o accettazione reale Unity Editor/Play/Player/IL2CPP.
