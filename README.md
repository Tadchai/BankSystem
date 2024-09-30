# Bank Application

## Description
This project is a simple banking system built using ASP.NET Core and Entity Framework Core. It allows users to manage their bank accounts with functionalities such as viewing their balance, depositing money, withdrawing money, and transferring money to other users. It also features transaction history tracking and user authentication via JWT tokens.

## Features
- User Authentication: Users can login to the system and receive a JWT token for authentication.
- View Balance: Users can view their current balance.
- Deposit & Withdraw: Users can deposit and withdraw money from their accounts.
- Transfer: Users can transfer money to other users.
- Transaction History: Users can view their transaction history (both deposits, withdrawals, and transfers).
- Secure: All routes are protected using JWT authentication.

## Technologies Used
- Frontend: HTML, JavaScript, jQuery, Bootstrap 
- Backend: ASP.NET Core, Entity Framework Core
- Database: MySQL
- Authentication: JWT (Json Web Token)

## Getting Started
#### 1. Clone the repository

```bash
    git clone https://github.com/YourUsername/BankApp.git
    cd BankApp
```

#### 2. Setup environment variables
Create a .env file in the root directory with the following content:
```bash
    DB_SERVER=
    DB_NAME=
    DB_USER=
    DB_PASSWORD=
    JWT_KEY=
```

#### 3. Install dependencies
```bash
    dotnet restore
```

#### 4. Run Migrations
Run the migrations to create the necessary tables in the database.
```bash
    dotnet ef database update
```

#### 5. Run the application
Start the ASP.NET Core application.
```bash
    dotnet run
```
## API Endpoints

#### Authentication
- POST /api/auth/register - Register a new user (Password must be at least 8 characters long.)
- POST /api/auth/login - Login and receive JWT token

#### Account Management
- GET /api/account/balance - Get current balance (requires JWT token)
- POST /api/account/deposit - Deposit money (requires JWT token)
- POST /api/account/withdraw - Withdraw money (requires JWT token)
- POST /api/account/transfer - Transfer money to another user (requires JWT token)
- GET /api/account/history - View transaction history (requires JWT token)
- GET /api/account/profile - View user profile (requires JWT token)