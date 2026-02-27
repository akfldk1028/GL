# GL Autonomous Decision System

> **For AI agents working on GL/Golem.** This is the design spec for how the character decides "what to do next."
> Research basis: [`gl-2025-agent-research.md`](./gl-2025-agent-research.md)

---

## 1. Project Context

| Key | Value |
|-----|-------|
| Workspace | `C:/DK/GL/` (project name: **GL**) |
| Product | Golem (`C:/DK/GL/Golem/`) — Unity 6000.3.3f1 |
| What it does | Virtual AI agent character: AI Server ↔ WebSocket ↔ 3D character autonomous behavior |
| Code | ~70 .cs files, Phase 1-6 complete on `master` |
| Architecture | 5-layer: PointClick → FSM(10 states) → Modules(5) → IdleScheduler → AI Server |

**What's done**: The execution framework (FSM, Modules, Pub/Sub, Routing) works end-to-end.

**What's missing**: The **decision brain** — currently `IdleScheduler` picks actions via weighted random. No intelligence.

**Research basis**: Design draws from 5 research sources spanning 2023-2025 (see [research.md](./gl-2025-agent-research.md)):
- Park et al. 2023 (foundation — memory stream, persona, reflection)
- Voyager 2023 (skill library, self-verification)
- Project Sid 2024 (multi-agent scalability validation)
- SIMA 2 2025 (confidence scoring, self-improvement loop)
- Agentic Architecture Surveys 2024-2025 (4-type memory, CoT/ReAct reasoning)

---

## 2. Design Decision: LLM-based Generative Agents (NOT VLA)

### VLA Rejected

VLA (RT-2, Octo) = robotic continuous motor control (joint angles at 10-100Hz, trained on thousands of episodes).
Golem = discrete high-level action selection (one ActionId every 30-120s). Animation/nav already handled by FSM + NavMesh.
**Mismatch on every dimension. VLA is wrong tool.**

### Chosen: Generative Agents Pattern

Character sends context to LLM, receives structured action decision.
Fits perfectly: discrete actions, personality-driven, memory-aware, uses existing pipeline.

### Research Comparison (Why Park 2023 Is Starting Point, Not Endpoint)

| Dimension | Park 2023 (Start) | Golem Tier 1 | Golem Tier 2 | Golem Tier 3 |
|-----------|-------------------|-------------|-------------|-------------|
| Decision | Plan→Act | + CoT reasoning, confidence | + ReAct evaluate loop | + self-improvement |
| Memory | Single stream | Working memory (prompt context) | + Episodic + Skill + Semantic | + reflection triggers |
| Verification | None | + "don't repeat" instruction | + Voyager self-verify | + SIMA 2 adapt |
| Cost model | GPT-3.5 ($$$) | GPT-4o-mini / local ($0.02/hr) | Same | Same (local preferred) |

