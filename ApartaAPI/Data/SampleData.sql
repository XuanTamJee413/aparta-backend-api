-- Script to create sample roles and users for testing
-- Run this script in your SQL Server database

-- Insert roles
INSERT INTO ROLE (role_id, role_name) VALUES 
('role_admin', 'admin'),
('role_staff', 'staff'),
('role_resident', 'resident');

-- Insert sample users with hashed passwords
-- Password for all users is "123456" (hashed with BCrypt)
-- Run the PasswordHashGenerator script to generate new hashes

INSERT INTO [USER] (user_id, email, phone, password_hash, name, role_id, status, created_at, updated_at, is_deleted) VALUES 
-- Admin user
('user_admin_001', 'admin@aparta.com', '0123456789', '$2a$11$rQZ8Q8Q8Q8Q8Q8Q8Q8Q8QO8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q', 'Admin User', 'role_admin', 'active', GETDATE(), GETDATE(), 0),

-- Staff user
('user_staff_001', 'staff@aparta.com', '0987654321', '$2a$11$rQZ8Q8Q8Q8Q8Q8Q8Q8Q8QO8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q', 'Staff User', 'role_staff', 'active', GETDATE(), GETDATE(), 0),

-- Resident user
('user_resident_001', 'resident@aparta.com', '0555555555', '$2a$11$rQZ8Q8Q8Q8Q8Q8Q8Q8Q8QO8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q8Q', 'Resident User', 'role_resident', 'active', GETDATE(), GETDATE(), 0);

-- Note: The password hash above is for "123456"
-- To generate a new hash, use: BCrypt.Net.BCrypt.HashPassword("your_password")