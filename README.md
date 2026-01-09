# CPayment - Open Source

**CPayment** is a lightweight, secure, and extensible library designed to simplify **crypto-based payments** in modern applications.

It provides a clean abstraction layer between traditional (Web2) systems and blockchain payment networks, enabling developers to integrate cryptocurrency payments without coupling their business logic to low-level blockchain concerns.

## Goals

CPayment is built with the following principles in mind:

-   **Simplicity first**  
    A minimal API surface that is easy to understand and adopt.
    
-   **Explicit and predictable behavior**  
    No hidden magic, no implicit global state, and no silent failures — especially critical for payment flows.
    
-   **Security by design**  
    Clear responsibility boundaries, strict configuration validation, and explicit error handling.
    
-   **Extensibility**  
    Support for multiple networks and providers through well-defined interfaces.
    
-   **Backend-friendly**  
    Designed primarily for server-side usage, but adaptable to different architectures.

## What CPayment Is

-   A **payment abstraction layer**, not a wallet
    
-   A **library**, not a hosted service
    
-   A **foundation** for building crypto payment workflows
    
-   Provider-agnostic and network-agnostic by design


## Supported Networks

CPayment is designed to support multiple networks. Initial development focuses on:

-   Bitcoin (Mainnet & Testnet)

## Status
CPayment is currently **under active development**.
The public API and internal structure are being carefully designed before usage examples and integrations are finalized.

## License

MIT