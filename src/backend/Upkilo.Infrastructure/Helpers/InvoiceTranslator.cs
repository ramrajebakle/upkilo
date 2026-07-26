using System.Collections.Generic;

namespace Upkilo.Infrastructure.Helpers;

public static class InvoiceTranslator
{
    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        ["en"] = new()
        {
            ["Invoice"] = "Invoice",
            ["Receipt"] = "Receipt",
            ["BillTo"] = "Bill To",
            ["Date"] = "Date",
            ["Description"] = "Description",
            ["UnitPrice"] = "Unit Price",
            ["Quantity"] = "Quantity",
            ["Total"] = "Total",
            ["Patient"] = "Patient",
            ["ThankYou"] = "Thank you for your business!",
            ["ScanToPay"] = "Scan to Pay",
            ["Paid"] = "PAID"
        },
        ["hi"] = new()
        {
            ["Invoice"] = "बीजक",
            ["Receipt"] = "रसीद",
            ["BillTo"] = "बिल प्राप्तकर्ता",
            ["Date"] = "तारीख",
            ["Description"] = "विवरण",
            ["UnitPrice"] = "इकाई मूल्य",
            ["Quantity"] = "मात्रा",
            ["Total"] = "कुल",
            ["Patient"] = "मरीज",
            ["ThankYou"] = "हमारे साथ व्यापार करने के लिए धन्यवाद!",
            ["ScanToPay"] = "भुगतान के लिए स्कैन करें",
            ["Paid"] = "भुगतान किया गया"
        },
        ["es"] = new()
        {
            ["Invoice"] = "Factura",
            ["Receipt"] = "Recibo",
            ["BillTo"] = "Facturar a",
            ["Date"] = "Fecha",
            ["Description"] = "Descripción",
            ["UnitPrice"] = "Precio unitario",
            ["Quantity"] = "Cantidad",
            ["Total"] = "Total",
            ["Patient"] = "Paciente",
            ["ThankYou"] = "¡Gracias por su negocio!",
            ["ScanToPay"] = "Escanear para pagar",
            ["Paid"] = "PAGADO"
        },
        ["fr"] = new()
        {
            ["Invoice"] = "Facture",
            ["Receipt"] = "Reçu",
            ["BillTo"] = "Facturer à",
            ["Date"] = "Date",
            ["Description"] = "Description",
            ["UnitPrice"] = "Prix unitaire",
            ["Quantity"] = "Quantité",
            ["Total"] = "Total",
            ["Patient"] = "Patient",
            ["ThankYou"] = "Merci pour votre confiance !",
            ["ScanToPay"] = "Scanner pour payer",
            ["Paid"] = "PAYÉ"
        },
        ["de"] = new()
        {
            ["Invoice"] = "Rechnung",
            ["Receipt"] = "Quittung",
            ["BillTo"] = "Rechnung an",
            ["Date"] = "Datum",
            ["Description"] = "Beschreibung",
            ["UnitPrice"] = "Einzelpreis",
            ["Quantity"] = "Menge",
            ["Total"] = "Gesamt",
            ["Patient"] = "Patient",
            ["ThankYou"] = "Vielen Dank für Ihre Bestellung!",
            ["ScanToPay"] = "Zum Bezahlen scannen",
            ["Paid"] = "BEZAHLT"
        }
    };

    public static string GetLabel(string key, string locale)
    {
        // Extract language code (e.g., "en-US" -> "en")
        var lang = locale.Split('-')[0].ToLower();

        if (Translations.TryGetValue(lang, out var langDict) && langDict.TryGetValue(key, out var label))
        {
            return label;
        }

        // Fallback to English
        if (Translations["en"].TryGetValue(key, out var fallbackLabel))
        {
            return fallbackLabel;
        }

        return key;
    }
}
