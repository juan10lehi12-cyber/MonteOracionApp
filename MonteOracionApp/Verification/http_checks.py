"""Checks only against the disposable integration-test database/server on localhost."""
import html, http.cookiejar, json, re, urllib.request, urllib.parse, urllib.error
from pathlib import Path
root=Path(__file__).resolve().parent.parent
fixture=json.loads((root/"artifacts/checks/fixture.json").read_text())
base="http://127.0.0.1:5158"
passed=0
def session():
    return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
def request(client,path,data=None):
    body=None if data is None else urllib.parse.urlencode(data).encode()
    try:
        response=client.open(base+path,body,timeout=20)
        return response.status,response.url,response.read()
    except urllib.error.HTTPError as error:
        return error.code,error.url,error.read()
def token(body):
    match=re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"',body.decode())
    assert match,"Token antifalsificación ausente"
    return html.unescape(match.group(1))
def check(name,condition):
    global passed
    assert condition,name
    passed+=1;print("OK",name)
anonymous=session()
for path in ["/","/Home?period=realizados","/Account/Login","/Donations/Create"]:
    code,url,body=request(anonymous,path)
    check("Página pública "+path,code==200)
code,url,body=request(anonymous,"/Portal")
check("Portal retirado ya no es público",code==404)
code,url,body=request(anonymous,"/Access")
check("Acceso retirado ya no es público",code==404 or code==500)
code,url,body=request(anonymous,"/Inventory")
check("Inventario requiere acceso", "/Account/Login" in url)
def login(role):
    client=session()
    code,url,body=request(client,"/Account/Login")
    code,url,body=request(client,"/Account/Login",{"Email":fixture[role],"Password":fixture["password"],"__RequestVerificationToken":token(body)})
    check("Acceso del personal "+role,code==200 and "/Dashboard" in url)
    return client
admin=login("admin")
for path in ["/Dashboard","/Donations","/Donations/Receive","/Donations/Details/1","/Congregants","/Congregants/Edit","/Congregants/Edit/1","/Inventory","/Inventory/Pending","/Inventory/Products","/Inventory/ProductEdit/1","/Inventory/BeneficiaryEdit/1","/Inventory/Beneficiaries","/Inventory/Requests","/Inventory/Create","/Inventory/Details/1","/Inventory/Lot/1","/Inventory/Legacy","/Treasury","/Treasury/Requests","/Treasury/Create","/Treasury/Details/1","/Treasury/Closings","/Staff","/Staff/Edit","/Staff/Edit/legacy-admin","/Publications","/Publications/Recaudacion","/Publications/Calendario","/Publications/Participants/1","/Publications/History","/Publications/Edit/1","/Reports","/Reports/Audit"]:
    code,url,body=request(admin,path)
    if code!=200:(root/"artifacts/checks/http-error.html").write_bytes(body)
    check("Pantalla administrativa "+path,code==200)
for kind in ["donaciones","dinero","entregas","inventario","auditoria"]:
    code,url,body=request(admin,"/Reports/Download?kind="+kind+"&dateMode=anio")
    check("PDF "+kind,code==200 and body.startswith(b"%PDF-1.4"))
    (root/("artifacts/checks/"+kind+".pdf")).write_bytes(body)
code,url,body=request(admin,"/Reports/Download?kind=cierre&id=1")
check("PDF de cierre",code==200 and body.startswith(b"%PDF"))
treasurer=login("treasurer")
for path in ["/Staff","/Publications/History"]:
    code,url,body=request(treasurer,path);check("Tesorero sin permiso "+path,code==403)
warehouse=login("warehouse")
for path in ["/Treasury","/Treasury/Closings","/Staff","/Publications/Participants/1","/Reports/Download?kind=dinero"]:
    code,url,body=request(warehouse,path);check("Almacenero sin permiso "+path,code==403)
code,url,body=request(warehouse,"/Donations")
check("Almacenero ve bienes y no importes de donaciones económicas",code==200 and b"TEST-0001" not in body and b"S/ 100.00" not in body)

