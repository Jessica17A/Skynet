async function generarReporteAsignacionCompleta(idSolicitud) {
    const apiUrl = `/api/solicitudes/detalle-completo/${idSolicitud}`;

    const logoPath = '/img/logo1.png';
    const empresa = 'SkyNet S.A.';
    const eslogan = 'Soluciones técnicas rápidas y confiables';
    const contacto = 'Tel: (502) 5555-0000  ·  soporte@skynet.com  ·  www.skynet.com';

    Swal.fire({
        title: 'Cargando datos del reporte...',
        allowOutsideClick: false,
        didOpen: () => Swal.showLoading()
    });

    const res = await fetch(apiUrl);
    if (!res.ok) { Swal.fire('Error', 'No se pudieron obtener los datos.', 'error'); return; }
    const data = await res.json();
    Swal.close();
    if (!data.length) { Swal.fire('Sin datos', 'No hay registros para esta solicitud.', 'warning'); return; }

    const cab = data[0];

    const { jsPDF } = window.jspdf;
    const doc = new jsPDF({ orientation: 'p', unit: 'pt', format: 'a4' });
    const pageWidth = doc.internal.pageSize.getWidth();
    const margin = 48;
    let y = 56;

    // ===== utilidades =====
    const fmt = (v, alt = '-') => (v ?? '') === '' ? alt : String(v);
    const fmtDt = (iso) => iso ? new Date(iso).toLocaleString('es-GT', { hour12: false }) : '-';

    // Columna alineada: label en X=margin, valor en X=valueX
    const valueX = margin + 110;   // <<-- mueve esta cifra si quieres más/menos sangría
    const lineGap = 16;

    const kv = (label, value) => {
        doc.setFont('helvetica', 'bold'); doc.text(`${label}:`, margin, y);
        doc.setFont('helvetica', 'normal'); doc.text(value, valueX, y);
        y += lineGap;
    };
    const kvWrap = (label, value, maxWidth = pageWidth - valueX - margin) => {
        doc.setFont('helvetica', 'bold'); doc.text(`${label}:`, margin, y);
        doc.setFont('helvetica', 'normal');
        const lines = doc.splitTextToSize(value, maxWidth);
        doc.text(lines, valueX, y);
        y += lineGap + (lines.length > 1 ? (lines.length - 1) * 12 : 0);
    };

    async function loadImageAsDataURL(src) {
        return new Promise(resolve => {
            const img = new Image();
            img.crossOrigin = 'anonymous';
            img.onload = () => {
                try {
                    const c = document.createElement('canvas');
                    c.width = img.width; c.height = img.height;
                    const ctx = c.getContext('2d'); ctx.drawImage(img, 0, 0);
                    resolve(c.toDataURL('image/png'));
                } catch { resolve(null); }
            };
            img.onerror = () => resolve(null);
            img.src = src;
        });
    }

    // ===== Encabezado =====
    const logoData = await loadImageAsDataURL(logoPath);
    const logoW = 64, logoH = 64;
    if (logoData) doc.addImage(logoData, 'PNG', margin, y, logoW, logoH);

    doc.setFont('helvetica', 'bold'); doc.setFontSize(18);
    doc.text('INFORME DE VISITA TÉCNICA', pageWidth / 2, y + 18, { align: 'center' });

    doc.setFontSize(14); doc.text(empresa, pageWidth / 2, y + 44, { align: 'center' });
    doc.setFont('helvetica', 'normal'); doc.setFontSize(10);
    doc.text(eslogan, pageWidth / 2, y + 60, { align: 'center' });
    doc.text(contacto, pageWidth / 2, y + 74, { align: 'center' });

    y += Math.max(logoH, 84) + 10;
    doc.setLineWidth(0.6); doc.line(margin, y, pageWidth - margin, y); y += 18;

    // ===== Datos del ticket =====
    doc.setFont('helvetica', 'bold'); doc.setFontSize(12); doc.text('DATOS DEL TICKET', margin, y); y += 14;
    doc.setFont('helvetica', 'normal'); doc.setFontSize(12);
    kv('Ticket', fmt(cab.ticket));
    kv('Tipo', fmt(cab.tipo));
    kv('Prioridad', fmt(cab.prioridad));
    kv('Supervisor', fmt(cab.supervisorNombre, 'N/D'));
    
    y += 4; doc.setLineWidth(0.3); doc.line(margin, y, pageWidth - margin, y); y += 18;

    // ===== Datos del cliente solicitante =====
    doc.setFont('helvetica', 'bold'); doc.text('DATOS DEL SOLICITANTE', margin, y); y += 14;
    doc.setFont('helvetica', 'normal');
    kv('Nombre', fmt(cab.nombre));
    kv('Correo', fmt(cab.email));
    kv('Teléfono', fmt(cab.telefono));
    kvWrap('Dirección', fmt(cab.direccion));

    y += 4; doc.setLineWidth(0.3); doc.line(margin, y, pageWidth - margin, y); y += 18;

    // ===== Técnicos asignados (sin notas) =====
    doc.setFont('helvetica', 'bold'); doc.text('TECNICOS ASIGNADOS', margin, y); y += 16;
    doc.setFont('helvetica', 'normal');
    const vistos = new Set();
    data.forEach((t) => {
        const key = `${t.tecnicoNombre}|${t.asignacionFechaInicio}|${t.asignacionFechaFin}`;
        if (vistos.has(key)) return; vistos.add(key);

        // nombre técnico alineado a la izquierda
        doc.text(`${vistos.size}. ${fmt(t.tecnicoNombre)}`, margin, y); y += 14;
        // fechas en una misma línea
        doc.text(`Inicio: ${fmtDt(t.asignacionFechaInicio)}`, margin + 18, y);
        doc.text(`Fin: ${fmtDt(t.asignacionFechaFin)}`, margin + 240, y);
        y += 18;
    });

    y += 6; doc.setLineWidth(0.3); doc.line(margin, y, pageWidth - margin, y); y += 18;

    // ===== Descripción del trabajo =====
    doc.setFont('helvetica', 'bold'); doc.text('Descripción del trabajo', margin, y); y += 14;
    doc.setFont('helvetica', 'normal');
    const desc = doc.splitTextToSize(fmt(cab.descripcion), pageWidth - margin * 2);
    doc.text(desc, margin, y);
    y += desc.length * 12 + 10;

    // ===== Pie y fecha de generación =====
    y += 18; doc.setLineWidth(0.3); doc.line(margin, y, pageWidth - margin, y); y += 14;
    doc.setFontSize(9);
    const genStr = new Date().toLocaleString('es-GT', { hour12: false });
    
    doc.text(`Guatemala: ${genStr}`, pageWidth / 2, y, { align: 'center' });

   
    const blobUrl = doc.output('bloburl');
    window.open(blobUrl, '_blank');
}
