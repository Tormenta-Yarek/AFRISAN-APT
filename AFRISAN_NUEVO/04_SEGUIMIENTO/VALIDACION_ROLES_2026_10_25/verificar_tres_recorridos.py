"""Tres recorridos de revisión. Solo API local en memoria; nunca ejecuta SQL.

Ejecutar con Python 3 y Pillow, con API 5181 iniciada. Cada ejecución genera
códigos y series únicos y conserva un informe independiente, sin tokens.
"""
import argparse
import base64
import io
import json
import urllib.error
import urllib.parse
import urllib.request
import uuid
from datetime import datetime, timezone, timedelta
from pathlib import Path

from PIL import Image, ImageDraw


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-url", default="http://localhost:5181")
    parser.add_argument("--out", type=Path, default=Path(__file__).resolve().parent / "ejecuciones")
    args = parser.parse_args()
    url = urllib.parse.urlparse(args.base_url)
    if url.hostname not in ("localhost", "127.0.0.1") or url.scheme != "http":
        parser.error("Esta prueba solo admite HTTP local; crea datos sintéticos en memoria.")
    stamp = datetime.now(timezone(timedelta(hours=-3))).strftime("%Y%m%d-%H%M%S")
    run_id = stamp + "-" + uuid.uuid4().hex[:6]
    out = args.out / run_id
    out.mkdir(parents=True, exist_ok=False)
    base = args.base_url.rstrip("/") + "/api/v1/"
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
    tokens, checks, cases = {}, [], []
    report = {"ejecucion": run_id, "fecha_local": stamp, "zona": "America/Santiago",
              "tipo": "HTTP real contra API local; datos e imágenes sintéticos",
              "estado": "EN_CURSO", "comprobaciones": checks, "casos": cases}

    def persist():
        (out / "resultados.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

    def check(condition, label, **evidence):
        checks.append({"resultado": "OK" if condition else "FALLO", "comprobacion": label, **evidence})
        persist()
        if not condition:
            raise AssertionError(label)
        print("OK:", label, flush=True)

    def request(method, path, data=None, role="Bodega", status=200, label=None, binary=False):
        headers = {"Content-Type": "application/json"}
        if role:
            headers["Authorization"] = "Bearer " + tokens[role]
        body = json.dumps(data).encode("utf-8") if data is not None else None
        req = urllib.request.Request(base + path, data=body, headers=headers, method=method)
        try:
            with opener.open(req, timeout=20) as res:
                code, raw = res.status, res.read()
        except urllib.error.HTTPError as ex:
            code, raw = ex.code, ex.read()
        check(code == status, label or f"{role or 'Sin sesión'} {method} {path}",
              rol=role, metodo=method, ruta=path, http=code, esperado=status,
              **({"detalle": raw.decode(errors="replace")[:500]} if code != status or code >= 400 else {}))
        if binary:
            return raw
        return json.loads(raw) if raw else None

    try:
        with opener.open(args.base_url.rstrip("/") + "/health/ready", timeout=10) as response:
            health = json.load(response)
        check(health.get("almacenamiento") == "Memoria" and health.get("supabase") is False,
              "Entorno de revisión en memoria y sin Supabase", salud=health)
        environment = request("GET", "entorno", role=None)
        report["instancia"] = environment["instancia"]
        check(environment["modo"] == "Memoria", "Modo de revisión comprobado antes de crear ejemplos")
        for role in ("Bodega", "Autorizador", "Tecnico"):
            session = request("POST", "sesiones", {"email": role.lower() + "@afrisan.example",
                              "password": environment["passwordDemo"]}, role=None, label=f"Inicio de sesión {role}")
            check(session["rol"] == role, f"Sesión con rol correcto: {role}")
            tokens[role] = session["token"]
        request("GET", "cilindros", role=None, status=401, label="Inventario exige sesión")
        initial_inventory = request("GET", "cilindros")
        initial_catalog = request("GET", "refrigerantes")
        initial_orders = request("GET", "ordenes-trabajo", role="Autorizador")

        specs = [
            ("R134A", "Sucursal de prueba Norte", "Dirección simulada Norte 101", [
                (10000, 2000, 3000), (12000, 2500, 11500), (15000, 3000, 13000)], 2000, 11000,
             "Etiqueta del envase desgastada; código contrastado con la ficha.", "Rectificacion"),
            ("R410A", "Sucursal de prueba Centro", "Dirección simulada Centro 202", [
                (9000, 1800, 3800), (18000, 3500, 17500), (22000, 4200, 20200)], 2300, 17000,
             "Demora simulada en acceso a sala técnica; trabajo completado.", "Incidencia"),
            ("R404A", "Sucursal de prueba Sur", "Dirección simulada Sur 303", [
                (11000, 2200, 5200), (24000, 4800, 22800), (30000, 6000, 28000)], 3400, 22000,
             "Lectura repetida de balanza para corroborar el peso registrado.", "Incidencia"),
        ]
        for index, (gas_name, branch, address, weights, retired_weight, installed_weight, reason, incident_type) in enumerate(specs, 1):
            prefix = f"Caso {index} {gas_name}"
            case = {"caso": index, "gas": gas_name, "sucursal": branch, "cilindros_iniciales": []}
            cases.append(case)
            gas_payload = {"codigo": f"{gas_name}-P{run_id[-6:]}", "nombre": f"{gas_name} - prueba integral {index}"}
            gas = request("POST", "refrigerantes", gas_payload, status=201, label=prefix + ": Bodega registra refrigerante")
            case["refrigerante"] = gas
            request("POST", "refrigerantes", gas_payload, status=409, label=prefix + ": rechaza refrigerante duplicado")
            request("POST", "refrigerantes", gas_payload, role="Tecnico", status=403, label=prefix + ": Técnico no administra catálogo")
            request("POST", "refrigerantes", gas_payload, role="Autorizador", status=403, label=prefix + ": Autorizador no administra catálogo")
            for position, (capacity, tare, gross) in enumerate(weights):
                payload = {"refrigeranteId": gas["id"], "numeroSerie": f"PR-{run_id}-{index}-{position}",
                           "capacidadGr": capacity, "taraGr": tare, "pesoInicialGr": None if position == 2 else gross,
                           "ubicacionInicial": "Sucursal" if position == 0 else "Bodega",
                           "sucursalInicial": branch if position == 0 else None,
                           "observacion": f"Dato sintético. Caso {index}; envase {position + 1}."}
                request("POST", "cilindros", {**payload, "pesoInicialGr": tare + capacity + 1}, status=400,
                        label=prefix + f": rechaza sobrecapacidad del envase {position + 1}")
                cylinder = request("POST", "cilindros", payload, status=201, label=prefix + f": alta cilindro {position + 1}")
                if position == 2:
                    check(cylinder["estado"] == "PendientePesaje" and cylinder["contenidoSegunPesajeGr"] is None,
                          prefix + ": alta sin pesaje no inventa contenido")
                    request("POST", f"cilindros/{cylinder['codigo']}/pesaje-inicial", {"pesoBrutoGr": gross,
                            "observacion": "Primer pesaje simulado"}, status=201, label=prefix + ": Bodega registra primer pesaje")
                    request("POST", f"cilindros/{cylinder['codigo']}/pesaje-inicial", {"pesoBrutoGr": gross}, status=409,
                            label=prefix + ": no sobrescribe primer pesaje")
                    cylinder = request("GET", f"cilindros/{cylinder['codigo']}")
                check(cylinder["contenidoSegunPesajeGr"] == gross - tare, prefix + f": contenido correcto en cilindro {position + 1}")
                case["cilindros_iniciales"].append(cylinder)
                png = request("GET", f"cilindros/{cylinder['codigo']}/qr", binary=True, label=prefix + f": QR cilindro {position + 1}")
                check(png.startswith(b"\x89PNG\r\n\x1a\n"), prefix + f": QR tiene formato PNG {position + 1}")
                (out / f"{cylinder['codigo']}.png").write_bytes(png)
            old, new, spare = case["cilindros_iniciales"]
            request("POST", "cilindros", payload, status=409, label=prefix + ": rechaza serie duplicada")
            requested = weights[1][2] - weights[1][1]
            ot_payload = {"idOperacion": str(uuid.uuid4()), "sucursal": branch, "direccion": address,
                          "descripcion": f"Prueba integral {index}: reemplazo {gas_name} y devolución sin uso",
                          "tecnicoEmail": "tecnico@afrisan.example", "detalle": [
                              {"refrigeranteId": gas["id"], "grSolicitados": requested + index * 1000, "grAutorizados": requested}],
                          "cilindrosARetirar": [old["codigo"]]}
            request("POST", "ordenes-trabajo", {**ot_payload, "cilindrosARetirar": [new["codigo"]]}, role="Autorizador", status=409,
                    label=prefix + ": OT no convierte cilindro de Bodega en retiro")
            request("POST", "ordenes-trabajo", ot_payload, role="Bodega", status=403, label=prefix + ": Bodega no autoriza OT")
            ot = request("POST", "ordenes-trabajo", ot_payload, role="Autorizador", status=201, label=prefix + ": Autorizador crea OT")
            case["ot_id"], case["ot_codigo"] = ot["id"], ot["codigo"]
            root = f"ordenes-trabajo/{ot['id']}"
            repeated = request("POST", "ordenes-trabajo", ot_payload, role="Autorizador", status=201,
                               label=prefix + ": reintento de creación de OT")
            check(repeated["id"] == ot["id"], prefix + ": reintento no duplica OT")
            listed = request("GET", "ordenes-trabajo", role="Tecnico")
            check(any(item["id"] == ot["id"] for item in listed) and all(item["tecnicoEmail"] == "tecnico@afrisan.example" for item in listed),
                  prefix + ": Técnico consulta sus OT asignadas")
            request("POST", root + "/entregas", {"codigo": new["codigo"]}, role="Tecnico", status=403,
                    label=prefix + ": Técnico no registra despacho de Bodega")
            for cylinder in (new, spare):
                delivered = request("POST", root + "/entregas", {"codigo": cylinder["codigo"]},
                                    label=prefix + f": Bodega entrega {cylinder['codigo']}")
                current = request("GET", f"cilindros/{cylinder['codigo']}")
                check(current["ubicacion"] == "Tecnico" and current["tecnico"] == "tecnico@afrisan.example",
                      prefix + f": custodia correcta en {cylinder['codigo']}")
            repeated = request("POST", root + "/entregas", {"codigo": new["codigo"]}, label=prefix + ": reintento de despacho")
            check(len(repeated["entregas"]) == 2, prefix + ": reintento no duplica entrega")
            op_payload = {"id": str(uuid.uuid4()), "instancia": environment["instancia"], "version": 0,
                          "tipo": "Reemplazo", "codigoRetirado": old["codigo"], "codigoInstalado": new["codigo"],
                          "pesoRetiradoGr": retired_weight, "pesoInstaladoGr": installed_weight,
                          "observacion": f"Reemplazo sintético {index}; fotos de prueba, no evidencia de servicio real."}
            op = request("PUT", root + "/operaciones", op_payload, role="Tecnico", label=prefix + ": Técnico guarda reemplazo")
            op_root = root + f"/operaciones/{op['id']}"
            request("POST", op_root + f"/confirmar?version={op['version']}", role="Tecnico", status=400,
                    label=prefix + ": no confirma sin fotos de pesaje")
            for group, cylinder, weight in (("Retirado", old, retired_weight), ("Instalado", new, installed_weight)):
                image = Image.new("RGB", (800, 450), (225 - index * 10, 235, 245 if group == "Instalado" else 210))
                draw = ImageDraw.Draw(image)
                draw.multiline_text((30, 35), f"PRUEBA SINTETICA - NO ES UNA FOTO REAL\n\nCaso {index}: {gas_name}\n{group}: {cylinder['codigo']}\nPeso bruto simulado: {weight} g\n{run_id}", fill="black", spacing=14)
                buffer = io.BytesIO()
                image.save(buffer, "PNG")
                photo_name = f"caso-{index}-{group.lower()}.png"
                (out / photo_name).write_bytes(buffer.getvalue())
                photo = {"id": str(uuid.uuid4()), "version": op["version"], "grupo": group, "nombre": photo_name,
                         "contentType": "image/png", "base64": base64.b64encode(buffer.getvalue()).decode()}
                before_version = op["version"]
                op = request("POST", op_root + "/fotos", photo, role="Tecnico", label=prefix + f": Técnico adjunta pesaje {group}")
                request("POST", op_root + f"/confirmar?version={before_version}", role="Tecnico", status=409,
                        label=prefix + f": rechaza versión antigua tras foto {group}")
                same = request("POST", op_root + "/fotos", photo, role="Tecnico", label=prefix + f": reintento foto {group}")
                check(same["version"] == op["version"] and len(same["fotos"]) == len(op["fotos"]), prefix + f": foto {group} no se duplica")
                downloaded = request("GET", op_root + f"/fotos/{photo['id']}", role="Autorizador", binary=True,
                                     label=prefix + f": Autorizador consulta foto privada {group}")
                check(downloaded == buffer.getvalue(), prefix + f": evidencia {group} conserva sus bytes")
            op = request("POST", op_root + f"/confirmar?version={op['version']}", role="Tecnico", label=prefix + ": Técnico confirma reemplazo")
            check(op["estado"] == "Registrada" and len(op["fotos"]) == 2, prefix + ": reemplazo registrado con ambos pesajes y fotos")
            current_old = request("GET", f"cilindros/{old['codigo']}")
            current_new = request("GET", f"cilindros/{new['codigo']}")
            check(current_old["ubicacion"] == "Tecnico" and current_new["ubicacion"] == "Sucursal" and current_new["sucursal"] == branch,
                  prefix + ": retiro e instalación cambian ubicación al confirmar")
            request("POST", op_root + "/informar-entrega", role="Tecnico", label=prefix + ": Técnico informa devolución del retirado")
            check(request("GET", f"cilindros/{old['codigo']}")["ubicacion"] == "Tecnico",
                  prefix + ": informar devolución no suplanta recepción de Bodega")
            incident_id = str(uuid.uuid4())
            request("POST", root + "/incidencias", {"id": incident_id, "operacionId": op["id"], "tipo": incident_type,
                    "motivo": "SIMULADO: " + reason}, role="Tecnico", label=prefix + ": Técnico registra incidencia o solicitud")
            ended = request("POST", root + "/termino-tecnico", {"observacion": f"Prueba {index} completa; pendiente revisión y recepción."},
                            role="Tecnico", label=prefix + ": Técnico informa término")
            check(ended["estado"] == "PendienteRevision", prefix + ": término técnico separado del cierre administrativo")
            request("POST", root + "/cerrar", role="Autorizador", status=400, label=prefix + ": bloquea cierre sin recepción del retirado")
            request("POST", root + "/recepciones", {"codigo": old["codigo"], "pesoBrutoGr": retired_weight}, role="Tecnico", status=403,
                    label=prefix + ": Técnico no recibe por Bodega")
            request("POST", root + "/recepciones", {"codigo": old["codigo"], "pesoBrutoGr": retired_weight,
                    "observacion": "Recepción simulada; peso coincide con el informado por técnico."}, label=prefix + ": Bodega recibe retirado")
            request("POST", root + "/cerrar", role="Autorizador", status=400, label=prefix + ": bloquea cierre con entrega sin uso pendiente")
            request("POST", root + "/recepciones", {"codigo": spare["codigo"], "pesoBrutoGr": weights[2][2],
                    "observacion": "Devolución de envase sin uso, mismo peso que al despachar."}, label=prefix + ": Bodega recibe envase sin uso")
            request("POST", root + "/cerrar", role="Autorizador", status=400, label=prefix + ": bloquea cierre con incidencia sin revisión")
            request("POST", root + f"/incidencias/{incident_id}/resolver", {"respuesta": "Revisión simulada: " + reason + " No se modifican pesajes confirmados."},
                    role="Autorizador", label=prefix + ": Autorizador revisa incidencia")
            request("POST", root + "/cerrar", role="Tecnico", status=403, label=prefix + ": Técnico no cierra administrativamente")
            closed = request("POST", root + "/cerrar", role="Autorizador", label=prefix + ": Autorizador cierra OT")
            check(closed["estado"] == "Cerrada" and closed["operaciones"][0]["recibidoBodega"] and
                  closed["operaciones"][0]["entregaInformada"] and all(e["estado"] in ("Instalado", "Devuelto") for e in closed["entregas"]),
                  prefix + ": cierre completo con custodia resuelta")
            request("POST", root + "/entregas", {"codigo": spare["codigo"]}, status=409, label=prefix + ": OT cerrada no admite nuevos despachos")
            case["ot_final"] = closed
            case["cilindros_finales"] = [request("GET", f"cilindros/{c['codigo']}") for c in (old, new, spare)]
            final_old, final_new, final_spare = case["cilindros_finales"]
            check(final_old["ubicacion"] == "Bodega" and final_old["contenidoSegunPesajeGr"] == retired_weight - weights[0][1]
                  and final_new["contenidoSegunPesajeGr"] == installed_weight - weights[1][1]
                  and final_spare["ubicacion"] == "Bodega" and final_spare["contenidoSegunPesajeGr"] == weights[2][2] - weights[2][1],
                  prefix + ": pesos, contenido y ubicaciones finales correctos")
            for cylinder in (old, new, spare):
                history = request("GET", f"cilindros/{cylinder['codigo']}/historial")
                check(bool(history) and all(h["usuario"] and h["fecha"] for h in history), prefix + f": historial trazable {cylinder['codigo']}")
                case.setdefault("historiales_cilindros", {})[cylinder["codigo"]] = history
            check({h["usuario"] for h in closed["historial"]} == {"bodega@afrisan.example", "autorizador@afrisan.example", "tecnico@afrisan.example"},
                  prefix + ": historial OT identifica los tres usuarios")

        inventory = request("GET", "cilindros")
        catalog = request("GET", "refrigerantes")
        orders = request("GET", "ordenes-trabajo", role="Autorizador")
        check(len(inventory) - len(initial_inventory) == 9 and len(catalog) - len(initial_catalog) == 3 and len(orders) - len(initial_orders) == 3,
              "Se crearon exactamente 3 refrigerantes, 9 cilindros y 3 OT, sin duplicados por reintento")
        report["resumen"] = {"refrigerantes_creados": 3, "cilindros_creados": 9, "ot_cerradas": 3,
                             "reemplazos_confirmados": 3, "fotos_sinteticas": 6, "despachos": 6,
                             "recepciones": 6, "incidencias_revisadas": 3}
        report["estado"] = "CORRECTO"
    except Exception as ex:
        report["estado"] = "FALLO"
        report["error"] = str(ex)
        raise
    finally:
        report["cantidad_comprobaciones"] = len(checks)
        persist()
        print("Resultados:", out, flush=True)


if __name__ == "__main__":
    main()
