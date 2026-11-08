# Tres recorridos completos: Bodega, Autorizador y Técnico

**Antecedente del flujo anterior.** Después de esta prueba Dante aclaró que el
Técnico registra el retiro con foto y que Bodega aprueba antes del Autorizador.
El [flujo corregido y su nuevo guion](../FLUJO_TERRENO_2026_10_25/README.md)
sustituyen esta secuencia. Los resultados y el script de esta carpeta son históricos;
para repetir con el código actual se debe usar el guion nuevo.

Validación del **25 de octubre de 2026**, hora de Chile, sobre la aplicación nueva
(`03_CODIGO`, API 5181 / Web 5286). Se interpretó el ejemplo solicitado como
**tres refrigerantes y tres cilindros por refrigerante: nueve cilindros en total**.

**Resultado: tres OT cerradas y 278 comprobaciones satisfactorias.** La solución
compiló con cero errores y cero advertencias. No se hicieron cambios funcionales
en la aplicación ni escrituras en PostgreSQL/Supabase.

## Evidencia de ejecución

- [Resultados completos, respuestas finales e historiales](ejecuciones/20261025-204238-19c246/resultados.json).
- [Script para repetir la prueba](verificar_tres_recorridos.py).
- Instancia de API: `635d5d2712714befb26e5e479b5784e5`.
- Ejecución satisfactoria: `20261025-204238-19c246`.
- Cuentas utilizadas: `bodega@afrisan.example`, `autorizador@afrisan.example` y
  `tecnico@afrisan.example`; son las cuentas públicas de revisión.

Las operaciones se ejecutaron mediante solicitudes HTTP reales con una sesión
firmada distinta por rol. Después se inició sesión en el navegador con las tres
cuentas y se comprobaron catálogo, inventario, listado de OT, detalles y fotografías.
Las capturas acreditan esas consultas; no representan la ejecución de todos los
formularios desde la interfaz.

## Datos de los tres ejemplos

Los nombres y valores son **sintéticos**. Los sufijos `P19C246` identifican este
ensayo y evitan confundir el catálogo con un catálogo operativo.

Todos los pesos se expresan en gramos. Capacidad significa masa máxima de gas;
peso bruto incluye tara. El contenido es peso bruto menos tara.

| Caso | Refrigerante | Sucursal | OT | Solicitado | Autorizado | Resultado |
| --- | --- | --- | --- | ---: | ---: | --- |
| 1 | R134A-P19C246 | Sucursal de prueba Norte | OT-000001 | 10.000 | 9.000 | Cerrada |
| 2 | R410A-P19C246 | Sucursal de prueba Centro | OT-000002 | 16.000 | 14.000 | Cerrada |
| 3 | R404A-P19C246 | Sucursal de prueba Sur | OT-000003 | 21.000 | 18.000 | Cerrada |

| Cilindro | Gas | Función en el ejemplo | Capacidad | Tara | Bruto inicial | Bruto final | Contenido final | Ubicación / estado final |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| AFR-000001 | R134A | Retirado de sucursal | 10.000 | 2.000 | 3.000 | 2.000 | 0 | Bodega / Vacío |
| AFR-000002 | R134A | Reemplazo instalado | 12.000 | 2.500 | 11.500 | 11.000 | 8.500 | Sucursal Norte / Instalado |
| AFR-000003 | R134A | Despachado y devuelto sin uso | 15.000 | 3.000 | 13.000 | 13.000 | 10.000 | Bodega / Disponible |
| AFR-000004 | R410A | Retirado de sucursal | 9.000 | 1.800 | 3.800 | 2.300 | 500 | Bodega / Disponible |
| AFR-000005 | R410A | Reemplazo instalado | 18.000 | 3.500 | 17.500 | 17.000 | 13.500 | Sucursal Centro / Instalado |
| AFR-000006 | R410A | Despachado y devuelto sin uso | 22.000 | 4.200 | 20.200 | 20.200 | 16.000 | Bodega / Disponible |
| AFR-000007 | R404A | Retirado de sucursal | 11.000 | 2.200 | 5.200 | 3.400 | 1.200 | Bodega / Disponible |
| AFR-000008 | R404A | Reemplazo instalado | 24.000 | 4.800 | 22.800 | 22.000 | 17.200 | Sucursal Sur / Instalado |
| AFR-000009 | R404A | Despachado y devuelto sin uso | 30.000 | 6.000 | 28.000 | 28.000 | 22.000 | Bodega / Disponible |

Los cilindros 003, 006 y 009 se dieron de alta sin lectura, se comprobó que el
contenido era desconocido y luego Bodega registró su primer pesaje. Los cilindros
001, 004 y 007 se registraron como existencias ya instaladas en sus sucursales;
crear una OT no trasladó cilindros desde Bodega para preparar el ejemplo.

## Acciones repetidas con tres ejemplos

| Usuario | Acciones ejecutadas | Cantidad |
| --- | --- | ---: |
| Bodega | Registrar refrigerante | 3 |
| Bodega | Registrar cilindros con valores diferentes | 9; tres por gas |
| Bodega | Registrar primer pesaje posterior al alta | 3 |
| Bodega | Despachar reemplazo y envase de reserva | 6; dos por OT |
| Bodega | Recibir envase retirado | 3 |
| Bodega | Recibir envase devuelto sin uso | 3 |
| Autorizador | Crear y asignar OT, distinguiendo solicitado y autorizado | 3 |
| Autorizador | Consultar evidencias y revisar incidencias | 3 revisiones; seis imágenes |
| Autorizador | Cerrar administrativamente la OT | 3 |
| Técnico | Consultar OT asignada | 3 |
| Técnico | Guardar y confirmar reemplazo con dos pesos y sus imágenes | 3 |
| Técnico | Informar devolución del retirado | 3 |
| Técnico | Registrar incidencia o solicitud de rectificación | 3 |
| Técnico | Informar término para revisión | 3 |

