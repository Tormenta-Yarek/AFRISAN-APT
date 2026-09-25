# ARQUITECTURA DEL SISTEMA

Esta carpeta contiene los documentos y diagramas técnicos que describen la arquitectura propuesta para el proyecto AFRISAN durante la Fase 1 del Proyecto APT.

## Arquitectura seleccionada

AFRISAN utiliza una arquitectura cliente-servidor de tres capas:

1. **Capa de presentación:** `Afrisan.Web`, desarrollada con Blazor WebAssembly.
2. **Capa de lógica de negocio:** `Afrisan.Api`, desarrollada con ASP.NET Core Web API.
3. **Capa de persistencia:** PostgreSQL / Supabase.

Esta separación permite centralizar las reglas de negocio, mantener la integridad de los datos y facilitar el mantenimiento y evolución del sistema.

## Contenido

- Modelo Entidad Relación (MER / DER)
- Diagrama de Componentes
- Diagrama de Despliegue
- Arquitectura Técnica
- Vistas 4+1
- Identificación de APIs
- Recursos Técnicos
- Factibilidad Técnica

## Objetivo

El objetivo de estos documentos es representar la estructura lógica y técnica del sistema, mostrando sus principales componentes, relaciones, tecnologías y forma de despliegue.

La documentación distingue entre la arquitectura definida para el proyecto y las funcionalidades futuras o pendientes de implementación.

## Archivos editables

Cuando corresponda, se incluyen también los archivos fuente editables, como archivos `.drawio`, para permitir futuras modificaciones de los diagramas.
