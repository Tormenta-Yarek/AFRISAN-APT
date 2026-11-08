# Revisión académica de AFRISAN — continuidad individual

Fecha de revisión: 20 de octubre de 2026. Responsable actual del proyecto: Dante Moya. Alcance de esta revisión: guías entregadas, rúbrica, acta, backlog, planificación, documentos de arquitectura y estado documental/Git. No se modificaron fuentes ni se ejecutaron pruebas de la aplicación. Las extracciones de lectura están en `extracciones_capstone/`; `P` identifica un párrafo XML del DOCX, no una página impresa.

**Dato informado por Dante durante esta revisión:** defensa y entrega de «mitad del avance» este jueves, 22 de octubre de 2026, a las 7; dispone de tres horas al día. No se confirmó AM/PM. Este plazo proviene del estudiante, no de la pauta. «Mitad del avance» no permite deducir que deba estar aceptado exactamente el 50% de las funcionalidades o los puntos del backlog.

**Restricción vigente de Dante:** no crear commits todavía. Esta revisión y sus evidencias se mantienen locales; registrar cambios mediante commit y publicar mediante push queda pendiente hasta una autorización posterior. No se solicita esa autorización en esta revisión.

## 1. Qué exige realmente la documentación académica

| Fuente y ubicación verificable | Exigencia | Aplicación a esta entrega |
|---|---|---|
| `2.5_APT122_Sumativa Avance Fase2.pdf`, pp. 1 y 4 | Avance en semana 9, ponderación de 20% de la nota; modalidad grupal de 2–3 integrantes. | Confirmar con docente la adaptación individual y el significado del avance solicitado para el jueves. No convertir semana 9 en una fecha por cálculo propio. |
| Misma pauta, pp. 1–2 y 6–8, IE1–IE8 | Ajustes justificados 10%; metodología 10%; evidencias de documentación, programación y almacenamiento 25%; lenguaje técnico 5%; redacción/citas 5%; estructura y evidencias en Git 20%; aportes individuales 15%; trabajo en equipo 10%. | Es una rúbrica de evidencia y desarrollo, no una lista que exige terminar todas las HU en este avance. Los dos últimos criterios necesitan tratamiento acordado para un solo estudiante. |
| Misma pauta, p. 4 | Portada, índice, abstract, desarrollo de ingeniería, conclusiones y reflexiones individuales. Abstract en español e inglés; conclusiones individuales y reflexión solo en inglés. Arial/Verdana/Calibri 11 o 12; interlineado 1 o 1,5; número de página; bibliografía/anexo cuando corresponda. | El informe EVA2 ya contiene estructura y apartados lingüísticos. Hace falta actualizar el contenido a la solución nueva y revisar la presentación final antes de entregarlo. No se certificó su aspecto visual en esta auditoría. |
| `2.4_GuiaEstudiante_Fase 2_DesarrolloProyecto APT.docx`, secciones 1–3, P20–29, P31–52, P55–74 | Resumen de avances, evidencias justificadas, monitoreo con ocho columnas, dificultades/facilitadores y ajustes; objetivos y metodología se repiten si cambian. Estados: En curso / Con retraso / No iniciado / Completado / Ajustada. | Documentar cambio de equipo, redefinición operativa de OT y nueva aplicación como ajustes concretos. No inventar ceremonias Scrum. |
| `2.5...pdf`, pp. 2–3 | El docente selecciona indicadores de calidad de las competencias involucradas: diseñar/aplicar pruebas/mejorar, planificar/controlar, diseñar/implementar datos, construir/integrar/implantar software. | Determinar cuáles revisará ahora. Tener una aplicación visual no demuestra automáticamente persistencia ni integración. |
| `2.6_GuiaEstudiante_Fase 2_Informe Final Proyecto APT.docx`, secciones 1–6, P28–59 | Informe final: relevancia, objetivos, metodología, desarrollo/ajustes, evidencias y proyección profesional. | Preparar evidencia reutilizable; no redactar un cierre final antes de obtener resultados. Esta guía no aporta una fecha de defensa. |

Los objetivos cuantitativos del acta —80% de cobertura, 15 casos UAT, 50 despachos, 20 registros offline, etc.— son compromisos del proyecto. La pauta general no los convierte por sí sola en exigencias completadas para este jueves. Cualquier reprogramación debe quedar expresada y acordada; no se eliminan silenciosamente.

## 2. Incongruencias y riesgos prioritarios

### A. El informe de avance describe la aplicación anterior