Park et al. proved the concept. Golem modernizes it with 2025 patterns. (see [research.md §5](./gl-2025-agent-research.md#5-park-et-al-2023--generative-agents-foundation) for limitations)

---

## 3. Architecture

### Current State (Phase 5 — Weighted Random)

```
IdleScheduler (timer expires, ~30-120s interval)
  │
  │ PickRandomAction() — weighted random from 5 action types
  │
  ▼
Managers.PublishAction(actionId, payload)
  │
  ▼
CharacterCommandRouter → FSM → Modules → Animator
(existing pipeline, works)
```

### Target: Tier 1 (LLM Query)

```
IdleScheduler (timer expires)
  │
  │ context: {fsm_state, position, nearby_objects, last_5_actions, personality}
  │
  ▼
AIDecisionConnector (HTTP POST)  →  AI Server / LLM
  │
  │ response: {"action":"SitAtChair", "target":"bench_01",
  │            "thought":"tired from walking",
  │            "confidence":0.8, "reasoning":"haven't sat in 5 min"}
  │
  ▼
Parse + Validate ActionId
  │
  ├── confidence ≥ 0.3 → Managers.PublishAction(actionId, payload) ──┐
  └── confidence < 0.3 → PickRandomAction() (fallback) ────────────┤
                                                                    ▼
                                                CharacterCommandRouter → FSM → Modules → Animator
                                                (existing pipeline, zero changes)
```

### Target: Tier 2 (Memory + Skills)

```
IdleScheduler (timer expires)
  │
  ├─────────────────────────────────────┐
  │                                     │
  ▼                                     ▼
MemoryStore.Retrieve(context)    SkillLibrary.Match(context)
  │  top-K episodic memories       │  cached action sequences
  │                                │
  └──────────┬─────────────────────┘
             ▼
   AIDecisionConnector (HTTP POST)
   prompt += memories + skills + personality
             │
             ▼
   Execute Action → Evaluate Outcome (ReAct)
             │
             ├── Success → SkillLibrary.Store(situation, action)
             │              MemoryStore.Add(episode, success=true)
             └── Failure → MemoryStore.Add(episode, success=false)
                            retry with error context (1x)
```

### What Changes vs What Stays

| Class | Role | Tier 1 Change |
|-------|------|---------------|
| `IdleScheduler` | autonomous behavior trigger | **MODIFY**: weighted random → LLM query |
| `CFConnector` / WebSocket | AI server comms | **none** (LLM uses separate HTTP) |
| `AIDecisionConnector` (new) | HTTP → LLM API | **CREATE** |
| `CharacterCommandRouter` | routes 13 commands to FSM | none |
| `CharacterBehaviorFSM` | 10-state FSM | none |
| `GolemCharacterController` | orchestrator | none |
| `ActionMessageBus` | pub/sub | none |

**95% of existing code untouched.** Only IdleScheduler internals + new HTTP connector.

---

## 4. Implementation Tiers

### Tier 1 — LLM Query (replaces weighted random)

**Files to change**:
- `IdleScheduler.cs` — on timer expiry, call LLM instead of random pick
- New: `AIDecisionConnector.cs` — HTTP POST to LLM endpoint
- Response parser: JSON → `ActionId` + `IActionPayload`

**Prompt Template (v2 — with CoT reasoning)**:

```
You are {name}, a character in a virtual world.

## Current State
- FSM state: {current_fsm_state}
- Position: {world_position}
- Nearby objects: {object_list}
- Recent actions (last 5): {last_5_actions}

## Personality
{personality_json}

## Rules
1. Think step by step about what you want to do and why.
2. Do NOT repeat the same action 3 times in a row.
3. Choose actions that fit your personality and current context.
4. If you just sat for a long time, consider standing up and walking.

## Valid Actions
Idle, MoveToLocation, TurnTo, SitAtChair, StandUp, LookAt, Lean, ExamineMenu, PlayArcade, PlayClaw, Wave

Respond ONLY with JSON (no markdown, no explanation):
{
  "reasoning": "<2-3 sentences: why this action>",
  "action": "<ActionId from valid list>",
  "target": "<object_name or null>",
  "thought": "<one sentence: character's inner thought>",
  "confidence": <0.0-1.0>
}
```

**Prompt Changes from v1**:
- Added CoT reasoning step (research.md §4 — structured reasoning before JSON output)
- Added confidence field (SIMA 2 pattern — enables fallback)
- Added reasoning field (debugging + Tier 3 self-evaluation input)
- Added "don't repeat" instruction (Voyager self-verification pattern)
- Expanded valid actions list to match full ActionId set

**Fallback Strategy**:

| Condition | Behavior |
|-----------|----------|
| HTTP failure (timeout, 5xx) | `PickRandomAction()` — existing weighted random |
| Invalid JSON response | Retry once with error context; if still fails → random |
| confidence < 0.3 | `PickRandomAction()` — LLM is uncertain |
| Unknown ActionId in response | Map to closest valid ActionId; if none → random |

**Cost**: GPT-4o-mini ~$0.02/hr (30s interval). Local Qwen2.5-3B = free.

---

### Tier 2 — Memory + Personality + Skills

Adds persistent memory so the character avoids repetition, learns from experience, and develops behavioral patterns.

**Four-Type Memory Model** (see [research.md §4](./gl-2025-agent-research.md#4-agentic-architecture-surveys-2024-2025)):

#### Working Memory (already exists in Tier 1)

Current context passed in each prompt: fsm_state, position, nearby_objects, last_5_actions.
No persistence needed — rebuilt every query.

#### Episodic Memory (new)

Timestamped log of actions with outcomes.

```csharp
struct EpisodeEntry {
    DateTime timestamp;
    ActionId action;
    string target;
    string thought;
    float importance;       // 0-1, unusual actions score higher
    bool succeeded;         // did the action complete normally?
    Vector3 position;       // where it happened
    string contextHash;     // situation fingerprint for skill matching
}
```

**Retrieval**: When querying LLM, include top-K episodes scored by:
- Recency (exponential time decay, half-life ~10 minutes game time)
- Importance (unusual actions score higher)
- Relevance (situation similarity to current context)

#### Skill Memory (new — Voyager pattern)

Successful action sequences cached and reused.

```csharp
struct SkillEntry {
    string situationPattern;     // e.g., "idle + near_bench + walked_recently"
    ActionId recommendedAction;
    string target;
    int useCount;
    float successRate;           // tracked over time
}
```

**Workflow**:
1. Before LLM query, check SkillLibrary for matching situation
2. If match with successRate > 0.7 → use cached action (skip LLM call, save cost)
3. If no match → query LLM → if success → store as new skill
4. Skills with successRate < 0.3 after 5+ uses → auto-remove

#### Semantic Memory (personality + world knowledge)

Per-character JSON config (persistent, rarely changes):

```json
{
  "name": "Golem",
  "traits": ["curious", "calm", "observant"],
  "preferences": {
    "favorite_spot": "garden_bench",
    "dislikes": "standing still too long"
  },
  "world_knowledge": [
    "The cafe has 4 chairs and an arcade area",
    "The claw machine is near the entrance"
  ]
}
```

#### Reflection (periodic, Park et al. pattern)

Every N actions (e.g., 20), LLM generates abstract observations:
- "I tend to sit on benches after walking" → stored as high-importance episodic memory
- Triggered when accumulated importance scores exceed threshold

**Reactive Re-planning** (ReAct pattern):

```
Execute Action
    ↓
Observe Outcome (ActionCompleted / ActionFailed)
    ↓
Evaluate: expected outcome vs actual?
    ├── Match → continue normal schedule
    └── Mismatch → re-query LLM with failure context (1 retry)
```

---

### Tier 3 — Local Inference + Self-Improvement

#### Local Model via Ollama

Replace cloud LLM with local model via Ollama (`http://localhost:11434/api/generate`).
Same HTTP interface — swap URL only.

| Model | VRAM | Latency | Notes |
|-------|------|---------|-------|
| Qwen2.5-3B | 2GB | ~200ms | Good structured JSON output |
| Qwen2.5-7B | 4GB | ~400ms | Better reasoning, still fast |
| Phi-3.5-mini 3.8B | 2.5GB | ~250ms | Strong reasoning |
| Llama 3.2 3B | 2GB | ~200ms | Meta's latest small model |
| Gemma 2 2B | 1.5GB | ~150ms | Google's efficient model |

#### Self-Improvement Loop (SIMA 2 pattern)

```
Every M decisions (e.g., 50):
    1. Collect recent episodes with reasoning fields
    2. Query LLM: "Review these decisions. Which were good? Which were bad? Why?"
    3. Generate behavioral adjustments:
       - Adjust personality weights
       - Add/remove skills from SkillLibrary
       - Update world_knowledge in semantic memory
    4. Store adjustment as high-importance memory
```

This is **optional** and targets Tier 3 only. The system works without it.

---

## 5. Key APIs

### Existing APIs (no changes needed)

```csharp
// Pub/Sub
Managers.Subscribe(ActionId, Action<ActionMessage>) → IDisposable
Managers.PublishAction(ActionId) / PublishAction(ActionId, IActionPayload)

// FSM
CharacterBehaviorFSM.ForceTransition(CharacterStateId)
CharacterBehaviorFSM.RequestTransition(CharacterStateId)

// Modules
BehaviorModuleRegistry.Register(IBehaviorModule)
BehaviorModuleRegistry.Get<T>()
BehaviorModuleRegistry.UpdateAll(float deltaTime)

// Command Router
CharacterCommandRouter.HandleCommand(string commandName, CommandPayload payload)
```

### New APIs (Tier 1-2 — preview, not yet implemented)

```csharp
// Tier 1: LLM Decision Connector
public class AIDecisionConnector
{
    Task<DecisionResult> QueryAsync(DecisionContext context);
    void SetEndpoint(string url);           // swap cloud/local
    void SetModel(string modelName);        // "gpt-4o-mini", "qwen2.5:3b"
}

struct DecisionResult {
    ActionId Action;
    string Target;
    string Thought;
    float Confidence;
    string Reasoning;
}

struct DecisionContext {
    CharacterStateId FsmState;
    Vector3 Position;
    string[] NearbyObjects;
    ActionId[] RecentActions;    // last 5
    string PersonalityJson;
}

// Tier 2: Memory Store
public class MemoryStore
{
    void AddEpisode(EpisodeEntry entry);
    EpisodeEntry[] RetrieveTopK(DecisionContext context, int k);
    void TriggerReflection();               // periodic abstraction
}

// Tier 2: Skill Library
public class SkillLibrary
{
    SkillEntry? Match(DecisionContext context);
    void Store(string situationPattern, ActionId action, string target);
    void UpdateSuccessRate(string situationPattern, bool succeeded);
    void PruneFailedSkills();               // remove successRate < 0.3
}
```

---

## 6. References

1. Park et al. (2023). *Generative Agents: Interactive Simulacra of Human Behavior*. UIST 2023. arXiv:2304.03442.
2. Wang et al. (2023). *Voyager: An Open-Ended Embodied Agent with Large Language Models*. arXiv:2305.16291.
3. Altera (2024). *Project Sid: Many-agent simulations toward AI civilization*. arXiv:2411.00114.
4. DeepMind (2025). *SIMA 2: A Generalist Embodied Agent for Virtual Worlds*. arXiv:2512.04797.
5. Xi et al. (2025). *The Rise and Potential of Large Language Model Based Agents: A Survey*. arXiv:2309.07864v3.
6. Wang et al. (2024). *A Survey on Large Language Model based Autonomous Agents*. arXiv:2308.11432v2.
7. Sumers et al. (2024). *Cognitive Architectures for Language Agents*. arXiv:2309.02427.
8. Brohan et al. (2023). *RT-2: Vision-Language-Action Models*. (rejected for this project — wrong domain)

---

## 7. Maintenance Rules

### Code Review Checklist

When modifying the decision system:

- [ ] Does the change affect the prompt template? → Update §4 Tier 1
- [ ] Does the change add a new memory type? → Verify it fits the 4-type model (§4 Tier 2)
- [ ] Does the change add a new ActionId? → Update "Valid Actions" in prompt template
- [ ] Does the change affect API signatures? → Update §5 Key APIs
- [ ] Does the change reference new research? → Add to `gl-2025-agent-research.md` first

### Dependency Direction

```
gl-2025-agent-research.md (reference only, no implementation details)
        ↑ references
gl-autonomous-decision-system.md (spec — design decisions + API shapes)
        ↑ implements
Golem/Assets/Scripts/ (code — must match spec)
        ↑ documents
Golem/README.md (overview — high-level summary)
```

**Rule**: Changes flow top-down. Research informs spec, spec guides code, README summarizes.
Never modify research doc to match code — if code diverges, update the spec.

### ActionId Sync

The prompt template's "Valid Actions" list must stay synchronized with `ActionId.cs`.
Current valid actions for autonomous behavior (subset of 42 total ActionIds):

```
Idle, MoveToLocation, TurnTo, SitAtChair, StandUp, LookAt,
Lean, ExamineMenu, PlayArcade, PlayClaw, Wave
```

Social actions (Greet, Nod, HeadShake, Point) are available but currently handled by `GolemSocialHandler` via AI server commands, not autonomous decisions.

### Cross-Document Links

| Document | Purpose | Links To |
|----------|---------|----------|
| This file | Implementation spec | research.md (foundations), README (overview) |
| `gl-2025-agent-research.md` | Research references | This file (applied in) |
| `Golem/README.md` | Project overview | This file (detailed spec) |
| `docs/research/natural-ai-character-behavior.md` | Behavior techniques | This file (motion/idle patterns) |
