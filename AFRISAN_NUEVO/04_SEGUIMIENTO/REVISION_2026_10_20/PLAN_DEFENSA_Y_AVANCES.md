# Plan de preparación y próximos avances

**Dante Moya · 20 de octubre de 2026**

Entrega informada: jueves 22 de octubre a las 7. No se confirmó AM/PM; el plan
termina el miércoles. Disponibilidad: tres horas diarias. Se asume que todavía
se dispone de tres horas hoy; si son menos, se reduce el ejemplo y se conserva
el tiempo de informe y ensayo.

Por instrucción expresa, todos los resultados permanecen en esta carpeta local.
**No crear commits ni hacer push hasta una petición posterior de Dante.**

## 1. Objetivo concreto de la entrega

Defender el problema del cliente, la arquitectura elegida, la evolución de los
requisitos y un recorrido del prototipo con evidencia. Mostrar con precisión
qué se implementó, qué se comprobó y qué sigue pendiente. La frase «mitad del
avance» no acredita por sí sola 50% de historias aceptadas.

La demostración elegida debe corresponder a `Afrisan.Nuevo.slnx`, Web 5286 y API
5181. La app antigua de 5226 se conserva como antecedente; evitar mezclar sus
pantallas, modelo de datos o resultados con la solución nueva.

## 2. Martes: tres horas

| Tiempo | Trabajo | Resultado |
| --- | --- | --- |
| 30 min | Fijar alcance de defensa y consultar al docente modalidad individual, significado del avance, horario y evidencia de almacenamiento exigida. Revisar solución/puertos y seleccionar un ejemplo. | Alcance escrito y asuntos académicos enviados por Dante; continuar lo independiente mientras responde. |
| 90 min | Recorrer una sola OT de reemplazo con datos simulados y corregir únicamente bloqueos reproducidos del ejemplo. Una pestaña de edición por operación; registrar limitaciones conocidas. | Registro de pasos y resultados observados. Si no se completa, el informe indica el punto alcanzado y el defecto. |
| 45 min | Capturar evidencia de compilación y del recorrido realmente ejecutado, incluyendo API, fotos y recepción/cierre si funcionan. Conservar capturas o video de respaldo. | Evidencia fechada con versión/ruta, datos de ejemplo y resultado, sin afirmar persistencia en nube. |
| 15 min | Ordenar los archivos elegidos, revisar nombres, datos privados y coherencia del corte. | Paquete local preparado; Git/publicación siguen pendientes. |

La pauta sí considera almacenamiento dentro de IE3. Mostrar SQL como diseño y
RAM como almacenamiento temporal puede no satisfacer la expectativa concreta
del docente sobre persistencia. Si exige base operativa este jueves, ese requisito
debe desplazar tareas menos importantes y acotar el incremento, por ejemplo a
catálogo/inventario persistente correctamente modelado. No conectar el flujo
completo al SQL incompatible ni prometer que esa integración cabe en 90 minutos.

## 3. Miércoles: tres horas

| Tiempo | Trabajo | Resultado |
| --- | --- | --- |
| 60 min | Actualizar informe de avance o añadir adenda del 20/10; alinear el plan de ocho columnas, responsable Dante y estados. Revisar abstract bilingüe y conclusiones/reflexión individual en inglés según pauta. | Informe que describe la misma app de la demostración y explica los ajustes. |
| 45 min | Completar evidencia de validaciones críticas y corregir solo bloqueos del recorrido. | Resultados observados para permisos, pesajes y fotos obligatorias. |
| 60 min | Preparar y ensayar la explicación del problema, flujo, arquitectura, implementación, pendientes y plan siguiente. | Defensa ensayada y respaldo visual del flujo. |
| 15 min | Revisar archivos/enlaces, copia local y arranque de ambos proyectos. | Carpeta de entrega ordenada; estado de publicación explícito. |

