# DocuFlow AI - Enterprise Document Processing Platform

DocuFlow AI is a production-grade, AI-powered document processing platform built with **.NET 8** and **Azure**. It demonstrates an event-driven architecture designed for scalability, security, and automation.

## 🚀 Key Features
- **Secure Direct Uploads:** Uses Azure Blob Storage Shared Access Signatures (SAS) to offload file uploads from the API.
- **Event-Driven Processing:** Decouples ingestion from analysis using **Azure Service Bus**.
- **AI Document Extraction:** Leverages **Azure AI Document Intelligence** (formerly Form Recognizer) to extract structured data from Invoices, Receipts, and more.
- **Serverless Background Workers:** Uses **Azure Functions (Isolated Worker)** for scalable, cost-effective processing.
- **Clean Architecture & CQRS:** Implements **MediatR** for a decoupled, maintainable codebase.

## 🏗️ Architecture
1. **API:** Receives upload requests, creates metadata in the DB, generates a SAS URI, and sends a "Processing" event to Service Bus.
2. **Blob Storage:** Receives the file directly from the client.
3. **Functions:** Triggered by Service Bus, downloads the file, calls Azure AI, and updates the database with extracted JSON metadata.

## 🛠️ Tech Stack
- **Backend:** .NET 8, Minimal APIs, MediatR, Azure SDKs.
- **Cloud:** Azure Blob Storage, Azure Service Bus, Azure Functions, Azure AI Document Intelligence.
- **Local Dev:** Azurite (Storage emulator).

## 🚦 Getting Started
1. Run `docker compose up -d` to start local emulators.
2. Open `DocuFlow.sln` in Visual Studio or VS Code.
3. Run the `DocuFlow.Api` project.
4. Use Swagger at `http://localhost:<port>/swagger` to test the `/api/documents` endpoint.

---
*Created for portfolio impact and senior backend/cloud role benchmarking.*