**Verificado.** `EVIDENCIAS_GRUPALES/EVA_2/02_Informe_Avance_Fase2_AFRISAN.docx`, P7, P29–49, declara corte 16/10, usa `Afrisan.Web → Afrisan.Api → AfrisanDbContext`, y deja terreno/offline/fotos pendientes. El Excel `01_Plan_de_Trabajo_y_Backlog_Fase2_AFRISAN.xlsx`, hoja `Product Backlog Fase 2`, H7:I9, declara HU04–HU06 no iniciadas. En cambio, `AFRISAN_NUEVO/03_CODIGO/TRABAJO_TECNICO.md`, líneas 3–18 y 107–116, describe otro prototipo .NET8 con operaciones/fotos/IndexedDB y persistencia central pendiente.

**Riesgo:** presentar el informe viejo mientras se muestra la nueva aplicación hace que el evaluador no sepa qué entrega se está defendiendo. **Acción inmediata:** anexar un corte del 20/10 que identifique solución de demostración, cambios desde el 16/10, capacidades observadas y pendientes. Actualizar Word/Markdown/Excel con el mismo corte si se alcanza; si no, entregar una adenda explícita y conservar el corte anterior como antecedente. No declarar HU aceptadas por presencia de código.

### B. Lo nuevo aún no está registrado en Git

**Verificado al revisar:** `git status --short` muestra `?? AFRISAN_NUEVO/` y `?? EVIDENCIAS_GRUPALES/EVA_2/`, además de múltiples cambios anteriores. Los ocho commits más recientes consultados terminan en `c2a7bfd`, del 08/10/2026. No se consultó el estado remoto.

**Riesgo:** IE6 vale 20% de esta rúbrica y pide evidencias en Git; disponer de archivos en OneDrive o en el computador no demuestra su entrega por Git. IE7 pide identificar aportes. **Acción autorizada ahora:** revisar y organizar localmente el trabajo actual y evidencias elegidas, sin secretos, bases locales, fotos privadas ni binarios de compilación. Registrar mediante commit y publicar mediante push permanece pendiente por decisión expresa de Dante, hasta una autorización posterior; el requisito académico sigue documentado. No se realizó commit ni push en esta auditoría.

### C. La modalidad individual necesita una constancia, no otro integrante ficticio

**Verificado.** Pauta p.4 exige 2–3 integrantes; IE7/IE8 representan 15%+10% de la rúbrica. EVA2 Word P10/P42–43 y Excel `Plan de Trabajo (8 Col)!A3` reconocen que la aplicación individual está pendiente. No se encontró una autorización docente en las fuentes inspeccionadas.

**Acción:** obtener y guardar el acuerdo de continuidad individual y cómo se aplicarán esos dos criterios. Esto no impide seguir desarrollando. Dante debe poder explicar y defender decisiones, resultados y código; la asistencia de IA no constituye otro estudiante. Conservar autoría histórica. La configuración local de Git ya identifica a Dante Moya; los commits históricos consultados conservan autores Dante/Yarek.

### D. La composición histórica no debe presentarse como distribución de trabajo actual

**Verificado.** Acta P21–23 y Gantt `Carta Gantt!E2:E8` mantienen Dante/Yarek; RACI p.1 dice «equipo actual» de dos personas; arquitectura 4+1 actual P12–16 incluye tres integrantes y responsable histórico de la entrega. `Documentación del Proyecto/ACTUAL/README.md` ya explica que son referencias de Fase1 y conserva autoría. No corresponde reescribir esos aportes como si Dante los hubiese realizado solo.

**Acción:** una asignación prospectiva sencilla basta: Dante ejecuta desarrollo, datos, documentación y evidencias; cliente valida negocio; docente evalúa y acuerda alcance académico. La planificación vigente no debe seguir suponiendo un QA separado disponible. Para el jueves basta explicitarlo en la adenda; no hace falta rehacer retrospectivas históricas.

### E. El negocio cambió y los criterios originales no se actualizaron

**Verificado.** Acta RN02 P166 exige cantidad de vacíos devueltos igual a despachados; RN03 P169–171 calcula semáforo por carga/capacidad de equipo; RF04 P195 registra kilos inyectados, equipo y falla. Backlog original, hoja `Criterios de Aceptación`, F15–16 y F24–25 usa esas mismas reglas. Dante confirmó reemplazo físico, cilindro instalado que permanece en sucursal, límites por semana/mes y rol Autorizador en `AFRISAN_NUEVO/01_NEGOCIO/07_OT_DEFINICION_DANTE.md`, líneas 27–41 y 129–148.

