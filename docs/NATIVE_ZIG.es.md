# Frontera Zig nativa

[English](NATIVE_ZIG.md) · [简体中文](NATIVE_ZIG.zh-CN.md) · [Français](NATIVE_ZIG.fr.md) · [Русский](NATIVE_ZIG.ru.md) · [日本語](NATIVE_ZIG.ja.md) · [한국어](NATIVE_ZIG.ko.md) · [Deutsch](NATIVE_ZIG.de.md) · **Español** · [Italiano](NATIVE_ZIG.it.md) · [Português](NATIVE_ZIG.pt-BR.md)

El propietario reanudó la migración del lenguaje nativo el 2026-09-10. Los
prototipos nativos bajo `legacy/native` se han migrado a Zig, incluidas sus
pruebas y sus hosts. Siguen siendo un runtime de investigación separado: `Power.Core` y
`Power.Assets` conservan sus contratos gestionados sin dependencias y sus dos destinos,
y Unity sigue cargando los ensamblados gestionados.

## Interoperabilidad

La biblioteca compartida nativa conserva el punto de entrada versionado `pwr_get_api`,
campos escalares de ancho fijo, tamaños de estructura, tiempo entero en nanosegundos, handles
de generación, búferes de instantánea propiedad del llamador y la tabla de funciones. Las declaraciones `extern struct`
de Zig y las convenciones de llamada `.c` expresan el ABI binario existente;
no exigen fuentes ni cabeceras C en este repositorio. El ejecutor de experimentos
por P/Invoke de C# en la herramienta de build de .NET sigue siendo un consumidor de esta frontera.
Los prototipos nativos de motor,
escape y transmisión siguen siendo APIs internas de Zig, en lugar de
añadirse en silencio al esquema del modelo gestionado o a las capacidades nativas públicas.

La migración debe conservar las ecuaciones, los límites del modelo, los IDs estables, los diagnósticos,
la reversión de lotes, los libros de energía y de masa, y los escenarios de regresión. La fidelidad
de los prototipos nativos y la calibración `unverified` de las muestras no cambian. La
verificación nativa es independiente de la evidencia real de Unity Editor, Play Mode e IL2CPP
y de completar el objetivo del grupo motopropulsor completo.

## Procedencia

La implementación C original se puede recuperar del commit de Git
`c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3`. Las rutas y los hashes de sus archivos fuente
están registrados en `legacy/native/migration-manifest.json`. El puerto Zig conserva
los avisos originales de copyright y de GPL-3.0-or-later con la excepción de enlazado de Unity.
Los documentos históricos de diseño e investigación conservan sus citas
originales; sus descripciones de la época C no describen la build nueva.

El lanzador LuaInstaller no usado y el README de su empaquetado se retiraron el
2026-09-11. Sus rutas y hashes originales están en el mismo manifiesto
y remiten a la misma revisión de origen. El lanzador dependía de un puente
`power_native` no implementado y nunca formó parte de una build que funcionara. Las operaciones actuales de la CLI y
del modelo usan los hosts existentes de C#/JSON y de Zig, más el host del ABI de P/Invoke
de la herramienta de build de C#. Las propuestas de Lua
en los documentos históricos son registros de procedencia, no dependencias actuales ni
requisitos de implementación.


<a id="build-and-maintenance"></a>
## Compilación y mantenimiento

El compilador está fijado a Zig 0.15.2 en `.zig-version`. El comando
`install-zig` de la herramienta de build (dentro de `tools/Build.cs`) usa los
[metadatos oficiales de descarga de Zig](https://ziglang.org/download/index.json)
con hashes de archivo por plataforma confirmados en el repositorio. La build no necesita un traductor de C, cabeceras, CMake ni
compilación de fuentes C. Linux no necesita libc; macOS usa
la `libSystem` que proporciona el sistema. El puerto se tradujo una vez al principio
y después se dividió en módulos Zig mantenidos con diseños binarios compartidos. La memoria,
las funciones matemáticas y las operaciones atómicas usan Zig y las APIs del sistema operativo de la plataforma.
Las comprobaciones de seguridad siguen activadas en las builds ReleaseSafe.

Las fuentes Zig usan checkouts LF en todas las plataformas. En macOS, las herramientas
de verificación desactivan el descubrimiento del SDK de Apple solo en sus subprocesos de build de Zig
asignando `DEVELOPER_DIR=/dev/null`. Esto selecciona los stubs del enlazador Darwin incluidos en Zig
y evita la [incompatibilidad de Zig 0.15.2 con Xcode 26.4 y SDKs más nuevos](https://github.com/ghostty-org/ghostty/issues/11991),
cuyo stub de `libSystem` usa destinos arm64e. La selección de Xcode del sistema no cambia;
estos destinos nativos no necesitan frameworks de Apple ni cabeceras del SDK.

`dotnet run --file tools/Build.cs -- verify` ejecuta la verificación gestionada y después
la verificación nativa en serie. `native-verify` ejecuta solo la parte nativa.
La implementación de `native-verify` (dentro de `tools/Build.cs`) rechaza fuentes y cabeceras
C/C++, además de código fuente, bytecode y paquetes Lua. Comprueba
la fijación del compilador y el formato, compila la biblioteca y los dos hosts de Zig, ejecuta las
suites de Zig y la suite del host del ABI de P/Invoke de C#, y compara el experimento electrotérmico
con su fixture de línea base C original. En Linux comprueba además que
solo `pwr_get_api` se exporte de forma pública y que la biblioteca no tenga símbolos
externos sin resolver.

La comparación con la línea base conserva las huellas de modelo, las unidades, las correspondencias de canales,
11 instantes de muestra y los valores físicos. Los valores entre cadenas de herramientas usan tolerancias
absolutas y relativas explícitas; los hashes de repetición deben coincidir dentro del mismo binario.
Los informes bajo `artifacts/reports` distinguen la ejecución, los resultados de KPI y de repetición y
la calibración sin verificar. Las cachés del compilador ignoradas y los checkouts privados de referencia
de terceros no son fuente del repositorio.

## Correcciones de las pruebas del archivo

Tres puntos de entrada de prueba antiguos devolvían cero incluso cuando fallaba su macro `CHECK`.
El puerto propaga estos fallos. La suite del motor se detenía antes en un
fallo oculto porque su muestra final de carga de 22 Nm podía estar en el corte de combustible
del limitador de régimen. Ese escenario se conserva como prueba explícita del limitador; la combustión continua
usa una carga de prueba sintética de 32 Nm. La comparación de contrapresión parte ahora de un
estado de funcionamiento compartido y comprueba el incremento analítico del par de bombeo antes de
comparar la velocidad, evitando la confusión entre arranque y calado. No se cambiaron ecuaciones del motor ni
valores de calibración de producción. La cobertura del grupo motopropulsor automático
ejercita ahora también la repetición, la energía diferencial, la caída de alimentación y la reversión; la build
antigua de CMake omitía ese módulo entero.
