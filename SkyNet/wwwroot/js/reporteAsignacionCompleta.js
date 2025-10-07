async function generarReporteAsignacionCompleta(idSolicitud) {
    const apiUrl = `/api/solicitudes/detalle-completo/${idSolicitud}`;

    Swal.fire({
        title: 'Cargando datos del reporte...',
        allowOutsideClick: false,
        didOpen: () => Swal.showLoading()
    });

    const res = await fetch(apiUrl);
    if (!res.ok) {
        Swal.fire('Error', 'No se pudieron obtener los datos del reporte.', 'error');
        return;
    }
    const data = await res.json();
    Swal.close();

    if (!data.length) {
        Swal.fire('Sin datos', 'No hay asignaciones registradas para esta solicitud.', 'warning');
        return;
    }

    // Tomamos la primera fila como cabecera general
    const cab = data[0];

    const { jsPDF } = window.jspdf;
    const doc = new jsPDF({ orientation: 'p', unit: 'pt', format: 'a4' });
    const pageWidth = doc.internal.pageSize.getWidth();
    const margin = 40;
    let y = 60;

    // ENCABEZADO
    doc.setFontSize(18);
    doc.setFont('helvetica', 'bold');
    doc.text('INFORME DE VISITA TÉCNICA', pageWidth / 2, y, { align: 'center' });
    y += 30;

    doc.setFontSize(12);
    doc.setFont('helvetica', 'normal');

    doc.text(`Ticket: ${cab.Ticket}`, margin, y); y += 16;
    doc.text(`Cliente: ${cab.Nombre}`, margin, y); y += 16;
    doc.text(`Dirección: ${cab.Direccion}`, margin, y); y += 16;
    doc.text(`Tipo: ${cab.Tipo}     Prioridad: ${cab.Prioridad}`, margin, y); y += 16;
    doc.text(`Supervisor: ${cab.SupervisorNombre || 'N/D'}`, margin, y); y += 16;
    doc.text(`Grupo: ${cab.GrupoEtiqueta}`, margin, y); y += 30;

    // TÉCNICOS
    doc.setFont('helvetica', 'bold');
    doc.text('TÉCNICOS ASIGNADOS', margin, y); y += 20;

    doc.setFont('helvetica', 'normal');
    data.forEach((t, i) => {
        doc.text(`${i + 1}. ${t.TecnicoNombre}`, margin + 10, y);
        y += 14;
        if (t.AsignacionFechaInicio)
            doc.text(`Inicio: ${new Date(t.AsignacionFechaInicio).toLocaleString()}`, margin + 30, y);
        if (t.AsignacionFechaFin)
            doc.text(`   Fin: ${new Date(t.AsignacionFechaFin).toLocaleString()}`, margin + 250, y);
        y += 14;
        if (t.AsignacionNotas)
            doc.text(`Notas: ${t.AsignacionNotas}`, margin + 30, y);
        y += 18;
    });

    y += 20;
    doc.setFont('helvetica', 'bold');
    doc.text('Descripción del trabajo:', margin, y); y += 16;
    doc.setFont('helvetica', 'normal');
    doc.text(doc.splitTextToSize(cab.Descripcion || '-', pageWidth - margin * 2), margin, y);

    // VISUALIZAR PDF (no descarga)
    doc.output('dataurlnewwindow');
}
