# Fuentes oficiales consultadas

Consulta: 20 de octubre de 2026. Estas fuentes respaldan decisiones técnicas;
no sustituyen los requisitos del cliente ni la pauta académica. No se configuró
ningún servicio externo durante la revisión.

## Plataforma .NET

Microsoft fija el fin de soporte de .NET 8 el **10 de noviembre de 2026**.
Al corte de esta revisión sigue soportado, pero quedan 21 días. La decisión
vigente de Dante es mantener .NET 8 conforme a la arquitectura. Recomendación:
mantener estable la entrega del jueves y registrar después una decisión de
continuidad/migración si el uso del sistema supera esa fecha. No se migró ni
se considera aprobada una migración por este informe.

Fuente: [Política de soporte de .NET](https://dotnet.microsoft.com/en-us/platform/support/policy).

## Conexión PostgreSQL y Power BI

Supabase distingue conexión de backend, Data API y conexión de herramientas BI.
Power BI dispone de un conector PostgreSQL. Por ello, no es necesario exponer
la tabla Usuario por la Data API para construir informes. La propuesta para
AFRISAN es una identidad de consulta con acceso solo a vistas analíticas y
configuración de conexión apropiada a la red; su funcionamiento aún no se probó.

Fuentes: [Conexiones a Supabase PostgreSQL](https://supabase.com/docs/guides/database/connecting-to-postgres)
y [Conector PostgreSQL de Power Query](https://learn.microsoft.com/en-us/power-query/connectors/postgresql).

## Presentación de informes dentro de AFRISAN

La opción de incrustación segura de Power BI exige autenticación y permisos de
Power BI, además de las licencias o capacidad aplicables. El login propio de
AFRISAN no concede esos permisos automáticamente. Microsoft también requiere
HTTPS para la página anfitriona de esta opción; la app actual funciona en HTTP
local. Los filtros por URL no son controles de seguridad. Esto debe resolverse
antes de prometer un informe integrado disponible a todos los roles.

Fuente: [Incrustar informes de forma segura](https://learn.microsoft.com/en-us/power-bi/collaborate-share/service-embed-secure).

La opción «Publicar en la web» permite acceso público sin autenticación, incluido
el acceso a datos subyacentes. No sirve para restringir datos operativos privados
por los roles de AFRISAN. Una maqueta con datos completamente simulados debe
seguir identificándose como maqueta, sin afirmar integración productiva.

Fuente: [Publicar en la web](https://learn.microsoft.com/en-us/power-bi/collaborate-share/service-publish-to-web).

## Modelo de informes

Microsoft recomienda definir consistentemente qué representa cada fila de
hechos y relacionarla con dimensiones. Aplicación propuesta a AFRISAN: separar
cantidades pedidas/autorizadas, operaciones físicas y recepciones; utilizar
dimensiones de fecha, sucursal y refrigerante. Unir una operación directamente
a sus muchas fotos antes de sumar puede multiplicar sus kilos. Esto no requiere
crear otra plataforma de datos para el capstone.

Fuente: [Esquema de estrella en Power BI](https://learn.microsoft.com/en-us/power-bi/guidance/star-schema).

## Evidencias fotográficas duraderas

Supabase Storage permite buckets privados y acceso autenticado o mediante enlaces
firmados temporales. Propuesta: metadatos del pesaje en PostgreSQL y fotos en
almacenamiento privado, con validación de acceso a la OT en la API. El esquema
SQL o una ruta de imagen por sí solos no prueban que el archivo esté conservado.

Fuente: [Buckets de Supabase Storage](https://supabase.com/docs/guides/storage/buckets/fundamentals).

## Emisiones y movimiento de gas

La guía técnica de EPA describe métodos de estimación basados en balances de
refrigerante, inventarios y entradas/salidas. Su utilidad aquí es metodológica;
no se presenta como obligación normativa chilena. Inferencia para AFRISAN:
reemplazar un cilindro no demuestra por sí solo que su contenido se emitió.
Antes de mostrar CO₂e deben definirse la masa estimada emitida, el método y el
factor aplicable. Un dato desconocido no debe representarse como cero.

Fuente: [Guía de emisiones fugitivas de refrigerantes, sección 2.3](https://www.epa.gov/sites/production/files/2020-12/documents/fugitiveemissions.pdf).
