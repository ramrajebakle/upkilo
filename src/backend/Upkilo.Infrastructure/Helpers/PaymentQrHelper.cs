using QRCoder;

namespace Upkilo.Infrastructure.Helpers;

public static class PaymentQrHelper
{
    public static byte[] GenerateUpiQrCode(string upiId, string name, decimal amount, string currency, string transactionId)
    {
        // UPI URI format: upi://pay?pa=<upi_id>&pn=<name>&am=<amount>&cu=<currency>&tn=<note>
        var upiUri = $"upi://pay?pa={upiId}&pn={Uri.EscapeDataString(name)}&am={amount:F2}&cu={currency}&tn={Uri.EscapeDataString(transactionId)}";

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(upiUri, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);

        return qrCode.GetGraphic(20);
    }
}
