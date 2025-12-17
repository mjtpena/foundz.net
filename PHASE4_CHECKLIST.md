# Phase 4: Agent Orchestration & Tool Registry - Implementation Checklist

## Overview
With 38 production-ready tools from Phase 3, Phase 4 focuses on orchestrating these tools through an AI agent.

---

## 🎯 Phase 4 Objectives

### Primary Goals
1. Build Tool Registry for auto-discovery and management
2. Implement Agent Orchestrator with agentic loop
3. Integrate tools with AI function calling
4. Add confirmation and safety systems
5. Implement context management
6. Build session persistence

---

## ✅ Task Checklist

### 1. Tool Registry (Core.ToolRegistry/)

- [ ] **ToolRegistry.cs**
  - [ ] Implement IToolRegistry interface
  - [ ] Auto-discover tools from assembly
  - [ ] Register tools by name and category
  - [ ] Support plugin loading
  - [ ] Tool validation on registration
  - [ ] Thread-safe registration

- [ ] **ToolExecutor.cs**
  - [ ] Execute tools with timeout
  - [ ] Handle async tool execution
  - [ ] Apply resource limits
  - [ ] Capture stdout/stderr
  - [ ] Handle cancellation
  - [ ] Collect execution metrics

- [ ] **ToolSchemaGenerator.cs**
  - [ ] Convert ITool.ParametersSchema to OpenAI format
  - [ ] Convert to Anthropic format
  - [ ] Validate JSON schemas
  - [ ] Generate documentation from schema

- [ ] **ToolConfirmationManager.cs**
  - [ ] Check if tool requires confirmation
  - [ ] Present confirmation UI
  - [ ] Track user responses
  - [ ] Support --yes mode (skip all)
  - [ ] Audit log confirmations

### 2. Agent Orchestrator (Core.Orchestration/)

- [ ] **AgentOrchestrator.cs**
  - [ ] Implement agentic loop (15 iterations max)
  - [ ] Parse AI responses for tool calls
  - [ ] Execute tools in order
  - [ ] Format tool results for AI
  - [ ] Handle iteration limits
  - [ ] State machine implementation

- [ ] **ContextManager.cs**
  - [ ] Maintain conversation history
  - [ ] Sliding window (last 20 messages)
  - [ ] Token budget tracking
  - [ ] Include relevant files
  - [ ] Add project metadata
  - [ ] Intelligent truncation

- [ ] **ConversationHistory.cs**
  - [ ] Store messages in order
  - [ ] Support branching conversations
  - [ ] Export/import capability
  - [ ] Search conversation
  - [ ] Summarization

- [ ] **ToolCallParser.cs**
  - [ ] Parse OpenAI function calls
  - [ ] Parse Anthropic tool use
  - [ ] Extract tool name and arguments
  - [ ] Validate arguments
  - [ ] Handle malformed calls

- [ ] **ToolResultFormatter.cs**
  - [ ] Format results for OpenAI
  - [ ] Format results for Anthropic
  - [ ] Truncate large outputs
  - [ ] Highlight errors
  - [ ] Add metadata

### 3. AI Client Updates (Core.AI/)

- [ ] **Update IAIClient**
  - [ ] Add tool/function calling support
  - [ ] Support parallel tool calls
  - [ ] Handle tool_use response type

- [ ] **Update AzureAIClient**
  - [ ] Implement function calling
  - [ ] Parse tool_calls from response
  - [ ] Support streaming with tools
  - [ ] Handle tool choice (auto/required/none)

- [ ] **Update Provider Adapters**
  - [ ] AnthropicAdapter - tool_use format
  - [ ] OpenAIAdapter - function_call format
  - [ ] Test with Claude 3.5 Sonnet
  - [ ] Test with GPT-4
  - [ ] Test with other models

### 4. Session Management (Data/)

- [ ] **SessionManager.cs**
  - [ ] Create new sessions
  - [ ] Resume existing sessions
  - [ ] Save session state
  - [ ] Load session state
  - [ ] Archive old sessions
  - [ ] Delete sessions

