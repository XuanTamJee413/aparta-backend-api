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
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("HỢP ĐỒNG THUÊ CĂN HỘ")
                        .SemiBold().FontSize(18).AlignCenter();

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(8);

                        col.Item().Text($"Mã hợp đồng: {contract.ContractId}");
                        col.Item().Text($"Căn hộ: {contract.ApartmentCode ?? "-"}");
                        col.Item().Text($"Chủ hộ: {contract.OwnerName ?? "-"}");
                        col.Item().Text($"SĐT: {contract.OwnerPhoneNumber ?? "-"}");
                        col.Item().Text($"Email: {contract.OwnerEmail ?? "-"}");

                        col.Item().Text($"Ngày bắt đầu: {FormatDate(contract.StartDate)}");
                        col.Item().Text($"Ngày kết thúc: {FormatDate(contract.EndDate)}");

                        if (!string.IsNullOrWhiteSpace(contract.Image))
                        {
                            col.Item().Text($"Tệp đính kèm: {contract.Image}");
                        }

                        col.Item().PaddingTop(20).Text("Điều khoản cơ bản:")
                            .SemiBold();

                        col.Item().Text(text =>
                        {
                            text.Span("1. Bên thuê cam kết sử dụng căn hộ đúng mục đích, chấp hành nội quy tòa nhà.").LineHeight(1.2f);
                            text.Line("");
                            text.Span("2. Bên cho thuê đảm bảo căn hộ đủ điều kiện sử dụng, hỗ trợ bên thuê trong quá trình cư trú.").LineHeight(1.2f);
                            text.Line("");
                            text.Span("3. Các điều khoản chi tiết khác được quy định trong hệ thống quản lý Aparta.").LineHeight(1.2f);
                        });

                        col.Item().PaddingTop(40).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("BÊN CHO THUÊ").SemiBold();
                                c.Item().PaddingTop(40).Text("Ký, ghi rõ họ tên");
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("BÊN THUÊ").SemiBold();
                                c.Item().PaddingTop(40).Text(contract.OwnerName ?? "________________");
                            });
                        });
                    });

                    page.Footer()
                        .AlignRight()
                        .Text(text =>
                        {
                            text.Span("Ngày in: ").Italic();
                            text.Span(DateTime.Now.ToString("dd/MM/yyyy"));
                        });
                });
            });

            return document.GeneratePdf();
        }

        private static string FormatDate(DateOnly? date)
            => date?.ToString("dd/MM/yyyy") ?? "";
    }
}
