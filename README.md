# DocuFlow: AI-Powered Document Management System

**DocuFlow** is a modern, distributed document processing platform built on **.NET Clean Architecture**. It leverages Azure's AI capabilities to automatically extract, categorize, and analyze data from various document types—from mobile-captured photos to 1,000-page medical manuals.

## 🚀 Key Features
- **Clean Architecture:** Strictly decoupled layers (Domain, Application, Infrastructure, API, and Functions).
- **Mobile First:** Native iOS/Android app built with **Expo SDK 54** featuring direct camera-to-cloud scanning.
- **Dynamic AI Extraction:** Integrated with **Azure AI Document Intelligence**, supporting Invoices, Receipts, and Identity Documents.
- **Smart Summaries:** Powered by **Azure OpenAI (GPT-4o/5-mini)** using a parallel **Map-Reduce** pattern to summarize massive 1,000+ page documents with high precision.
- **"Stay Free" Hybrid Strategy:** Uses local C# PDF parsing for digital docs to bypass Azure AI free-tier limits.
- **Professional Reports:** Automatically generates beautifully styled **PDF summary reports** using **QuestPDF**.
- **Side-car Storage:** Scalable hybrid storage strategy using **Cosmos DB** for metadata and **Blob Storage** for massive OCR text files.

## 🛠️ Tech Stack
- **Backend:** .NET 8 / ASP.NET Core Minimal APIs
- **Mobile:** React Native / Expo SDK 54 / Expo Router / React Query
- **Background Worker:** Azure Functions (Isolated Worker Model)
- **Messaging:** Azure Service Bus (Async Job Queue)
- **Database:** Azure Cosmos DB (NoSQL)
- **Storage:** Azure Blob Storage
- **AI:** Azure AI Document Intelligence & Azure OpenAI
- **PDF Gen:** QuestPDF & PdfPig

## 📂 Project Structure
- `DocuFlow.Api`: Entry point for client interactions and file initiation.
- `DocuFlow.Mobile`: The Expo mobile application.
- `DocuFlow.Functions`: Background worker that handles the AI and PDF generation heavy lifting.
- `DocuFlow.Application`: Core business logic, Commands, and Queries.
- `DocuFlow.Domain`: Shared entities, Enums, and core logic.
- `DocuFlow.Infrastructure`: Implementation of external services (Storage, DB, Messaging).

---

## 📜 Documentation
For a deep dive into the technical choices made during development, see [ADR.md](./ADR.md).

*Developed as an intelligent, scalable foundation for document automation.*
