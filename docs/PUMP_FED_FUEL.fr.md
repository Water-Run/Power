# Rampe de carburant liquide alimentée par pompe

[English](PUMP_FED_FUEL.md) · [简体中文](PUMP_FED_FUEL.zh-CN.md) · **Français** · [Русский](PUMP_FED_FUEL.ru.md) · [日本語](PUMP_FED_FUEL.ja.md) · [한국어](PUMP_FED_FUEL.ko.md) · [Deutsch](PUMP_FED_FUEL.de.md) · [Español](PUMP_FED_FUEL.es.md) · [Italiano](PUMP_FED_FUEL.it.md) · [Português](PUMP_FED_FUEL.pt-BR.md)

## Contrat

`liquid_rail_feed` associe un injecteur liquide à une pompe volumétrique existante et à une frontière matière/thermique explicite. Le nœud de sortie hydraulique doit correspondre à la compliance et à la pression absolue initiale de la rampe. Pompe et injecteur possèdent ce nœud ; les autres chemins fluides non suivis sont rejetés.

L'énergie de pression est stockée une seule fois dans le nœud hydraulique. Débit et réaction d'arbre suivent la résolution couplée conservatrice. Le carburant entrant apporte énergie calorique et chimique ; le stockage calorique de la rampe mélange sa température. Le débit inverse signé retourne du carburant à la température actuelle de la rampe. Décharge, chauffage de paroi, vapeur et combustion prescrite restent distincts.

`pump-fed-liquid-cylinder` et `pump-fed-needle-cylinder` conservent l'injection physique et le mouvement d'aiguille facultatif. Les KPI de pression utilisent une borne déclarée de pompe seule avec unités explicites. Lire `total_fuel_delivered`, `reservoir_enthalpy` et `fuel_energy_in` sur l'ID 1511 ; la pompe ID 1510 expose le travail réel arbre-fluide.

## Preuves et limites

Le format v26 conserve les liens et la température source et lit v1-v25. Échange analytique arbre/pression, raffinement ODE simultané indépendant, mélange calorique, bilans masse/carburant/énergie/volume, retour inverse et rollback complet ont des contrôles séparés.

La source est une frontière externe explicite, pas un réservoir fini modélisé. Épuisement du réservoir, efficacité/régulation, pertes de lignes, cavitation, propriétés dépendant de pression et spray à volume fini restent ouverts. Paramètres `unverified` ; aucune calibration OEM ni acceptation réelle Unity Editor/Play/Player/IL2CPP n'est établie.