Si un problema consume el bloque reservado, dejarlo documentado y ajustar lo
que se demuestra. El tiempo del informe y ensayo también forma parte de la
entrega. No reservar trabajo imprescindible para el jueves.

## 4. Ejemplo propuesto para validar, todavía no ejecutado por esta investigación

Usar un refrigerante y dos cilindros diferentes del mismo tipo. Registrar tara,
capacidad y pesos consistentes; identificar todos los datos como demostración.

1. Bodega registra refrigerante y los dos envases. Si se muestran cuatro tipos
   por el objetivo del acta, no afirmar que eso prueba cuatro recorridos completos.
2. Autorizador crea la OT para una sucursal y un técnico, con solicitado y
   autorizado. Selecciona el envase que representa la existencia inicial en
   sucursal. Explicar el atajo temporal de ubicación que utiliza el prototipo.
3. Bodega entrega el envase de reemplazo.
4. Técnico abre su OT, registra retirado e instalado, ingresa ambos pesos y
   adjunta varias fotos identificadas por pesaje. Las fotos de ejemplo no se
   presentan como evidencia de un servicio real del cliente.
5. Técnico confirma el cambio, informa devolución y término.
6. Bodega registra la recepción del retirado y su peso.
7. Autorizador revisa y cierra; mostrar ubicación final de ambos envases.

Resultados esperados: instalado en sucursal; retirado recibido en Bodega;
operación y fotografías identificadas; actores y fechas visibles. No hacer
pasar la diferencia entre dos pesos brutos de envases distintos por consumo.

Casos de comprobación seleccionados para el bloque de evidencia:

- Rechazo de peso menor que tara o superior a tara más capacidad.
- Imposibilidad de confirmar reemplazo sin evidencia de ambos pesajes.
- Imposibilidad de realizar una acción de Autorizador con credenciales de Técnico.
- Imposibilidad de cerrar sin la recepción exigida de Bodega.
- Caso C03: borrador extra que bloquea término; si se corrige, conservar resultado
  antes/después. No crear operaciones ficticias para ocultar el bloqueo.

El aislamiento entre dos técnicos requiere dos identidades de prueba; hoy solo
hay una cuenta demo de Técnico. No atribuir esa prueba a un simple cambio de rol.
Los escenarios de varias OT, conflictos entre pestañas y reinicio/offline son
parte del siguiente bloque de validación; su omisión en la demo debe constar.

Antes de reiniciar la API, conservar evidencia de pantalla/video: el servidor
actual perderá sus datos. Un respaldo visual permite explicar el avance, pero
no sustituye la persistencia ni demuestra que el software funcione sin red.

## 5. Qué incluir en el informe de avance

Reutilizar el informe EVA2 con una actualización o adenda que contenga:

- Corte y solución exactos que se muestran.
- Continuidad individual y capacidad de tres horas diarias; acuerdos del docente
  si se reciben. Autorías anteriores conservadas como antecedentes.
- Cambio de negocio: reemplazo físico, OT/técnico, roles y límites confirmados.
- Evidencia de cada función realmente comprobada y sus resultados.
- Lista breve de defectos y limitaciones relevantes, con prioridad y próximo paso.
- Estado de Supabase, Power BI, límites, costos/emisiones y pruebas pendientes.
- Plan de ocho columnas actualizado, siguiendo la guía 2.4.

Conservar los estados documentales En curso / Con retraso / No iniciado /
Completado / Ajustada. La existencia de código no convierte automáticamente
una historia en aceptada. Registrar el criterio de cierre de cada incremento.

## 6. Preguntas probables de la defensa

