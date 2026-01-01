# ApartaAPI

A comprehensive .NET 8.0 Web API for Apartment Management System that provides backend services for managing apartments, users, services, billing, and more.

## 🚀 Features

- **User Management**: Authentication, authorization, and user profiles
- **Apartment Management**: CRUD operations for apartments and buildings
- **Billing & Invoicing**: Monthly billing, payment processing
- **Service Management**: Service booking and management
- **Asset Tracking**: Management of apartment assets
- **Document Generation**: PDF generation for invoices and reports
- **Real-time Chat**: Communication between users
- **Task Management**: Assignment and tracking of maintenance tasks
- **Visitor Management**: Logging and tracking of visitors
- **Subscription Services**: Subscription management with expiration handling
- **Payment Integration**: PayOS payment gateway integration
- **Cloud Storage**: Cloudinary integration for media storage

## 🛠️ Tech Stack

- **Framework**: .NET 8.0
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: JWT Bearer Authentication
- **File Storage**: Cloudinary
- **Payment Processing**: PayOS
- **Document Generation**: QuestPDF
- **API Documentation**: Swagger/OpenAPI
- **Containerization**: Docker support included
- **Background Jobs**: For scheduled tasks and notifications

## 📦 Dependencies

- AutoMapper for object mapping
- Entity Framework Core for database operations
- BCrypt for password hashing
- MailKit for email services
- Swashbuckle for API documentation
- Cloudinary for cloud storage
- PayOS for payment processing
- QuestPDF for PDF generation

## 🔧 Setup & Installation

1. **Prerequisites**
   - .NET 8.0 SDK
   - SQL Server (or Docker for containerized SQL Server)
   - Node.js (for frontend if applicable)

2. **Configuration**
   - Clone the repository
   - Set up your database connection string in `appsettings.json`
   - Configure other services (Cloudinary, PayOS) in `appsettings.json`
   - Run database migrations:
     ```
     dotnet ef database update
     ```

3. **Running the Application**
   ```
   dotnet run
   ```
   - The API will be available at `https://localhost:5001` or `http://localhost:5000`
   - Access Swagger documentation at `/swagger`

## 📚 API Documentation

API documentation is available via Swagger UI at `/swagger` when running the application.

## 🌐 Environment Variables

Required environment variables:
- `ConnectionStrings:DefaultConnection`: Database connection string
- `Jwt:Key`: JWT secret key
- `Jwt:Issuer`: JWT issuer
- `Cloudinary:CloudName`: Cloudinary cloud name
- `Cloudinary:ApiKey`: Cloudinary API key
- `Cloudinary:ApiSecret`: Cloudinary API secret
- `PayOS:ClientId`: PayOS client ID
- `PayOS:ApiKey`: PayOS API key
- `PayOS:ChecksumKey`: PayOS checksum key

## 🧪 Testing

Run tests using:
```
dotnet test
```

## 🐳 Docker Support

Build and run using Docker:
```bash
docker build -t aparta-api .
docker run -p 8080:80 -e ASPNETCORE_ENVIRONMENT=Development aparta-api
```

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📧 Contact

For any inquiries, please contact the development team.

---

Built with ❤️ by sonpxhe171548@fpt.edu.vn