- [ ] **Update Database Schema**
  - [ ] Add ToolCalls table
  - [ ] Add ToolResults table
  - [ ] Add indexes for performance
  - [ ] Migration script

- [ ] **Repository Updates**
  - [ ] SessionRepository enhancements
  - [ ] ToolExecutionRepository
  - [ ] Query optimization

### 5. System Prompt (Core/)

- [ ] **SystemPromptBuilder.cs**
  - [ ] Build system prompt with tools
  - [ ] Include tool descriptions
  - [ ] Add usage guidelines
  - [ ] Include examples
  - [ ] Project-specific instructions
  - [ ] Safety guidelines

### 6. Parallel Execution (Core/)

- [ ] **ParallelToolExecutor.cs**
  - [ ] Identify safe parallel tools
  - [ ] Execute independent tools in parallel
  - [ ] Aggregate results
  - [ ] Handle partial failures
  - [ ] Respect resource limits

### 7. CLI Integration (Cli/Commands/)

- [ ] **Update ChatCommand**
  - [ ] Integrate with AgentOrchestrator
  - [ ] Display tool executions
  - [ ] Show confirmations
  - [ ] Stream responses
  - [ ] Handle errors gracefully

- [ ] **Update TaskCommand**
  - [ ] One-shot task execution
  - [ ] Non-interactive mode
  - [ ] Return structured output

### 8. Progress & Feedback (Cli/)

- [ ] **ProgressIndicator**
  - [ ] Show AI thinking
  - [ ] Show tool execution
  - [ ] Show progress percentage
  - [ ] Estimated time remaining

- [ ] **ToolExecutionDisplay**
  - [ ] Show tool name and args
  - [ ] Show execution status
  - [ ] Show results/errors
  - [ ] Format for readability

### 9. Testing (Tests/)

- [ ] **Unit Tests**
  - [ ] ToolRegistry tests
  - [ ] AgentOrchestrator tests
  - [ ] ToolExecutor tests
  - [ ] ContextManager tests
  - [ ] Parser tests

- [ ] **Integration Tests**
  - [ ] End-to-end agentic loop
  - [ ] Tool execution pipeline
  - [ ] Session persistence
  - [ ] Multi-turn conversations

- [ ] **Mock Tools**
  - [ ] Create test tools
  - [ ] Fast execution
  - [ ] Predictable results

### 10. Configuration (Infrastructure/)

- [ ] **Update FoundzConfiguration**
  - [ ] Add agent settings
  - [ ] Add tool settings
  - [ ] Add confirmation settings
  - [ ] Validation rules

### 11. Error Handling

- [ ] **Error Recovery**
  - [ ] Handle tool failures gracefully
  - [ ] Continue on non-critical errors
  - [ ] Retry with exponential backoff
  - [ ] Fallback to alternative tools

- [ ] **User Communication**
  - [ ] Clear error messages
  - [ ] Suggested actions
  - [ ] Debug information (verbose mode)

### 12. Observability

- [ ] **Logging**
  - [ ] Log all AI requests/responses
  - [ ] Log tool executions
  - [ ] Log errors and warnings
  - [ ] Performance metrics

- [ ] **Telemetry**
  - [ ] Track tool usage
  - [ ] Track AI token usage
  - [ ] Track execution time
  - [ ] Track error rates

### 13. Documentation

- [ ] **Architecture Documentation**
  - [ ] Agent flow diagram
  - [ ] Tool execution pipeline
  - [ ] Sequence diagrams

- [ ] **User Documentation**
  - [ ] How to use the agent
  - [ ] Tool confirmation system
  - [ ] Session management
  - [ ] Troubleshooting

---

## 🎨 Implementation Order

### Week 1: Foundation
1. ToolRegistry + ToolExecutor
2. ToolSchemaGenerator
3. Unit tests for registry

### Week 2: Orchestration
1. AgentOrchestrator skeleton
2. ToolCallParser
3. ToolResultFormatter
4. Basic agentic loop

### Week 3: Integration
1. Update AI clients
2. Integrate with CLI
3. Progress display
4. Error handling

