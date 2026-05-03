# 📚 MVC Web App Project - Library Management System

## 🔎 Project Overview
This project is a **Library Management System** developed using **ASP.NET Core MVC**. It allows users to manage books, handle borrowing (loans), and maintain library records through a user-friendly web interface.  

The project demonstrates practical implementation of **MVC architecture**, **authentication**, and **database management** in a real-world scenario.

---

## 🚀 Features
- User registration and login (ASP.NET Identity)
- Librarian/admin access
- Add, edit, delete, and view books
- Manage book loans (issue and return)
- Track borrowing records
- Secure authentication system
- SQLite database integration
- Clean MVC architecture

---

## 🛠️ Technologies Used
- ASP.NET Core MVC  
- C#  
- Entity Framework Core  
- SQLite  
- HTML, CSS, Bootstrap  
- Razor Pages  
- ASP.NET Identity  

---

## 🧠 MVC Architecture
- **Model:** Handles data (Book, Loan, User)
- **View:** Displays UI using Razor (.cshtml)
- **Controller:** Processes user requests and connects Model & View

---

## 🗄️ Database
The project uses **SQLite** with Entity Framework Core.  
Migrations are used to create and update database tables.

---

## ▶️ How to Run the Project

```bash
dotnet restore
dotnet ef database update
dotnet run
