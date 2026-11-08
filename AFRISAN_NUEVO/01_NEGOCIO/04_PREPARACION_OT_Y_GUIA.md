# PN-03 · Preparar la OT y su guía de despacho

Estado: propuesta para revisión de Dante; no aprobada ni implementada.

**Antecedente previo:** Dante precisó después el significado de OT en
[Definición de Dante](07_OT_DEFINICION_DANTE.md). Este documento no es un flujo
aprobado. OT/despacho/retorno quedan fuera del desarrollo inmediato, limitado
por ahora a PN-01 y PN-02.
Referencia: HU-01, RF-02 y dependencias de Defontana del acta de constitución.

## Objetivo

Identificar el trabajo que justifica el retiro de cilindros antes de registrar
su salida. La OT describe el trabajo y la guía documenta el despacho; no deben
confundirse ni reemplazarse mutuamente.

## Actores y entradas

- Bodega consulta y prepara el despacho.
- El responsable de importar/registrar las referencias y el autorizador están
  pendientes de definir; no se asignan automáticamente a un rol.
- Datos: referencia de OT y guía de Defontana, cliente, sucursal/destino,
  descripción y estado del trabajo. Técnico destinatario antes del retiro.

## Flujo propuesto

1. Obtener las referencias existentes en Defontana mediante el mecanismo que
   se acuerde: exportación estructurada CSV/Excel o servicio simulado admitido
   por el acta; no asumir que existe una API del ERP disponible.
2. Registrar/importar las referencias en AFRISAN sin duplicarlas por reimportación.
3. Vincular el cliente y la sucursal correspondiente.
4. Bodega busca y selecciona la OT y la guía para preparar el retiro.
5. Verificar que el trabajo permita nuevos despachos y que el destino esté claro.
6. Si se requiere autorización previa, registrarla antes del retiro.
7. Continuar a PN-04 para identificar y pesar los cilindros que realmente saldrán.

## Reglas y alternativas

- Una OT admite varios cilindros y varias intervenciones, según lo definido por Dante.
- Las referencias locales de demostración deben distinguirse de las importadas.
- Sin una referencia válida no se presenta un despacho como vinculado al ERP.
- Si falta OT/guía, informar y resolver el antecedente; no inventar su número.
- No confirmar salida física al guardar únicamente los datos de la OT.
- Cliente, destino y referencias deben ser coherentes antes de entregar cilindros.

## Resultado

Trabajo y documento de despacho identificados para asociar los cilindros en PN-04.
Todavía no existe una salida ni una custodia abierta por esta preparación.

## Preguntas pendientes

1. ¿Quién obtiene/importa la OT y la guía? ¿Se permite registro manual de respaldo?
2. ¿Una OT admite varias guías? ¿Una guía puede asociarse a varias OT?
3. ¿Una OT puede abarcar varias sucursales o técnicos?
4. ¿Quién autoriza, qué autoriza y en qué momento?
5. ¿Se indica una cantidad prevista de cilindros y luego se confirma la entregada?

Impacto en datos: el script actual tiene CodigoExterno/Origen para OT, pero falta
resolver el registro de guías y su multiplicidad. No se da por resuelto ese vínculo.
