# Procesos del Autorizador

Actualización confirmada el 25/10/2026:
[revisión de fotos y registro después de la aprobación de Bodega](12_FLUJO_TERRENO_DESPACHO_Y_REVISION.md).
El Autorizador conserva la preparación de OT y realiza su revisión final solamente
después de la confirmación/aprobación de los cilindros y pesos por Bodega.

Fecha: 20 de octubre de 2026. Responsable del proyecto: Dante Moya.

Estado: recorrido para revisión. Las responsabilidades 1, 2, 7, 8 y 9 fueron
confirmadas por Dante en la conversación; los pasos detallados siguientes
organizan esos acuerdos y no constituyen aprobación del cliente.

## 1. Recibir la solicitud y crear la OT

Entrada: una sucursal solicita reemplazar o agregar cilindros físicos.

1. Identificar la sucursal y registrar el motivo de la solicitud.
2. Indicar los tipos de refrigerante y los kilos solicitados por cada tipo.
3. Identificar los cilindros que se reemplazarán, cuando corresponda.
4. Diferenciar reemplazo de cilindro y agregado sin retiro.
5. Registrar la solicitud para su autorización, conservando lo solicitado.

Resultado: OT con su necesidad identificada. Cada cilindro tiene un solo tipo
de refrigerante; una OT puede incluir varios cilindros y refrigerantes.

## 2. Autorizar y asignar un técnico

1. Revisar la solicitud y la información de inventario disponible.
2. Consultar los límites semanales y mensuales de la sucursal y los antecedentes
   de consumo, cuando el cálculo de esos límites esté definido e implementado.
3. Registrar la cantidad autorizada por refrigerante, diferenciándola de la
   solicitada. La cantidad realmente instalada se registra durante la ejecución.
4. Asignar un técnico responsable a la OT.
5. Dejar identificados el autorizador y la fecha de autorización.
6. Dejar la OT disponible para que Bodega prepare y entregue los cilindros.

Resultado: trabajo autorizado y técnico asignado. No se exige un segundo
autorizador sin una regla expresa. Rechazo, anulación, reasignación y cambios
posteriores a la autorización requieren definir sus condiciones.

## 3. Verificar el trabajo y cerrar la OT

Entrada: el técnico informa que terminó su trabajo y Bodega registra las
recepciones físicas que correspondan.

1. Comparar lo solicitado, autorizado y realizado.
2. Revisar los cilindros retirados e instalados y sus pesajes.
3. Revisar las fotografías asociadas a cada cilindro y pesaje: en un reemplazo
   hay evidencia del retirado y del instalado; pueden existir varias fotografías.
4. Revisar observaciones, trabajo parcial, diferencias e incidencias.
5. Verificar que Bodega haya recibido los cilindros retirados y los entregados
   al técnico que finalmente no fueron utilizados.
6. Registrar la revisión y cerrar administrativamente la OT cuando corresponda.

El cilindro instalado permanece en la sucursal; no se espera su retorno a Bodega
como condición normal del mismo trabajo. El Autorizador verifica la recepción;
la recepción física y su registro corresponden a Bodega.

Las condiciones del cierre son una propuesta apoyada por el prototipo actual.
Falta acordar cómo devolver una OT al técnico para corrección y cómo tratar las
excepciones que impidan completar una recepción.

## 4. Revisar excesos y antecedentes de autorización

1. Revisar alertas y diferencias frente a los límites de la sucursal.
2. Consultar las OT, cantidades, entregas, registros técnicos y autorizaciones
   involucradas en el período.
3. Dejar registro de información incorrecta o diferencias detectadas.
4. Documentar los participantes y sus acciones para que Jefatura investigue las
   responsabilidades, sin atribuir culpabilidad automáticamente.
5. Registrar la revisión o derivación y conservar los antecedentes.

Reglas confirmadas:

- Bodega y Jefatura definen límites semanales y mensuales por sucursal.
- Si Jefatura modifica un límite, Bodega no puede editarlo hasta que Jefatura
  vuelva a habilitarlo. Autorizador tampoco puede saltarse esa protección.
- Superar un límite exige registro y revisión; no se ha confirmado un bloqueo
  automático del trabajo.

Pendientes: definir si Autorizador puede modificar límites no protegidos; si
una modificación suya bloquea a Bodega; qué kilos se acumulan y qué fechas
delimitan las semanas y meses. No se implementarán esos permisos por inferencia.

## 5. Revisar y autorizar correcciones de información

1. Recibir una solicitud que identifique la OT, el registro afectado y el error.
2. Revisar la evidencia y el motivo presentado.
3. Registrar una conclusión y la identidad y fecha de quien revisó.
4. Cuando se permita una corrección, conservar valor anterior, nuevo valor,
   motivo, responsable y fecha; mantener accesible la evidencia original.

Falta precisar qué datos puede corregir el Autorizador, cómo se ajustan pesos
ya confirmados y si se puede modificar una OT cerrada o se exige reapertura.
No se considera aprobada la eliminación de movimientos ni de evidencia histórica.

## Relación entre roles

Jerarquía confirmada: **Bodega → Autorizador → Jefatura**.
No implica heredar automáticamente todas las tareas de Bodega. El Técnico
realiza el reemplazo/agregado y registra la evidencia; Bodega entrega y recibe;
Autorizador autoriza y revisa; Jefatura interviene según su autoridad y las
reglas de límites acordadas.

## Estado de la implementación (actualizado al 02/11/2026)

La implementación vigente en `../03_CODIGO` persiste en PostgreSQL (`afrisan`) la creación
manual de OT y asignación de técnico por el Autorizador, la resolución de incidencias y la
revisión final y cierre administrativo con validaciones de estado en servidor (`PgTrabajo.cs`).
Las fotografías se leen desde el bucket privado `afrisan-evidencias` en Supabase Storage únicamente
después de que Bodega registra su aprobación (`RevisionOT`). Los límites semanales/mensuales y la
integración con Defontana mantienen sus definiciones funcionales pendientes con la contraparte.
