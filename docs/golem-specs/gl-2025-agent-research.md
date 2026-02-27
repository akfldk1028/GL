# GL 2025 Agent Research References

> **For AI agents working on GL/Golem.** Research foundations for the autonomous decision system.
> Referenced by: [`gl-autonomous-decision-system.md`](./gl-autonomous-decision-system.md)

---

## 1. SIMA 2 — Scalable Instructable Multimodal Agent (DeepMind, 2025)

**Source**: DeepMind (2025). *SIMA 2: A Generalist Embodied Agent for Virtual Worlds*. arXiv:2512.04797.

### Key Contributions

| Concept | Description | Golem Relevance |
|---------|-------------|-----------------|
| **Agentic Loop** | Perceive → Plan → Act → Evaluate → Adapt 5-stage loop | Tier 1-2 decision cycle model |
| **Confidence Scoring** | Agent outputs confidence 0.0-1.0 per decision; low confidence triggers fallback | Tier 1 prompt template — confidence field |
| **Self-Improvement** | Agent evaluates own action outcomes, adjusts future behavior | Tier 3 self-evaluation loop |
| **Multimodal Input** | Vision + language + spatial awareness fused | Future: screen capture as context |
| **Instruction Grounding** | Natural language → executable action mapping | Already done via ActionTypeRegistry |

### Architecture Pattern

```
[World State] → Perceive → [Context Builder]
                              ↓
                         Plan (LLM)  ← Memory + Goals
                              ↓
                         Act (Execute Action)
                              ↓
                         Evaluate (Success/Failure)
                              ↓
                         Adapt (Update Memory/Weights)
```

### Golem Application

- **Tier 1**: Adopt confidence scoring in prompt response format
- **Tier 2**: Evaluate-Adapt cycle for episodic memory (success/failure tagging)
- **Tier 3**: Self-improvement loop — agent reviews its own decision history

---

## 2. Voyager — LLM-Powered Embodied Agent (MineDojo/NVIDIA, 2023)

**Source**: Wang et al. (2023). *Voyager: An Open-Ended Embodied Agent with Large Language Models*. arXiv:2305.16291.

### Key Contributions

| Concept | Description | Golem Relevance |
|---------|-------------|-----------------|
| **Skill Library** | Successful action sequences cached and reused | Tier 2 Skill Memory |
| **Automatic Curriculum** | Agent generates progressively harder goals | Future: behavioral complexity growth |
| **Self-Verification** | Agent checks if action achieved its goal before storing as skill | Tier 1 "반복 회피" prompt instruction |
| **Iterative Prompting** | Failed actions → error feedback → retry with corrections | Tier 1 fallback: JSON parse retry |

### Skill Library Pattern

```
Action Sequence Execution
    ↓
Self-Verify: Did it work?
    ├── YES → Store in Skill Library (name, preconditions, actions, postconditions)
    └── NO  → Retry with error context (max 3 attempts)

Next Decision:
    1. Check Skill Library for matching situation
    2. If match found → execute cached sequence
    3. If no match → generate new plan via LLM
```

### Golem Application

- **Tier 2**: `SkillMemory` — cache successful action→outcome pairs
  - Key: situation hash (fsm_state + nearby_objects pattern)
  - Value: ActionId + target + success rate
- **Tier 1**: Self-verification prompt: "Do not repeat the same action 3+ times in a row"
- **Tier 1**: On JSON parse failure, retry once with error context

---

## 3. Project Sid — 1000 Agent Civilization (Altera, 2024)

**Source**: Altera (2024). *Project Sid: Many-agent simulations toward AI civilization*. arXiv:2411.00114.

### Key Contributions

| Concept | Description | Golem Relevance |
|---------|-------------|-----------------|
| **PIANO Architecture** | Perceive → Interpret → Anticipate → Navigate → Operate | Alternative to Park's Perceive-Plan-Act |
| **Social Memory** | Relationship tracking between agents | Future: multi-agent Golem |
| **Emergent Norms** | Agents develop social rules without explicit programming | Future: behavioral evolution |
| **Scalable Autonomy** | 1000 concurrent agents with manageable compute | Architecture validation — LLM query is viable at scale |

### PIANO Architecture

```
P - Perceive:    Gather raw world state
I - Interpret:   Understand context and meaning
A - Anticipate:  Predict outcomes of possible actions
N - Navigate:    Choose action considering social norms
O - Operate:     Execute action in world
```

### Golem Application

- **Design validation**: Confirms LLM-based discrete action selection scales
- **Future (multi-agent)**: Social memory + relationship tracking
- **Tier 2**: "Anticipate" step maps to reasoning field in prompt response

---

## 4. Agentic Architecture Surveys (2024-2025)

**Sources**:
- Xi et al. (2025). *The Rise and Potential of Large Language Model Based Agents: A Survey*. arXiv:2309.07864v3.
- Wang et al. (2024). *A Survey on Large Language Model based Autonomous Agents*. arXiv:2308.11432v2.
- Sumers et al. (2024). *Cognitive Architectures for Language Agents*. arXiv:2309.02427.

