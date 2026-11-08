# Revisión del código de AFRISAN NUEVO — 20/10/2026

Responsable del proyecto: Dante Moya. Revisión de solo lectura de API, contratos, Web y documentos de negocio. No se modificó código, no se ejecutaron pruebas, no se compiló y no se detuvieron servidores. Los escenarios siguientes se deducen de las condiciones y transiciones del código; **no son fallos reproducidos en ejecución**. No se encontraron AGENTS.md en el árbol revisado ni en los directorios antecesores comprobados. Se consultó la skill aspnet-core para estructura de API y validación.

Restricción expresa recibida durante la revisión: **no crear commits ni publicar; mantener todo local**. Este informe es el único archivo escrito por esta revisión de código. Las propuestas de implementación, pruebas, Git o despliegue son trabajo futuro y no acciones ejecutadas ni autorizaciones nuevas.

Prioridad: P1 afecta integridad o bloquea un recorrido válido; P2 afecta un caso secundario o la operación fuera del entorno actual. Una limitación declarada del prototipo no se presenta como defecto oculto ni incumplimiento de una entrega cuya fecha/criterio no se haya confirmado.

## Estado verificable

La nueva solución contiene tres proyectos .NET 8: contratos, ASP.NET Core Minimal API y Blazor WebAssembly. Hay inventario, preparación manual de OT, entrega, registro físico, varias fotos por pesaje, incidencias, recepción y cierre. La API valida roles y restringe al técnico a sus OT. Los repositorios actuales son colecciones en memoria; no hay DbContext, migraciones, conexión operativa a Supabase ni integración con Power BI en esta solución.