# Exercise staff lifecycle through the real forms.
code,url,body=request(admin,"/Staff/Edit")
code,url,body=request(admin,"/Staff/Edit",{"FullName":"Personal HTTP de prueba","Email":"nuevo@test.local","Password":fixture["password"],"IsActive":"true","Treasurer":"true","__RequestVerificationToken":token(body)})
check("Crear cuenta del personal desde formulario",code==200 and "/Staff" in url and "nuevo@test.local" in body.decode())
newuser=session()
code,url,body=request(newuser,"/Account/Login")
code,url,body=request(newuser,"/Account/Login",{"Email":"nuevo@test.local","Password":fixture["password"],"__RequestVerificationToken":token(body)})
check("Cuenta creada inicia sesión y muestra su nombre",code==200 and b"Personal HTTP de prueba" in body)
code,url,body=request(admin,"/Staff")
row=re.search(r'<tr>[^<]*<td>Personal HTTP de prueba.*?href="/Staff/Edit/([^"]+)"',body.decode(),re.S)
assert row
uid=row.group(1)
code,url,body=request(admin,"/Staff/Edit/"+uid)
code,url,body=request(admin,"/Staff/Edit",{"UserId":uid,"FullName":"Personal HTTP de prueba","Email":"nuevo@test.local","IsActive":"false","Treasurer":"true","__RequestVerificationToken":token(body)})
check("Desactivar personal conserva ficha",code==200 and "/Staff" in url)
code,url,body=request(newuser,"/Dashboard")
check("Cuenta desactivada pierde sesión", "/Account/Login" in url)

code,url,body=request(treasurer,"/Inventory/Create")
code,url,body=request(treasurer,"/Inventory/Create",{"BeneficiaryId":"1","Purpose":"Entrega HTTP","Kind":"Entrega","Priority":"Normal","NeededOn":fixture["today"],"Lines[0].StockLotId":"1","Lines[0].Quantity":"1","__RequestVerificationToken":token(body)})
check("Solicitud de bienes desde formulario",code==200 and "/Inventory/Details/" in url)
rid=url.rsplit("/",1)[1]
code,url,body=request(admin,"/Inventory/Details/"+rid)
code,url,body=request(admin,"/Inventory/Decide/"+rid,{"approve":"true","reason":"Aprobación HTTP","__RequestVerificationToken":token(body)})
check("Aprobación mediante formulario",code==200 and b"Aprobada" in body)
code,url,body=request(warehouse,"/Inventory/Details/"+rid)
form=re.search(r'<form[^>]+action="/Inventory/Deliver/[^"]+".*?</form>',body.decode(),re.S).group(0)
def hidden(name):
    return html.unescape(re.search(r'name="'+name+r'"[^>]*value="([^"]+)"',form).group(1))
fields={"lineId":hidden("lineId"),"commandId":hidden("commandId"),"quantity":"0.5","receiver":"Receptor HTTP","__RequestVerificationToken":token(form.encode())}
boundary="MonteBoundary"+__import__("uuid").uuid4().hex
parts=[]
for key,value in fields.items():
    parts.append(('--'+boundary+'\r\nContent-Disposition: form-data; name="'+key+'"\r\n\r\n'+value+'\r\n').encode())
png=__import__("base64").b64decode("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jfXkAAAAASUVORK5CYII=")
parts.append(('--'+boundary+'\r\nContent-Disposition: form-data; name="evidence"; filename="constancia.png"\r\nContent-Type: image/png\r\n\r\n').encode()+png+b"\r\n")
parts.append(('--'+boundary+'--\r\n').encode())
upload=urllib.request.Request(base+"/Inventory/Deliver/"+rid,data=b"".join(parts),headers={"Content-Type":"multipart/form-data; boundary="+boundary})
response=warehouse.open(upload,timeout=20);body=response.read()
check("Entrega parcial con constancia desde formulario",response.status==200 and b"Parcial" in body)
response=warehouse.open(upload,timeout=20);body=response.read()
check("Reenvío del formulario informa operación duplicada",response.status==200 and "ya fue registrada" in body.decode())