En cada recorrido se comprobó que el instalado quedara en Sucursal, que informar
la devolución no equivaliera a recepción y que solamente Bodega devolviera el
retirado al inventario físico. Se conservaron actor y fecha en los historiales.

Los motivos fueron distintos: etiqueta desgastada, demora de acceso a sala y
repetición de lectura de balanza. La revisión de la solicitud de rectificación
registra una conclusión; no modifica pesos confirmados.

## Bloqueos y reintentos comprobados

- Inventario sin sesión: 401.
- Técnico y Autorizador no pueden crear refrigerantes: 403.
- Bodega no puede autorizar OT; Técnico no puede despachar, recibir por Bodega
  ni cerrar administrativamente: 403.
- Refrigerantes y series duplicados: 409; peso mayor a tara más capacidad: 400.
- No se puede sobrescribir un primer pesaje: 409.
- Un cilindro en Bodega no puede seleccionarse como retiro de sucursal: 409.
- Confirmar reemplazo sin las fotos exigidas: 400; versión antigua: 409.
- Reintentar creación de OT, despacho y carga de imágenes no duplica registros.
- Cerrar sin recepción del retirado, con una entrega sin devolver o con incidencia
  sin revisar: 400. Los tres bloqueos se probaron por separado en cada OT.
- Despachar nuevamente después de cerrar: 409.
- Los QR descargados son PNG; las imágenes privadas se recuperan sin cambiar
  sus bytes. No se ensayó aquí la decodificación QR ni la cámara.

Las 278 comprobaciones incluyen respuestas HTTP y verificaciones de estado;
no son 278 escenarios independientes ni cobertura exhaustiva del sistema.

## Capturas del navegador

- [Bodega: los nueve cilindros](BODEGA_INVENTARIO.png).
- [Bodega: tres refrigerantes](BODEGA_REFRIGERANTES.png).
- [Autorizador: las tres OT cerradas](AUTORIZADOR_TRES_OT_CERRADAS.png).
- [Autorizador: caso 1 y evidencias](AUTORIZADOR_CASO_1.png).
- [Autorizador: caso 2 y evidencias](AUTORIZADOR_CASO_2.png).
- [Técnico: tres OT asignadas](TECNICO_TRES_OT.png).
- [Técnico: caso 3 y evidencias](TECNICO_CASO_3.png).

Las seis imágenes adjuntadas como pesajes fueron creadas para esta prueba y llevan
la marca **PRUEBA SINTÉTICA — NO ES UNA FOTO REAL**. Se probó su asociación,
obligatoriedad y consulta; no acreditan trabajos físicos ni lecturas de una balanza.

## Repetir el proceso

1. Iniciar la solución nueva con Visual Studio o `03_CODIGO/Iniciar-Revision.ps1`.
   No iniciar dos API ni reiniciar una instancia que contenga trabajo que se quiera
   conservar: el almacenamiento actual es en memoria.
2. Con Python 3 y Pillow disponibles, ejecutar desde la raíz del repositorio:

   ```powershell
   python AFRISAN_NUEVO/04_SEGUIMIENTO/VALIDACION_ROLES_2026_10_25/verificar_tres_recorridos.py
   ```

3. Consultar la carpeta nueva de `ejecuciones`. Cada repetición añade ejemplos
   únicos a la instancia y guarda sus propios resultados, QR e imágenes. Los
   códigos de cilindro y OT pueden cambiar si ya había datos.

El script comprueba `/health/ready` y modo Memoria antes de crear datos, solo
admite localhost/127.0.0.1 y no conserva tokens. Guarda el resultado parcial si
falla; no tiene una operación para borrar datos o reiniciar la API.

La primera ejecución parcial, `20261025-204202-1c1a33`, se conserva para trazabilidad:
el guion esperaba PendientePesaje en un envase ya instalado en Sucursal. Se corrigió
esa suposición del guion aplicando el primer pesaje diferido a un envase de Bodega,
como exige la implementación. Se reinició la API de prueba para quitar ese ejemplo
incompleto y luego se realizó la ejecución satisfactoria desde inventario vacío.
No fue necesario alterar la aplicación para que pasaran los tres recorridos.

## Antes de conectar la base de datos

Esta ejecución valida los tres recorridos indicados **en el proveedor temporal**.
Los ejemplos permanecen visibles mientras siga encendida la API; los archivos de
evidencia se conservan aunque se reinicie. No se realizó el traspaso de estos datos
a la base ni se declara listo el despliegue persistente.

Según la implementación y los pendientes documentados en `03_CODIGO/README.md`
y `03_CODIGO/TRABAJO_TECNICO.md`, todavía se debe resolver:

- Alinear esquema y migración con Autorizador, ubicaciones, pesaje inicial,
  estados, reemplazos, evidencias, recepciones, incidencias e historiales.
- Sustituir los stores en memoria por persistencia y transacciones; repetir
  estos mismos tres recorridos contra la implementación persistente y comprobar
  que los datos sobreviven al reinicio de la API.
- Aprovisionar usuarios reales, autenticación y almacenamiento privado durable
  de fotografías.
- Definir y validar los límites semanales/mensuales y la conciliación cuantitativa
  de reemplazos, que esta prueba no implementa ni acredita.

Tampoco se verificaron cámara física, funcionamiento sin red, sincronización entre
dispositivos, concurrencia real ni aislamiento entre varios técnicos: la demo solo
ofrece una cuenta de Técnico. Deben validarse por separado antes del uso operativo.
