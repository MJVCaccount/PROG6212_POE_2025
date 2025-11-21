# PROG6212 POE Part 3: Contract Monthly Claim System (CMCS) - Final Submission

This repository contains the final full-stack implementation of the Contract Monthly Claim System (CMCS) for the Programming 2B (PROG6212) Portfolio of Evidence (POE). This submission builds upon the Part 2 prototype, transforming it into a secure, database-driven, and automated enterprise web application.

- **YouTube Presentation:** [https://youtu.be/CAByRNV5rkY](https://youtu.be/CAByRNV5rkY)
- **GitHub Repository:** [https://github.com/MJVCaccount/PROG6212_POE_2025](https://github.com/MJVCaccount/PROG6212_POE_2025)

---

## 📝 Updates for Part 3 (Response to Feedback)

Based on the feedback received from Part 2, the following enhancements have been implemented to achieve a robust and professional standard:

1.  **Version Control & Commit Hygiene:**
    - **Feedback:** "Commits were infrequent or lacked clarity."
    - **Update:** Adopted an atomic commit strategy. The Part 3 development lifecycle includes frequent, granular commits with descriptive prefixes (e.g., `Feat:`, `Fix:`, `Docs:`) to accurately track the evolution of features.

2.  **System Robustness & Validation:**
    - **Feedback:** "Ensure functionality meets automation standards and error handling is robust."
    - **Update:** Critical business logic was moved from client-side JavaScript to a dedicated server-side `ClaimAutomationService`. This ensures that calculations (Hours * Rate) and validations (Policy violations) are tamper-proof and secure.

3.  **Security & Access Control:**
    - **Update:** Implemented strict **Role-Based Access Control (RBAC)** using Sessions. Public registration has been removed; users can now only be onboarded by the HR Admin to ensure system integrity.

---

## 🏗 System Architecture

The application is built on the **ASP.NET Core MVC (Net 8.0)** framework, adhering to the separation of concerns principle.

### Data Persistence (New for Part 3)
- **Technology:** Microsoft SQL Server (LocalDB).
- **ORM:** Entity Framework Core (Code-First Approach).
- **Schema Strategy:** The database schema (`Lecturers`, `Claims`, `SupportingDocuments`) was generated dynamically from C# models. This replaces the in-memory lists used in Part 2, ensuring persistent and reliable data storage.
- **Seeding:** The application includes an automated seeder (`Program.cs`) that populates the database with default Admin, HR, and Lecturer accounts upon the first run.

### Design & UI
- **Style:** A modern **"Glassmorphism"** aesthetic using custom CSS variables and Bootstrap 5.
- **Features:** Semi-transparent cards, smooth `FadeInUp` animations, and a responsive mobile-first layout to minimize user fatigue.

---

## 🚀 Key Features

### 1. HR "Super User" & Administration (New)
- **Centralized Onboarding:** Public registration is disabled. HR Admins add new Lecturers, Coordinators, and Managers via a secured dashboard.
- **Rate Management:** HR sets the **Hourly Rate** during onboarding. This value is locked and cannot be edited by the lecturer, preventing fraud.
- **Reporting:** HR can generate payment reports using **LINQ** aggregations to view total hours and payouts per lecturer.

### 2. Lecturer Automation
- **Auto-Calculation:** The "Total Amount" is calculated automatically server-side (`Hours * Rate`). The Hourly Rate is pulled from the database and displayed as read-only.
- **Server-Side Validation:** Claims exceeding **180 hours** are automatically rejected by the `ClaimAutomationService` with clear error messaging.
- **Document Security:** Supporting documents are encrypted before storage and decrypted only upon authorized download.

### 3. Approval Workflows
- **Coordinator View:** Verifies pending claims.
- **Manager View:** Performs final financial approval (Approve/Reject).
- **Tracking:** Lecturers can track the status of their claims in real-time (Pending -> Verified -> Approved).

---

## 🛠 Setup & Login Instructions

### Prerequisites
- Visual Studio 2022
- .NET 8.0 SDK
- SQL Server LocalDB

### Database Setup
No manual SQL scripts are required. The application uses **EF Core Code-First Migrations**.
1. Open the solution in Visual Studio.
2. Run the application (F5).
3. The application will automatically create the database (`ClaimsDB`) and seed the test users if they do not exist.

### Default Login Credentials
Use these credentials to test the different roles:

| Role | User ID | Password |
| :--- | :--- | :--- |
| **HR Admin** | `HR2025001` | `Admin@123` |
| **Lecturer** | `IIE2024001` | `Lecturer@123` |
| **Coordinator** | `COORD2025001` | `Coord@123` |
| **Manager** | `MGR2025001` | `Manager@123` |

---

## 📚 References
- Japikse, P. & Troelsen, A., 2022. *Pro C# 10 with .NET 6: Foundational Principles and Practices in Programming*. New York: Apress.
- Microsoft, 2024. *ASP.NET Core Documentation*. [Online] Available at: https://learn.microsoft.com/en-us/aspnet/core/
- Troelsen, A. & Japikse, P., 2022. *Pro C# 10 with .NET 6: Foundational Principles and Practices in Programming*. 11 ed. New York: Apress.