**Riesgo:** una demostración que cumple el nuevo flujo podría evaluarse contra otro problema. **Acción:** una adenda de requisitos debe indicar qué se conserva, qué se corrige y qué necesita acuerdo. Preservar IDs HU01–HU10; agregar criterios nuevos/revisados con fecha. No aplicar la resta salida/retorno de un mismo envase a dos cilindros diferentes ni confundir instalado con gas emitido. La tolerancia original `<0,2 kg` de `Criterios!F24` también necesita revisión para el flujo físico actual.

### F. Power BI es parte explícita del alcance

**Verificado.** Acta P105–109 y RF10 P213; backlog `Historias de Usuario!C14` y `Criterios de Aceptación!F29:F31`. El acta pide cinco KPI: kg cargados/devueltos, costo de refrigerantes, CO2e, criticidad por local y porcentaje de devolución; motor para cuatro refrigerantes. El usuario reafirmó Power BI. La nota `11_MODULO_POWER_BI.md` registra que el prototipo aún no tiene conexión ni persistencia central.

**Acción para jueves:** explicar fuentes y definiciones pendientes, y mostrar solo indicadores cuya semántica esté acordada. Si se prepara una maqueta con datos simulados, rotularla; no anunciar integración operativa. Un dashboard nuevo en las próximas seis horas es secundario frente a una demostración confiable del núcleo. Mantenerlo en el próximo incremento, no descartarlo. Costos históricos deben contemplar que RN05 del acta P177 incluye mano de obra, repuestos y traslado además del refrigerante.

### G. PWA/IndexedDB es una decisión defendible, pero falta consolidarla

**Verificado.** Acta P100 contempla PWA o MAUI y P154/P278 admite SQLite/IndexedDB. Backlog `Criterios!F12:F13` conserva SQLite. Arquitectura 4+1 P81/P85 y arquitectura técnica P158/P164 proponen MAUI/SQLite. El nuevo módulo usa Blazor/IndexedDB según `TRABAJO_TECNICO.md:18`.

**Acción:** registrar la elección de PWA/IndexedDB y el motivo de viabilidad individual, conservando ASP.NET/API/PostgreSQL. Ajustar el criterio técnico y demostrar guardado/sincronización cuando se valide. No desarrollar un segundo cliente MAUI solo para resolver una palabra desactualizada. Tampoco afirmar inicio offline o sincronización en segundo plano ya verificados.

### H. Fechas, aprobación y tamaño del alcance tienen fuentes diferentes

**Verificado.** El acta declara hitos semanas 6/8/9/12/17; plan de hitos `Hoja 1!D5:D9` usa 7/9/10/13/18. Gantt repite la segunda secuencia. La planificación EVA2 `Plan de Trabajo (8 Col)!D6:D13` sigue «Por estimar». Acta aprobaciones P300–307 registra cliente «Aprobado» y docente «Pendiente» al 03/09. La arquitectura fue confirmada aprobada por Dante posteriormente, lo que EVA2 P47 reconoce. No hay base para desautorizar esa confirmación ni para decir que el último rediseño de negocio tenga aprobación formal automática.

**Acción:** el plazo del jueves informado por Dante manda para preparar la entrega. Acordar después la línea vigente y la capacidad real de tres horas/día; no convertir los 47 puntos históricos en 47 horas, ni estimar porcentaje de avance sumando pantallas. Aclarar qué se evaluará como «mitad del avance».

## 3. Plan de seis horas: hoy y mañana

Propuesta de preparación, no compromiso de desarrollo completo. No se reserva trabajo imprescindible para el jueves.

