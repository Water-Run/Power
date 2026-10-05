# Realimentación de AT hidráulica

[English](AT_CONTROL.md) · [简体中文](AT_CONTROL.zh-CN.md) · [Français](AT_CONTROL.fr.md) · [Русский](AT_CONTROL.ru.md) · [日本語](AT_CONTROL.ja.md) · [한국어](AT_CONTROL.ko.md) · [Deutsch](AT_CONTROL.de.md) · **Español** · [Italiano](AT_CONTROL.it.md) · [Português](AT_CONTROL.pt-BR.md)

## Contrato

`at_controller` acepta una marcha solicitada entera en [-1,4]; cero es punto muerto. Controla cinco pares de válvulas de llenado/vaciado y el bloqueo opcional del convertidor. El orden es entrada del portasatélites, solar pequeño, solar grande, freno del portasatélites, freno del solar grande y bloqueo.

Antes de aplicar una marcha incompatible, la fuerza real de las pastillas confirma la liberación. El PI de presión limitado usa presión medida en las cámaras. Una marcha solo está activa al confirmar los contactos y el bloqueo físico de los embragues. Las solicitudes fraccionarias y las escrituras directas a válvulas controladas devuelven errores útiles.

Fase muestreada, fallo, integrales de presión y tiempos forman el estado transaccional completo. Cancelación, fallos tardíos y ramas conservan las mismas historias. Los fallos incluyen tiempos de liberación/aplicación, baja alimentación, cambio de sentido y pérdida de bloqueo confirmado. Una orden de vaciado no libera un drenaje físicamente bloqueado.

El bloqueo opcional usa límites de marcha adelante, velocidad de entrada, deslizamiento y espera, con histéresis de desbloqueo separada. La salida describe el estado real Released/Applying/Locked/Releasing. Es un embrague de pistón físico, no una igualdad de velocidades impuesta.

## Evidencia y límites

`controlled-hydraulic-ravigneaux` y `controlled-fired-hydraulic-ravigneaux` usan canal 900 e ID 1400. Conservan 99 y 122 estados declarados dentro del límite sin cambios de 128. v25 conserva rutas, ganancias y relojes y lee v1-v24.

Son controles de investigación y los parámetros siguen `unverified`. Coordinación de par ECU, sensores/válvulas detallados, fallos completos del vehículo y calibración OEM siguen pendientes. Las pruebas managed y Standard no acreditan Unity Editor/Play/Player/IL2CPP real.

