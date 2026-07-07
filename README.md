[![License: MIT](https://img.shields.io/github/license/<tnshabazkhan>/<DocuFlow>)](https://github.com/<tnshabazkhan>/<DocuFlow>/blob/main/LICENSE)

# DocuFlow: AI-Powered Document Management System

**DocuFlow** is a modern, distributed document processing platform built on **.NET Clean Architecture**. It leverages Azure's AI capabilities to automatically extract, categorize, and analyze data from various document types—from mobile-captured photos to 1,000-page medical manuals.

<p align="center">
  <img src="https://github.com/user-attachments/assets/e7aaca0a-d055-4524-853f-ca854d4564e2" width="180" alt="Home Screen" style="margin: 5px;" />
  <img src="https://github.com/user-attachments/assets/c11ff690-4aa5-46f6-a017-a39f72c4b3f4" width="180" alt="Document Details" style="margin: 5px;" />
  <img src="https://github.com/user-attachments/assets/a27029c6-5654-45e1-b479-f00ebd4cd02b" width="180" alt="Analytics" style="margin: 5px;" />
  <img src="https://github.com/user-attachments/assets/e787371e-ed7b-4f93-8be8-3ec101d87b04" width="180" alt="Upload" style="margin: 5px;" />
</p>

## 🚀 Key Features
- **Clean Architecture:** Strictly decoupled layers (Domain, Application, Infrastructure, API, and Functions).
- **Professional Mobile UI:** Sleek, high-fidelity native app built with **Expo SDK 54** featuring modern typography, professional color palettes, and intuitive UX.
- **Fluid Animations:** High-quality startup animations and staggered entrance effects for a premium feel (using React Native's `Animated` API).
- **Dynamic AI Extraction:** Integrated with **Azure AI Document Intelligence**, supporting Invoices, Receipts, and Identity Documents.
- **Robust Smart Summaries:** Powered by a **Hybrid AI Intelligence** strategy using a parallel **Map-Reduce** pattern. Optimized for 1,000+ page docs with **exponential backoff** and **idempotent processing**.
- **Dual-Model Architecture:** Leverages **GPT-4o-mini** for cost-efficient bulk chunk analysis and **GPT-4o** for high-fidelity master report synthesis.
- **"Stay Free" Hybrid Strategy:** Uses local C# PDF parsing for digital docs to bypass Azure AI free-tier limits.
- **Professional Reports:** Automatically generates beautifully styled **PDF summary reports** using **QuestPDF**.
- **Side-car Storage:** Scalable hybrid storage strategy using **Cosmos DB** for metadata and **Blob Storage** for massive OCR text files.

## 🛠️ Tech Stack
- **Backend:** .NET 10 / ASP.NET Core Minimal APIs
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
