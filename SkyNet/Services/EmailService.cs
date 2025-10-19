using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks; // usamos este Task
using TaskSys = System.Threading.Tasks.Task;

namespace SkyNet.Services
{
    public class EmailService
    {
        private readonly string _apiKey = "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";

        public EmailService()
        {
            Configuration.Default.ApiKey.Add("api-key", _apiKey);
        }

        public async TaskSys EnviarCorreoFinalizacionAsync(string emailDestino, string nombreCliente, string ticket)
        {
            try
            {
                var apiInstance = new TransactionalEmailsApi();
                var remitente = new SendSmtpEmailSender(name: "SkyNet S.A.", email: "pruebacurso4965@gmail.com");

                var destinatarios = new List<SendSmtpEmailTo>
                {
                    new SendSmtpEmailTo(emailDestino, nombreCliente)
                };

                string asunto = $"Finalización de su solicitud #{ticket}";

                // 🔹 URL pública del logo en Cloudinary
                string logoUrl = "https://res.cloudinary.com/dz4b8ug9h/image/upload/v1760849473/logo_guhibi.png";

                string contenido = $@"
                <!DOCTYPE html>
                <html lang='es'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <style>
                        body {{
                            font-family: 'Segoe UI', Roboto, Arial, sans-serif;
                            background-color: #f6f7fb;
                            margin: 0;
                            padding: 0;
                        }}
                        .container {{
                            max-width: 600px;
                            margin: 30px auto;
                            background: #ffffff;
                            border-radius: 10px;
                            overflow: hidden;
                            box-shadow: 0 4px 10px rgba(0,0,0,0.08);
                        }}
                        .header {{
                            background: linear-gradient(90deg, #0078ff, #00c4ff);
                            color: white;
                            text-align: center;
                            padding: 30px 20px;
                            border-radius: 10px 10px 0 0;
                        }}
                        .header img {{
                            width: 90px;
                            border-radius: 12px;
                            background-color: white;
                            padding: 6px;
                            display: block;
                            margin: 0 auto 10px auto;
                        }}
                        .header h1 {{
                            margin: 0;
                            font-size: 26px;
                            font-weight: bold;
                            color: white;
                        }}
                        .content {{
                            padding: 30px;
                            color: #333;
                            text-align: left;
                        }}
                        .content h2 {{
                            color: #0078ff;
                            margin-bottom: 10px;
                        }}
                        .content p {{
                            line-height: 1.6;
                            font-size: 15px;
                        }}
                        .footer {{
                            background: #f0f0f0;
                            text-align: center;
                            padding: 15px;
                            font-size: 12px;
                            color: #777;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <img src='{logoUrl}' alt='SkyNet Logo'>
                            <h1>Solicitud Finalizada</h1>
                        </div>

                        <div class='content'>
                            <h2>Hola {nombreCliente},</h2>
                            <p>Nos complace informarte que tu solicitud con número de ticket 
                            <strong>#{ticket}</strong> ha sido finalizada exitosamente.</p>

                            <p>Gracias por confiar en <strong>SkyNet S.A.</strong>. 
                            Nuestro equipo técnico se esfuerza por brindarte siempre el mejor servicio.</p>
                        </div>

                        <div class='footer'>
                            <p>Este es un mensaje automático, por favor no responder.</p>
                            <p>&copy; 2025 SkyNet S.A. | Todos los derechos reservados</p>
                        </div>
                    </div>
                </body>
                </html>";





                var email = new SendSmtpEmail(
                    sender: remitente,
                    to: destinatarios,
                    subject: asunto,
                    htmlContent: contenido
                );

                var resultado = await apiInstance.SendTransacEmailAsync(email);
                Console.WriteLine($"✅ Correo enviado correctamente a {emailDestino}. ID: {resultado.MessageId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al enviar correo: {ex.Message}");
            }

            return;
        }
    }
}
