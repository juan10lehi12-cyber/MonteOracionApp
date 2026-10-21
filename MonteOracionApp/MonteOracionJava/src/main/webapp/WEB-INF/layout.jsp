<%@ page contentType="text/html; charset=UTF-8" pageEncoding="UTF-8" %>
<%@ page import="pe.monteoracion.Html,pe.monteoracion.Security" %>
<% String base=request.getContextPath(); Security.User user=Security.user(request); %>
<!doctype html>
<html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title><%=Html.e(request.getAttribute("title"))%> · Monte de Oración</title><link rel="stylesheet" href="<%=base%>/assets/app.css"></head>
<body><a class="skip" href="#contenido">Ir al contenido</a>
<header><a class="brand" href="<%=base%>/">Monte de Oración<small>Comunidad · Comas, Lima</small></a><nav aria-label="Principal"><a href="<%=base%>/donar">Donar</a><a href="<%=base%>/consulta">Mis donaciones</a>
<% if(user==null) { %><a href="<%=base%>/login">Personal</a><% } else { %><a href="<%=base%>/panel"><%=Html.e(user.name())%></a><form method="post" action="<%=base%>/logout"><input type="hidden" name="csrf" value="<%=Security.csrf(request)%>"><button class="quiet">Salir</button></form><% } %></nav></header>
<div class="shell">
<% if(user!=null) { %>
<aside><nav aria-label="Módulos"><a href="<%=base%>/panel">Mi panel</a>
<a href="<%=base%>/donaciones">Donaciones</a><a href="<%=base%>/recibir">Registrar recepción</a>
<% if(user.has("ADMINISTRADOR","ALMACENERO")) { %><a href="<%=base%>/almacen">Almacén</a><a href="<%=base%>/catalogo?modulo=productos">Productos</a><a href="<%=base%>/catalogo?modulo=beneficiarios">Beneficiarios</a><% } %>
<a href="<%=base%>/bienes">Solicitudes de bienes</a><a href="<%=base%>/dinero">Solicitudes de dinero</a>
<% if(user.has("ADMINISTRADOR","TESORERO")) { %><a href="<%=base%>/saldos">Saldos y fondos</a><a href="<%=base%>/cierres">Cierres y conciliación</a><% } %>
<% if(user.has("ADMINISTRADOR")) { %><a href="<%=base%>/catalogo?modulo=congregantes">Congregantes</a><a href="<%=base%>/publicaciones">Eventos y publicaciones</a><a href="<%=base%>/personal">Personal y permisos</a><a href="<%=base%>/catalogo?modulo=fondos">Fondos</a><a href="<%=base%>/catalogo?modulo=medios">Medios de pago</a><a href="<%=base%>/catalogo?modulo=iglesia&id=1">Datos de la iglesia</a><a href="<%=base%>/auditoria">Auditoría</a><% } %>
<a href="<%=base%>/reportes">Reportes PDF</a><a href="<%=base%>/password">Cambiar contraseña</a></nav></aside>
<% } %>
<main id="contenido">
<% Object flash=session.getAttribute("flash"); if(flash!=null) {session.removeAttribute("flash");%><div class="message" role="status"><%=Html.e(flash)%></div><% } %>
<%-- content solo contiene HTML construido por Html; los valores de usuario se escapan en el servidor. --%>
<%=request.getAttribute("content")%>
</main></div><footer>Monte de Oración · Comas, Lima · <a href="<%=base%>/privacidad">Privacidad</a></footer>
<script src="<%=base%>/assets/app.js" defer></script></body></html>
