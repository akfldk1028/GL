# Natural AI Character Behavior in 3D Environments
## State-of-the-Art Research & Implementation Guide for Golem

**Date**: 2026-02-17
**Purpose**: Practical techniques to make virtual AI agent characters feel like real people in Unity 3D environments.

---

## Table of Contents
1. [Stanford Generative Agents Architecture](#1-stanford-generative-agents-architecture)
2. [BML (Behavior Markup Language)](#2-bml-behavior-markup-language)
3. [Natural Locomotion](#3-natural-locomotion)
4. [Idle Behaviors That Feel Alive](#4-idle-behaviors-that-feel-alive)
5. [Transition Smoothness](#5-transition-smoothness)
6. [Unity-Specific Techniques](#6-unity-specific-techniques)
7. [TOP 10 Most Impactful Techniques (Impact-to-Effort Ranking)](#7-top-10-most-impactful-techniques)

---

## 1. Stanford Generative Agents Architecture

**Source**: Park et al., "Generative Agents: Interactive Simulacra of Human Behavior" (Stanford + Google, 2023)
**Reference**: [arXiv:2304.03442](https://arxiv.org/abs/2304.03442) | [ACM UIST 2023](https://dl.acm.org/doi/10.1145/3586183.3606763)

### 1.1 Core Loop: Perceive -> Plan -> Act -> Reflect

The architecture extends an LLM with three capabilities that together create believable autonomous behavior:

```
[Environment] --> PERCEIVE --> [Memory Stream] --> RETRIEVE --> PLAN --> ACT
                                    ^                            |
                                    |                            v
                                    +-------- REFLECT <----------+
```

**Perceive**: Agent continuously observes the world. Each observation enters the memory stream as a natural language record with a timestamp, e.g., "Klaus Mueller is reading a book on the park bench."

**Plan**: Top-down recursive decomposition:
- **Day level**: 5-8 broad objectives generated from agent persona + previous day summary
- **Hour level**: Each objective decomposed into hour-long activities
- **Granular level**: Further decomposed into 5-15 minute action chunks

**Act**: Execute the current 5-15 minute action chunk. Continuously check whether to follow the plan or react to something new.

**Reflect**: Triggered when accumulated importance scores of recent memories exceed a threshold (roughly 2-3 times per day). Two-step process:
1. LLM analyzes the 100 most recent memories to identify the **3 most salient high-level questions**
2. LLM generates **5 high-level insights** with citations to supporting memories

### 1.2 Memory Stream (The Key Innovation)

Every experience is stored as a natural language record. Retrieval uses a **composite score** of three factors:

| Factor | How It Works | Score Range |
|--------|-------------|-------------|
| **Recency** | Exponential decay based on last access time | 0.0 - 1.0 |
| **Importance** | LLM rates 1-10 (1 = brushing teeth, 10 = divorce) | Normalized 0.0 - 1.0 |
| **Relevance** | Cosine similarity between query embedding and memory embedding | 0.0 - 1.0 |

**Final score** = recency + importance + relevance (equal weighting, summed)

Only highest-scoring memories fitting within the LLM's context window are retrieved.

### 1.3 Reactive Replanning

When an agent perceives something unexpected:
1. LLM evaluates: "Should I continue my plan or react?"
2. Considers relationship context: "What is [agent]'s relationship with [observed entity]?"
3. If reaction warranted: generates new dialogue/action using retrieved memories from all parties
4. Plan is updated; old plan nodes are replaced

### 1.4 Key Takeaway for Golem

The recursive plan decomposition is directly applicable: the AI server can generate a day plan, decompose to hours, then to 5-minute action chunks sent via WebSocket. The agent on the Unity side only needs to execute the current chunk and report completion. The memory and reflection loop runs server-side.

**Critical insight**: Plans stored as natural language in the memory stream means the agent considers observations, reflections, AND plans together when making decisions. Plans are not a separate rigid system -- they are just memories with planning intent.

---

## 2. BML (Behavior Markup Language)

**Source**: [SAIBA Framework](https://projects.cs.ru.is/projects/behavior-markup-language/wiki) | [Springer: BML Recent Developments](https://link.springer.com/chapter/10.1007/978-3-540-74997-4_10)

### 2.1 Core Concept: Multi-Channel Behavior Coordination

BML is an XML-based language for controlling verbal and nonverbal behavior of virtual characters. Its key innovation is **sync points** -- named temporal anchors within behaviors that other behaviors can reference.

### 2.2 Sync Points

Every behavior has phases bounded by sync points:

```
[start] --> [ready] --> [strokeStart] --> [stroke] --> [strokeEnd] --> [relax] --> [end]
```

- **start/end**: Universal on all behaviors
- **ready**: Preparation phase complete (hand raised, ready to wave)
- **stroke**: The meaningful peak of the action (the wave itself)
- **relax**: Return to neutral

### 2.3 Simultaneous Behavior Specification

Behaviors in the same BML block are resolved together before execution:

```xml
<bml id="bml1">
  <speech id="speech1">
    <text>Look over there!</text>
  </speech>
  <gesture id="gesture1" type="POINT" target="blueBox"
           start="speech1:stroke"/>
  <gaze id="gaze1" target="blueBox"
        start="speech1:strokeStart"/>
  <head id="head1" type="NOD"
        start="speech1:end" end="speech1:end+0.5"/>
</bml>
```

This means: gaze shifts to the box just before the key word, the pointing gesture starts at the key word, and a nod follows after speaking.

### 2.4 Composition Modes

When a new BML block arrives while a previous one is still executing:

| Mode | Behavior |
|------|----------|
| **MERGE** (default) | New behaviors layer on top; conflicts favor earlier block |
| **APPEND** | New block waits for all previous blocks to complete |
| **REPLACE** | Terminates all prior blocks, resets to neutral |

### 2.5 Persistent vs. Temporary Behaviors

- **Temporary** (`<posture>`, `<gaze>`): Character reverts to ground state after completion
- **Persistent** (`<postureShift>`, `<gazeShift>`): Creates a new ground state

### 2.6 Key Takeaway for Golem

We do not need to implement BML literally. But the **design principles** are gold:
- **Sync points**: Define key moments in each action (preparation, peak, recovery) and let other behaviors anchor to them
- **Composition modes**: MERGE/APPEND/REPLACE maps directly to our ActionBus/ActionQueue system
- **Multi-channel**: Separate body (locomotion), head (gaze), hands (gestures), face (expressions) as independent but synchronizable channels

---

## 3. Natural Locomotion

### 3.1 Acceleration/Deceleration Curves

Real humans do not go from 0 to walk speed instantly. The key is **ease-in/ease-out**:

```
Speed
  |        _______________
  |      /                \
  |    /                    \
  |  /                        \
  |/____________________________\___
  Time
  ^accel    ^cruise      ^decel
```

**Implementation**: Use `AnimationCurve` in Unity to define custom S-curves for NavMeshAgent speed ramping. Typical values:
- Standing start to walk: 0.3-0.5 seconds
- Walk to stop: 0.2-0.4 seconds (slightly faster than starting, humans brake quicker)
- Walk to run: 0.5-0.8 seconds

```csharp
[SerializeField] AnimationCurve accelerationCurve; // ease-in-out
float targetSpeed;
float currentSpeedT; // 0..1 parameter on curve

void Update() {
    currentSpeedT = Mathf.MoveTowards(currentSpeedT,
        targetSpeed > 0 ? 1f : 0f,
        Time.deltaTime / accelerationTime);
    agent.speed = accelerationCurve.Evaluate(currentSpeedT) * maxSpeed;
}
```

### 3.2 Head/Body Anticipation

**The Principle**: Humans look before they turn. Eyes move first (50-100ms), then head (200-300ms), then body follows.

**Implementation approach**:
1. When a new NavMesh waypoint requires a direction change > 15 degrees:
   - Frame 0: Eyes shift (via blend shape or bone rotation)
   - Frame ~6 (100ms): Head begins turning toward target (Animation Rigging Multi-Aim)
   - Frame ~15 (250ms): Body begins following (NavMeshAgent rotation)
2. Use `NavMeshAgent.steeringTarget` to get the next corner point and pre-rotate the head toward it

### 3.3 Foot IK and Ground Adaptation

Two levels of complexity:

**Basic (recommended first)**: Use Animation Rigging's Two Bone IK constraint on each leg. Raycast from hip down to find ground contact point. Adjust foot bone target position to match terrain height. Align foot rotation to terrain normal.

**Advanced**: Full foot placement system with:
- Heel-strike and toe-off detection based on animation clip events
- Slope compensation (tilt the pelvis based on the slope angle under each foot)
- Stair step alignment (snap feet to step edges)

### 3.4 Speed Variation While Walking

Humans do not walk at a perfectly constant speed. Add subtle variation:

```csharp
float baseSpeed = 1.4f; // m/s typical human walk
float speedNoise = Mathf.PerlinNoise(Time.time * 0.3f, seed) * 0.1f;
float contextualModifier = isThinking ? 0.85f : isExcited ? 1.15f : 1.0f;
agent.speed = (baseSpeed + speedNoise) * contextualModifier;
```

This gives a +/- 7% natural variation plus emotional context modifiers.

---

## 4. Idle Behaviors That Feel Alive

### 4.1 Weight Shifting

Real humans never stand perfectly still. They shift weight between feet every 5-15 seconds.

**Implementation**: An additive animation layer or procedural hip offset:
```csharp
float weightShiftCycle = Mathf.Sin(Time.time * 0.4f) * 0.03f; // slow, subtle
hipBone.localPosition += new Vector3(weightShiftCycle, 0, 0);
```

### 4.2 Looking Around

Idle characters should periodically glance at interesting things:

- **Frequency**: Every 3-8 seconds (randomized)
- **Duration**: Hold gaze 0.5-2.0 seconds
- **Priority**: Other characters > moving objects > random directions
- **Weight**: Never snap; always interpolate the head look weight over 0.3s

```csharp
IEnumerator IdleLookAround() {
    while (true) {
        yield return new WaitForSeconds(Random.Range(3f, 8f));
        Vector3 lookTarget = PickInterestPoint();
        float holdTime = Random.Range(0.5f, 2.0f);
        yield return SmoothLookAt(lookTarget, 0.3f); // blend in
        yield return new WaitForSeconds(holdTime);
        yield return SmoothLookAt(forward, 0.4f); // blend out
    }
}
```

### 4.3 Micro-Gestures

Small fidget animations played on an additive layer at random intervals:
- Subtle face touch / scratch (every 30-90 seconds)
- Hand position adjustment
- Slight posture correction
- Head tilt shift

**Implementation**: A pool of 5-8 short (1-2 second) additive animation clips. Play one randomly every 20-60 seconds on an upper body override layer.

### 4.4 Breathing Animation

The single most important "alive" signal. A subtle chest/shoulder rise and fall:

**Option A (Procedural)**:
```csharp
float breathCycle = Mathf.Sin(Time.time * 0.8f); // ~12 breaths/min at rest
spineBone.localRotation *= Quaternion.Euler(breathCycle * 0.5f, 0, 0);
shoulderBone.localPosition += Vector3.up * breathCycle * 0.002f;
```

**Option B (Additive Animation)**: A looping 4-second breathing clip on an additive layer with 0.3-0.5 weight. More natural, less code.

### 4.5 Blink

Often overlooked but immediately noticeable when absent:
- Average blink rate: 15-20 per minute
- Blink duration: 100-400ms
- Also blink on gaze shifts and at speech pauses

---

## 5. Transition Smoothness

### 5.1 Animation Blend Trees

Standard 1D blend tree for locomotion:

```
Parameter: Speed (0.0 - 1.0 - 2.0)

0.0  = Idle
0.5  = Walk (slow)
1.0  = Walk (normal)
1.5  = Jog
2.0  = Run
```

**Critical requirements for natural blending**:
- All clips in the tree must have matching foot cycles (e.g., all start with right foot forward)
- All clips should be normalized to the same loop length
- Use foot phase sync to prevent foot sliding during blends

For directional movement, use a 2D Freeform blend tree with velocityX and velocityZ parameters.

### 5.2 Procedural Head Look-At with IK

Animation Rigging's Multi-Aim constraint on the head bone:

```
Rig Builder
  └── Head Look Rig
       └── Multi-Aim Constraint
            Constrained Object: Head bone
            Source Objects: [LookTarget (empty GameObject)]
            Aim Axis: Z (forward)
            Weight: controlled via script (0..1)
```

**Smooth weight transition**:
```csharp
float targetWeight = hasPointOfInterest ? 0.7f : 0f;
rig.weight = Mathf.SmoothDamp(rig.weight, targetWeight, ref velocity, 0.3f);
```

Note: Use 0.7 max weight, not 1.0. Full head lock looks unnatural -- humans never fully lock their head onto a target while their body continues other motion.

### 5.3 Natural Pauses Between Actions

**This is one of the most underrated techniques**. Real humans pause briefly between activities:

| Transition | Natural Pause |
|-----------|---------------|
| Arrive at location -> Start action | 0.5 - 1.5 seconds |
| Finish speaking -> Start walking | 0.3 - 0.8 seconds |
| Observe something -> React | 0.2 - 0.6 seconds |
| Complete one task -> Start next | 1.0 - 3.0 seconds |

**Implementation**: In the ActionQueue, insert a `WaitAction` with randomized duration between substantive actions:

```csharp
public class ThinkPauseAction : IAgentAction {
    float duration;
    public ThinkPauseAction(float min = 0.5f, float max = 1.5f) {
        duration = Random.Range(min, max);
    }
    // During pause: play subtle "thinking" idle variation
}
```

During these pauses, the character should display "thinking" micro-behaviors: slight gaze shift, weight transfer, maybe a small gesture. Never just freeze.

---

## 6. Unity-Specific Techniques

### 6.1 Animation Rigging Package

**Package**: `com.unity.animation.rigging` (version 1.2+ for Unity 6)

Key constraints to use:

| Constraint | Use Case |
|-----------|----------|
| **Multi-Aim** | Head/eye tracking toward points of interest |
| **Two Bone IK** | Foot placement on terrain, hand reaching for objects |
| **Multi-Position** | Subtle procedural offsets (weight shifting) |
| **Damped Transform** | Smooth follow for secondary motion (hair, accessories) |
| **Override Transform** | Direct bone manipulation for micro-gestures |

**Setup pattern**:
```
Character
  ├── Animator (with base animation controller)
  ├── Rig Builder
  │    ├── HeadLookRig (Multi-Aim on head + Multi-Aim on eyes)
  │    ├── FootIKRig (Two Bone IK on each leg)
  │    └── HandRig (Two Bone IK on each arm, for object interaction)
  └── NavMeshAgent
```

All rigs can have independent weights, enabling smooth blending between procedural IK and authored animations.

### 6.2 NavMeshAgent + Animation Synchronization

**The fundamental choice**: Root Motion vs. Agent-Driven.

**Agent-Driven (Recommended for AI Characters)**:
- NavMeshAgent controls position and rotation
- Animator parameters are set from agent velocity
- Pro: Reliable pathfinding, no desync
- Con: Possible foot sliding if not tuned

```csharp
void Update() {
    // Feed NavMeshAgent velocity to Animator
    Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);
    animator.SetFloat("VelocityX", localVelocity.x, 0.1f, Time.deltaTime);
    animator.SetFloat("VelocityZ", localVelocity.z, 0.1f, Time.deltaTime);
    animator.SetFloat("Speed", agent.velocity.magnitude, 0.1f, Time.deltaTime);
}
```

**Root Motion (Higher quality, more complex)**:
- Animation drives movement via `OnAnimatorMove()`
- NavMeshAgent only provides direction
- Pro: Zero foot sliding, animation-perfect motion
- Con: Must handle NavMesh boundary correction

```csharp
void OnAnimatorMove() {
    Vector3 position = animator.rootPosition;
    position.y = agent.nextPosition.y; // keep on NavMesh height
    agent.nextPosition = position;
    transform.rotation = animator.rootRotation;
}
```

**Hybrid approach** (best of both worlds): Use agent-driven for long-distance travel, blend to root motion for the last 2 meters of approach and for close-quarters actions. Track desync distance and correct gradually.

### 6.3 Root Motion vs Agent-Driven: Decision Matrix

| Scenario | Recommended | Why |
|----------|------------|-----|
| Walking to a distant waypoint | Agent-driven | Reliable path following |
| Approaching a character to talk | Root motion | Natural deceleration and stop |
| Sitting down in a chair | Root motion | Precise alignment needed |
| Running away from danger | Agent-driven | Speed/responsiveness matters |
| Idle in place | Neither | Procedural + additive layers |

---

## 7. TOP 10 Most Impactful Techniques

### Ordered by Impact-to-Effort Ratio (highest first)

---

### #1. Procedural Head Look-At (IK)
**Impact**: 9/10 | **Effort**: 2/10 | **Ratio**: 4.5

Characters that track nearby entities with their gaze feel dramatically more alive. A character staring blankly forward while someone walks past is immediately perceived as robotic.

**What to implement**:
- Animation Rigging Multi-Aim constraint on head bone
- Script that picks the most interesting nearby target (other agents, player, relevant objects)
- Smooth weight interpolation (0 to 0.6-0.7, never 1.0)
- Natural gaze shifts every 3-8 seconds during idle

**Why highest ratio**: One component, one constraint, a small target-picking script, and the character instantly appears aware and present. Takes about 2 hours to set up.

---

### #2. Natural Pauses Between Actions ("Think Time")
**Impact**: 8/10 | **Effort**: 1/10 | **Ratio**: 8.0

The difference between a robot and a person is that a person takes a moment before doing the next thing. Insert randomized 0.3-2.0 second pauses between actions in the queue. During pauses, play subtle idle variations.

**What to implement**:
- `ThinkPauseAction` in the action queue with randomized duration
- During pause: weight shift, slight gaze movement, maybe a blink
- Context-sensitive timing (shorter pauses for urgent reactions, longer for contemplation)

**Why so high ratio**: Literally adding `yield return new WaitForSeconds(Random.Range(0.5f, 1.5f))` between actions transforms behavior from robotic to considered. Near-zero implementation cost.

---

### #3. Breathing Animation (Additive Layer)
**Impact**: 7/10 | **Effort**: 1/10 | **Ratio**: 7.0

A 4-second looping breathing clip on an additive animator layer with 0.3-0.5 weight. Or a procedural spine/shoulder oscillation via script. This is the most fundamental "alive" signal.

**What to implement**:
- One additive animation layer in the Animator Controller
- One short breathing loop clip (or procedural sine wave on spine bone)
- Variable rate: 12 breaths/min at rest, 20+ after physical activity

**Why so high ratio**: A single clip or 5 lines of procedural code. Visible immediately. Without it, characters look dead even if everything else is perfect.

---

### #4. Locomotion Blend Tree (Idle/Walk/Run)
**Impact**: 8/10 | **Effort**: 3/10 | **Ratio**: 2.7

A 1D blend tree that smoothly interpolates between idle, walk, and run based on NavMeshAgent velocity magnitude. This eliminates the jarring hard-switch between standing and moving.

**What to implement**:
- 1D Blend Tree: Speed parameter driving idle -> walk -> run
- Feed `agent.velocity.magnitude` into the Speed parameter with smoothing (`Mathf.SmoothDamp`)
- Ensure animation clips have matching foot cycles
- For directional movement: 2D Freeform Directional blend tree

**Why this rank**: Fundamental requirement, but needs animation clips and some tuning. Still moderate effort for a must-have result.

---

### #5. Acceleration/Deceleration Curves
**Impact**: 7/10 | **Effort**: 2/10 | **Ratio**: 3.5

Characters that start and stop with natural easing feel dramatically more physically present. Replace instant speed changes with `AnimationCurve`-based ramping.

**What to implement**:
- `AnimationCurve` field on the movement controller
- S-curve (ease-in-out) for acceleration: 0.3-0.5 second ramp
- Slightly faster deceleration: 0.2-0.4 seconds
- Emotional modifiers: excited = faster accel, thoughtful = slower

**Why this rank**: Simple `AnimationCurve.Evaluate()` call replaces `agent.speed = targetSpeed`. Huge visual improvement for a few lines of code.

---

### #6. Idle Look-Around + Weight Shifting
**Impact**: 7/10 | **Effort**: 3/10 | **Ratio**: 2.3

Standing characters that periodically look at interesting things and shift their weight feel present in the world. Without this, idle = dead.

**What to implement**:
- Coroutine-based idle behavior system
- Look at interest points (other characters, moving objects) every 3-8s
- Weight shift via subtle hip displacement every 8-15s
- Random micro-gestures from a clip pool every 30-60s

**Why this rank**: More code than breathing alone, requires interest-point detection, but transforms idle from "frozen" to "living."

---

### #7. Stanford-Style Recursive Plan Decomposition
**Impact**: 9/10 | **Effort**: 6/10 | **Ratio**: 1.5

The AI server generates a day plan, decomposes to hours, then to 5-15 minute action chunks. Creates the feeling of a character with intentions, routines, and a life.

**What to implement** (server-side):
- Day plan prompt: "Given [agent persona] and [yesterday summary], create 5-8 objectives for today"
- Hour decomposition prompt: "Break objective X into hourly activities"
- Minute decomposition prompt: "Break [hour activity] into 5-15 minute action steps"
- Store plan in memory stream alongside observations
- Reactive replanning when significant events occur

**Why this rank**: Massive impact on perceived intelligence and believability. Higher effort because it requires prompt engineering, memory management, and integration between AI server and Unity client. But the payoff is the difference between a prop and a character.

---

### #8. NavMeshAgent + Animation Sync (Hybrid Approach)
**Impact**: 7/10 | **Effort**: 4/10 | **Ratio**: 1.75

Agent-driven for travel, root motion for the last 2 meters and interactions. Eliminates both foot sliding and pathfinding desync.

**What to implement**:
- Agent-driven locomotion as default with velocity-fed blend tree
- Root motion switch for approach and interaction sequences
- `OnAnimatorMove()` override that syncs root position to NavMesh
- Gradual correction for desync (lerp agent position toward animator position)

**Why this rank**: Requires more sophisticated switching logic, but eliminates the two most common visual artifacts: foot sliding (agent-driven) and path deviation (root motion).

---

### #9. Foot IK Ground Adaptation
**Impact**: 6/10 | **Effort**: 5/10 | **Ratio**: 1.2

Characters whose feet properly contact uneven terrain, stairs, and slopes feel grounded in the world. Without it, feet hover or clip through surfaces.

**What to implement**:
- Two Bone IK constraints on each leg via Animation Rigging
- Raycast from hips to find ground contact per foot
- Adjust IK target position to terrain height
- Rotate foot to match surface normal
- Adjust pelvis height to compensate for leg extension differences

**Why this rank**: Significant visual improvement but requires raycast setup, IK tuning, and careful handling of edge cases (stairs, ledges). Worth doing after higher-ratio items are solid.

---

### #10. BML-Inspired Multi-Channel Behavior System
**Impact**: 8/10 | **Effort**: 7/10 | **Ratio**: 1.14

Separate behavior channels (locomotion, gaze, gesture, speech, face) that can run simultaneously with sync-point coordination. This is what makes a character walk AND look AND gesture naturally.

**What to implement**:
- Behavior channel system: Body, Head, Hands, Face, Voice
- Each channel has its own queue/state and can be independently controlled
- Sync points: "start gesture at speech stroke" type references
- Composition modes: MERGE (default), APPEND, REPLACE
- Action definitions include channel assignments and sync references

**Why this rank**: The most architecturally complex item, requiring a full behavior scheduling system. But it is the foundation for truly natural multi-modal behavior. Without it, actions are sequential rather than overlapping -- characters do one thing at a time, which is distinctly non-human.

---

## Summary: Implementation Priority Roadmap

### Phase A: "Not Dead" (Week 1) - Items #1, #2, #3
Get breathing, head look, and pauses working. The character goes from mannequin to "something alive is in there." These three alone are a transformative improvement for minimal effort.

### Phase B: "Moves Like a Person" (Week 2) - Items #4, #5, #6
Locomotion blend tree, acceleration curves, idle behaviors. Movement becomes smooth and organic instead of robotic sliding.

### Phase C: "Has a Mind" (Week 3-4) - Item #7
Recursive plan decomposition on the AI server. The character now has daily routines, goals, and reactive replanning. It feels like it has intentions.

### Phase D: "Fully Embodied" (Week 5-6) - Items #8, #9, #10
Hybrid motion sync, foot IK, and multi-channel behavior coordination. The character is now a convincing physical presence that can walk, look, gesture, and speak simultaneously.

---

## Key Design Principles (Cross-Cutting)

1. **Never 1.0, Never 0.0**: IK weights, blend parameters, and volumes should live in the 0.3-0.8 range. Full-on or full-off looks mechanical.

2. **Randomize Everything Slightly**: Add +/- 10-20% randomness to all timings, speeds, and pause durations. Perfect regularity is the enemy of believability.

3. **Anticipation Before Action**: Eyes before head, head before body. Announce the next action with a preparatory micro-movement.

4. **Stylize Over Simulate**: You do not need physically accurate motion. You need motion that *reads* as natural. Slightly exaggerated weight shifts and gaze movements are better than physically correct but imperceptible ones.

5. **Layer, Don't Replace**: Additive animation layers let you stack breathing + idle fidgets + gaze + base animation. Each layer adds life without replacing the others.

6. **The 2-Second Rule**: If a character is visibly doing nothing for more than 2 seconds, something is wrong. There should always be at least breathing + subtle gaze activity.

---

## Sources

- [Stanford Generative Agents Paper (arXiv)](https://arxiv.org/abs/2304.03442)
- [Stanford HAI: Computational Agents Exhibit Believable Behavior](https://hai.stanford.edu/news/computational-agents-exhibit-believable-humanlike-behavior)
- [Generative Agents Review (Gonzo ML)](https://gonzoml.substack.com/p/generative-agents-interactive-simulacra)
- [BML Wiki (Reykjavik University)](https://projects.cs.ru.is/projects/behavior-markup-language/wiki)
- [BML Specification (Springer)](https://link.springer.com/chapter/10.1007/978-3-540-74997-4_10)
- [SmartBody & BML (Meta-Guide)](https://meta-guide.com/embodiment/smartbody-bml-behavior-markup-language)
- [Unity Animation Rigging Docs](https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.3/manual/index.html)
- [Unity Multi-Aim Constraint](https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.1/manual/constraints/MultiAimConstraint.html)
- [Unity Animation Blend Trees](https://docs.unity3d.com/6000.2/Documentation/Manual/class-BlendTree.html)
- [Unity Coupling Animation and Navigation](https://docs.unity3d.com/Packages/com.unity.ai.navigation@1.1/manual/CouplingAnimationAndNavigation.html)
- [NavMeshAgent Root Motion Tutorial (Llama Academy)](https://github.com/llamacademy/ai-series-part-42)
- [Animation Rigging IK Tutorial (2025)](https://generalistprogrammer.com/tutorials/unity-animation-rigging-complete-character-ik-tutorial)
- [Procedural Animation Techniques (Wayline)](https://www.wayline.io/blog/procedural-animation-techniques)
- [12 Principles of Animation in Games (Game Anim)](https://www.gameanim.com/2019/05/15/the-12-principles-of-animation-in-video-games/)
- [Motion Matching for Unity (GitHub)](https://github.com/JLPM22/MotionMatching)
- [Idle Animation Tips (garagefarm)](https://garagefarm.net/blog/idle-animation-tips-to-animate-your-characters)
- [Escaping the Uncanny Valley (Wayline)](https://www.wayline.io/blog/escaping-the-uncanny-valley-crafting-believable-ai)
- [Unity HeightMesh Documentation](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/HeightMesh.html)
- [F.E.A.R. GOAP AI (Gamedeveloper)](https://www.gamedeveloper.com/design/building-the-ai-of-f-e-a-r-with-goal-oriented-action-planning)
