# Project Structure

## Scripts

```
Assets/Scripts/
├── BubbleSpinner/                        # Standalone dialogue engine — no Unity UI dependencies
│   ├── Core/
│   │   ├── IBubbleSpinnerCallbacks.cs    # Interface — save, load, events contract for the bridge
│   │   ├── BubbleSpinnerParser.cs        # Parses .bub text files into structured dialogue data
│   │   ├── DialogueExecutor.cs           # Executes parsed nodes — fires message, choice, media events
│   │   └── ConversationManager.cs        # Manages active conversations, executor lifecycle, cache
│   └── Data/
│       ├── MessageData.cs                # Data model for a single message (sender, text, type)
│       ├── ConversationAsset.cs          # ScriptableObject — character data, chapters, CG keys
│       └── CharacterDatabase.cs          # ScriptableObject — registry of all ConversationAssets
│
└── ChatSim/
    ├── Core/
    │   ├── AddressablesImageLoader.cs    # Async CG image loader via Addressables
    │   ├── BubbleSpinnerBridge.cs        # Implements IBubbleSpinnerCallbacks — save/load/reset
    │   ├── GameBootstrap.cs              # Entry point — initializes all managers, loads first scene
    │   ├── GameEvents.cs                 # Static event bus — decoupled cross-system communication
    │   ├── PlayerPrefKeys.cs             # Shared PlayerPrefs key constants and default values.
    │   ├── SaveManager.cs                # Reads and writes save data to disk with backup recovery
    │   ├── SceneFlowManager.cs           # Handles scene transitions and load validation
    │   └── SceneNames.cs                 # String constants for all scene names
    │
    ├── Data/
    │   ├── GameConfig.cs
    │   └── SaveData.cs                   # Serializable save data model — conversation states, CG unlocks
    │
    └── UI/
        ├── ChatApp/
        │   ├── Components/
        │   │   ├── TextMessageBubble.cs          # Text bubble — auto-resizes to content
        │   │   ├── ImageMessageBubble.cs          # CG image bubble — tap to open fullscreen viewer
        │   │   └── ChoiceButton.cs                # Individual choice button — fires selection event on click
        │   ├── Controllers/
        │   │   ├── ChatAppController.cs           # Main controller — receives engine events, drives UI
        │   │   ├── ChatAutoScroller.cs            # Scrolls chat to latest message automatically
        │   │   ├── ChatChoiceSpawner.cs           # Spawns and clears choice buttons from pool
        │   │   ├── ChatMessageSpawner.cs          # Spawns message bubbles from pool into chat content
        │   │   └── ChatTimingController.cs        # Queues messages with typing indicator delays
        │   ├── Panels/
        │   │   ├── ContactListPanel.cs            # Populates and displays the contact list
        │   │   └── ContactListItem.cs             # Individual contact row — opens conversation on tap
        │   ├── ChatAppNavButtons.cs               # Navigation bar buttons for the chat app scene
        │   └── FullscreenCGViewer.cs              # Fullscreen overlay for tapped CG images
        │
        ├── Common/
        │   └── Components/
        │       ├── AutoResizeText.cs              # Resizes TMP bubble width to fit text content
        │       ├── PooledObject.cs                # Marks a GameObject as poolable — handles return logic
        │       └── PoolingManager.cs              # Object pool — spawns and recycles UI prefabs
        │
        ├── HomeScreen/
        │   ├── Contacts/
        │   │   ├── ContactsAppDetails.cs          # Character detail view — profile, bio, traits (future)
        │   │   ├── ContactsAppItems.cs            # Individual contact row in the contacts app
        │   │   ├── ContactsAppPanels.cs           # Manages contacts panel display and population
        │   │   └── ResetConfirmationDialog.cs     # Confirmation dialog before resetting a character story
        │   ├── Gallery/
        │   │   ├── GalleryController.cs           # Populates gallery grid from save data CG unlocks
        │   │   ├── GalleryFullscreenViewer.cs     # Fullscreen CG viewer for the gallery panel
        │   │   └── GalleryThumbnailItems.cs       # Individual CG thumbnail — locked/unlocked state
        │   ├── Setting/
        │   │   └── SettingPanel.cs
        │   ├── HomeScreenController.cs            # Home screen app launcher — opens scenes or panels
        │   └── HomeScreenNavButtons.cs            # Navigation bar buttons for the phone home screen
        │
        ├── Overlay/
        │   ├── Dialog/
        │       └── ResetConfirmationDialog.cs
        │
        └── Screens/
            ├── DisclaimerScreen.cs                # First-launch TOS screen — skipped after acceptance
            └── LockScreen.cs                      # Lock screen entry point — tap to unlock
```