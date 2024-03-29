﻿/*!
 * Ladda 0.9.4 (2014-06-21, 11:24)
 * http://lab.hakim.se/ladda
 * MIT licensed
 *
 * Copyright (C) 2014 Hakim El Hattab, http://hakim.se
 */
(function (t, e) { "object" == typeof exports ? module.exports = e(require("spin.js")) : "function" == typeof define && define.amd ? define(["spin"], e) : t.Ladda = e(t.Spinner) })(this, function (t) { "use strict"; function e(t) { if (t === void 0) return console.warn("Ladda button target must be defined."), void 0; t.querySelector(".ladda-label") || (t.innerHTML = '<span class="ladda-label">' + t.innerHTML + "</span>"); var e, n = document.createElement("span"); n.className = "ladda-spinner", t.appendChild(n); var r, a = { start: function () { return e || (e = o(t)), t.setAttribute("disabled", ""), t.setAttribute("data-loading", ""), clearTimeout(r), e.spin(n), this.setProgress(0), this }, startAfter: function (t) { return clearTimeout(r), r = setTimeout(function () { a.start() }, t), this }, stop: function () { return t.removeAttribute("disabled"), t.removeAttribute("data-loading"), clearTimeout(r), e && (r = setTimeout(function () { e.stop() }, 1e3)), this }, toggle: function () { return this.isLoading() ? this.stop() : this.start(), this }, setProgress: function (e) { e = Math.max(Math.min(e, 1), 0); var n = t.querySelector(".ladda-progress"); 0 === e && n && n.parentNode ? n.parentNode.removeChild(n) : (n || (n = document.createElement("div"), n.className = "ladda-progress", t.appendChild(n)), n.style.width = (e || 0) * t.offsetWidth + "px") }, enable: function () { return this.stop(), this }, disable: function () { return this.stop(), t.setAttribute("disabled", ""), this }, isLoading: function () { return t.hasAttribute("data-loading") }, remove: function () { clearTimeout(r), t.removeAttribute("disabled", ""), t.removeAttribute("data-loading", ""), e && (e.stop(), e = null); for (var n = 0, i = u.length; i > n; n++)if (a === u[n]) { u.splice(n, 1); break } } }; return u.push(a), a } function n(t, e) { for (; t.parentNode && t.tagName !== e;)t = t.parentNode; return e === t.tagName ? t : void 0 } function r(t) { for (var e = ["input", "textarea"], n = [], r = 0; e.length > r; r++)for (var a = t.getElementsByTagName(e[r]), i = 0; a.length > i; i++)a[i].hasAttribute("required") && n.push(a[i]); return n } function a(t, a) { a = a || {}; var i = []; "string" == typeof t ? i = s(document.querySelectorAll(t)) : "object" == typeof t && "string" == typeof t.nodeName && (i = [t]); for (var o = 0, u = i.length; u > o; o++)(function () { var t = i[o]; if ("function" == typeof t.addEventListener) { var s = e(t), u = -1; t.addEventListener("click", function () { var e = !0, i = n(t, "FORM"); if (i !== void 0) for (var o = r(i), d = 0; o.length > d; d++)"" === o[d].value.replace(/^\s+|\s+$/g, "") && (e = !1); e && (s.startAfter(1), "number" == typeof a.timeout && (clearTimeout(u), u = setTimeout(s.stop, a.timeout)), "function" == typeof a.callback && a.callback.apply(null, [s])) }, !1) } })() } function i() { for (var t = 0, e = u.length; e > t; t++)u[t].stop() } function o(e) { var n, r = e.offsetHeight; 0 === r && (r = parseFloat(window.getComputedStyle(e).height)), r > 32 && (r *= .8), e.hasAttribute("data-spinner-size") && (r = parseInt(e.getAttribute("data-spinner-size"), 10)), e.hasAttribute("data-spinner-color") && (n = e.getAttribute("data-spinner-color")); var a = 12, i = .2 * r, o = .6 * i, s = 7 > i ? 2 : 3; return new t({ color: n || "#fff", lines: a, radius: i, length: o, width: s, zIndex: "auto", top: "auto", left: "auto", className: "" }) } function s(t) { for (var e = [], n = 0; t.length > n; n++)e.push(t[n]); return e } var u = []; return { bind: a, create: e, stopAll: i } });