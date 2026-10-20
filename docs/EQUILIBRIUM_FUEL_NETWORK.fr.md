# Réseau de transport de carburant en équilibre

[English](EQUILIBRIUM_FUEL_NETWORK.md) · [简体中文](EQUILIBRIUM_FUEL_NETWORK.zh-CN.md) · **Français** · [Русский](EQUILIBRIUM_FUEL_NETWORK.ru.md) · [日本語](EQUILIBRIUM_FUEL_NETWORK.ja.md) · [한국어](EQUILIBRIUM_FUEL_NETWORK.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_NETWORK.de.md) · [Español](EQUILIBRIUM_FUEL_NETWORK.es.md) · [Italiano](EQUILIBRIUM_FUEL_NETWORK.it.md) · [Português](EQUILIBRIUM_FUEL_NETWORK.pt-BR.md)

## Portée du modèle

Le domaine `fuel` transporte ensemble liquide pur et vapeur dans des récipients élastiques finis. Chaque récipient conserve indépendamment masse totale et énergie calorique plus élastique ; le flash détermine température, pression et fractions de vapeur. `fuel_restriction` échange le fluide donneur homogène et `fuel_heat_link` échange avec un nœud thermique fini. L'énergie chimique du pouvoir calorifique garde son bilan séparé.

Un nœud carburant utilise `storage` pour le volume de référence en `m3` ou `l`, `initial` pour la température en `k`, et `position` pour la masse totale en `kg`. Son objet `fuel` déclare propriétés, compliance et limites de pression/température. Les récipients connectés partagent densité, références calorique et de phase, limites thermiques et pouvoir calorifique. Les deux ports de restriction sont finis ; le lien thermique joint carburant et nœud thermique. L'ouverture est une fraction dans [0,1], avec canal facultatif.

## Écoulement conservatif

La restriction suit une loi dissipative quasi statique de volume de référence avec conductance K explicite en `m3_s_pa`. Elle néglige inertie du lien, énergie cinétique stockée et glissement de phase. La densité donneuse convertit le débit volumique en masse totale ; l'enthalpie donneuse transporte les deux phases. Aucune source extérieure de travail de pression ni chaleur supplémentaire de perte de charge n'est ajoutée. Le travail élastique est déjà stocké dans le récipient.

```text
Q_ref = K opening (p_s - p_r)
mdot = Q_ref / v_s
q = dt K opening (p_s' - p_r') / v_s'
H = q h_s'
M_s' = M_s - q; E_s' = E_s - H
M_r' = M_r + q; E_r' = E_r + H
h = (E - E_elastic + p V) / M
```

## Intégration temporelle et entropie

Un Euler implicite borné trouve les mêmes masse q et enthalpie H acceptées pour les deux récipients. Les liens avancent par ID de composant stable ; le bain fini utilise un autre sous-pas conservatif. Le couplage est d'ordre un. Masse, température ou pression hors limites et échecs rejettent tout le lot, entrées, historiques compensés et intervalles spéculatifs d'embrayage compris.

La thermodynamique compatible et une conductance positive entraînée par la pression donnent une production continue d'entropie de paire non négative. Euler implicite ajoute de l'entropie numérique. `entropy_produced` conserve la variation complète acceptée, mélange et discrétisation compris ; ce n'est pas une carte mesurée de pertes. Des contrôles indépendants complètent conservation et raffinement.

## Utilisation et preuves

Demander `equilibrium-fuel-network` ou `electrothermal-fuel-network` par MCP, ou exécuter le même JSON par CLI. Lire `mass`, `internal_energy`, `elastic_energy`, `vapor_mass`, `vapor_mass_fraction`, `vapor_volume_fraction`, `entropy` et `specific_enthalpy` par récipient. Ici `internal_energy` comprend la paroi élastique ; l'énergie chimique est séparée. Les liens exposent débits signés, masse cumulée, `transport_energy` et variation d'entropie de paire.

Les laboratoires couvrent décharge liquide vers coexistence, inversion, changements d'ouverture et couplage thermique fini/électromécanique. Une exponentielle indépendante de pression liquide, une EDO simultanée diphasique masse/enthalpie/chaleur, l'inversion par chauffage et la capture spéculative d'embrayage vérifient précision et composition. Assets et MCP réel conservent chaque valeur et hash échantillonnés. L'asset v34 conserve les propriétés et lit v1-v33. Unity Editor/Play/Player/IL2CPP réel reste non vérifié.

## Travail restant

Le transport carburant compose maintenant avec `fuel_chamber` entraîné par arbre et `fuel_check_valve` idéal. Injecteur physique, inertie/blocage sonique et mélanges réels mesurés restent séparés. Les paramètres restent `unverified`. [Pompe carburant diphasique entraînée par arbre](FUEL_CHAMBER_PUMP.fr.md)

[Modelica: two-port transport](https://doc.modelica.org/om/Modelica.Fluid.Interfaces.PartialTwoPortTransport.html) · [CompliantFuelVolume](COMPLIANT_FUEL_VOLUME.fr.md) · [VALIDATION.md](VALIDATION.fr.md)
