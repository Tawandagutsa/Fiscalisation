using System.Text.Json.Serialization;

namespace FiscalisationService.Models;

public sealed class ReceiptDetails
{
    [JsonPropertyName("receiptType")]
    public int ReceiptType { get; set; }
    [JsonPropertyName("receiptCurrency")]
    public string ReceiptCurrency { get; set; } = "";
    [JsonPropertyName("deviceID")]
    public string DeviceId { get; set; } = "";
    [JsonPropertyName("invoiceNo")]
    public string InvoiceNo { get; set; } = "";
    [JsonPropertyName("buyerData")]
    public BuyerData BuyerData { get; set; } = new();
    [JsonPropertyName("receiptNotes")]
    public string ReceiptNotes { get; set; } = "";
    [JsonPropertyName("receiptDate")]
    public string ReceiptDate { get; set; } = "";
    [JsonPropertyName("receiptLines")]
    public List<ReceiptLine> ReceiptLines { get; set; } = new();
    [JsonPropertyName("receiptPayments")]
    public List<ReceiptPayment> ReceiptPayments { get; set; } = new();
    [JsonPropertyName("receiptTotal")]
    public string ReceiptTotal { get; set; } = "";
}

public sealed class BuyerData
{
    [JsonPropertyName("buyerRegisterName")]
    public string BuyerRegisterName { get; set; } = "";
    [JsonPropertyName("buyerTradeName")]
    public string BuyerTradeName { get; set; } = "";
    [JsonPropertyName("vatNumber")]
    public string VatNumber { get; set; } = "";
    [JsonPropertyName("buyerTIN")]
    public string BuyerTin { get; set; } = "";
    [JsonPropertyName("buyerContacts")]
    public BuyerContacts BuyerContacts { get; set; } = new();
    [JsonPropertyName("buyerAddress")]
    public BuyerAddress BuyerAddress { get; set; } = new();
}

public sealed class BuyerContacts
{
    [JsonPropertyName("phoneNo")]
    public string PhoneNo { get; set; } = "";
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";
}

public sealed class BuyerAddress
{
    [JsonPropertyName("province")]
    public string Province { get; set; } = "";
    [JsonPropertyName("street")]
    public string Street { get; set; } = "";
    [JsonPropertyName("houseNo")]
    public string HouseNo { get; set; } = "";
    [JsonPropertyName("city")]
    public string City { get; set; } = "";
}

public sealed class ReceiptLine
{
    [JsonPropertyName("receiptLineType")]
    public string ReceiptLineType { get; set; } = "";
    [JsonPropertyName("receiptLineNo")]
    public int ReceiptLineNo { get; set; }
    [JsonPropertyName("receiptLineHSCode")]
    public string ReceiptLineHSCode { get; set; } = "";
    [JsonPropertyName("receiptLineName")]
    public string ReceiptLineName { get; set; } = "";
    [JsonPropertyName("receiptLinePrice")]
    public string ReceiptLinePrice { get; set; } = "";
    [JsonPropertyName("receiptLineQuantity")]
    public int ReceiptLineQuantity { get; set; }
    [JsonPropertyName("receiptLineTotal")]
    public string ReceiptLineTotal { get; set; } = "";
    [JsonPropertyName("taxPercent")]
    public decimal TaxPercent { get; set; }
}

public sealed class ReceiptPayment
{
    [JsonPropertyName("moneyTypeCode")]
    public int MoneyTypeCode { get; set; }
    [JsonPropertyName("paymentAmount")]
    public string PaymentAmount { get; set; } = "";
}

public sealed class FiscalResponse
{
    [JsonPropertyName("VerificationCode")]
    public string? VerificationCode { get; set; }

    [JsonPropertyName("qrlUrl")]
    public string? QrUrl { get; set; }

    [JsonPropertyName("FiscalisationStatus")]
    public string? FiscalisationStatus { get; set; }

    [JsonPropertyName("dreceiptNumber")]
    public string? DReceiptNumber { get; set; }

    [JsonPropertyName("invoiceDate")]
    public string? InvoiceDate { get; set; }

    [JsonPropertyName("deviceID")]
    public string? DeviceId { get; set; }
}
