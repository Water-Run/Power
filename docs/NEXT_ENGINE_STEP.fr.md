# Notes de reprise du développement moteur

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · **Français** · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Point de reprise

Le réservoir résout le transfert vapeur à volume fini avec potentiels corrigés et aspiration liquide séparée.

[Équilibre du réservoir à volume fini](TANK_PHASE_EQUILIBRIUM.fr.md)

La fermeture du récipient déformable de carburant pur résout liquide, vapeur et coexistence avec stockage élastique. Cette référence constitutive locale attend encore son intégration au transport par pompe et buse du graphe.

[Référence de volume de carburant déformable](COMPLIANT_FUEL_VOLUME.fr.md)

## Prochaine étape

Relier cette fermeture au transport conservatif diphasique par pompe et rampe, puis résoudre pertes locales d'entrée, quantité de mouvement du mélange et décharge de buse. Garder propriétés explicites et couplage complet volume/travail. Les mesures OEM absentes restent absentes. Poursuivre séparément allumage, admission/échappement, pertes mécaniques et coordination ECU/TCU.

## Acceptation

Exiger références analytiques ou limites indépendantes, bilans complets masse/énergie et raffinement du pas adapté. Préserver retour arrière complet, annulation, branches, canaux stables, rejeu portable et anciens lecteurs. Continuer ensuite allumage, admission/échappement, pertes mécaniques, transmission et coordination ECU/TCU.

Les exemples couplés pompe/chaleur/évent montrent un raffinement du premier ordre. Les deux phases sources restent dans l'intervalle thermique déclaré et sous la densité liquide avec marge d'arrondi binary64. Densité et propriétés gazeuses constantes, équilibre instantané et interface plane sont des approximations de recherche. Mélanges réels, tension superficielle, ballottement, cinétique finie, calibration mesurée et Unity Editor/Play/Player/IL2CPP réel restent non vérifiés.

[DEVELOPMENT_STATUS.fr.md](DEVELOPMENT_STATUS.fr.md) · [ROADMAP.fr.md](ROADMAP.fr.md) · [VALIDATION.fr.md](VALIDATION.fr.md)
