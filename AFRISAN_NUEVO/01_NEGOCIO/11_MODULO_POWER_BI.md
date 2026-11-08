# Módulo de gráficos con Power BI

Fecha: 20 de octubre de 2026.
Última actualización de alcance: **2 de noviembre de 2026**.

## Alcance confirmado

Dante recordó expresamente que la aplicación debe incluir un módulo de gráficos
para Power BI. Se incorpora al alcance de la nueva aplicación; la preparación
posterior del 02/11/2026 se describe en Estado actual.

El 02/11/2026 Dante dejó las emisiones de carbono como módulo opcional futuro.
La entrega actual mantiene Power BI para indicadores operativos; su aceptación
no depende de cálculos, factores ni gráficos de emisiones o CO₂e.

## Contenido para revisar

Propuesta de indicadores, sujeta a definir fórmulas y permisos:

- Inventario de cilindros por refrigerante, estado y ubicación.
- Kilos solicitados, autorizados e instalados por sucursal, refrigerante y período.
- Comparación con límites semanales y mensuales, una vez acordado su cómputo.
- OT por estado, sucursal y técnico.
- Cilindros retirados pendientes de recepción y diferencias registradas.
- Costos históricos según las cantidades y precios aplicados a cada operación.

Filtros propuestos: fechas, sucursal, refrigerante, técnico y estado de OT.

## Módulo opcional futuro: emisiones de carbono

Los indicadores ambientales de CO₂e, las fórmulas de emisiones y los factores
GWP quedan diferidos. No son requisitos de implementación ni de aceptación de
la entrega actual. Si se retoman, deberán acordarse masa, factores, fuentes y
metodología; los kilos de cilindros reemplazados no equivalen automáticamente
a emisiones reales. Se conserva el material existente como antecedente.

## Datos y acceso pendientes

Distribución inicial de esta preparación: Administrador y Jefatura, con alcance
global de gestión y autorización en la API. Otros roles no acceden al módulo.
Confirmar cualquier ampliación y el aislamiento por sucursal antes de compartir.

Preparar datos de consulta para BI a partir de los registros operativos,
conservando el historial de autorizaciones, precios y límites aplicados. La
integración no requiere exponer claves de usuario ni fotografías de pesaje.

La preparación usa importación manual por paquete privado y futura inserción
segura con inicio de sesión de Microsoft. Quedan por cerrar actualización
automática y requisitos de acceso/licencias. No hay servicio Power BI configurado.

## Estado actual

Dante solicitó preparar el módulo y confirmó que no tiene un informe publicado.
La base PostgreSQL y Storage ya están activados. La aplicación incorpora
`/power-bi`, vista previa sobre registros reales, filtros y descarga de seis
tablas CSV con consultas Power Query, medidas DAX, tema y guía para crear PBIX.
La configuración de ReportId/TenantId permite insertar un informe privado
cuando se publique. No se han agregado tablas, permisos ni datos remotos.

El contenido inicial cubre inventario, OT, kilos en cilindros instalados,
retirados pendientes e incidencias. Las fórmulas distinguen fechas de creación
y confirmación y excluyen borradores. Límites, costos y emisiones no se
representan como indicadores calculados mientras falte completar sus procesos.

No existe todavía PBIX publicado ni actualización automática. Las pruebas de
la aplicación no acreditan revisión visual del modelo en Power BI Desktop ni
inicio de sesión real de Microsoft. [Guía y límites](../03_CODIGO/POWER_BI.md).
