(function ($) {
    'use strict';

    Dropzone.autoDiscover = false;

    let timerId = null;
    let h = 0;
    let m = 0;
    let s = 0;
    let modoNotaInterna = false;
    let autoGuardadoTimer = null;

    function pad(value) {
        return String(value).padStart(2, '0');
    }

    function paintTimer() {
        const value = `${pad(h)}:${pad(m)}:${pad(s)}`;
        $('#hms').text(value);
        $('#Duracion').val(value);
    }

    function startTimer() {
        if (timerId) return;
        timerId = window.setInterval(function () {
            s++;
            if (s > 59) { s = 0; m++; }
            if (m > 59) { m = 0; h++; }
            paintTimer();
        }, 1000);
    }

    function stopTimer() {
        window.clearInterval(timerId);
        timerId = null;
        paintTimer();
    }

    function resetTimer() {
        stopTimer();
        h = 0; m = 0; s = 0;
        paintTimer();
    }

    function mostrarResultado(exito, titulo, mensaje) {
        const modal = $('#modalResultado');
        $('#resultadoIcono')
            .attr('class', exito ? 'fa fa-check-circle text-success' : 'fa fa-exclamation-circle text-danger');
        $('#resultadoTitulo').text(titulo);
        $('#resultadoMensaje').text(mensaje);
        modal.modal('show');
    }

    function cerrarModalResultado() {
        $('#modalResultado').modal('hide');
    }

    function mensajeError(xhr) {
        const data = xhr.responseJSON;
        return data && data.mensaje
            ? data.mensaje
            : 'No se pudo completar la operación. Intente nuevamente.';
    }

    function token() {
        return $('#formTipos input[name="__RequestVerificationToken"]').val();
    }

    function validarTipo() {
        const tipo = $('#Tiquete_Tipo').val();
        if (tipo) return true;

        mostrarResultado(
            false,
            'Tipo requerido',
            'Debe seleccionar el tipo del ticket antes de guardar, responder o agregar una nota interna.'
        );
        $('#Tiquete_Tipo').focus();
        return false;
    }

    function bloquearBoton(boton, bloqueado, texto) {
        if (bloqueado) {
            boton.data('html-original', boton.html());
            boton.prop('disabled', true).html(`<i class="fa fa-spinner fa-spin mr-2"></i>${texto}`);
        } else {
            boton.prop('disabled', false).html(boton.data('html-original'));
        }
    }

    function escapar(valor) {
        return $('<div>').text(valor || '').html();
    }


    function contenidoEditor(selector) {
        const editor = $(selector);
        return editor.is('[contenteditable]')
            ? editor.html().trim()
            : editor.val().trim();
    }

    function limpiarEditor(selector) {
        const editor = $(selector);
        if (editor.is('[contenteditable]')) editor.empty();
        else editor.val('');
    }

    function redimensionarImagen(archivo) {
        return new Promise(function (resolve, reject) {
            if (archivo.size > 8 * 1024 * 1024) {
                reject(new Error('La imagen pegada supera el límite de 8 MB.'));
                return;
            }

            const lector = new FileReader();
            lector.onerror = () => reject(new Error('No se pudo leer la imagen.'));
            lector.onload = function (evento) {
                const imagen = new Image();
                imagen.onerror = () => reject(new Error('El contenido pegado no es una imagen válida.'));
                imagen.onload = function () {
                    const maximo = 1400;
                    const escala = Math.min(1, maximo / Math.max(imagen.width, imagen.height));
                    const canvas = document.createElement('canvas');
                    canvas.width = Math.round(imagen.width * escala);
                    canvas.height = Math.round(imagen.height * escala);

                    const contexto = canvas.getContext('2d');
                    contexto.drawImage(imagen, 0, 0, canvas.width, canvas.height);

                    const formato = archivo.type === 'image/png' ? 'image/png' : 'image/jpeg';
                    resolve(canvas.toDataURL(formato, 0.82));
                };
                imagen.src = evento.target.result;
            };
            lector.readAsDataURL(archivo);
        });
    }

    async function insertarImagenPegada(editor, archivo) {
        try {
            const dataUrl = await redimensionarImagen(archivo);
            editor.focus();
            document.execCommand(
                'insertHTML',
                false,
                `<img src="${dataUrl}" alt="Imagen pegada" style="display:block;max-width:100%;height:auto;margin:12px 0" /><br>`
            );
        } catch (error) {
            mostrarResultado(false, 'No se pudo pegar la imagen', error.message);
        }
    }

    function configurarPegadoImagenes() {
        $('#inputRespuesta').on('paste', function (evento) {
            const clipboard = evento.originalEvent.clipboardData;
            if (!clipboard || !clipboard.items) return;

            const imagenes = Array.from(clipboard.items)
                .filter(item => item.kind === 'file' && item.type.indexOf('image/') === 0)
                .map(item => item.getAsFile())
                .filter(Boolean);

            if (!imagenes.length) return;

            evento.preventDefault();
            if (imagenes.length > 3) {
                mostrarResultado(false, 'Demasiadas imágenes', 'Puede pegar un máximo de 3 imágenes por respuesta.');
                return;
            }

            const editor = $(this);
            imagenes.forEach(imagen => insertarImagenPegada(editor, imagen));
        });
    }

    function agregarAlHistorial(respuesta) {
        const clase = respuesta.esNotaInterna ? 'internal' : 'support';
        const icono = respuesta.esNotaInterna ? 'fa-lock' : 'fa-headset';
        const tipo = respuesta.esNotaInterna ? 'Nota interna' : 'Soporte';
        const html = `
            <article class="message-row ${clase}" data-respuesta-id="${respuesta.id}">
                <div class="message-avatar"><i class="fa ${icono}"></i></div>
                <div class="message-card">
                    <div class="message-header d-flex justify-content-between align-items-start">
                        <div>
                            <strong>${escapar(respuesta.autor)}</strong>
                            <span class="message-type">${tipo}</span>
                        </div>
                        <small class="text-muted">${escapar(respuesta.fecha)}</small>
                    </div>
                    <div class="message-body">${respuesta.texto}</div>
                </div>
            </article>`;

        $('#historialRespuestas').append(html);
        const contador = $('#contadorRespuestas');
        contador.text((parseInt(contador.text(), 10) || 0) + 1);
    }

    function enviarRespuesta() {
        const boton = $('#EnviarRespuesta');
        const selector = modoNotaInterna ? '#inputComentarios' : '#inputRespuesta';
        const texto = contenidoEditor(selector);

        if (!validarTipo()) return;

        if (!texto) {
            mostrarResultado(false, 'Mensaje requerido', 'Escriba un mensaje antes de continuar.');
            $(selector).focus();
            return;
        }

        bloquearBoton(boton, true, modoNotaInterna ? 'Guardando…' : 'Enviando…');

        const datos = $('#formTipos').serializeArray();
        datos.push({ name: 'idTicket', value: $('#TicketId').val() });
        datos.push({ name: 'texto', value: texto });
        datos.push({ name: 'esNotaInterna', value: modoNotaInterna });

        $.ajax({
            url: `${window.location.pathname}?handler=Responder`,
            method: 'POST',
            data: datos
        }).done(function (data) {
            limpiarEditor(selector);
            agregarAlHistorial(data.respuesta);
            mostrarResultado(true, modoNotaInterna ? 'Nota guardada' : 'Respuesta enviada', data.mensaje);
        }).fail(function (xhr) {
            mostrarResultado(false, 'No se completó la operación', mensajeError(xhr));
        }).always(function () {
            bloquearBoton(boton, false);
        });
    }

    function guardarTicket() {
        const boton = $('#GuardarCambios');
        if (!validarTipo()) return;

        bloquearBoton(boton, true, 'Guardando…');

        $.ajax({
            url: `${window.location.pathname}?handler=Guardar`,
            method: 'POST',
            data: $('#formTipos').serialize()
        }).done(function (data) {
            mostrarResultado(true, 'Cambios guardados', data.mensaje);
        }).fail(function (xhr) {
            mostrarResultado(false, 'No se guardaron los cambios', mensajeError(xhr));
        }).always(function () {
            bloquearBoton(boton, false);
        });
    }

    function guardarCamposAutomaticamente() {
        window.clearTimeout(autoGuardadoTimer);

        autoGuardadoTimer = window.setTimeout(function () {
            if (!validarTipo()) return;

            const boton = $('#GuardarCambios');
            bloquearBoton(boton, true, 'Guardando…');

            $.ajax({
                url: `${window.location.pathname}?handler=GuardarCampos`,
                method: 'POST',
                data: $('#formTipos').serialize()
            }).done(function () {
                boton.data(
                    'html-original',
                    '<i class="fa fa-save mr-2"></i>Guardar cambios'
                );
                boton.prop('disabled', false)
                    .html('<i class="fa fa-check mr-2"></i>Guardado');

                window.setTimeout(function () {
                    boton.html('<i class="fa fa-save mr-2"></i>Guardar cambios');
                }, 1400);
            }).fail(function (xhr) {
                bloquearBoton(boton, false);
                mostrarResultado(false, 'No se guardaron los cambios', mensajeError(xhr));
            });
        }, 350);
    }

    function configurarAdjuntos() {
        const previous = ($('#Adjunto').val() || '').split('¶').filter(Boolean);
        previous.forEach(function (url, index) {
            $('#src' + (index + 1)).attr('src', url);
        });

        new Dropzone('#dropzoneForm', {
            url: window.location.href,
            autoProcessQueue: false,
            maxFiles: Math.max(0, 2 - previous.length),
            maxFilesize: 3,
            acceptedFiles: '.png,.jpg,.jpeg',
            addRemoveLinks: true,
            dictDefaultMessage: '<strong>Arrastra imágenes aquí</strong><br>o haz clic para seleccionarlas (máximo 2)',
            dictRemoveFile: 'Eliminar',
            init: function () {
                const actualizar = () => {
                    const nuevas = this.files.map(file => file.dataURL).filter(Boolean);
                    $('#Adjunto').val(previous.concat(nuevas).slice(0, 2).join('¶'));
                };
                this.on('addedfile', actualizar);
                this.on('thumbnail', actualizar);
                this.on('removedfile', actualizar);
            }
        });
    }

    window.abrirModal = function () {
        $('#modalAdjuntos').modal('show');
    };

    $(function () {
        const duration = ($('#Duracion').val() || '00:00:00').split(':');
        h = parseInt(duration[0], 10) || 0;
        m = parseInt(duration[1], 10) || 0;
        s = parseInt(duration[2], 10) || 0;
        paintTimer();

        $('.start').on('click', startTimer);
        $('.stop').on('click', stopTimer);
        $('.reiniciar').on('click', resetTimer);

        $('.composer-tab').on('click', function () {
            const target = $(this).data('target');
            modoNotaInterna = target === 'notasPanel';
            $('.composer-tab').removeClass('active');
            $('.composer-panel').removeClass('active');
            $(this).addClass('active');
            $('#' + target).addClass('active');
            $('#EnviarRespuesta').html(modoNotaInterna
                ? '<i class="fa fa-lock mr-2"></i>Guardar nota interna'
                : '<i class="fa fa-paper-plane mr-2"></i>Enviar respuesta');
        });

        $('#EnviarRespuesta').on('click', enviarRespuesta);
        $('#GuardarCambios').on('click', guardarTicket);
        $('#CerrarModalResultado').on('click', cerrarModalResultado);
        $('#Tiquete_idLoginAsignado, #Tiquete_idEmpresa, #Tiquete_Tipo')
            .on('change', guardarCamposAutomaticamente);
        $('#formTipos').on('submit', function (event) { event.preventDefault(); });

        configurarAdjuntos();
        configurarPegadoImagenes();
    });
})(jQuery);



