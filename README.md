# Ajans Yönetim Sistemi (Agency Management System)

A comprehensive desktop application designed from scratch for creative and digital agencies to manage their end-to-end business processes. 

![WPF](https://img.shields.io/badge/WPF-512BD4?style=for-the-badge&logo=windows&logoColor=white)
![.NET 8](https://img.shields.io/badge/.NET_8-512BD4?style=for-the-badge&logo=.net&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-07405E?style=for-the-badge&logo=sqlite&logoColor=white)
![Firebase](https://img.shields.io/badge/Firebase-039BE5?style=for-the-badge&logo=Firebase&logoColor=white)

## 📖 About the Project

This is a **Personal Project** built as a Windows Desktop Application. It aims to solve the operational tracking problems of digital agencies by providing a centralized system for clients, projects, employees, and finance management.

I focused heavily on performance optimization (implementing debounce techniques for search) and debugging potential system bottlenecks. Additionally, the application integrates **Firebase** for secure cloud backups and authentication, while utilizing **SQLite** for efficient local data storage.

## ✨ Key Features

* **Dashboard:** Interactive KPIs and charts (LiveCharts) showing project distributions and revenue trends.
* **Client Management:** Full CRM capabilities including domestic/international tagging, note-taking, and CSV import/export.
* **Project Tracking:** Status workflows with Kanban board integration. Multi-currency pricing (TL, USD, EUR) with live exchange rates (Frankfurter API → TCMB).
* **Employee Management:** Performance metrics and department-based task assignments.
* **Invoicing & Reporting:** Professional PDF invoice/quote generation (QuestPDF) and Excel data exports (ClosedXML).
* **Cloud Sync & Security:** Firebase Realtime DB integration for backups with GZip compression, OTP email verification for authentication, and session timeout locks.

## 🛠️ Tech Stack

* **Language:** C#
* **Framework:** .NET 8, WPF
* **Database & Cloud:** SQLite (Local Data) & Firebase (Cloud Backup, Authentication)
* **Analytics/Charts:** LiveCharts
* **PDF & Excel:** QuestPDF, ClosedXML

## 👨‍💻 Author

**Hakan Kırık**
* Computer Programmer & Software Developer
* [LinkedIn](https://linkedin.com/in/hakankirik)
* [GitHub](https://github.com/hakankk0)