La autenticación es deliberadamente de demostración y la aplicación rechaza arrancar fuera de Development. La restricción es explícita en [Program.cs:10](../../03_CODIGO/Afrisan.Nuevo.Api/Program.cs#L10). Los pendientes de producción están reconocidos en [TRABAJO_TECNICO.md:105](../../03_CODIGO/TRABAJO_TECNICO.md#L105).

## Hallazgos

### C01 — P1, defecto: eliminar una foto puede anular la protección contra edición concurrente

**Evidencia:** [TrabajoLocalService.cs:86](../../03_CODIGO/Afrisan.Nuevo.Web/Services/TrabajoLocalService.cs#L86) elimina fotos, toma la versión devuelta y la aplica a los datos locales antes de guardar; [TrabajoStore.cs:141](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L141) no exige versión para eliminar fotos; la protección del guardado está en [TrabajoStore.cs:104](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L104).

**Escenario:** A tiene versión 2 con peso antiguo; B cambia el peso y guarda versión 3. A quita una foto. La eliminación responde versión 4 y el cliente A sustituye su número de versión, pero conserva su peso antiguo; su PUT posterior pasa y sobrescribe el peso nuevo sin conflicto explícito. Incluso una foto ya eliminada puede devolver la versión actual y causar el mismo salto.

**Impacto:** el control de versiones prometido no protege todas las escrituras. Confirmar tampoco recibe la versión esperada ([TrabajoEndpoints.cs:36](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoEndpoints.cs#L36)), de modo que puede confirmar cambios de otra pestaña posteriores a la revisión del usuario.

**Corrección mínima futura:** versión esperada para borrar/confirmar, y guardado atómico de la revisión completa o respuesta de conflicto antes de sustituir versiones. Para la demostración: una pestaña por operación; no afirmar que la concurrencia está validada.

### C02 — P1, defecto: la custodia y el cierre pueden confundirse entre OT

**Evidencia:** [TrabajoStore.cs:211](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L211) admite recibir un cilindro si figura en cualquier entrega histórica de esa OT y hoy está con el mismo técnico. No comprueba una entrega vigente vinculada a esa OT. [TrabajoStore.cs:276](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L276) bloquea el cierre por la ubicación global actual de cada cilindro entregado.

**Escenario:** A entrega C, Bodega recibe C sin usar; después B entrega C al mismo técnico. Una recepción tardía sobre A puede recibir C que ahora pertenece a B. Alternativamente, A no puede cerrarse porque C volvió a estar con un técnico por B, aunque su obligación en A ya se completó. Un instalado de A que luego sea retirado por otra OT provoca un problema similar en el cierre de A.

**Corrección mínima futura:** registrar entrega/recepción por movimiento e identidad de OT, con estado de cumplimiento propio; el cierre debe consultar ese historial, no la ubicación presente del envase. No bloquea necesariamente una demo de una sola OT.

### C03 — P1, defecto: un borrador adicional puede impedir terminar toda la OT

**Evidencia:** [TrabajoStore.cs:260](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L260) exige confirmar todos los borradores. La lista completa de [TrabajoEndpoints.cs](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoEndpoints.cs#L8) no incluye descartar/cancelar una operación. La Web tampoco tiene esa acción.

**Escenario:** se completa correctamente un reemplazo, luego se crea por error un segundo borrador vacío y se sincroniza. Ya no puede finalizarse la OT sin convertir ese borrador en otro trabajo físico. Con una sola entrega no existe una segunda operación legítima que lo complete.

**Impacto:** puede bloquear la demo básica. Implementar descarte trazado de borradores, excluyéndolos del término, sin borrar trabajos confirmados. La preparación de la demo debe contemplar expresamente este caso.

### C04 — P2, defecto: un cilindro devuelto sin uso no se puede entregar nuevamente a la misma OT

**Evidencia:** [TrabajoStore.cs:73](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L73) devuelve éxito si el código aparece en las entregas históricas, antes de actualizar custodia. [TrabajoStore.cs:215](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L215) sí lo devuelve a Bodega.

**Escenario:** entrega → devolución sin uso → entrega corregida del mismo cilindro. La API responde con la OT, pero C permanece en Bodega; confirmar su instalación falla porque no está en custodia del técnico.

**Corrección mínima futura:** idempotencia por ID de movimiento y una nueva entrega para cada salida legítima. No usar la mera presencia histórica del código como idempotencia.

### C05 — P2, defecto: la API permite nuevas entregas después del término técnico

**Evidencia:** [TrabajoStore.cs:71](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L71) usa Abierta, que solo excluye Cerrada ([línea 292](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L292)); la pantalla solo ofrece entrega en Asignada ([DetalleOt.razor:23](../../03_CODIGO/Afrisan.Nuevo.Web/Pages/DetalleOt.razor#L23)). Las operaciones técnicas sí requieren Asignada.

**Escenario:** una pantalla antigua o llamada directa entrega en PendienteRevision. El técnico no puede usar ese cilindro ni reabrir la OT; debe devolverlo para poder cerrar. Alinear transición en API con la pantalla y definir reapertura/reasignación antes de añadirlas.

### C06 — P2, defecto: la caché de OT puede mostrar registros de ejecuciones anteriores

**Evidencia:** [TrabajoLocalService.cs:31](../../03_CODIGO/Afrisan.Nuevo.Web/Services/TrabajoLocalService.cs#L31) añade las OT actuales pero no retira caché antigua; [línea 38](../../03_CODIGO/Afrisan.Nuevo.Web/Services/TrabajoLocalService.cs#L38) recupera por usuario; [línea 52](../../03_CODIGO/Afrisan.Nuevo.Web/Services/TrabajoLocalService.cs#L52) no incorpora instancia a la clave de OT. Los borradores sí comprueban instancia al enviar.

**Escenario:** reiniciar la API, iniciar sesión, consultar lista vacía y luego quedarse sin red puede hacer reaparecer las OT antiguas como copia local. El usuario no puede distinguir por esa lista que corresponden a una instancia irrecuperable.

**Corrección mínima futura:** particionar caché de consulta por instancia o marcar registros archivados; conservar los borradores pendientes para recuperación. No borrar indiscriminadamente las evidencias locales.

### C07 — P1, brecha de diseño: no se conserva fecha del hecho físico offline

**Evidencia:** [TrabajoTecnico.cs:47](../../03_CODIGO/Afrisan.Nuevo.Contratos/TrabajoTecnico.cs#L47) no incluye fecha del cambio/pesaje. [TrabajoStore.cs:189](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L189) asigna UtcNow al confirmar; las fotos reciben fecha al subir ([línea 134](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L134)).

**Escenario:** trabajo domingo sin red, sincronización lunes. El dato estructurado disponible refleja el registro del lunes; no permite decidir luego a qué semana pertenece el hecho. Afecta los límites semanales/mensuales y las gráficas por período.

**Próximo diseño mínimo:** FechaHecho declarada, FechaRecepcionServidor y zona horaria empresarial; política de corrección de fechas. El período que usa negocio sigue pendiente de confirmación. No convertir automáticamente una fecha de dispositivo en prueba de autenticidad.

### C08 — P1 para versión operativa, limitación explícita: preparar una OT reubica físicamente inventario

**Evidencia:** [TrabajoStore.cs:52](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L52) acepta un cilindro que aún figura en Bodega y [línea 60](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L60) lo coloca en Sucursal/Instalado al crear la OT. Se declara como soporte temporal en [TRABAJO_TECNICO.md:31](../../03_CODIGO/TRABAJO_TECNICO.md#L31).

**Impacto:** un error de código retira stock de Bodega y lo declara instalado solo con autorizar un pedido. Es admisible para preparar la demostración si se explica; no debe sobrevivir como regla productiva. Separar inventario inicial por ubicación del pedido/OT.

### C09 — P1 para control de negocio, limitación declarada: no se controlan kilos ni límites de sucursal

**Evidencia:** [TrabajoStore.cs:40](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L40) guarda gramos solicitados/autorizados positivos; [línea 75](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L75) solo compara refrigerante al entregar; [línea 268](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L268) cierra sin conciliación cuantitativa. Pendiente explícito en [TRABAJO_TECNICO.md:107](../../03_CODIGO/TRABAJO_TECNICO.md#L107).

**Escenario:** autorizado 1 kg, entregado/instalado 20 kg; hoy no existe detección automática, límite semanal/mensual ni registro automático del exceso. No se propone bloquearlo automáticamente: Dante acordó registro e investigación, cuya fórmula todavía falta. Definir masa computada y período; guardar límite aplicado e incidencia, respetando bloqueo de Jefatura.

### C10 — P1 para informes históricos, limitación declarada: sucursal es texto sin identidad estable

**Evidencia:** [TrabajoTecnico.cs:17](../../03_CODIGO/Afrisan.Nuevo.Contratos/TrabajoTecnico.cs#L17), [NuevaOt.razor:16](../../03_CODIGO/Afrisan.Nuevo.Web/Pages/NuevaOt.razor#L16) y [TRABAJO_TECNICO.md:95](../../03_CODIGO/TRABAJO_TECNICO.md#L95).

**Impacto:** “Sucursal Centro”, “Centro” y errores tipográficos pueden dividir límites e indicadores; dos clientes con igual nombre de sucursal pueden mezclarse. El prototipo no tiene ClienteId/SucursalId en su contrato. Añadir catálogo con ID estable antes de acumular límites o presentar indicadores históricos como confiables.

### C11 — P1 para persistencia, limitación explícita: reiniciar elimina los datos y fotos centrales

**Evidencia:** [Program.cs:14](../../03_CODIGO/Afrisan.Nuevo.Api/Program.cs#L14), [TrabajoStore.cs:12](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L12) y [TRABAJO_TECNICO.md:75](../../03_CODIGO/TRABAJO_TECNICO.md#L75). Las imágenes tienen límite conjunto de 256 MiB ([TrabajoStore.cs:133](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L133)).

**Impacto:** no hay histórico central durable, restauración ni recuperación automática de las OT después de reiniciar. IndexedDB conserva parte del trabajo local, pero no restaura toda la base central. Las listas de objetos no equivalen a un MER implementado. Preparar un recorrido repetible para la demo; posteriormente persistir transacciones y evidencias privadas de forma coordinada.

### C12 — P1 para corrección operativa, limitación declarada: responder una rectificación no corrige el registro

**Evidencia:** [TrabajoStore.cs:248](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L248) solo guarda Respuesta/Revisor/FechaRevision; [línea 277](../../03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs#L277) considera pendiente cualquier incidencia sin respuesta. La limitación se reconoce en [TRABAJO_TECNICO.md:113](../../03_CODIGO/TRABAJO_TECNICO.md#L113).

**Escenario:** el técnico pide corregir 10 kg a 1 kg; Autorizador responde “corregido” y puede cerrar, pero el pesaje sigue 10 kg. Falta distinguir aceptada/rechazada/aplicada y registrar ajuste con antes/después/motivo, sin alterar la evidencia original. Evitar presentar “conclusión registrada” como una corrección ejecutada en la defensa.

### C13 — P1 para publicación, limitación explícita: autenticación, red y offline todavía son de revisión local

**Evidencia:** [InventarioEndpoints.cs:14](../../03_CODIGO/Afrisan.Nuevo.Api/InventarioEndpoints.cs#L14) publica cuentas y contraseña demo; [Program.cs:10](../../03_CODIGO/Afrisan.Nuevo.Api/Program.cs#L10) impide Production; [Web/Program.cs:10](../../03_CODIGO/Afrisan.Nuevo.Web/Program.cs#L10) fija localhost:5181; [SesionService.cs:54](../../03_CODIGO/Afrisan.Nuevo.Web/Services/SesionService.cs#L54) exige sesión no vencida para recuperar localmente; [TRABAJO_TECNICO.md:85](../../03_CODIGO/TRABAJO_TECNICO.md#L85) distingue F5 de publicación offline.

**Impacto:** abrir desde un teléfono distinto no implica conectarse a la API del PC: localhost será el teléfono. La sesión demo dura dos horas y una sesión nueva no inicia sin API. No es una vulnerabilidad de cuentas reales desplegadas: es un modo demo intencional que necesita sustitución antes de publicar. Para plazo corto usar el mismo PC y documentar la limitación; después configurar orígenes/HTTPS, usuarios individuales y política offline.

### C14 — P1 para el alcance Capstone, pendiente declarado: Power BI y sus datos no están implementados

**Evidencia:** [11_MODULO_POWER_BI.md:42](../../01_NEGOCIO/11_MODULO_POWER_BI.md#L42). Las rutas registradas en ambos archivos Endpoints no incluyen informes/BI. [Inventario.cs:22](../../03_CODIGO/Afrisan.Nuevo.Contratos/Inventario.cs#L22) solo contiene ID/código/nombre para refrigerante; no tiene factor GWP, versión/fuente, precios históricos ni hechos de masa emitida.

**Impacto:** todavía no hay integración, actualización, permisos del informe ni base histórica fiable para costos/CO₂e. No equiparar kg de cilindros instalados con emisiones. Primer incremento pequeño: informe de inventario y OT con datos explícitamente de demostración y definiciones de medidas; integración protegida y valores ambientales después de fijar fuentes y fórmulas.

## Inventario de API y páginas

Todas las rutas siguientes parten de `/api/v1`, salvo salud. Son 28 combinaciones método/ruta en los dos archivos de endpoints (sin contar Swagger).

En las filas de Bodega, operación, fotos, incidencias y cierre, `/{id}` continúa después de `/ordenes-trabajo`; `.../{op}` abrevia `/{id}/operaciones/{op}`. GET y POST de pesaje inicial existen por separado en InventarioEndpoints.cs, líneas 48 y 45 respectivamente.

| Módulo | Rutas reales | Acceso actual |
| --- | --- | --- |
| Salud/entorno/acceso | GET `/health/ready`; GET `/entorno`; POST `/sesiones` | Públicos de demostración |
| Refrigerantes | GET/POST `/refrigerantes`; GET `/refrigerantes/{id}` | Lectura: Bodega, Autorizador, Jefatura, Administrador; alta: Bodega, Jefatura, Administrador |
| Cilindros | GET/POST `/cilindros`; GET `/cilindros/{codigo}`; GET `.../historial`; GET/POST `.../pesaje-inicial`; GET `.../qr` | Lista: roles de lectura; alta/pesaje: Bodega/Administrador; ficha/historial/QR: autenticados, incluido Técnico |
| OT | GET/POST `/ordenes-trabajo`; GET `/{id}` | Alta: Autorizador; técnico solo propias; otros roles autenticados consultan |
| Bodega | POST `/{id}/entregas`; POST `/{id}/recepciones` | Bodega |
| Operación técnica | PUT `/{id}/operaciones`; POST `.../{op}/confirmar`; POST `.../{op}/informar-entrega` | Técnico asignado |
| Fotos | POST `.../{op}/fotos`; GET/DELETE `.../{op}/fotos/{foto}` | Escritura: Técnico; lectura: acceso a la OT; solo borrar borrador |
| Incidencias y revisión | POST `/{id}/incidencias`; POST `.../{incidencia}/resolver` | Crear: Técnico/Bodega; resolver: Autorizador |
| Término y cierre | POST `/{id}/termino-tecnico`; POST `/{id}/cerrar` | Técnico y Autorizador respectivamente |

Páginas: `/acceso`, `/`, `/refrigerantes`, `/inventario`, `/cilindros/nuevo`, `/cilindros/{codigo}`, `/identificar`, `/ordenes`, `/ordenes/nueva`, `/ordenes/{id}`, `/ordenes/{id}/operacion/{operacionId}`. Identificar acepta texto/lector tipo teclado; la cámara QR está explícitamente pendiente. La lista de OT y fotos tienen autorización de servidor; no se detectó aquí una eliminación libre de evidencia confirmada.

## Próximos pasos ajustados a seis horas disponibles

El plazo comunicado durante esta revisión es jueves 22/10 a las 7, con unas tres horas diarias. No cabe prometer conectar nube, cerrar todas las reglas, construir Power BI completo y validar offline integral en seis horas. El siguiente plan protege una entrega demostrable; es una propuesta de trabajo, no trabajo ejecutado.

| Tiempo | Trabajo priorizado | Evidencia de salida |
| --- | --- | --- |
| Día 1, 30 min | Fijar con la rúbrica qué demuestra la entrega y qué seguirá como pendiente. Elegir una sola OT de reemplazo y los tres roles. | Lista de aceptación y alcance honesto |
| Día 1, 60 min | Corregir descarte de borradores (C03) y alinear entrega al estado Asignada (C05), si forman parte del recorrido elegido. | Cambios pequeños y revisables |
| Día 1, 90 min | Ejecutar un recorrido controlado con dos cilindros y fotografías reales de revisión: alta → OT → entrega → cambio → recepción → cierre. Revisar una negativa por fotos faltantes y una por rol incorrecto. | Capturas, resultados y errores observados, sin afirmar pruebas aún no hechas |
| Día 2, 60 min | Corregir solo bloqueos observados; repetir los pasos afectados. Comprobar arranque API+Web desde Visual Studio. | Guion reproducible y puertos claros |
| Día 2, 60 min | Preparar el mínimo de Power BI que exija la rúbrica: medidas definidas y, si es viable con licencia/herramienta disponible, informe con datos de demostración identificados como tales. | PBIX/informe o diseño y pendiente explícito según criterio real |
| Día 2, 60 min | Evidencias, alcance pendiente, decisiones abiertas y ensayo de defensa individual. | Entrega consistente con el estado real |

No reiniciar la API tras preparar el recorrido sin contar con un guion para recrear los datos. No alterar la fecha del dispositivo para eludir sesiones ni convertir datos de demo en registros reales.

Después de la evaluación: resolver identidad de sucursal y fechas del hecho; corregir custodia por OT y conflictos; persistir con EF Core/PostgreSQL y Storage; implementar límites con jerarquía y rectificación; conectar indicadores; finalmente verificar publicación, móviles y offline. Estos incrementos aprovechan los contratos y pantallas actuales; no requieren empezar la aplicación otra vez.

