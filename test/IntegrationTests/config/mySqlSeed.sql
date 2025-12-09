-- Create database
CREATE DATABASE IF NOT EXISTS cqrs_command;
USE cqrs_command;

-- Create products table
CREATE TABLE IF NOT EXISTS products (
    product_id CHAR(36) PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    sku VARCHAR(100) NOT NULL,
    price DECIMAL(10,2) NOT NULL,
    stock_level INT NOT NULL,
    is_active BOOLEAN NOT NULL
);

-- Create last_info table
CREATE TABLE IF NOT EXISTS last_info (
    id INT AUTO_INCREMENT PRIMARY KEY,
    last_event_id CHAR(36) NOT NULL
);

-- Insert initial last_event_id with a new GUID
INSERT INTO last_info (last_event_id)
VALUES (UUID());