### Week 4: Polish
1. Session management
2. Context management
3. Confirmation system
4. Documentation

---

## 🧪 Testing Strategy

### Unit Tests
- Test each component in isolation
- Mock dependencies
- Cover edge cases
- Test error scenarios

### Integration Tests
- Test component interactions
- Test with real tools
- Test agentic loop
- Test persistence

### End-to-End Tests
- Full agent conversation
- Multiple tool executions
- Session save/resume
- Error recovery

### Manual Testing
- Test with Claude 3.5 Sonnet
- Test with GPT-4
- Test all tool categories
- Test confirmation system

---

## 📊 Success Criteria

### Functional
- [ ] Agent can execute at least 5 tools in sequence
- [ ] Agentic loop completes successfully
- [ ] Confirmation system works
- [ ] Sessions save and resume
- [ ] Context management handles 20+ messages

### Performance
- [ ] Agent response time < 5 seconds
- [ ] Tool execution overhead < 50ms
- [ ] Context selection < 100ms
- [ ] Session save < 200ms

### Reliability
- [ ] Handles tool failures gracefully
- [ ] No data loss on crash
- [ ] Recovers from AI errors
- [ ] Cancellation works properly

### Usability
- [ ] Clear progress indicators
- [ ] Helpful error messages
- [ ] Confirmation prompts are clear
- [ ] Tool output is readable

---

## 🚀 Quick Start Commands

### Setup
```bash
cd /Users/mjtpena/dev/foundz.net
dotnet restore
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Run Agent (once implemented)
```bash
dotnet run --project src/Foundz.Net.Cli -- chat
```

---

## 📝 Key Files to Create

1. `src/Foundz.Net.Core/ToolRegistry/ToolRegistry.cs`
2. `src/Foundz.Net.Core/ToolRegistry/ToolExecutor.cs`
3. `src/Foundz.Net.Core/Orchestration/AgentOrchestrator.cs`
4. `src/Foundz.Net.Core/Orchestration/ContextManager.cs`
5. `src/Foundz.Net.Core/Orchestration/ToolCallParser.cs`
6. `src/Foundz.Net.Core/Orchestration/ToolResultFormatter.cs`
7. `tests/Foundz.Net.Tests.Unit/ToolRegistryTests.cs`
8. `tests/Foundz.Net.Tests.Unit/AgentOrchestratorTests.cs`
9. `tests/Foundz.Net.Tests.Integration/AgentIntegrationTests.cs`

---

## 🎯 Dependencies Needed

Already have from Phase 2:
- ✅ Azure.AI.Inference
- ✅ Microsoft.SemanticKernel (if needed)
- ✅ Serilog
- ✅ Entity Framework Core

May need to add:
- [ ] Polly (for retry logic)
- [ ] System.Text.Json (already included)
- [ ] FluentValidation (for advanced validation)

---

## 🔍 Reference Implementation

Look at Phase 2 for:
- AzureAIClient structure
- Provider adapter pattern
- Async patterns
- Error handling

Look at Phase 3 for:
- Tool interface implementation
- Parameter validation
- Error responses
- Metadata collection

---

## ⚠️ Common Pitfalls to Avoid

1. **Don't** block async methods - use await throughout
2. **Don't** forget CancellationToken propagation
3. **Don't** ignore resource cleanup (use using statements)
4. **Don't** hard-code model-specific logic
5. **Don't** forget to handle tool timeouts
6. **Don't** expose sensitive data in logs
7. **Don't** forget to test with multiple AI models

---

## 🎉 When Phase 4 is Complete

You will have:
- ✅ A working AI agent that can use 38 tools
- ✅ Full agentic loop implementation
- ✅ Session management and persistence
- ✅ Safety and confirmation system
- ✅ Context-aware conversations
- ✅ Ready for Phase 5 (Enhanced Features)

---

**Phase 3 Status**: ✅ Complete  
**Phase 4 Status**: ⏳ Ready to Start  
**Target Completion**: 4 weeks

Let's build an intelligent agent! 🤖✨
