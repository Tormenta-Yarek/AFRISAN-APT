"""Regresión del retiro por Técnico y aprobación Bodega -> Autorizador.

Solo localhost en modo Memoria. Crea 3 refrigerantes, 9 cilindros y 3 OT.
Python 3 y Pillow. No utiliza SQL ni conserva sesiones en archivos.
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
    if url.scheme != "http" or url.hostname not in ("localhost", "127.0.0.1"):
        parser.error("Solo se admite HTTP local en memoria.")
    run = datetime.now(timezone(timedelta(hours=-3))).strftime("%Y%m%d-%H%M%S") + "-" + uuid.uuid4().hex[:6]
    out = args.out / run
    out.mkdir(parents=True)
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
    tokens, checks, cases = {}, [], []
    report = {"ejecucion": run, "estado": "EN_CURSO", "tipo": "Datos e imágenes sintéticos; API HTTP real",
              "comprobaciones": checks, "casos": cases}

    def persist():
        report["cantidad_comprobaciones"] = len(checks)
        (out / "resultados.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

    def check(ok, name, **evidence):
        checks.append({"resultado": "OK" if ok else "FALLO", "comprobacion": name, **evidence})
        persist()
        assert ok, (name, evidence)
        print("OK:", name, flush=True)

    def req(method, path, data=None, role="Tecnico", status=200, name=None, binary=False):
        headers = {"Content-Type": "application/json"}
        if role:
            headers["Authorization"] = "Bearer " + tokens[role]
        request = urllib.request.Request(args.base_url + "/api/v1/" + path,
                                        json.dumps(data).encode() if data is not None else None, headers, method=method)
        try:
            with opener.open(request, timeout=15) as response:
                code, raw = response.status, response.read()
        except urllib.error.HTTPError as error:
            code, raw = error.code, error.read()
        check(code == status, name or f"{role} {method} {path}", http=code, esperado=status,
              **({"detalle": raw.decode(errors="replace")[:400]} if code >= 400 else {}))
        return raw if binary else json.loads(raw) if raw else None

    def photo(case, group, code, weight):
        image = Image.new("RGB", (800, 450), (220, 230 - case * 10, 245))
        ImageDraw.Draw(image).multiline_text((30, 30),
            f"PRUEBA SINTETICA - NO ES FOTO REAL\n\n{run}\nCaso {case} / {group}\n{code}: {weight} g brutos", fill="black", spacing=15)
        buffer = io.BytesIO()
        image.save(buffer, "PNG")
        filename = f"caso-{case}-{group}-{code}.png"
        (out / filename).write_bytes(buffer.getvalue())
        return {"id": str(uuid.uuid4()), "nombre": filename, "contentType": "image/png", "base64": base64.b64encode(buffer.getvalue()).decode()}

    try:
        with opener.open(args.base_url + "/health/ready") as response:
            health = json.load(response)
        check(health["almacenamiento"] == "Memoria" and health["supabase"] is False, "Solo entorno en memoria sin Supabase")
        environment = req("GET", "entorno", role=None)
        report["instancia"] = environment["instancia"]
        for role in ("Bodega", "Autorizador", "Tecnico"):
            tokens[role] = req("POST", "sesiones", {"email": role.lower() + "@afrisan.example", "password": environment["passwordDemo"]}, role=None)["token"]
        req("GET", "cilindros", role="Tecnico", status=403, name="Técnico no lista inventario global")
        before_cylinders = req("GET", "cilindros", role="Bodega")
        before_gases = req("GET", "refrigerantes", role="Bodega")
        before_orders = req("GET", "ordenes-trabajo", role="Autorizador")
        for i, gas_code in enumerate(("R134A", "R410A", "R404A"), 1):
            tag = f"Caso {i}"
            branch = f"Sucursal prueba terreno {i}"
            gas = req("POST", "refrigerantes", {"codigo": gas_code + "-" + run[-6:], "nombre": gas_code + " prueba de terreno"}, role="Bodega", status=201)
            cylinders = []
            for j in range(3):
                cylinders.append(req("POST", "cilindros", {"refrigeranteId": gas["id"], "numeroSerie": f"TER-{run}-{i}-{j}",
                    "capacidadGr": 10000 + i * 1000 + j * 2000, "taraGr": 1000 + i * 100 + j * 200,
                    "pesoInicialGr": 3000 + i * 100 + j * 2000, "ubicacionInicial": "Sucursal" if j == 0 else "Bodega",
                    "sucursalInicial": branch if j == 0 else None}, role="Bodega", status=201))
            old, new, spare = cylinders
            ot = req("POST", "ordenes-trabajo", {"idOperacion": str(uuid.uuid4()), "sucursal": branch, "direccion": f"Dirección simulada {i}",
                "descripcion": f"Prueba {i} retiro técnico con foto, terreno y doble revisión", "tecnicoEmail": "tecnico@afrisan.example",
                "detalle": [{"refrigeranteId": gas["id"], "grSolicitados": 7000 + i * 1000, "grAutorizados": 6000 + i * 1000}],
                "cilindrosARetirar": [old["codigo"]]}, role="Autorizador", status=201)
            root = f"ordenes-trabajo/{ot['id']}"
            case = {"caso": i, "ot_id": ot["id"], "ot_codigo": ot["codigo"], "cilindros_iniciales": cylinders}
            cases.append(case)
            available = req("GET", root + "/cilindros-disponibles")
            check({c["codigo"] for c in available} == {new["codigo"], spare["codigo"]}, tag + ": selección limitada a Bodega y gas encargado")
            req("GET", root + "/cilindros-disponibles", role=None, status=401)
            for cylinder in (new, spare):
                gross = cylinder["pesajeRegistrado"]["pesoBrutoGr"] - i * 10
                dispatch = {"idOperacion": str(uuid.uuid4()), "codigo": cylinder["codigo"], "pesoBrutoGr": gross,
                            "foto": photo(i, "Despacho", cylinder["codigo"], gross)}
                req("POST", root + "/entregas", dispatch, role="Bodega", status=403, name=tag + ": Bodega no suplanta retiro del Técnico")
                req("POST", root + "/entregas", {**dispatch, "foto": None}, status=400, name=tag + ": retiro sin foto rechazado")
                req("POST", root + "/entregas", {**dispatch, "foto": {**dispatch["foto"], "base64": "no es imagen"}}, status=400,
                    name=tag + ": imagen inválida no cambia custodia")
                req("POST", root + "/entregas", {**dispatch, "pesoBrutoGr": cylinder["taraGr"] - 1}, status=400)
                current = req("GET", f"cilindros/{cylinder['codigo']}", role="Bodega")
                check(current["ubicacion"] == "Bodega", tag + ": intentos inválidos no despachan")
                delivered = req("POST", root + "/entregas", dispatch, name=tag + ": Técnico retira con peso y fotografía")
                check(delivered["estado"] == "EnProcesoDespacho" and delivered["entregas"][-1]["usuario"] == "tecnico@afrisan.example", tag + ": OT en proceso de despacho y actor correcto")
                repeated = req("POST", root + "/entregas", dispatch)
                check(len(repeated["entregas"]) == len(delivered["entregas"]), tag + ": reintento del retiro no duplica custodia ni foto")
                req("POST", root + "/entregas", {**dispatch, "pesoBrutoGr": gross - 1}, status=409)
                current = req("GET", f"cilindros/{cylinder['codigo']}", role="Bodega")
                check(current["estado"] == "EnProcesoDespacho" and current["ubicacion"] == "Tecnico" and current["pesajeRegistrado"]["pesoBrutoGr"] == gross,
                      tag + ": cilindro bajo custodia con el peso físico de salida")
                photo_path = root + f"/entregas/{dispatch['idOperacion']}/foto"
                req("GET", photo_path, role=None, status=401)
                req("GET", photo_path, role="Autorizador", status=409, name=tag + ": Autorizador espera aprobación de Bodega para fotos de salida")
                raw = req("GET", photo_path, role="Bodega", binary=True)
                check(raw == base64.b64decode(dispatch["foto"]["base64"]), tag + ": Bodega consulta foto de salida exacta")
            check(req("GET", root + "/cilindros-disponibles") == [], tag + ": retirados dejan de estar disponibles")
            old_weight = old["taraGr"] + i * 100
            new_weight = new["pesajeRegistrado"]["pesoBrutoGr"] - i * 100
            op = req("PUT", root + "/operaciones", {"id": str(uuid.uuid4()), "instancia": environment["instancia"], "version": 0,
                "tipo": "Reemplazo", "codigoRetirado": old["codigo"], "codigoInstalado": new["codigo"],
                "pesoRetiradoGr": old_weight, "pesoInstaladoGr": new_weight, "observacion": "Prueba sintética de cambio físico"})
            op_root = root + f"/operaciones/{op['id']}"
            req("POST", op_root + f"/confirmar?version={op['version']}", status=400, name=tag + ": cambio sin foto de terreno rechazado")
            for group, cylinder, weight in (("Retirado", old, old_weight), ("Instalado", new, new_weight)):
                foto = {**photo(i, group, cylinder["codigo"], weight), "grupo": group, "version": op["version"]}
                stale = op["version"]
                op = req("POST", op_root + "/fotos", foto)
                req("POST", op_root + f"/confirmar?version={stale}", status=409)
            op = req("POST", op_root + f"/confirmar?version={op['version']}")
            check(op["estado"] == "Registrada" and req("GET", root)["estado"] == "EnTerreno", tag + ": cambio registrado y OT en terreno")
            terrain_path = op_root + f"/fotos/{op['fotos'][0]['id']}"
            req("GET", terrain_path, role="Autorizador", status=409)
            req("POST", root + "/recepciones", {"codigo": old["codigo"], "pesoBrutoGr": old_weight}, role="Bodega", status=409,
                name=tag + ": Bodega espera devolución informada por Técnico")
            req("POST", op_root + "/informar-entrega")
            incident_id = str(uuid.uuid4())
            req("POST", root + "/incidencias", {"id": incident_id, "operacionId": op["id"], "tipo": "Incidencia", "motivo": f"SIMULADO: control adicional de pesaje del caso {i}."})
            req("POST", root + f"/incidencias/{incident_id}/resolver", {"respuesta": "Revisión prematura"}, role="Autorizador", status=409)
            req("POST", root + "/aprobar-bodega", {"confirmoRevision": True}, role="Bodega", status=409, name=tag + ": no aprueba antes del término técnico")
            ended = req("POST", root + "/termino-tecnico", {"observacion": "Trabajo finalizado; entregado a revisión de Bodega."})
            check(ended["estado"] == "PendienteBodega" and ended["aprobacionBodega"] is None, tag + ": término enviado primero a Bodega")
            req("POST", root + "/aprobar-bodega", {"confirmoRevision": True}, role="Bodega", status=400, name=tag + ": no aprueba con retirado pendiente")
            req("POST", root + "/revision-autorizador", {"confirmoRevision": True}, role="Autorizador", status=409)
            req("POST", root + "/aprobar-bodega", {"confirmoRevision": True}, status=403)
            received_weight = old_weight + (50 if i == 2 else 0)
            received = req("POST", root + "/recepciones", {"codigo": old["codigo"], "pesoBrutoGr": received_weight,
                "observacion": "Diferencia simulada de 50 g visible para revisión." if i == 2 else "Peso corroborado por Bodega."}, role="Bodega")
            check(received["operaciones"][0]["pesoRecepcionGr"] == received_weight and received["operaciones"][0]["pesoRetiradoGr"] == old_weight,
                  tag + ": recepción conserva ambos pesos sin sobrescribir terreno")
            req("POST", root + "/recepciones", {"codigo": old["codigo"], "pesoBrutoGr": received_weight + 1}, role="Bodega", status=409,
                name=tag + ": peso de recepción confirmado no se sobrescribe")
            req("POST", root + "/aprobar-bodega", {"confirmoRevision": True}, role="Bodega", status=400, name=tag + ": no aprueba con reserva sin devolver")
            spare_delivery = received["entregas"][-1]
            req("POST", root + "/recepciones", {"codigo": spare["codigo"], "pesoBrutoGr": spare_delivery["pesoBrutoGr"]}, role="Bodega")
            req("POST", root + "/aprobar-bodega", {"confirmoRevision": False}, role="Bodega", status=400)
            approved = req("POST", root + "/aprobar-bodega", {"confirmoRevision": True, "observacion": "Cilindros, pesos y fotos revisados por Bodega."}, role="Bodega")
            check(approved["estado"] == "PendienteRevision" and approved["aprobacionBodega"]["usuario"] == "bodega@afrisan.example", tag + ": aprobación de Bodega habilita Autorizador")
            req("POST", root + "/cerrar", role="Autorizador", status=409, name=tag + ": no cierra sin revisión de fotos del Autorizador")
            req("POST", root + "/revision-autorizador", {"confirmoRevision": True}, role="Autorizador", status=400, name=tag + ": revisión espera resolución de incidencias")
            req("POST", root + f"/incidencias/{incident_id}/resolver", {"respuesta": "Registro y pesajes revisados después de la aprobación de Bodega."}, role="Autorizador")
            req("GET", terrain_path, role="Autorizador", binary=True)
            for delivery in approved["entregas"]:
                req("GET", root + f"/entregas/{delivery['id']}/foto", role="Autorizador", binary=True)
            req("POST", root + "/revision-autorizador", {"confirmoRevision": False}, role="Autorizador", status=400)
            reviewed = req("POST", root + "/revision-autorizador", {"confirmoRevision": True, "observacion": "Fotografías comparadas con el registro de salida, terreno y recepción."}, role="Autorizador")
            check(reviewed["revisionAutorizador"]["usuario"] == "autorizador@afrisan.example" and reviewed["revisionAutorizador"]["fecha"] >= reviewed["aprobacionBodega"]["fecha"], tag + ": actor y orden de las revisiones correctos")
            closed = req("POST", root + "/cerrar", role="Autorizador")
            check(closed["estado"] == "Cerrada", tag + ": cierre con las dos revisiones registradas")
            req("POST", root + "/incidencias", {"id": str(uuid.uuid4()), "tipo": "Incidencia", "motivo": "No agregar después de cerrar"}, status=409)
            case["ot_final"] = closed
            case["cilindros_finales"] = [req("GET", f"cilindros/{c['codigo']}", role="Bodega") for c in cylinders]
        check(len(req("GET", "cilindros", role="Bodega")) - len(before_cylinders) == 9 and
              len(req("GET", "refrigerantes", role="Bodega")) - len(before_gases) == 3 and
              len(req("GET", "ordenes-trabajo", role="Autorizador")) - len(before_orders) == 3,
              "Tres refrigerantes, nueve cilindros y tres OT, sin duplicados de reintentos")
        report["estado"] = "CORRECTO"
    except Exception as error:
        report["estado"], report["error"] = "FALLO", str(error)
        raise
    finally:
        persist()
        print("Resultados:", out, flush=True)


if __name__ == "__main__":
    main()
