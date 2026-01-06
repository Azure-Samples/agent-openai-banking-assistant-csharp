---
goal: Migrate Agent-OpenAI-Banking-Assistant from Semantic Kernel Agent Framework to Microsoft Agent Framework Using Workflows and AI Handoff Pattern
version: 1.0
date_created: 2025-01-05
owner: Banking Assistant Team
status: 'Planned'
tags: ['migration', 'agent-framework', 'handoff-pattern', 'workflows']
---

# Implementation Plan: Agent Framework Migration with Handoff Workflows

## Executive Summary

This plan guides the migration of the banking assistant from Semantic Kernel v1.48.0 to Microsoft Agent Framework using HandoffOrchestration and AI Handoff Pattern for seamless agent choreography.

**Target Architecture**:
- Microsoft Agent Framework with HandoffOrchestration
- AI Handoff Pattern for sequential agent transitions
- Agent Workflows with InProcessRuntime
- ChatClientAgent for all domain agents

## 1. Requirements & Constraints

### Understanding the Handoff Pattern Migration

**Current State (Semantic Kernel - Being Replaced)**:
- AgentRouter uses SK's `AgentGroupChat` with external strategy pattern
- IntentExtractorAgent classifies intent separately
- Router makes routing decision via `KernelFunctionSelectionStrategy`
- AgentGroupChat then delegates to chosen agent
- No true handoff: agents don't control flow, strategy does

**Target State (Agent Framework - Handoff Pattern)**:
- TriageAgent is the entry point and actively hand offs to specialists
- Each specialist agent has full context and can hand off back if needed
- Agents control their own fate: they decide when to hand off
- HandoffOrchestration defines allowed handoff paths (edges)
- InProcessRuntime executes the workflow, preserving full context across all handoffs
- No external router or strategy: agents orchestrate themselves

### Functional Requirements
- REQ-001: Maintain feature parity with current SK implementation
- REQ-002: Support intent-based routing (Account, Payment, Transactions, None)
- REQ-003: Execute MCP tools from Account, Payment, Transactions APIs
- REQ-004: Execute OpenAPI tools from Transaction History API
- REQ-005: Support InvoiceScanPlugin for OCR
- REQ-006: Preserve conversation context across handoffs
- REQ-007: Support streaming and non-streaming responses
- REQ-008: Maintain authentication and user context isolation

### Technical Requirements
- TEC-001: Use Microsoft.Agents.AI namespaces (not Semantic Kernel)
- TEC-002: Use HandoffOrchestration<TInput, TOutput> pattern
- TEC-003: Use ChatClientAgent as base agent type
- TEC-004: Use Microsoft.Extensions.AI for chat abstractions
- TEC-005: Support .NET 9.0+ (consider .NET 10.0)
- TEC-006: Maintain API contract compatibility (ChatAppRequest/Response)

### Constraints
- CON-001: Cannot break existing frontend API contracts
- CON-002: MCP integration must work with AF tool patterns
- CON-003: Custom plugins must integrate with AF
- CON-004: Maintain clean DI registration

### Patterns
- PAT-001: HandoffOrchestration<TInput, TOutput> for orchestration
- PAT-002: OrchestrationHandoffs builder for relationships
- PAT-003: ChatClientAgent for Azure OpenAI
- PAT-004: AIFunctionFactory.Create() for tools
- PAT-005: InProcessRuntime for execution

## 2. Implementation Phases

### Phase 1: Foundation & Dependencies (8-12 hours)
- Review AF documentation and samples
- Verify .NET compatibility
- Update .csproj (remove SK agents, add AF packages)
- Update GlobalUsings.cs namespaces
- Create ChatClientInitialization helper
- Test basic client creation

**Key Changes**:
`csharp
// Add packages
Microsoft.Agents.AI (1.0.0+)
Microsoft.Agents.AI.Workflows (1.0.0+)
Microsoft.Extensions.AI (1.0.0+)
Azure.AI.OpenAI (2.3.0+)

// Remove packages
Microsoft.SemanticKernel.Agents.Core
Microsoft.SemanticKernel.Connectors.AzureOpenAI
Microsoft.SemanticKernel.Plugins.OpenApi
`

### Phase 2: Agent Infrastructure (12-16 hours)
- Create AgentFactory.cs for ChatClientAgent creation
- Create ToolRegistrationHelper.cs for tool management
- Refactor AgenticUtils.cs for AF patterns
- Create AIFunctionToolAdapter.cs
- Test tool registration (MCP, OpenAPI, custom)

### Phase 3: Agent Refactoring (20-28 hours)

