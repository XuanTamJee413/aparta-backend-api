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
        /* --- File: ApartaAPI/Utils/Helper/EmailTemplateHelper.cs --- */
        // Thêm phương thức này vào class EmailTemplateHelper

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
    }
}


