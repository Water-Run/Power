# Régulation hydraulique de boîte AT

[English](AT_CONTROL.md) · [简体中文](AT_CONTROL.zh-CN.md) · **Français** · [Русский](AT_CONTROL.ru.md) · [日本語](AT_CONTROL.ja.md) · [한국어](AT_CONTROL.ko.md) · [Deutsch](AT_CONTROL.de.md) · [Español](AT_CONTROL.es.md) · [Italiano](AT_CONTROL.it.md) · [Português](AT_CONTROL.pt-BR.md)

## Contrat

`at_controller` accepte un rapport demandé entier dans [-1,4] ; zéro désigne le point mort. Il commande cinq paires de vannes de remplissage/vidange et le verrouillage facultatif du convertisseur. L'ordre est entrée du porte-satellites, petit soleil, grand soleil, frein du porte-satellites, frein du grand soleil, puis verrouillage.

Le débrayage est confirmé par la force réelle des garnitures avant l'application d'un rapport incompatible. Le PI de pression borné utilise la pression mesurée des chambres. Un rapport n'est actif qu'après confirmation des contacts et du verrouillage physique des embrayages. Les demandes fractionnaires et les écritures directes aux vannes réservées renvoient des erreurs exploitables.

Phase échantillonnée, défaut, intégrales de pression et temps font partie de l'état transactionnel complet. Annulation, échecs tardifs et branches conservent les mêmes historiques. Les défauts couvrent les délais de débrayage/engagement, la faible alimentation, le changement de sens et la perte de verrouillage confirmé. Une commande de vidange ne libère pas un drain physiquement bouché.

Le verrouillage facultatif utilise rapport avant, vitesse d'entrée, glissement et temporisation, avec hystérésis de déverrouillage séparée. La sortie décrit l'état réel Released/Applying/Locked/Releasing. Il s'agit d'un embrayage à piston physique, pas d'une égalité de vitesse imposée.

## Preuves et limites

Les exemples `controlled-hydraulic-ravigneaux` et `controlled-fired-hydraulic-ravigneaux` utilisent le canal 900 et l'ID 1400. Ils conservent 99 et 122 états déclarés dans la limite inchangée de 128. Le format v28 conserve routes, gains et horloges et lit v1-v27.

Ces commandes sont expérimentales et les paramètres restent `unverified`. Coordination du couple ECU, capteurs/vannes détaillés, défauts véhicule complets et calibration OEM restent à réaliser. Les contrôles gérés et Standard ne valident pas Unity Editor/Play/Player/IL2CPP réel.