**3.1 Intent Extractor Agent**
- Refactor to AF chat patterns
- Remove IChatCompletionService
- Use ChatClientAgent or direct IChatClient
- Test intent extraction

**3.2 Account Agent**
- Convert to ChatClientAgent
- Register MCP Account API tools
- Test balance/payment methods queries

**3.3 Transactions Agent**
- Convert to ChatClientAgent
- Register MCP Account tools
- Register OpenAPI Transaction tools
- Test transaction history queries

**3.4 Payment Agent**
- Convert to ChatClientAgent
- Register Payment API MCP tools
- Register Account API MCP tools
- Register Transaction History OpenAPI tools
- Integrate InvoiceScanPlugin
- Test payment flow with OCR

### Phase 4: Orchestration with Handoff Pattern (16-24 hours)

#### 4.1 Handoff Pattern Architecture

**What is the Handoff Pattern?**

The handoff pattern enables agents to **explicitly transfer control** to one another based on context, with **no central orchestrator**. Each agent has full task ownership and receives the complete conversation history.

**Key Differences from Current Implementation**:
| Aspect | Current (SK GroupChat) | Handoff Pattern (AF) |
|--------|------------------------|----------------------|
| **Control Flow** | Central strategy routes between agents | Agents decide and hand off to each other |
| **Task Ownership** | Strategy retains responsibility | Receiving agent owns the task |
| **Context** | Limited history per agent | Full conversation history passed |
| **Message Flow** | Async iteration with event loop | Sequential agent transitions |
| **Handoff Logic** | External (in router) | Internal (in agent instructions) |

**Banking Assistant Architecture**:
```
ChatController (API layer)
  ↓
AgentOrchestrationService
  ↓
HandoffOrchestration<ChatContext, ChatResponse>
  ├─ TriageAgent
  │   System: "Classify user intent and hand off to appropriate specialist"
  │   Tools: None (classification only)
  │   Handoff: → AccountAgent | PaymentAgent | TransactionsAgent
  │
  ├─ AccountAgent
  │   System: "Handle account inquiries (balance, payment methods, etc.)"
  │   Tools: Account MCP API
  │   Handoff: → TriageAgent (for out-of-scope requests)
  │
  ├─ PaymentAgent
  │   System: "Process payments and bill payments"
  │   Tools: Payment MCP API, Account MCP API, Transaction History OpenAPI, InvoiceScanPlugin
  │   Handoff: → TriageAgent (for clarifications)
  │
  └─ TransactionsAgent
      System: "Provide transaction history and reports"
      Tools: Transaction History OpenAPI, Account MCP API
      Handoff: → TriageAgent (for out-of-scope requests)
  ↓
InProcessRuntime (Executes workflow, preserves context across handoffs)
```

#### 4.2 Implementation Details

**TriageAgent (NEW - Central to Handoff Pattern)**:
- **Role**: Initial entry point, responsible for intent classification and routing
- **System Instructions**: 
  - Analyze user intent (AccountInfo, BillPayment, RepeatTransaction, TransactionHistory)
  - Hand off with reasoning: "User is asking about [intent], handing off to [AgentName]"
  - DO NOT try to handle requests outside its scope
  - Provide clear handoff message with context
- **Tools**: None (uses inference only for routing)
- **Handoff Targets**: All other agents
- **Return Points**: Agents hand back when needing clarification or when task is unclear

**Handoff Orchestration Configuration**:
```csharp
// Core handoff relationships - explicit control transfer
var workflow = AgentWorkflowBuilder
    .StartHandoffWith(triageAgent)  // Entry point
    .WithHandoffs(triageAgent, [accountAgent, paymentAgent, transactionsAgent])
    .WithHandoff(accountAgent, triageAgent)  // Can return to triage
    .WithHandoff(paymentAgent, triageAgent)
    .WithHandoff(transactionsAgent, triageAgent)
    .Build();
```

**Message Flow Example (Handoff Pattern)**:
```
User: "What's my account balance?"

1. TriageAgent (Initial)
   - Receives: User message + empty history
   - Inference: "AccountInfo intent → AccountAgent"
   - Handoff: Transfer control + full context to AccountAgent

2. AccountAgent (Handoff Target)
   - Receives: User message + TriageAgent's message + conversation history
   - Tools: [GetAccountBalance, GetPaymentMethods, ...]
   - Executes: GetAccountBalance(userId)
   - Response: "Your account balance is $5,000"
   - Handoff: Not needed (task complete)

3. Response bubbles up to ChatController
   - No agent can spontaneously resume (explicit handoff only)
```

