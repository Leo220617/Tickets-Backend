(function ($) {
    'use strict';

    Dropzone.autoDiscover = false;


    let modoNotaInterna = false;
    let autoGuardadoTimer = null;

    let segundosAcumulados = 0;

    function convertirASegundos(valor) {
        const partes = (valor || '00:00:00').split(':');

        const horas = parseInt(partes[0], 10) || 0;
        const minutos = parseInt(partes[1], 10) || 0;
        const segundos = parseInt(partes[2], 10) || 0;

        return (horas * 3600) + (minutos * 60) + segundos;
    }

    function convertirAFormatoTiempo(totalSegundos) {
        totalSegundos = Math.max(0, totalSegundos);

        const horas = Math.floor(totalSegundos / 3600);
        const minutos = Math.floor(
            (totalSegundos % 3600) / 60
        );

        return String(horas).padStart(2, '0') +
            ':' +
            String(minutos).padStart(2, '0') +
            ':00';
    }

    function mostrarTiempoAcumulado() {
        const horas = Math.floor(segundosAcumulados / 3600);
        const minutos = Math.floor(
            (segundosAcumulados % 3600) / 60
        );

        $('#TiempoAcumulado').text(
            horas + (horas === 1 ? ' hora ' : ' horas ') +
            minutos + (minutos === 1 ? ' minuto' : ' minutos')
        );
    }

    function validarNumeroTiempo(selector, maximo, nombre) {
        const valor = parseInt($(selector).val(), 10);

        if (isNaN(valor) || valor < 0 || valor > maximo) {
            mostrarResultado(
                false,
                'Tiempo no válido',
                nombre + ' debe estar entre 0 y ' + maximo + '.'
            );

            $(selector).focus();
            return false;
        }

        return true;
    }

    function prepararTiempos() {
        if (!validarNumeroTiempo(
            '#TiempoHoras',
            999,
            'Las horas invertidas'
        )) {
            return false;
        }

        if (!validarNumeroTiempo(
            '#TiempoMinutos',
            59,
            'Los minutos invertidos'
        )) {
            return false;
        }

        if (!validarNumeroTiempo(
            '#EstimadoHoras',
            999,
            'Las horas estimadas'
        )) {
            return false;
        }

        if (!validarNumeroTiempo(
            '#EstimadoMinutos',
            59,
            'Los minutos estimados'
        )) {
            return false;
        }

        return true;
    }
    function confirmarTiempoGuardado(data) {
        if (data && data.duracion) {
            $('#Duracion').val(data.duracion);
            $('#DuracionReal').val(data.duracion);

            segundosAcumulados =
                convertirASegundos(data.duracion);
        }

        if (data && data.duracionEstimada) {
            $('#DuracionEstimada').val(
                data.duracionEstimada
            );
        }

        $('#TiempoHoras').val(0);
        $('#TiempoMinutos').val(0);

        mostrarTiempoAcumulado();
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

    function obtenerDestinatarios() {
        return ($('#DestinatariosRespuesta').val() || '')
            .split(/[;,]+/)
            .map(function (correo) {
                return correo.trim();
            })
            .filter(function (correo) {
                return correo.length > 0;
            })
            .filter(function (correo, indice, lista) {
                return lista.findIndex(function (elemento) {
                    return elemento.toLowerCase() === correo.toLowerCase();
                }) === indice;
            });
    }

    function correoValido(correo) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(correo);
    }

    function validarDestinatarios() {
        const destinatarios = obtenerDestinatarios();

        if (destinatarios.length === 0) {
            mostrarResultado(
                false,
                'Destinatario requerido',
                'Debe ingresar al menos un correo electrónico.'
            );

            $('#DestinatariosRespuesta').focus();
            return false;
        }

        const invalidos = destinatarios.filter(function (correo) {
            return !correoValido(correo);
        });

        if (invalidos.length > 0) {
            mostrarResultado(
                false,
                'Correo no válido',
                'Revise los siguientes correos: ' + invalidos.join(', ')
            );

            $('#DestinatariosRespuesta').focus();
            return false;
        }

        return true;
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
                `<img src="${dataUrl}" alt="Imagen pegada" style="display:block;max-width:100%;height:auto;margin:12px 0" /><div><br></div>`
            );

            editor[0].scrollIntoView({
                behavior: 'smooth',
                block: 'nearest'
            });
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

    async function enviarRespuesta() {
        const boton = $('#EnviarRespuesta');

        const selector = modoNotaInterna
            ? '#inputComentarios'
            : '#inputRespuesta';

        const texto = contenidoEditor(selector);

        if (!validarTipo()) {
            return;
        }

        if (!texto) {
            mostrarResultado(
                false,
                'Mensaje requerido',
                'Escriba un mensaje antes de continuar.'
            );

            $(selector).focus();
            return;
        }

        if (!modoNotaInterna && !validarDestinatarios()) {
            return;
        }

        let nuevoStatus = '';

        // Solo solicitar un estado cuando se envía una respuesta al cliente.
        if (!modoNotaInterna) {
            const resultado = await Swal.fire({
                icon: 'question',
                title: 'Estado del tiquete',
                text: '¿A qué estado desea pasar el tiquete?',
                input: 'select',
                inputOptions: {
                    V: 'Validación',
                    C: 'Cerrado'
                },
                inputPlaceholder: 'Seleccione un estado',
                showCancelButton: true,
                confirmButtonText: 'Enviar respuesta',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#6c757d',

                inputValidator: function (valor) {
                    if (!valor) {
                        return 'Debe seleccionar un estado.';
                    }
                }
            });

            if (!resultado.isConfirmed) {
                return;
            }

            nuevoStatus = resultado.value;
        }

        // Calcula el nuevo tiempo acumulado después de confirmar.
        if (!prepararTiempos()) {
            return;
        }

        bloquearBoton(
            boton,
            true,
            modoNotaInterna
                ? 'Guardando…'
                : 'Enviando…'
        );

        const datos = $('#formTipos').serializeArray();

        datos.push({
            name: 'idTicket',
            value: $('#TicketId').val()
        });

        datos.push({
            name: 'texto',
            value: texto
        });

        datos.push({
            name: 'esNotaInterna',
            value: modoNotaInterna
        });

        datos.push({
            name: 'nuevoStatus',
            value: nuevoStatus
        });

        $.ajax({
            url: `${window.location.pathname}?handler=Responder`,
            method: 'POST',
            data: datos
        })
            .done(function (data) {
                // Actualiza el acumulado y limpia horas/minutos ingresados.
                confirmarTiempoGuardado(data);

                limpiarEditor(selector);

                // Las notas internas permanecen en esta página.
                if (modoNotaInterna) {
                    agregarAlHistorial(data.respuesta);

                    mostrarResultado(
                        true,
                        'Nota guardada',
                        data.mensaje
                    );

                    return;
                }

                // Después de enviar y cambiar a Validación o Cerrado,
                // regresar al listado de tiquetes.
                if (nuevoStatus === 'V' ||
                    nuevoStatus === 'C') {

                    const mensajeEstado =
                        nuevoStatus === 'V'
                            ? 'El tiquete pasó a Validación.'
                            : 'El tiquete fue cerrado correctamente.';

                    Swal.fire({
                        icon: 'success',
                        title: 'Respuesta enviada',
                        text: mensajeEstado,
                        showConfirmButton: false,
                        timer: 1200,
                        timerProgressBar: true,
                        allowOutsideClick: false,
                        allowEscapeKey: false
                    }).then(function () {
                        window.location.href =
                            $('#UrlIndexTiquetes').val();
                    });

                    return;
                }

                agregarAlHistorial(data.respuesta);

                mostrarResultado(
                    true,
                    'Respuesta enviada',
                    data.mensaje
                );
            })
            .fail(function (xhr) {
                mostrarResultado(
                    false,
                    'No se completó la operación',
                    mensajeError(xhr)
                );
            })
            .always(function () {
                bloquearBoton(boton, false);
            });
    }

    function guardarTicket() {
        const boton = $('#GuardarCambios');

        if (!validarTipo()) {
            return;
        }

        if (!prepararTiempos()) {
            return;
        }

        bloquearBoton(
            boton,
            true,
            'Guardando…'
        );

        $.ajax({
            url: `${window.location.pathname}?handler=Guardar`,
            method: 'POST',
            data: $('#formTipos').serialize()
        })
            .done(function (data) {
                confirmarTiempoGuardado(data);

                mostrarResultado(
                    true,
                    'Cambios guardados',
                    data.mensaje
                );
            })
            .fail(function (xhr) {
                mostrarResultado(
                    false,
                    'No se guardaron los cambios',
                    mensajeError(xhr)
                );
            })
            .always(function () {
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
    function aplicarIconoAdjunto(archivo) {
        if (!archivo || !archivo.previewElement) {
            return;
        }

        const nombre = archivo.name || '';
        const extension = (
            nombre.split('.').pop() || ''
        ).toLowerCase();

        let claseIcono = 'fa fa-file-o';
        let claseColor = 'default';

        switch (extension) {
            case 'pdf':
                claseIcono = 'fa fa-file-pdf-o';
                claseColor = 'pdf';
                break;

            case 'xls':
            case 'xlsx':
                claseIcono = 'fa fa-file-excel-o';
                claseColor = 'excel';
                break;

            case 'doc':
            case 'docx':
                claseIcono = 'fa fa-file-word-o';
                claseColor = 'word';
                break;

            case 'csv':
                claseIcono = 'fa fa-file-text-o';
                claseColor = 'csv';
                break;

            case 'png':
            case 'jpg':
            case 'jpeg':
                claseIcono = 'fa fa-file-image-o';
                claseColor = 'image';
                break;
        }

        const contenedor =
            archivo.previewElement.querySelector(
                '.dz-image'
            );

        if (contenedor) {
            contenedor.innerHTML =
                '<i class="' +
                claseIcono +
                ' dz-file-icon ' +
                claseColor +
                '" aria-hidden="true"></i>';
        }

        const nombreElemento =
            archivo.previewElement.querySelector(
                '.dz-filename span'
            );

        if (nombreElemento) {
            nombreElemento.title = nombre;
        }
    }
    function configurarAdjuntos() {
        const permitidos =
            '.png,.jpg,.jpeg,.pdf,.xls,.xlsx,.doc,.docx,.csv';

        const limiteTotalBytes = 18 * 1024 * 1024;
        const maximoArchivos = 5;

        const anteriores = ($('#Adjunto').val() || '')
            .split('¶')
            .filter(function (contenido) {
                return contenido &&
                    contenido.trim().length > 0;
            });

        function calcularTamanoDataUrl(contenido) {
            if (!contenido) {
                return 0;
            }

            const posicionComa = contenido.indexOf(',');

            if (posicionComa < 0) {
                return 0;
            }

            const base64 = contenido
                .substring(posicionComa + 1)
                .replace(/\s/g, '');

            if (!base64.length) {
                return 0;
            }

            let relleno = 0;

            if (base64.endsWith('==')) {
                relleno = 2;
            } else if (base64.endsWith('=')) {
                relleno = 1;
            }

            return Math.floor(
                (base64.length * 3) / 4
            ) - relleno;
        }

        function calcularTamanoTotal(contenidos) {
            return contenidos.reduce(
                function (total, contenido) {
                    return total +
                        calcularTamanoDataUrl(contenido);
                },
                0
            );
        }

        function formatearMegabytes(bytes) {
            return (
                bytes / (1024 * 1024)
            ).toFixed(2);
        }

        function obtenerArchivosNuevos(dropzone) {
            return dropzone.files
                .map(function (archivo) {
                    return archivo.contenidoCompleto;
                })
                .filter(function (contenido) {
                    return contenido &&
                        contenido.length > 0;
                });
        }

        function actualizarAdjuntos(dropzone) {
            const nuevos =
                obtenerArchivosNuevos(dropzone);

            const todos = anteriores
                .concat(nuevos)
                .slice(0, maximoArchivos);

            $('#Adjunto').val(
                todos.join('¶')
            );

            mostrarListaAdjuntos(todos);
        }

        const tamanoAnterior =
            calcularTamanoTotal(anteriores);

        mostrarListaAdjuntos(anteriores);

        if (anteriores.length >= maximoArchivos) {
            $('#dropzoneForm').addClass('d-none');

            $('<div>', {
                class: 'alert alert-info',
                text:
                    'Este tiquete ya tiene el máximo de ' +
                    maximoArchivos +
                    ' archivos.'
            }).insertAfter('#dropzoneForm');

            return;
        }

        if (tamanoAnterior >= limiteTotalBytes) {
            $('#dropzoneForm').addClass('d-none');

            $('<div>', {
                class: 'alert alert-warning',
                text:
                    'Los archivos actuales ya alcanzan el límite de 18 MB.'
            }).insertAfter('#dropzoneForm');

            return;
        }

        new Dropzone('#dropzoneForm', {
            url: window.location.href,
            autoProcessQueue: false,

            maxFiles: Math.max(
                0,
                maximoArchivos - anteriores.length
            ),

            // Límite individual controlado por Dropzone.
            maxFilesize: 18,

            acceptedFiles: permitidos,
            addRemoveLinks: true,

            dictDefaultMessage:
                '<i class="fa fa-cloud-upload" style="font-size:30px;color:#0073bb"></i><br>' +
                '<strong>Haz clic o arrastra tus archivos aquí</strong><br>' +
                '<span>PDF, Excel, Word, CSV, PNG o JPG</span><br>' +
                '<small>Máximo 5 archivos y 18 MB en total</small>',

            dictRemoveFile: 'Eliminar',

            dictInvalidFileType:
                'Este tipo de archivo no está permitido.',

            dictFileTooBig:
                'El archivo supera el límite permitido de 18 MB.',

            dictMaxFilesExceeded:
                'Solamente se permiten 5 archivos.',

            init: function () {
                const dropzone = this;

                this.on('addedfile', function (archivo) {
                    aplicarIconoAdjunto(archivo);
                    // Dropzone puede agregar primero el archivo y
                    // después marcarlo como inválido.
                    if (archivo.size > limiteTotalBytes) {
                        dropzone.removeFile(archivo);

                        mostrarResultado(
                            false,
                            'Archivo demasiado grande',
                            'El archivo ' +
                            archivo.name +
                            ' supera el límite de 18 MB.'
                        );

                        return;
                    }

                    const tamanoOtrosNuevos =
                        dropzone.files
                            .filter(function (item) {
                                return item !== archivo &&
                                    !item.rechazadoPorTamano;
                            })
                            .reduce(function (total, item) {
                                return total + (item.size || 0);
                            }, 0);

                    const totalProyectado =
                        tamanoAnterior +
                        tamanoOtrosNuevos +
                        archivo.size;

                    if (totalProyectado > limiteTotalBytes) {
                        archivo.rechazadoPorTamano = true;
                        dropzone.removeFile(archivo);

                        mostrarResultado(
                            false,
                            'Límite total superado',
                            'No se puede agregar ' +
                            archivo.name +
                            '. El total sería de ' +
                            formatearMegabytes(
                                totalProyectado
                            ) +
                            ' MB y el máximo permitido es 18 MB.'
                        );

                        return;
                    }

                    const lector = new FileReader();

                    lector.onload = function (evento) {
                        const dataUrl =
                            evento.target.result;

                        const posicionComa =
                            dataUrl.indexOf(',');

                        if (posicionComa < 0) {
                            dropzone.removeFile(archivo);

                            mostrarResultado(
                                false,
                                'Archivo no válido',
                                'No fue posible procesar ' +
                                archivo.name
                            );

                            return;
                        }

                        const base64 =
                            dataUrl.substring(
                                posicionComa + 1
                            );

                        const tipo =
                            archivo.type ||
                            obtenerTipoContenido(
                                archivo.name
                            );

                        archivo.contenidoCompleto =
                            'data:' + tipo +
                            ';name=' +
                            encodeURIComponent(
                                archivo.name
                            ) +
                            ';base64,' +
                            base64;

                        actualizarAdjuntos(dropzone);
                    };

                    lector.onerror = function () {
                        dropzone.removeFile(archivo);

                        mostrarResultado(
                            false,
                            'Archivo no válido',
                            'No fue posible leer ' +
                            archivo.name
                        );
                    };

                    lector.readAsDataURL(archivo);
                });

                this.on('removedfile', function () {
                    actualizarAdjuntos(dropzone);
                });
            }
        });
    }

    function obtenerTipoContenido(nombre) {
        const extension = (nombre.split('.').pop() || '')
            .toLowerCase();

        const tipos = {
            pdf: 'application/pdf',
            xls: 'application/vnd.ms-excel',
            xlsx: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
            doc: 'application/msword',
            docx: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
            csv: 'text/csv',
            png: 'image/png',
            jpg: 'image/jpeg',
            jpeg: 'image/jpeg'
        };

        return tipos[extension] || 'application/octet-stream';
    }

    function mostrarListaAdjuntos(adjuntos) {
        const contenedor = $('#listaAdjuntos');
        contenedor.empty();

        if (!adjuntos.length) {
            contenedor.html(
                '<div class="text-muted text-center py-4">' +
                '<i class="fa fa-paperclip mr-2"></i>' +
                'No hay archivos adjuntos.' +
                '</div>'
            );

            return;
        }

        adjuntos.forEach(function (contenido, indice) {
            const informacion = leerInformacionAdjunto(
                contenido,
                indice
            );

            const elemento = $('<a>', {
                href: contenido,
                download: informacion.nombre,
                class: 'archivo-adjunto',
                title: 'Descargar ' + informacion.nombre
            });

            elemento.append(
                $('<span>', {
                    class: 'archivo-icono'
                }).append(
                    $('<i>', {
                        class: informacion.icono
                    })
                )
            );

            elemento.append(
                $('<span>', {
                    class: 'archivo-nombre',
                    text: informacion.nombre
                })
            );

            elemento.append(
                $('<i>', {
                    class: 'fa fa-download archivo-descarga'
                })
            );

            contenedor.append(elemento);
        });
    }

    function leerInformacionAdjunto(contenido, indice) {
        const coincidencia = contenido.match(
            /^data:([^;]+)(?:;name=([^;]+))?;base64,/i
        );

        let tipo = '';
        let nombre = 'Adjunto_' + (indice + 1);

        if (coincidencia) {
            tipo = coincidencia[1] || '';

            if (coincidencia[2]) {
                nombre = decodeURIComponent(coincidencia[2]);
            }
        }

        let icono = 'fa fa-file-o';

        if (tipo.indexOf('pdf') >= 0) {
            icono = 'fa fa-file-pdf-o';
        } else if (
            tipo.indexOf('excel') >= 0 ||
            tipo.indexOf('spreadsheet') >= 0 ||
            tipo.indexOf('csv') >= 0
        ) {
            icono = 'fa fa-file-excel-o';
        } else if (
            tipo.indexOf('word') >= 0 ||
            tipo.indexOf('document') >= 0
        ) {
            icono = 'fa fa-file-word-o';
        } else if (tipo.indexOf('image/') === 0) {
            icono = 'fa fa-file-image-o';
        }

        return {
            nombre: nombre,
            tipo: tipo,
            icono: icono
        };
    }

    window.abrirModalAdjuntos = function () {
        const modal = $('#modalAdjuntos');

        // Bootstrap 4 con jQuery.
        if (
            $.fn.modal &&
            typeof modal.modal === 'function'
        ) {
            modal.modal({
                backdrop: true,
                keyboard: true,
                show: true
            });

            return;
        }

        // Bootstrap 5 sin jQuery.
        if (
            window.bootstrap &&
            window.bootstrap.Modal
        ) {
            const elemento =
                document.getElementById(
                    'modalAdjuntos'
                );

            const instancia =
                window.bootstrap.Modal.getOrCreateInstance
                    ? window.bootstrap.Modal.getOrCreateInstance(
                        elemento
                    )
                    : new window.bootstrap.Modal(
                        elemento
                    );

            instancia.show();
            return;
        }

        console.error(
            'No se encontró el componente Modal de Bootstrap.'
        );
    };

    window.cerrarModalAdjuntos = function () {
        const modal = $('#modalAdjuntos');

        // Bootstrap 4 con jQuery.
        if (
            $.fn.modal &&
            typeof modal.modal === 'function'
        ) {
            modal.modal('hide');
            return;
        }

        // Bootstrap 5 sin jQuery.
        if (
            window.bootstrap &&
            window.bootstrap.Modal
        ) {
            const elemento =
                document.getElementById(
                    'modalAdjuntos'
                );

            const instancia =
                window.bootstrap.Modal.getInstance
                    ? window.bootstrap.Modal.getInstance(
                        elemento
                    )
                    : null;

            if (instancia) {
                instancia.hide();
            }

            return;
        }

        // Respaldo visual.
        modal.removeClass('show').hide();
        $('body').removeClass('modal-open');
        $('.modal-backdrop').remove();
    };

    $(function () {
        segundosAcumulados = convertirASegundos(
            $('#Duracion').val()
        );

        mostrarTiempoAcumulado();

        const estimadoInicial = convertirASegundos(
            $('#DuracionEstimada').val()
        );

        $('#EstimadoHoras').val(
            Math.floor(estimadoInicial / 3600)
        );

        $('#EstimadoMinutos').val(
            Math.floor((estimadoInicial % 3600) / 60)
        );

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
    $(document).on('click', '#UnificarTiquete', async function () {
        const ticketPrincipalId =
            parseInt($('#TicketId').val(), 10);

        if (!ticketPrincipalId) {
            Swal.fire(
                'Error',
                'No fue posible identificar el tiquete principal.',
                'error'
            );

            return;
        }

        const resultado = await Swal.fire({
            icon: 'info',
            title: 'Unificar tiquetes',
            html:
                '<p>El tiquete <strong>#' +
                ticketPrincipalId +
                '</strong> permanecerá como principal.</p>' +
                '<p>Digite el número del tiquete que desea incorporar.</p>',
            input: 'number',
            inputPlaceholder: 'Número del tiquete secundario',
            inputAttributes: {
                min: '1',
                step: '1'
            },
            showCancelButton: true,
            confirmButtonText: 'Continuar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#7b3fb4',
            inputValidator: function (valor) {
                const ticketSecundarioId =
                    parseInt(valor, 10);

                if (!ticketSecundarioId ||
                    ticketSecundarioId <= 0) {
                    return 'Digite un número de tiquete válido.';
                }

                if (ticketSecundarioId ===
                    ticketPrincipalId) {
                    return 'No puede unificar el tiquete consigo mismo.';
                }

                return null;
            }
        });

        if (!resultado.isConfirmed) {
            return;
        }

        const ticketSecundarioId =
            parseInt(resultado.value, 10);

        const confirmacion = await Swal.fire({
            icon: 'warning',
            title: '¿Confirmar unificación?',
            html:
                'El tiquete <strong>#' +
                ticketSecundarioId +
                '</strong> será cerrado y su historial será movido al ' +
                'tiquete principal <strong>#' +
                ticketPrincipalId +
                '</strong>.',
            showCancelButton: true,
            confirmButtonText: 'Sí, unificar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#7b3fb4'
        });

        if (!confirmacion.isConfirmed) {
            return;
        }

        Swal.fire({
            title: 'Unificando tiquetes',
            text: 'Por favor espere...',
            allowOutsideClick: false,
            allowEscapeKey: false,
            didOpen: function () {
                Swal.showLoading();
            }
        });

        const token = $('#formTipos input[name="__RequestVerificationToken"]')
            .val();

        $.ajax({
            url: window.location.pathname + '?handler=Unificar',
            method: 'POST',
            data: {
                __RequestVerificationToken: token,
                ticketPrincipalId: ticketPrincipalId,
                ticketSecundarioId: ticketSecundarioId
            }
        })
            .done(function (respuesta) {
                if (!respuesta || !respuesta.ok) {
                    Swal.fire(
                        'No se pudo unificar',
                        respuesta && respuesta.mensaje
                            ? respuesta.mensaje
                            : 'No fue posible completar la operación.',
                        'error'
                    );

                    return;
                }

                Swal.fire({
                    icon: 'success',
                    title: 'Tiquetes unificados',
                    text: respuesta.mensaje,
                    confirmButtonColor: '#0073bb'
                }).then(function () {
                    window.location.reload();
                });
            })
            .fail(function (xhr) {
                Swal.fire(
                    'Error',
                    mensajeError(xhr),
                    'error'
                );
            });
    });
})(jQuery);



