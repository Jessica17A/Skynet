// ticket-pdf.js
function generatePdf(ticket) {
    if (!window.jspdf || !window.jspdf.jsPDF) {
        console.warn('jsPDF no cargado; no se generó PDF.');
        return;
    }
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF({ unit: 'pt', format: 'letter' }); // carta 612x792 pt aprox

    const pageW = doc.internal.pageSize.getWidth();
    const centerX = pageW / 2;
    let y = 100;

    // LOGO (desde wwwroot/img/logo.png)
    const logo = new Image();
    logo.src = '/img/logo.png'; // ruta relativa a wwwroot

    logo.onload = function () {
        // Logo más grande
        doc.addImage(logo, 'PNG', centerX - 75, y, 150, 150);
        y += 200;

        // Encabezado super grande
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(28);
        doc.text('Unidad de Solicitudes', centerX, y, { align: 'center' });
        y += 40;

        doc.setFontSize(24);
        doc.text('Comprobante de Ticket', centerX, y, { align: 'center' });
        y += 40;

        // Línea de separación
        doc.setLineWidth(1.5);
        doc.line(56, y, pageW - 56, y);
        y += 60;

        // Número de ticket bien grande
        doc.setFontSize(32);
        doc.text(`Solicitud: ${ticket}`, centerX, y, { align: 'center' });
        y += 80;

        // Texto explicativo en grande
        const url = `${location.origin}/Solicitudes/Tracking?ticket=${encodeURIComponent(ticket)}`;
        doc.setFont('helvetica', 'normal');
        doc.setFontSize(18);
        doc.text(
            'Utilice estos datos para consultar el estado de su solicitud en:',
            centerX, y,
            { align: 'center' }
        );
        y += 40;

        doc.setTextColor(28, 98, 241);
        doc.setFontSize(18);
        doc.textWithLink(url, centerX, y, { align: 'center', url });
        doc.setTextColor(0, 0, 0);
        y += 100;

        // Pie de página con fecha grande
        const now = new Date();
        const fecha = now.toLocaleString('es-GT', { timeZone: 'America/Guatemala' });
        doc.setFontSize(14);
        doc.text(`Guatemala, ${fecha}`, centerX, 792 - 72, { align: 'center' });

        // Descargar
        doc.save(`Ticket-${ticket}.pdf`);
    };
}
