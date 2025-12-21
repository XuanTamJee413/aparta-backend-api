using ApartaAPI.DTOs.Contracts;
using ApartaAPI.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ApartaAPI.Services
{
    public class ContractPdfService : IContractPdfService
    {
        public byte[] GenerateContractPdf(ContractDto contract)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial"));

                    // 1. XỬ LÝ TIÊU ĐỀ DỰA TRÊN LOẠI HỢP ĐỒNG
                    string title = contract.ContractType == "Sale"
                        ? "HỢP ĐỒNG MUA BÁN CĂN HỘ"
                        : "HỢP ĐỒNG THUÊ CĂN HỘ";

                    page.Header()
                        .Text(title)
                        .SemiBold().FontSize(18).AlignCenter();

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Spacing(10);

                        // Thông tin chung
                        col.Item().Text(t => { t.Span("Mã hợp đồng: ").SemiBold(); t.Span(contract.ContractId); });
                        col.Item().Text(t => { t.Span("Căn hộ số: ").SemiBold(); t.Span(contract.ApartmentCode ?? "N/A"); });

                        col.Item().PaddingTop(10).Text("THÔNG TIN KHÁCH HÀNG (BÊN B)").SemiBold().FontSize(14);
                        col.Item().Text($"Họ và tên: {contract.OwnerName ?? "-"}");
                        col.Item().Text($"Số điện thoại: {contract.OwnerPhoneNumber ?? "-"}");
                        col.Item().Text($"Email: {contract.OwnerEmail ?? "-"}");

                        col.Item().PaddingTop(10).Text("CHI TIẾT HỢP ĐỒNG").SemiBold().FontSize(14);
                        col.Item().Text($"Ngày bắt đầu hiệu lực: {FormatDate(contract.StartDate)}");

                        // 2. XỬ LÝ NỘI DUNG RIÊNG BIỆT
                        if (contract.ContractType == "Sale")
                        {
                            // === FORM CHO HỢP ĐỒNG MUA BÁN ===
                            col.Item().PaddingTop(10).Text("Điều khoản mua bán:").SemiBold();
                            col.Item().Text("1. Bên B thanh toán đầy đủ theo tiến độ quy định.");
                            col.Item().Text("2. Bên A có trách nhiệm bàn giao căn hộ và hồ sơ pháp lý (Sổ hồng) đúng thời hạn.");
                            col.Item().Text("3. Căn hộ được bảo hành kỹ thuật 5 năm kể từ ngày bàn giao.");
                        }
                        else
                        {
                            // === FORM CHO HỢP ĐỒNG THUÊ ===
                            col.Item().Text($"Ngày kết thúc thuê: {FormatDate(contract.EndDate)}");
                            col.Item().PaddingTop(10).Text("Điều khoản thuê:").SemiBold();
                            col.Item().Text("1. Bên B cam kết sử dụng căn hộ đúng mục đích để ở, không kinh doanh trái phép.");
                            col.Item().Text("2. Tiền điện, nước, phí dịch vụ do Bên B chi trả hàng tháng.");
                            col.Item().Text("3. Tiền cọc sẽ được hoàn trả khi kết thúc hợp đồng nếu không có hư hại tài sản.");
                        }

                        // Phần ký tên (Chân trang)
                        col.Item().PaddingTop(50).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("ĐẠI DIỆN BQL (BÊN A)").SemiBold();
                                c.Item().PaddingTop(50).AlignCenter().Text("(Ký, đóng dấu, ghi rõ họ tên)");
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("KHÁCH HÀNG (BÊN B)").SemiBold();
                                c.Item().PaddingTop(50).AlignCenter().Text(contract.OwnerName ?? "________________");
                            });
                        });
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Trang ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static string FormatDate(DateOnly? date) => date?.ToString("dd/MM/yyyy") ?? "...";

        private static string FormatCurrency(decimal? amount)
            => amount.HasValue ? amount.Value.ToString("N0") : "0";
    }
}