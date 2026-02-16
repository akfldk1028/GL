# GolemBootstrap Unit Tests

This directory contains Unity Test Framework tests for the Golem AI Agent system.

## Test Structure

- **PlayMode/** - Tests that run in Play Mode (simulate runtime behavior)
  - `GolemBootstrapTests.cs` - Tests for GolemBootstrap singleton pattern and initialization

## Running Tests

### Option 1: Unity Test Runner (Recommended)

1. Open Unity Editor
2. Go to **Window > General > Test Runner**
3. Click on the **PlayMode** tab
4. Click **Run All** to run all PlayMode tests
5. Individual tests can be run by clicking on them and pressing **Run Selected**

### Option 2: Command Line

```bash
# Run all PlayMode tests
Unity.exe -runTests -batchmode -projectPath . -testPlatform PlayMode -testResults ./TestResults.xml

# Windows example:
"C:\Program Files\Unity\Hub\Editor\6000.3.3f1\Editor\Unity.exe" -runTests -batchmode -projectPath "C:\DK\GL\Golem" -testPlatform PlayMode -testResults "C:\DK\GL\Golem\TestResults.xml"
```

## Test Coverage

### GolemBootstrapTests

1. **SingletonPattern_PreventsDuplicateInitialization**
   - Verifies that creating two GolemBootstrap instances results in only one active instance
   - Checks that manager GameObjects are created only once

2. **SingletonPattern_LogsDestructionMessage**
   - Verifies that duplicate bootstrap instances log the expected destruction message

3. **SingletonPattern_OnlyOneDebugOverlayCreated**
   - Verifies that only one GolemDebugOverlay is created even with multiple bootstrap attempts
   - Tests conditional compilation (dev builds vs release builds)

4. **InitializationSequence_CreatesAllRequiredGameObjects**
   - Verifies that all required GameObjects are created (@Managers, @AINetworkManager)
   - Checks that singletons are accessible (Managers.Instance, AINetworkManager.Instance)

5. **InitializationSequence_LogsAllFiveSteps**
   - Verifies that all 5 initialization steps are logged correctly
   - Checks for proper log sequence including system start/ready messages
   - Validates Step 1-5 log messages and state machine entry log
   - Tests conditional compilation for debug overlay log message

6. **ManagersInstance_AllPropertiesAccessibleAfterBootstrap**
   - Verifies that all Managers static properties are accessible without NullReferenceException
   - Tests Managers.ActionBus, StateMachine, Data, Pool, Resource, and Agent properties
   - Validates that Managers.Initialized is true after bootstrap
   - Confirms basic functionality (publishing actions, checking current state)
   - Ensures the complete Managers singleton is properly initialized

7. **InitialState_IsSetToBoot**
   - Verifies that the initial state is set to StateId.Boot after bootstrap completes
   - Checks that the StateMachine logs "[StateMachine] Entered state: Boot"
   - Validates that Managers.CurrentStateId returns Boot immediately after initialization
   - Ensures the state machine is operational and can be queried
   - Directly tests the requirement: "Set initial state to StateId.Boot"

8. **AINetworkManager_CreatedAndPersistsAcrossSceneLoads**
   - Verifies that @AINetworkManager GameObject is created during bootstrap initialization
   - Validates that AINetworkManager component exists on the GameObject
   - Checks that AINetworkManager.Instance is accessible after bootstrap
   - Simulates scene load/transition using SceneManager
   - Verifies that @AINetworkManager persists after scene load (DontDestroyOnLoad)
   - Confirms the same instance persists (not recreated) by comparing InstanceIDs
   - Validates that the GameObject is moved to the "DontDestroyOnLoad" scene
   - Directly tests the integration requirement: "Test @AINetworkManager creation and persistence"

9. **GolemDebugOverlay_CreatedInDevBuildsAndF12Toggle**
   - Verifies that @GolemDebugOverlay GameObject is created during bootstrap (dev builds only)
   - Validates that GolemDebugOverlay component exists on the GameObject
   - Tests initial visibility state (_visible = false by default)
   - Simulates F12 key toggle using reflection to change _visible field
   - Verifies overlay can be toggled on (visible = true) and off (visible = false)
   - Checks that overlay subscribes to ActionBus for message logging
   - Validates overlay can access required properties (AINetworkManager.IsConnected, Managers.CurrentStateId, Managers.Agent.Count)
   - Tests conditional compilation (debug overlay created in dev builds, NOT in release builds)
   - Directly tests the integration requirement: "Test @GolemDebugOverlay creation (dev builds only)"

10. **StateRegistration_AllSevenStatesRegistered**
    - Verifies that all 7 required states are registered in the StateMachine after bootstrap
    - Uses reflection to access the private _states dictionary in StateMachine
    - Validates that each of the 7 states is registered: Boot, Initializing, Connected, Disconnected, Active, Idle, Performing
    - Confirms the total state count is exactly 7
    - Verifies current state is Boot after initialization
    - Tests state transitions to each of the 7 states to confirm they are functional
    - Validates that state transitions log proper entry/exit messages
    - Ensures the state machine is operational and all states can be accessed
    - Directly tests the integration requirement: "Test state registration (all 7 states)"

11. **AgentConnected_StateTransitionFromBootToConnected**
    - Verifies that publishing Agent_Connected action triggers a state transition from Boot to Connected
    - Validates initial state is Boot after bootstrap initialization
    - Publishes ActionId.Agent_Connected via Managers.PublishAction to simulate CFConnector connection
    - Uses LogAssert to verify proper exit/entry log messages: "[StateMachine] Exited state: Boot" and "[StateMachine] Entered state: Connected"
    - Confirms current state is Connected after Agent_Connected action is processed
    - Verifies state machine is operational after transition (can query current state)
    - Tests that debug overlay would display correct state (State: Connected)
    - Directly tests the integration requirement: "Test Agent_Connected state transition"
    - Simulates the full connection flow: Bootstrap → Boot state → Agent_Connected action → Connected state

12. **AgentDisconnected_StateTransitionFromConnectedToDisconnected**
    - Verifies that publishing Agent_Disconnected action triggers a state transition from Connected to Disconnected
    - First establishes Connected state by publishing Agent_Connected action (prerequisite)
    - Validates state successfully transitions to Connected before testing disconnection
    - Publishes ActionId.Agent_Disconnected via Managers.PublishAction to simulate disconnection
    - Uses LogAssert to verify proper exit/entry log messages: "[StateMachine] Exited state: Connected" and "[StateMachine] Entered state: Disconnected"
    - Confirms current state is Disconnected after Agent_Disconnected action is processed
    - Verifies state machine remains operational after disconnection transition
    - Tests that debug overlay would display correct state (State: Disconnected)
    - Directly tests the integration requirement: "Test Agent_Disconnected state transition"
    - Simulates the full disconnection flow: Boot → Connected → Agent_Disconnected action → Disconnected state

13. **E2E_FullSystemStartupSequence** (End-to-End Test)
    - **Comprehensive end-to-end verification of the complete Golem AI Agent system startup sequence**
    - Directly tests subtask-5-1: "Test full system startup sequence"
    - **Step 1: Play Mode Entry** - Simulates entering Play Mode from scratch by creating GolemBootstrap
    - **Step 2: Initialization Logs** - Verifies all 8 initialization log messages appear in correct order:
      - System initializing message
      - Step 1: Managers initialized
      - Step 2: AINetworkManager created
      - Step 3: Debug overlay created (or skipped in release builds)
      - Step 4: States registered
      - Step 5: Initial state set to Boot
      - System ready message
      - StateMachine entered Boot state
    - **Step 3: Hierarchy Verification** - Confirms all required GameObjects exist:
      - @Managers with Managers component initialized
      - @AINetworkManager with AINetworkManager component and accessible Instance
      - @GolemDebugOverlay in dev builds (absent in release builds)
    - **Step 4: Error-Free Startup** - Verifies no errors, warnings, or exceptions logged during initialization
    - **Step 5: State and Connection Verification** - Confirms initial system state:
      - Current state is Boot (debug overlay would show "State: Boot")
      - Connection status is DISCONNECTED (debug overlay would show "Connection: DISCONNECTED")
    - **Step 6: Functional Verification** - Tests all system components are operational:
      - All Managers properties accessible (ActionBus, StateMachine, Data, Pool, Resource, Agent)
      - ActionBus publish/subscribe functional (critical for CFConnector integration)
      - StateMachine state transitions functional
      - Agent_Connected state transition works (Boot → Connected)
      - Agent_Disconnected state transition works (Connected → Disconnected)
      - All persistent GameObjects remain (DontDestroyOnLoad verification)
    - Provides comprehensive coverage of all 6 verification requirements
    - Simulates the complete startup experience that existing Golem scripts (CFConnector, PointClickController, etc.) would encounter
    - Validates system is ready for runtime use and integration with existing codebase

14. **CFConnectorFlow_AINetworkManager_ToActionBus** (Integration Test)
    - **Comprehensive integration test verifying CFConnector → AINetworkManager → ActionBus communication flow**
    - Directly tests subtask-5-2: "Verify CFConnector → AINetworkManager → ActionBus flow"
    - **Step 1: System Initialization** - Creates GolemBootstrap to initialize all core systems
    - **Step 2: CFConnector Creation** - Creates CFConnector instance to simulate AI server connection
    - **Step 3: Event Subscription** - Waits for AINetworkManager to discover and subscribe to CFConnector events
    - **Step 4: ActionBus Listener Setup** - Sets up listener to verify Agent_Connected message propagation
    - **Step 5: Connection Simulation** - Simulates CFConnector.OnOpen event (connection established)
    - **Step 6: Log Verification** - Verifies AINetworkManager logs "[AINetworkManager] Connected to AI server"
    - **Step 7: ActionBus Message Verification** - Confirms ActionBus receives Agent_Connected message
    - **Step 8: State Machine Verification** - Verifies state machine transitions to Connected state
    - **Step 9: Connection Status Verification** - Confirms AINetworkManager.IsConnected is true
    - Tests complete integration chain:
      - CFConnector fires OnOpen event
      - AINetworkManager.HandleOpen() receives event via subscription
      - AINetworkManager logs connection message
      - AINetworkManager publishes Agent_Connected to ActionBus
      - ActionBus subscribers receive Agent_Connected message
      - StateMachine transitions to Connected state
      - AINetworkManager.IsConnected property reflects connected status
    - Uses reflection to access and invoke private events for testing
    - Simulates the exact flow that occurs when CFConnector connects to a real AI server
    - Validates debug overlay would receive correct state and connection information
    - Critical integration test for ensuring existing Golem scripts can react to AI agent connection events

## Manual Verification (if automated tests fail)

If automated tests fail or you need to manually verify the singleton pattern:

1. Open any scene in Unity Editor
2. Create two empty GameObjects named "Bootstrap1" and "Bootstrap2"
3. Add the `GolemBootstrap` component to both GameObjects
4. Enter Play Mode
5. Check the Console - you should see:
   ```
   [GolemBootstrap] === Golem AI Agent System Initializing ===
   [GolemBootstrap] Step 1: Managers initialized.
   [GolemBootstrap] Step 2: AINetworkManager created.
   [GolemBootstrap] Step 3: Debug overlay created (F12 to toggle).
   [GolemBootstrap] Step 4: States registered.
   [GolemBootstrap] Step 5: Initial state set to Boot.
   [GolemBootstrap] === Golem AI Agent System Ready ===
   [GolemBootstrap] Already bootstrapped. Destroying duplicate.
   ```
6. Check the Hierarchy - only one of each should exist:
   - @Managers
   - @AINetworkManager
   - @GolemDebugOverlay (in dev builds only)

## Notes

- Tests use reflection to reset the static `_bootstrapped` flag between test runs
- Tests clean up all created GameObjects in the TearDown phase
- PlayMode tests require entering Play Mode, so they take longer than EditMode tests
- All tests follow the Arrange-Act-Assert pattern for clarity

## Troubleshooting

**Issue: Tests don't appear in Test Runner**
- Solution: Make sure the `PlayMode.asmdef` file is present and Unity has recompiled the project

**Issue: Tests fail with NullReferenceException**
- Solution: Ensure all Phase 1-7 dependencies are present (22 files from previous phases)

**Issue: Tests fail with compilation errors**
- Solution: Check that Unity Test Framework package is installed (Window > Package Manager > Unity Test Framework)
