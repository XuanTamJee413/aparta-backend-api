namespace ApartaAPI.Utils.Helper
{
    public static class EmailTemplateHelper
    {
        public static string GetManagerWelcomeEmailTemplate(string managerName, string phone, string email, string password, string frontendUrl)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%); padding: 30px; border-radius: 10px 10px 0 0; text-align: center;'>
                            <h1 style='color: white; margin: 0; font-size: 24px;'>Chào mừng đến với Aparta System</h1>
                        </div>
                        <div style='background: #ffffff; padding: 30px; border: 1px solid #e5e7eb; border-top: none;'>
                            <p style='font-size: 16px; margin-bottom: 20px;'>Xin chào <strong>{managerName}</strong>,</p>
                            <p style='font-size: 16px; margin-bottom: 20px;'>Tài khoản Manager của bạn đã được tạo thành công trong hệ thống Aparta.</p>
                            
                            <div style='background: #f9fafb; border: 2px solid #e5e7eb; border-radius: 8px; padding: 20px; margin: 20px 0;'>
                                <h2 style='color: #1f2937; margin-top: 0; font-size: 18px;'>Thông tin đăng nhập:</h2>
                                <table style='width: 100%; border-collapse: collapse;'>
                                    <tr>
                                        <td style='padding: 10px 0; font-weight: 600; color: #4b5563; width: 120px;'>Số điện thoại:</td>
                                        <td style='padding: 10px 0; color: #1f2937; font-family: monospace; font-size: 16px;'><strong>{phone}</strong></td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 10px 0; font-weight: 600; color: #4b5563;'>Email:</td>
                                        <td style='padding: 10px 0; color: #1f2937;'><strong>{email}</strong></td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 10px 0; font-weight: 600; color: #4b5563;'>Mật khẩu:</td>
                                        <td style='padding: 10px 0; color: #1f2937; font-family: monospace; font-size: 16px; letter-spacing: 2px;'><strong>{password}</strong></td>
                                    </tr>
                                </table>
                            </div>

                            <div style='background: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                                <p style='margin: 0; color: #92400e; font-size: 14px;'>
                                    <strong>⚠️ Lưu ý quan trọng:</strong> Đây là lần đầu bạn đăng nhập. Bạn sẽ được yêu cầu đổi mật khẩu để bảo mật tài khoản.
                                </p>
                            </div>

                            <div style='margin: 30px 0; text-align: center;'>
                                <a href='{frontendUrl}/login' 
                                   style='background-color: #4f46e5; color: white; padding: 12px 30px; 
                                          text-decoration: none; border-radius: 6px; display: inline-block; 
                                          font-weight: 600; font-size: 16px;'>
                                    Đăng nhập ngay
                                </a>
                            </div>

                            <p style='font-size: 14px; color: #6b7280; margin-top: 30px;'>
                                Nếu bạn không tạo tài khoản này, vui lòng liên hệ với quản trị viên ngay lập tức.
                            </p>
                        </div>
                        <div style='background: #f9fafb; padding: 20px; border: 1px solid #e5e7eb; border-top: none; 
                                    border-radius: 0 0 10px 10px; text-align: center;'>
                            <p style='color: #6b7280; font-size: 12px; margin: 0;'>
                                Email này được gửi tự động, vui lòng không trả lời.<br>
                                © 2025 Aparta Co., Ltd. All rights reserved.
                            </p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        public static string GetInvoiceNotificationEmailTemplate(
            string residentName,
            string apartmentCode,
            string billingPeriod,
            string invoiceId,
            decimal totalAmount,
            DateOnly issueDate,
            DateOnly dueDate)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%); padding: 30px; border-radius: 10px 10px 0 0; text-align: center;'>
                            <h1 style='color: white; margin: 0; font-size: 24px;'>Thông báo hóa đơn mới</h1>
                        </div>
                        <div style='background: #ffffff; padding: 30px; border: 1px solid #e5e7eb; border-top: none;'>
                            <p style='font-size: 16px; margin-bottom: 20px;'>Xin chào <strong>{residentName}</strong>,</p>
                            <p style='font-size: 16px; margin-bottom: 20px;'>Hệ thống đã tạo hóa đơn mới cho căn hộ <strong>{apartmentCode}</strong> của bạn.</p>
                            
                            <div style='background: #f9fafb; border: 2px solid #e5e7eb; border-radius: 8px; padding: 20px; margin: 20px 0;'>
                                <h2 style='color: #1f2937; margin-top: 0; font-size: 18px;'>Thông tin hóa đơn:</h2>
                                <table style='width: 100%; border-collapse: collapse;'>
                                    <tr>
                                        <td style='padding: 10px 0; font-weight: 600; color: #4b5563; width: 150px;'>Kỳ thanh toán:</td>
                                        <td style='padding: 10px 0; color: #1f2937;'><strong>{billingPeriod}</strong></td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 10px 0; font-weight: 600; color: #4b5563;'>Mã hóa đơn:</td>
                                        <td style='padding: 10px 0; color: #1f2937; font-family: monospace;'><strong>{invoiceId}</strong></td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 10px 0; font-weight: 600; color: #4b5563;'>Tổng tiền:</td>
                                        <td style='padding: 10px 0; color: #dc2626; font-size: 18px; font-weight: 700;'><strong>{totalAmount:N0} VNĐ</strong></td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 10px 0; font-weight: 600; color: #4b5563;'>Ngày phát hành:</td>
                                        <td style='padding: 10px 0; color: #1f2937;'><strong>{issueDate:dd/MM/yyyy}</strong></td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 10px 0; font-weight: 600; color: #4b5563;'>Hạn thanh toán:</td>
                                        <td style='padding: 10px 0; color: #dc2626; font-weight: 600;'><strong>{dueDate:dd/MM/yyyy}</strong></td>
                                    </tr>
                                </table>
                            </div>

                            <div style='background: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                                <p style='margin: 0; color: #92400e; font-size: 14px;'>
                                    <strong>⚠️ Lưu ý:</strong> Vui lòng thanh toán trước ngày hạn để tránh phí phạt.
                                </p>
                            </div>

                            <div style='margin: 30px 0; text-align: center;'>
                                <p style='font-size: 14px; color: #6b7280;'>
                                    Vui lòng đăng nhập vào hệ thống để xem chi tiết hóa đơn và thanh toán.
                                </p>
                            </div>
                        </div>
                        <div style='background: #f9fafb; padding: 20px; border: 1px solid #e5e7eb; border-top: none; 
                                    border-radius: 0 0 10px 10px; text-align: center;'>
                            <p style='color: #6b7280; font-size: 12px; margin: 0;'>
                                Email này được gửi tự động, vui lòng không trả lời.<br>
                                © 2025 Aparta Co., Ltd. All rights reserved.
                            </p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        public static string GetStaffWelcomeEmailTemplate(string staffName, string phone, string email, string password, string frontendUrl)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background: linear-gradient(135deg, #10b981 0%, #34d399 100%); padding: 30px; border-radius: 10px 10px 0 0; text-align: center;'>
                            <h1 style='color: white; margin: 0; font-size: 24px;'>Chào mừng đến với Aparta System</h1>
                        </div>
                        <div style='background: #ffffff; padding: 30px; border: 1px solid #e5e7eb; border-top: none;'>
                            <p style='font-size: 16px; margin-bottom: 20px;'>Xin chào <strong>{staffName}</strong>,</p>
                            <p style='font-size: 16px; margin-bottom: 20px;'>Tài khoản Nhân viên của bạn đã được tạo thành công.</p>
                            
                            <div style='background: #f9fafb; border: 2px solid #e5e7eb; border-radius: 8px; padding: 20px; margin: 20px 0;'>
                                <h2 style='color: #1f2937; margin-top: 0; font-size: 18px;'>Thông tin đăng nhập:</h2>
                                <table style='width: 100%; border-collapse: collapse;'>
                                    <tr>
                                        <td style='padding: 10px 0; font-weight: 600; color: #4b5563; width: 120px;'>Số điện thoại:</td>
                                        <td style='padding: 10px 0; color: #1f2937; font-family: monospace; font-size: 16px;'><strong>{phone}</strong></td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 10px 0; font-weight: 600; color: #4b5563;'>Email:</td>
                                        <td style='padding: 10px 0; color: #1f2937;'><strong>{email}</strong></td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 10px 0; font-weight: 600; color: #4b5563;'>Mật khẩu:</td>
                                        <td style='padding: 10px 0; color: #1f2937; font-family: monospace; font-size: 16px; letter-spacing: 2px;'><strong>{password}</strong></td>
                                    </tr>
                                </table>
                            </div>

                            <div style='background: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                                <p style='margin: 0; color: #92400e; font-size: 14px;'>
                                    <strong>⚠️ Lưu ý:</strong> Đây là mật khẩu mặc định. Hệ thống sẽ yêu cầu bạn đổi mật khẩu ngay trong lần đăng nhập đầu tiên.
                                </p>
                            </div>

                <div style='margin: 30px 0; text-align: center;'>
                    <a href='{frontendUrl}/login' 
                       style='background-color: #10b981; color: white; padding: 12px 30px; 
                              text-decoration: none; border-radius: 6px; display: inline-block; 
                              font-weight: 600; font-size: 16px;'>
                        Đăng nhập ngay
                    </a>
                </div>
            </div>
            <div style='background: #f9fafb; padding: 20px; border: 1px solid #e5e7eb; border-top: none; 
                        border-radius: 0 0 10px 10px; text-align: center;'>
                <p style='color: #6b7280; font-size: 12px; margin: 0;'>
                    Email này được gửi tự động từ hệ thống Aparta.
                </p>
            </div>
        </div>
    </body>
    </html>";
        }

		public static string GetStaffPasswordResetEmailTemplate(string staffName, string newPassword, string frontendUrl)
		{
			return $@"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
        <h2 style='color: #d9534f; text-align: center;'>Thông báo Đặt lại Mật khẩu</h2>
        
        <p>Xin chào <strong>{staffName}</strong>,</p>
        
        <p>Quản trị viên hệ thống <strong>Aparta</strong> vừa thực hiện đặt lại mật khẩu cho tài khoản của bạn.</p>
        
        <div style='background-color: #f9f9f9; padding: 15px; margin: 20px 0; border-left: 4px solid #d9534f;'>
            <p style='margin: 0;'><strong>Mật khẩu mới của bạn:</strong></p>
            <p style='margin: 10px 0; font-size: 24px; font-weight: bold; letter-spacing: 2px; color: #333;'>{newPassword}</p>
        </div>

        <p>Vui lòng đăng nhập và đổi mật khẩu ngay lập tức để đảm bảo an toàn.</p>

        <div style='text-align: center; margin-top: 30px;'>
            <a href='{frontendUrl}/login' 
                style='background-color: #d9534f; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: bold;'>
                Đăng nhập ngay
            </a>
        </div>

        <p style='margin-top: 30px; font-size: 12px; color: #777; text-align: center;'>
            Đây là email tự động, vui lòng không trả lời.<br>
            © 2025 Aparta System
        </p>
    </div>
</body>
</html>";
		}
	}
}


