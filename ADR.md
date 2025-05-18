# DocuFlow: Technical Decisions & ADR (Architecture Decision Records)

This document tracks the key technical and architectural decisions made during the development of DocuFlow.

---

## 1. Multi-Tenant Distributed Architecture
**Decision**: Adopt .NET Clean Architecture with separate API and Background Worker projects.
**Reasoning**: Decoupling the API (Entry point) from the processing logic (Functions) allows each to scale independently. Using Service Bus as the glue ensures that document processing is asynchronous and highly reliable (retries on failure).

## 2. Shared Persistence (Cosmos DB)
**Decision**: Replaced the initial `InMemoryRepository` with **Azure Cosmos DB (NoSQL)**.
**Reasoning**: To enable the API and the separate Azure Function process to share data. NoSQL is preferred because AI-extracted document metadata is semi-structured and varies by document type.

## 3. Side-car Blob Storage Pattern
**Decision**: Store full OCR text as separate `.txt` files in Blob Storage, while keeping only a preview and a reference link in Cosmos DB.
**Reasoning**: **Cosmos DB has a 2MB limit per document.** A 400-page manual can exceed this. Storing large text in Blob storage is more scalable, cheaper, and prevents the UI from choking on massive JSON responses.

## 4. "Stay Free" PDF Extraction Strategy
**Decision**: Priority use of **PdfPig (C# library)** for digital PDFs, with automatic fallback to **Azure AI Document Intelligence**.
**Reasoning**: Azure AI's free tier has a 2-page limit. By extracting text from digital PDFs locally (for $0), we can process 1,000+ page documents for free and save Azure credits for the "Smart" summarization part.

## 5. Parallel Map-Reduce Summarization
**Decision**: Use a **Parallel Map-Reduce** pattern with **Semaphore Throttling** for OpenAI calls.
**Reasoning**: 
- **Map**: Splitting a 400-page doc into chunks ensures no context loss in the "middle."
- **Parallel**: Firing requests simultaneously reduced processing time for a 400-page doc from 5 minutes to ~40 seconds.
- **Throttling**: A semaphore of 5 prevents "Too Many Requests" (HTTP 429) errors by smoothing out the burst traffic.

## 6. Hybrid JSON Serialization
**Decision**: Switched DocuFlow.Api to use **Newtonsoft.Json** support.
**Reasoning**: The Azure Cosmos SDK natively uses Newtonsoft. There was a conflict with `System.Text.Json` (returning `ValueKind: 1`). Aligning the API to use Newtonsoft ensured complex AI data structures are returned correctly to the UI.

## 7. Mobile First (React Native / Expo SDK 54)
**Decision**: Standardized on **Expo SDK 54** using **Expo Router**.
**Reasoning**: React Native allows for a "Scanner" experience using the phone camera. SDK 54 was chosen to match the most stable version of the Expo Go app available in the App Store, ensuring perfect compatibility during development.

## 8. Secure Data Delimiting (Prompts)
**Decision**: Implemented `--- DOCUMENT CONTENT START ---` delimiters in all AI prompts.
**Reasoning**: To prevent **Instruction Injection Attacks**. This ensures the AI treats the uploaded document strictly as *data to analyze*, not as a *command to follow*.

## 9. Stable Mobile Animation Strategy
**Decision**: Standardized on React Native's built-in **`Animated` API** for high-fidelity UI transitions.
**Reasoning**: While libraries like `moti` and `reanimated` offer powerful features, they introduced stability issues (HostFunction errors) in the Expo environment. The built-in API provides zero-dependency stability while still delivering a professional, 60fps experience for splash screens and staggered list entrances.

## 10. Long-Running Message Reliability
**Decision**: Increased Service Bus **`maxAutoLockRenewalDuration` to 15 minutes**, added a global **`functionTimeout` of 15 minutes**, and implemented **Document Idempotency** checks.
**Reasoning**: Complex AI summarization of large books takes roughly 3-10 minutes. Setting a 15-minute window provides a safe buffer while ensuring that if a process truly hangs, the message is released and retried promptly. Combined with status checks (`if (document.Status == Processed) return;`), this ensures 100% reliability for massive documents.

## 11. Robust AI Map-Reduce (Enterprise Stability)
**Decision**: Standardized on **5 parallel chunks** and **6 retries** with exponential backoff and jitter.
**Reasoning**: To maximize reliability on a **250k TPM** Global Standard quota, using 5 parallel chunks (approx. 24% of quota per batch) provides maximum stability. The deep 6-retry safety net with jitter ensures that even sustained global traffic spikes do not fail the overall document processing job.

## 12. Hybrid GPT Model Architecture
**Decision**: Adopt a dual-model strategy using **`gpt-4o-mini`** for the "Map" phase and **`gpt-4o`** for the "Reduce" phase.
**Reasoning**: 
- **Cost Efficiency**: `gpt-4o-mini` is ~95% cheaper per token. Since the Map phase (reading individual document chunks) represents the bulk of token consumption, this significantly slashes operational costs.
- **Output Quality**: Flagship models like `gpt-4o` excel at high-level synthesis and reasoning. Using it for the final master report ensures the highest possible quality for the user-facing insights.
- **Throughput**: `gpt-4o-mini` typically offers much higher TPM (Tokens Per Minute) quotas, enabling faster parallel processing of document chunks.
