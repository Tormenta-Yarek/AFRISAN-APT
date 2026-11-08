# Contexto del negocio y acuerdos

Fecha: 19 de octubre de 2026. Responsable actual: Dante Moya.

## Contexto documental

AFRISAN presta servicios de refrigeración y utiliza cilindros para transportar
refrigerante desde bodega a trabajos en sucursales de clientes. El seguimiento
debe permitir identificar cada envase, su contenido, la custodia, los consumos,
su retorno y eventual disposición con Regener.

La operación comienza antes del despacho: se necesita conocer qué refrigerantes
existen y cuáles son los cilindros que los contienen. Defontana aporta referencias
de OT y guías; AFRISAN debe mantener la trazabilidad unitaria del envase.

Fuentes: acta de constitución y Product Backlog en
EVIDENCIAS_GRUPALES/Documentación del Proyecto/ACTUAL, desde la raíz del repositorio.

## Acuerdos expresos de esta conversación

- Entender el negocio antes de decidir datos y pantallas.
- El primer proceso a programar es el registro de refrigerantes y cilindros.
- **Cada cilindro tiene un único tipo de refrigerante.** Un tipo de refrigerante
  puede estar asociado a muchos cilindros.
- Se debe considerar la cantidad de gas al iniciar su seguimiento.
- Mantener las tecnologías definidas en acta, arquitectura y backlog: .NET 8,
  ASP.NET Core Web API, Blazor WebAssembly, EF Core y PostgreSQL/Supabase.
- No crear tabla Permisos; el diseño utiliza un rol por usuario.
- Los scripts de instalación solicitados más recientemente no deben precargar
  roles, usuarios ni otros registros.
- El trabajo nuevo se organiza aparte de los proyectos anteriores. Dante es el
  único integrante actual; se conservan los antecedentes históricos.
- En la OT descrita por Dante, el técnico reemplaza físicamente los cilindros.
  No se presupone que el trabajo sea recargar equipos con gas de un envase móvil.
- El técnico debe tomar fotos de los pesajes de ambos cilindros de cada
  reemplazo: retirado y reemplazo. Admitir varias fotos por operación y
  distinguir a qué envase/pesaje corresponde cada una.
- Bodega y jefatura pueden definir los límites de kilos por sucursal.
  Dante precisó que serán semanales y mensuales por sucursal. Ante exceso se
  registra la incidencia para investigar información/autorizaciones de técnicos
  y bodegueros con jefatura. Desglose y forma de computar kilos pendientes.
- Si jefatura modifica un límite, bodega no puede modificarlo hasta que jefatura
  vuelva a habilitar expresamente esa edición. Jefatura conserva prioridad.
- Autorizador es un rol nuevo distinto de Jefatura. Dante le asigna procesos
  1, 2, 7, 8 y 9 de la lista de pendientes; bodega 3, 6 y 8; técnico 4.
  La numeración de esa lista no es la numeración PN de los archivos.
- La jerarquía de negocio, de menor a mayor, es **Bodega → Autorizador → Jefatura**.
  No implica herencia automática de todas las funciones ni define el lugar de
  Técnico/Administrador en esa cadena. Autorizador no invalida el bloqueo de Jefatura.

## Distinciones necesarias

| Concepto | Significado |
| --- | --- |
| Refrigerante | Tipo de gas del catálogo, por ejemplo su código comercial. |
| Cilindro | Envase físico identificable que contiene un único refrigerante. |
| Capacidad | Máxima cantidad de refrigerante admitida; no es stock actual. |
| Tara | Masa del envase vacío. |
| Peso bruto registrado | Lectura de balanza: envase más contenido, con fecha. |
| Contenido según pesaje | Peso bruto menos tara. |
| Contenido estimado | Cálculo posterior a cargas declaradas; no es un nuevo pesaje. |

El esquema propuesto usa gramos enteros. La interfaz puede convertir a kg,
indicando siempre la unidad. El catálogo no representa un lote de gas: la
cantidad corresponde al contenido de los cilindros.

## Lo que aún no se da por definido

- Ingreso de cilindros nuevos, recargados o ambos.
- Forma de obtener y conservar el pesaje inicial.
- Si el mismo envase puede recargarse y qué historial requiere ese proceso.
- Si está permitido cambiar su tipo de refrigerante mediante un procedimiento
  especial. La regla acordada no autoriza mezclar gases ni cambiarlo libremente.
- Retornos siempre vacíos o también con contenido restante.
- Responsables exactos de registrar catálogos, autorizar, revisar y corregir.
- Si una OT puede abarcar varias sucursales o varias guías. Cada OT debe tener
  un técnico asignado; la posible reasignación todavía debe definirse.

El script existente no reemplaza estas decisiones. Su estructura debe ajustarse
cuando se confirme cada proceso.