**Context Preservation During Handoffs**:
- Full `ChatMessage[]` history passed to receiving agent
- Each agent adds its messages to conversation
- InProcessRuntime maintains message chain
- Frontend receives complete conversation timeline

**Tasks**:
- Design TriageAgent instructions (intent classification + routing logic)
- Create AgentOrchestrationService.cs with HandoffOrchestration builder
- Implement TInput/TOutput types for workflow (ChatContext → ChatResponse)
- Define all handoff edges with clear conditions
- Replace AgentRouter.cs with orchestration pattern
- Implement InProcessRuntime.StreamAsync() for execution
- Handle streaming: Subscribe to WorkflowEvent (AgentRunUpdateEvent, WorkflowOutputEvent)
- Test handoff conditions: all intent types, out-of-scope requests, clarifications

#### 4.3 Handoff Pattern Best Practices

**Agent Instructions Must Include Handoff Logic**:
```csharp
// TriageAgent Instructions Example
var triageInstructions = """
You are a banking assistant that routes user requests to the right specialist.
Analyze the user's request and determine the intent:
- AccountInfo: Questions about account balance, payment methods, account details
- BillPayment: Creating new payments or bill payments
- RepeatTransaction: Repeating previous payments
- TransactionHistory: Viewing past transactions or transaction reports

Once you've identified the intent, hand off to the appropriate agent:
- For AccountInfo → hand off to AccountAgent
- For BillPayment/RepeatTransaction → hand off to PaymentAgent
- For TransactionHistory → hand off to TransactionsAgent

If the request is unclear, ask a clarifying question WITHOUT handing off.
Examples of how to handoff in your response:
"This is about account information. I'll hand you off to our Account Specialist."
"I can help with that payment. Let me connect you with our Payment Agent."
""";
```

**Domain Agent Instructions Must Support Handoff Back**:
```csharp
// AccountAgent Instructions Example
var accountInstructions = """
You are a banking specialist for account inquiries.
Help the user with: account balance, payment methods, account details.

If the user's request is about payments, transactions, or something outside
account inquiries, respond with:
"This sounds like it needs a specialist in [domain]. Let me transfer you to the right team."

Always provide the answer to account-related questions using available tools.
Never hand off if you can handle the request.
""";
```

**Preserve and Pass Context Explicitly**:
- AF automatically includes full `ChatMessage[]` in handoff
- Ensure each agent appends its response to conversation
- InProcessRuntime manages this; verify in testing

**Test Handoff Conditions**:
```
Test Matrix:
┌─────────────────────┬──────────────────┬─────────────────────┐
│ User Intent         │ Expected Handoff  │ Tools Executed      │
├─────────────────────┼──────────────────┼─────────────────────┤
│ "What's my balance?" │ TriageAgent →    │ GetAccountBalance   │
│                     │ AccountAgent      │                     │
├─────────────────────┼──────────────────┼─────────────────────┤
│ "Pay my electric"   │ TriageAgent →    │ CreatePayment,      │
│                     │ PaymentAgent      │ GetPaymentMethods   │
├─────────────────────┼──────────────────┼─────────────────────┤
│ "Show last 10 txns" │ TriageAgent →    │ GetTransactionHist  │
│                     │ TransactionsAgent │                     │
└─────────────────────┴──────────────────┴─────────────────────┘
```

### Phase 5: API & Controller Updates (8-12 hours)
- Review ChatAppRequest/Response contracts
- Create message adapters (AF ↔ API models)
- Refactor ChatController.ChatWithOpenAI()
- Update request/response conversions
- Implement streaming support
- Test end-to-end chat flow

### Phase 6: Service Registration (6-10 hours)
- Refactor ServiceCollectionExtensions.cs
- Register IChatClient for Azure OpenAI
- Register agents as singletons
- Register AgentOrchestrationService
- Register runtime/orchestration
- Remove Kernel registration
- Test DI resolution

### Phase 7: Testing & Validation (24-36 hours)

**Unit Tests**:
- Agent creation with correct tools
- Tool registration (MCP, OpenAPI, Custom)
- Intent extraction accuracy
- InvoiceScanPlugin functionality

**Integration Tests**:
- Handoff routing (IntentExtractor → Domain Agent)
- Multi-turn conversations
- Tool execution during handoffs

**End-to-End Tests**:
- Account balance inquiry flow
- Transaction history search
- Payment with invoice submission
- Error handling/fallback

**Performance Tests**:
- Single request latency
- Memory footprint vs SK
- Concurrent request handling