| Pregunta | Evidencia o explicación que conviene preparar |
| --- | --- |
| ¿Qué problema resuelve? | Seguimiento por cilindro, gas, ubicación, pesaje, técnico y autorización; ejemplo de información que antes no quedaba relacionada. |
| ¿Por qué existe una API? | Centraliza reglas y autorización entre la Web y los datos; arquitectura documentada. Mostrar una validación concreta del servidor. |
| ¿Dónde se guardan los datos? | RAM central e IndexedDB local en el prototipo; Supabase aún no integrado. Mostrar la diferencia con el diseño futuro. |
| ¿Qué cambió desde la Fase 1? | Equipo individual, refinamiento de la operación física y nueva aplicación; explicar impacto sobre SQL y aceptación. |
| ¿Cómo sabes que funciona? | Pasos ejecutados, resultado esperado/observado y evidencia. No utilizar solo la compilación como prueba de negocio. |
| ¿Qué hace Power BI? | Requisito confirmado, indicadores previstos y fuentes necesarias. Señalar que la integración sigue pendiente. |
| ¿Por qué no está todo terminado? | Entrega de avance, alcance medido por evidencia, riesgos detectados y secuencia concreta del siguiente incremento. No prometer un porcentaje sin base. |
| ¿Qué hiciste tú? | Aportes actuales de Dante y explicación propia de decisiones/código; contribuciones históricas atribuidas correctamente. |

Si se necesita hablar en porcentajes, primero acordar con el docente qué se
mide, cuál es el alcance de referencia y qué cuenta como terminado. Mostrar
estados y evidencia mientras esa definición no exista.

## 7. Secuencia de desarrollo posterior a la defensa

| Orden | Incremento | Condición para darlo por terminado |
| --- | --- | --- |
| 1 | Consolidar reglas y corregir defectos del flujo en memoria | Un borrador puede descartarse de forma trazable; custodia y recepción pertenecen a una entrega/OT; validaciones de estado y concurrencia coherentes. |
| 2 | Modelo de datos alineado | MER, contratos y reglas describen reemplazos, pesajes, fotos, sucursal/usuario estables y fechas físicas/servidor; incompatibilidades SQL resueltas explícitamente. |
| 3 | Persistencia de un recorrido completo | Registro → OT → entrega → reemplazo → recepción → cierre conserva datos después de reiniciar. Fotos privadas accesibles según OT; recuperación de errores y transacciones comprobadas. |
| 4 | Usuarios reales y autorización | Una estrategia de autenticación coherente con la arquitectura, usuarios/roles/activo persistidos, credenciales protegidas, acceso ajeno denegado. Evitar mantener dos fuentes independientes de usuarios. |
| 5 | Límites y revisiones | Fórmula acordada, período correcto, exceso registrado con límite aplicado; protección de Jefatura y habilitación de Bodega verificadas. Correcciones con historial. |
| 6 | Primer módulo Power BI | Datos con definiciones claras, informe reproducible, filtro por fecha/sucursal/gas, controles de acceso y actualización comprobados. Costos/CO₂e solo cuando existan datos y método. |
| 7 | Robustez offline y alcance restante | Reintentos sin duplicar, conflicto resuelto sin pérdida, recuperación de sesión/instancia, evidencias duraderas; Defontana/certificados/semáforo según alcance vigente y evaluación final. |

Los pasos 2 y 4 se diseñan antes de conectar usuarios y datos reales. La
validación se realiza durante cada incremento, no se acumula para el último.
Los detalles de persistencia deben conservar IDs de idempotencia y claves de
operaciones offline para evitar duplicaciones al sincronizar.

Planificar cada sesión de tres horas como una tarea observable, con una parte
para construir y otra para comprobar/documentar. Reestimar al terminar el modelo
alineado; asignar ahora una fecha final al sistema completo sería poco fiable.
Mantener una sola tarea de desarrollo principal a la vez y preparar Power BI
desde las definiciones de datos, sin crear servicios adicionales innecesarios.

Los entregables originales de costos, semáforo, certificados e integraciones
no desaparecen porque esta secuencia los coloque después. Un cambio de alcance
debe quedar registrado y acordado; el manual y la matriz formal continúan
aplazados según la decisión de Dante.