| Bloque | Tiempo | Resultado defendible |
|---|---:|---|
| Hoy: fijar alcance de la defensa | 30 min | Identificar solución nueva; solicitar confirmación docente de modalidad individual, formato de avance y horario; escoger un recorrido central. Se sigue trabajando mientras responde. |
| Hoy: estabilizar el recorrido | 90 min | Resolver únicamente bloqueos del ejemplo refrigerante → cilindros → OT → entrega → reemplazo con dos grupos de fotos → recepción → revisión/cierre. Usar datos simulados y casos pequeños. Si un paso falla, registrar el límite y reducir la demostración con honestidad. |
| Hoy: capturar evidencia técnica | 45 min | Compilación con entorno, capturas y respuestas de API, evidencia de datos realmente guardados y de su alcance. Si el servidor sigue en RAM, documentar esa limitación y mostrar el diseño SQL como diseño, sin presentarlo como persistencia operativa. |
| Hoy: preparar el incremento local | 15 min | Revisar y organizar cambios/evidencias en la carpeta local de revisión. No crear commits ni hacer push; ambos quedan pendientes hasta una autorización posterior de Dante. |
| Mañana: actualizar el informe | 60 min | Adenda/informe con fecha actual, abstract bilingüe, conclusiones/reflexión de Dante en inglés, plan de ocho columnas y ajustes justificados. Sin manual de usuario ni nueva matriz formal. |
| Mañana: terminar evidencias críticas | 45 min | Registrar casos de permisos, fotos obligatorias y guardado/sincronización si están en el alcance de defensa. Indicar resultado esperado/observado y limitaciones; reparar un defecto que impida demostrar. |
| Mañana: preparar y ensayar defensa | 60 min | Relato comprensible del problema, flujo, arquitectura, datos, ejemplo, límites y siguiente incremento. Poder explicar un endpoint y una relación de datos. Video/capturas de respaldo por si falla el entorno en vivo. |
| Mañana: consolidar entrega local | 15 min | Enlaces/archivos correctos, copia local de entrega y entorno listo. Revisión de estado y próximos pasos; registrar/publicar en Git permanece pendiente hasta autorización posterior. |

**Regla de prioridad:** obtener una cadena coherente entre necesidad, implementación y resultado pesa más que añadir varias funciones aisladas. Los tiempos son una propuesta de trabajo, no una estimación que garantice terminar integración de Supabase, Power BI y todas las pruebas en seis horas.

## 4. Paquete de evidencia pequeño y suficiente para explicar el avance

1. Informe/adenda de avance: qué se cambió, por qué, qué funciona, qué se observó y qué falta; distingue el corte anterior.
2. Plan de ocho columnas con Dante como responsable actual, bloques realistas y estado de cada actividad.
3. Una demostración registrada del flujo central con datos simulados identificados como tales; fotos de ejemplo apropiadas, sin datos privados.
4. Evidencia técnica con comando/fecha/entorno, rutas API y capturas de resultados. Una compilación correcta no se etiqueta como prueba integral aprobada.
5. Diseño de datos vigente y evidencia de almacenamiento que realmente exista. SQL disponible no demuestra tablas ejecutadas ni app conectada.
6. Atribución histórica y relación breve entre archivos locales y lo que prueban; explicación del siguiente incremento con Power BI incluido. La referencia al futuro corte Git queda pendiente de la autorización de Dante para registrar/publicar.

Se recomienda mantener estas seis piezas en una carpeta por entrega, reutilizando los documentos existentes. Es una organización propuesta para una persona; la pauta no obliga a generar seis documentos nuevos. Los manuales y la matriz formal de trazabilidad permanecen aplazados por decisión de Dante.

## 5. Lista de preparación para defender sin exagerar resultados

- [ ] Puedo explicar por qué el cliente necesita controlar envases físicos y cómo distingue un reemplazo de una carga de gas.
- [ ] Puedo identificar los roles actuales y dónde la API limita acciones.
- [ ] Puedo mostrar el código de un flujo y decir qué datos guarda en memoria, navegador o base central.
- [ ] Puedo mostrar una OT sin atribuir aceptación de cliente a una demostración propia.
- [ ] El informe que entrego describe la misma solución que abro en Visual Studio.
- [ ] Las evidencias locales están organizadas; su registro y disponibilidad para el evaluador en Git permanecen pendientes hasta autorización posterior de Dante.
- [ ] Explico la continuidad individual y conservo los aportes anteriores.
- [ ] Explico Power BI como requisito y presento con precisión su estado.
- [ ] Distingo una regla confirmada, una interpretación provisional y una decisión pendiente.
- [ ] Los siguientes pasos se ajustan a tres horas diarias y al calendario confirmado por el docente.

## 6. Después de la entrega próxima

Priorizar: consolidar reglas de reemplazo/custodia y límites; alinear esquema y persistencia Supabase con ese flujo; conservar fotos e historial de forma durable; demostrar autorización y recuperación de operaciones; preparar datos analíticos y Power BI; completar integración acordada con Defontana, certificados y validación final según alcance replanificado. La arquitectura aprobada se conserva; se actualizan decisiones concretas y sus evidencias.

El riesgo académico inmediato no es que falten todos los módulos finales. Es presentar como una única entrega documentos del sistema anterior, código nuevo sin evidencias versionadas y reglas de negocio incompatibles. Este corte permite explicar esas diferencias y convertirlas en ajustes verificables.

La restricción actual se mantiene: todo lo producido en esta revisión queda local, sin commits ni push.