### Phase 8: Documentation & Cleanup (8-12 hours)
- Update README.md with new architecture
- Document handoff relationships
- Create developer guide
- Update troubleshooting guide
- Remove deprecated SK code
- Add XML documentation
- Review and merge

## 3. Files

### Create
- Agents/Infrastructure/AgentFactory.cs
- Agents/Infrastructure/ToolRegistrationHelper.cs
- Agents/Infrastructure/AIFunctionToolAdapter.cs
- Agents/Infrastructure/ChatClientInitialization.cs
- Agents/Orchestration/AgentOrchestrationService.cs
- Models/MessageAdapters.cs

### Modify
- banking-assistant.csproj
- GlobalUsings.cs
- Agents/IntentExtractorAgent.cs
- Agents/AccountAgent.cs
- Agents/PaymentAgent.cs
- Agents/TransactionsReportingAgent.cs
- Controllers/ChatController.cs
- Extensions/ServiceCollectionExtensions.cs
- Agents/Utils/AgenticUtils.cs
- README.md

### Remove
- Agents/AgentRouter.cs

## 4. Timeline

| Phase | Hours | Week |
|-------|-------|------|
| 1. Foundation | 8-12 | Week 1 |
| 2. Infrastructure | 12-16 | Week 2 |
| 3. Agent Refactoring | 20-28 | Week 2-3 |
| 4. Orchestration | 16-24 | Week 4 |
| 5. API Updates | 8-12 | Week 4-5 |
| 6. Service Registration | 6-10 | Week 5 |
| 7. Testing | 24-36 | Week 5-6 |
| 8. Documentation | 8-12 | Week 6 |
| **Total** | **102-150** | **6 weeks** |

**Estimated**: 3-4 weeks for 1 full-time developer

## 5. Success Criteria

✅ All agents created as ChatClientAgent with correct tools
✅ HandoffOrchestration routes intents properly
✅ MCP/OpenAPI/Custom tools execute correctly
✅ InvoiceScanPlugin integrated and functional
✅ Streaming responses work end-to-end
✅ All test categories pass
✅ Performance meets/exceeds SK baseline
✅ API contracts maintained
✅ Code compiles without warnings
✅ Documentation complete
✅ Ready for production review

## 6. Key Risks

### Handoff-Specific Risks

- **Intent Classification at Handoff Decision Point**: TriageAgent must reliably classify intent in system prompt
  - *Risk*: Model may not hand off when it should, or hand off with wrong agent
  - *Mitigation*: Use few-shot examples in TriageAgent instructions; test with diverse intents; fallback to TriageAgent on errors

- **Context Explosion Across Handoffs**: Each handoff includes full history; long conversations grow exponentially
  - *Risk*: Token usage increases; latency grows; context window exhaustion
  - *Mitigation*: Implement conversation summarization on long histories; use context reducers; test token budgets

- **Circular Handoffs**: Agents handing back and forth infinitely (e.g., TriageAgent ↔ DomainAgent)
  - *Risk*: Infinite loops; wasted tokens; poor user experience
  - *Mitigation*: Add max iteration counts; restrict handoff edges (no cycles); test edge cases

- **Tool Availability During Handoff**: Tools registered on one agent must be accessible after handoff
  - *Risk*: Agent hands off to domain agent, but tools not available on new agent
  - *Mitigation*: Ensure all MCP/OpenAPI tools registered on all capable agents; test tool access after handoff

### General Migration Risks

- **MCP Integration**: AF tool discovery patterns may differ
  - *Mitigation*: Study samples thoroughly; create adapters if needed

- **Complex Parameters**: Object parameters need special handling
  - *Mitigation*: Use custom serializers; test parameter passing

- **Streaming Implementation**: AF streaming differs from SK
  - *Mitigation*: Thorough testing; proper event handlers

- **Message Format Compatibility**: AF messages may not align with frontend
  - *Mitigation*: Adapter layer; maintain API contract

- **Runtime Stability**: Edge cases in InProcessRuntime
  - *Mitigation*: Comprehensive error testing; retry logic

## 7. Resources

### Official Migration Samples
https://github.com/microsoft/semantic-kernel/tree/main/dotnet/samples/AgentFrameworkMigration

### Key Examples
- Step03_Handoff: Full handoff orchestration example
- Step04_Handoff: Customer support triage system
- GettingStartedWithAgents: AF patterns and practices

### Documentation Links
- AgentFrameworkMigration/README.md
- HandoffOrchestration API documentation
- ChatClientAgent usage patterns
- AIFunctionFactory tool creation