# Public event images, assignment notices and private attendance.
code,url,body=request(admin,"/Publications/Participants/1")
code,url,body=request(admin,"/Publications/RemoveParticipant/1",{"reason":"Asignación de prueba retirada","__RequestVerificationToken":token(body)})
check("Retirar asignación conserva registro",code==200 and b"Retirado" in body)
code,url,body=request(anonymous,"/Portal")
check("Portal retirado no expone asignaciones ni consultas",code==404)
code,url,body=request(admin,"/Publications/Participants/1")
code,url,body=request(admin,"/Publications/Assign/1",{"congregantId":"1","__RequestVerificationToken":token(body)})
check("Restablecer asignación conserva historial",code==200 and "Asignación activa" in html.unescape(body.decode()))

code,url,body=request(admin,"/Publications/Edit")
date=__import__("datetime").date.fromisoformat(fixture["today"])
future=date+__import__("datetime").timedelta(days=3)
event_fields={"Kind":"Evento","Title":"Evento imagen HTTP","Body":"Contenido público del evento","Location":"Lima","EventAt":str(future)+"T09:00","EventEndsAt":str(future)+"T12:00","IsPublished":"true","__RequestVerificationToken":token(body)}
boundary="MonteEvent"+__import__("uuid").uuid4().hex
parts=[('--'+boundary+'\r\nContent-Disposition: form-data; name="'+key+'"\r\n\r\n'+value+'\r\n').encode() for key,value in event_fields.items()]
parts.append(('--'+boundary+'\r\nContent-Disposition: form-data; name="image"; filename="evento.png"\r\nContent-Type: image/png\r\n\r\n').encode()+png+b"\r\n")
parts.append(('--'+boundary+'--\r\n').encode())
response=admin.open(urllib.request.Request(base+"/Publications/Edit",data=b"".join(parts),headers={"Content-Type":"multipart/form-data; boundary="+boundary}),timeout=20)
body=response.read()
check("Crear evento con imagen, horario y lugar",response.status==200 and b"Evento imagen HTTP" in body)
row=re.search(r'<tr><td>Evento imagen HTTP.*?href="/Publications/Participants/([^"]+)"',body.decode(),re.S)
assert row
eventid=row.group(1)
code,url,image=request(anonymous,"/Publications/Image/"+eventid)
check("Imagen de evento publicado es pública",code==200 and image.startswith(b"\x89PNG"))
code,url,body=request(admin,"/Publications/Participants/"+eventid)
code,url,body=request(admin,"/Publications/Assign/"+eventid,{"congregantId":"1","__RequestVerificationToken":token(body)})
code,url,body=request(admin,"/Publications/Edit/"+eventid)
past=date-__import__("datetime").timedelta(days=1)
event_fields.update({"EventAt":str(past)+"T10:00","EventEndsAt":str(past)+"T12:00","__RequestVerificationToken":token(body)})
code,url,body=request(admin,"/Publications/Edit/"+eventid,event_fields)
check("Editar horario conserva imagen del evento",code==200 and "/Publications" in url)
code,url,body=request(admin,"/Publications/Participants/"+eventid)
form=re.search(r'<form[^>]+action="/Publications/Attendance/([^"]+)".*?</form>',body.decode(),re.S)
assert form
participantid=form.group(1)
version=html.unescape(re.search(r'name="version"[^>]*value="([^"]+)"',form.group(0)).group(1))
fields={"status":"Justificada","note":"NOTA-ASISTENCIA-PRIVADA-HTTP","version":version,"__RequestVerificationToken":token(form.group(0).encode())}
code,url,body=request(admin,"/Publications/Attendance/"+participantid,fields)
check("Administrador registra ausencia justificada",code==200 and b"NOTA-ASISTENCIA-PRIVADA-HTTP" in body)
fields["note"]="ESTO-NO-DEBE-SOBRESCRIBIR"
fields["status"]="NoAsistio"
code,url,body=request(admin,"/Publications/Attendance/"+participantid,fields)
check("Pantalla antigua no sobrescribe asistencia",code==200 and "Otra persona modificó" in html.unescape(body.decode()) and b"ESTO-NO-DEBE-SOBRESCRIBIR" not in body)
code,url,body=request(anonymous,"/Home?period=realizados")
check("Evento realizado visible sin notas privadas",code==200 and b"Evento imagen HTTP" in body and b"NOTA-ASISTENCIA-PRIVADA-HTTP" not in body)

print(f"{passed} comprobaciones HTTP superadas.")