### Four-Type Memory Classification

| Memory Type | Definition | Duration | Golem Mapping |
|-------------|-----------|----------|---------------|
| **Working Memory** | Current context window | Session | Tier 1 prompt context (fsm_state, position, nearby) |
| **Episodic Memory** | Past experiences with timestamps | Persistent | Tier 2 MemoryEntry[] with success/failure tags |
| **Semantic Memory** | World knowledge, personality | Persistent | Personality JSON + world facts |
| **Procedural/Skill Memory** | Learned action sequences | Persistent | Tier 2 SkillLibrary (Voyager pattern) |

### Reasoning Patterns

| Pattern | Description | Golem Use |
|---------|-------------|-----------|
| **CoT (Chain-of-Thought)** | Step-by-step reasoning before answer | Tier 1 prompt: "Think step by step" |
| **ReAct** | Reason → Act → Observe → Reason loop | Tier 2: evaluate action results, feed back |
| **ToT (Tree-of-Thought)** | Explore multiple reasoning branches | Overkill for 30s decisions — skip |
| **Reflexion** | Self-critique and improvement | Tier 3 self-improvement loop |

### Golem Application

- **Tier 1**: CoT reasoning in prompt template (structured thinking before JSON output)
- **Tier 2**: 4-type memory as organizational framework
- **Tier 2**: ReAct pattern for action evaluation loop
- **Tier 3**: Reflexion for self-improvement

---

## 5. Park et al. 2023 — Generative Agents (Foundation)

**Source**: Park et al. (2023). *Generative Agents: Interactive Simulacra of Human Behavior*. UIST 2023. arXiv:2304.03442.

### Why It's Our Foundation

This is the **original** paper that proved LLM-based autonomous agents work for virtual character behavior. Core contributions adopted by Golem:

| Concept | Status in Golem |
|---------|----------------|
| Memory Stream | Tier 2 — planned (EpisodicMemory) |
| Retrieval (recency × importance × relevance) | Tier 2 — planned |
| Reflection | Tier 2 — planned (periodic high-level summaries) |
| Planning (day → hour → 5-min chunks) | Simplified: single next-action query |
| Persona-driven behavior | Tier 1 — personality in prompt |

### Known Limitations (2025 Perspective)

| Limitation | 2025 Solution |
|------------|---------------|
| No confidence scoring | SIMA 2 confidence field |
| No skill reuse | Voyager Skill Library |
| No self-improvement | SIMA 2 Evaluate-Adapt loop |
| Single-agent focus | Project Sid multi-agent |
| Text-only perception | SIMA 2 multimodal |
| Day/hour/minute planning too heavy | Simplified: 1 decision per query |
| GPT-3.5 era costs | 2025: GPT-4o-mini $0.15/M, local models free |

---

## 6. Comparison Matrix

| Dimension | Park 2023 | Voyager 2023 | Project Sid 2024 | SIMA 2 2025 | Surveys 2024-25 |
|-----------|-----------|-------------|-----------------|-------------|--------------|
| **Decision Model** | Plan→Act | Curriculum→Act→Verify | PIANO | Perceive→Plan→Act→Evaluate→Adapt | CoT/ReAct/ToT |
| **Memory** | Stream (single type) | Skill Library | Social + Episodic | Multimodal context | 4 types classified |
| **Self-Improvement** | Reflection only | Self-verification | Emergent norms | Evaluate-Adapt loop | Reflexion pattern |
| **Scale** | 25 agents | 1 agent | 1000 agents | 1 agent | N/A (survey) |
| **Domain** | 2D town sim | Minecraft | Minecraft | 3D environments | General |
| **Action Space** | Discrete (text) | Discrete (code) | Discrete (text) | Discrete + continuous | Varies |
| **Golem Tier** | T1+T2 foundation | T2 skills | Future (multi-agent) | T1 confidence, T3 self-improve | T1 CoT, T2 memory (2024-2025) |

---

## Maintenance Rules

### Adding New Research

1. Add a new numbered section following the existing pattern
2. Include: Source citation, Key Contributions table, Architecture diagram (if applicable), Golem Application subsection
3. Update the Comparison Matrix (Section 6) with the new entry
4. Update the References section in `gl-autonomous-decision-system.md`
5. Do NOT modify existing section numbers — append only

### Cross-Reference Rules

- This file is **read-only reference** — it does not define implementation
- Implementation decisions live in `gl-autonomous-decision-system.md`
- When referencing specific research in the spec, use format: `(see research.md §N)`
- Keep each section self-contained — readers should not need to read other sections

### Staleness Check

- Review annually or when a major new paper appears (e.g., Voyager 2, SIMA 3)
- If a paper's claims are superseded, add a note to its section rather than removing it
