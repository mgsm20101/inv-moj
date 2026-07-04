using WIMS.Domain.Common;

namespace WIMS.Domain.Suppliers;

/// <summary>المورّد — يُربط بسندات الاستلام (GRN) فقط.</summary>
public class Supplier : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }

    /// <summary>الرقم الضريبي (VAT/ZATCA) — مهم للمالية.</summary>
    public string? TaxNumber { get; set; }
    public string? CommercialReg { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
}
