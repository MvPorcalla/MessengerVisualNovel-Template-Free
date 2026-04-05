// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/BubbleSpinner/Data/MessageData.cs
// Core data types for BubbleSpinner: messages, choices, dialogue nodes,
// pause points, resume targets, and serializable conversation state.
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;

namespace BubbleSpinner.Data
{
    // ═══════════════════════════════════════
    // MESSAGE DATA
    // ═══════════════════════════════════════

    [Serializable]
    public class MessageData
    {
        public enum MessageType { Text, Image, System }

        public MessageType type;
        public string speaker;
        public string content;
        public string imagePath;
        public string timestamp;
        public string messageId;    // Assigned later by parser; empty until then
        public bool shouldUnlockCG;

        /// <summary>
        /// Initializes a blank message with an empty ID and the current time as timestamp.
        /// </summary>
        public MessageData()
        {
            messageId = "";
            timestamp = DateTime.Now.ToString("HH:mm");
        }

        /// <summary>
        /// Initializes a message with the specified type, speaker, content, and optional image path.
        /// </summary>
        public MessageData(MessageType msgType, string msgSpeaker, string msgContent, string imgPath = "")
        {
            type           = msgType;
            speaker        = msgSpeaker;
            content        = msgContent;
            imagePath      = imgPath;
            timestamp      = DateTime.Now.ToString("HH:mm");
            messageId      = "";
            shouldUnlockCG = false;
        }

        /// <summary>
        /// Returns true if this message was sent by the player.
        /// Canonical speaker check — used by timing, spawning, and any future consumers.
        /// </summary>
        public bool IsPlayerMessage =>
            string.Equals(speaker, "player", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true if this message is a system message (timestamps, scene labels, etc).
        /// </summary>
        public bool IsSystemMessage =>
            string.Equals(speaker, "system", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true if this message was sent by an NPC.
        /// Any speaker that is not the player and not the system.
        /// </summary>
        public bool IsNpcMessage => !IsPlayerMessage && !IsSystemMessage;
    }

    // ═══════════════════════════════════════
    // JUMP TARGET
    // ═══════════════════════════════════════

    /// <summary>
    /// Represents a jump destination parsed from a <<jump>> command in a .bub file.
    ///
    /// Two cases:
    ///   isChapterJump = false  — local node jump within the same chapter file
    ///   isChapterJump = true   — cross-chapter jump, loads a new chapter file
    ///
    /// For chapter jumps, nodeName defaults to "Start" if not explicitly specified.
    /// </summary>
    [Serializable]
    public class JumpTarget
    {
        /// <summary>
        /// True if this jump crosses into another chapter file.
        /// False if this jump stays within the current chapter file.
        /// </summary>
        public bool isChapterJump;

        /// <summary>
        /// The chapter ID to load. Null or empty for local node jumps.
        /// Must match a ChapterEntry.chapterId in ConversationAsset.
        /// </summary>
        public string chapterId;

        /// <summary>
        /// The node to jump to. For local jumps this is the node name.
        /// For chapter jumps this defaults to "Start" if not specified.
        /// </summary>
        public string nodeName;

        /// <summary>
        /// Creates a local node jump targeting the given node in the current chapter.
        /// </summary>
        public static JumpTarget ToNode(string node)
        {
            return new JumpTarget
            {
                isChapterJump = false,
                chapterId     = null,
                nodeName      = node
            };
        }

        /// <summary>
        /// Creates a cross-chapter jump to the given chapter.
        /// Defaults to the "Start" node if no target node is specified.
        /// </summary>
        public static JumpTarget ToChapter(string chapter, string node = "Start")
        {
            return new JumpTarget
            {
                isChapterJump = true,
                chapterId     = chapter,
                nodeName      = string.IsNullOrEmpty(node) ? "Start" : node
            };
        }

        /// <summary>
        /// Returns true if this jump target has a valid destination.
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(nodeName) || !string.IsNullOrEmpty(chapterId);
    }

    // ═══════════════════════════════════════
    // CHOICE DATA
    // ═══════════════════════════════════════

    [Serializable]
    public class ChoiceData
    {
        public string choiceText;
        public JumpTarget jump;                 // replaces targetNode string
        public List<MessageData> preJumpMessages;

        /// <summary>
        /// Initializes a choice with empty text, no jump target, and an empty pre-jump message list.
        /// </summary>
        public ChoiceData()
        {
            preJumpMessages = new List<MessageData>();
        }

        /// <summary>
        /// Initializes a choice with the given button text and jump target.
        /// </summary>
        public ChoiceData(string text, JumpTarget jumpTarget)
        {
            choiceText      = text;
            jump            = jumpTarget;
            preJumpMessages = new List<MessageData>();
        }

        /// <summary>
        /// Returns true if this choice has a valid jump destination.
        /// </summary>
        public bool HasJump => jump != null && jump.IsValid;

        /// <summary>
        /// Returns true if this choice has dialogue messages to display before jumping.
        /// </summary>
        public bool HasPreJumpMessages => preJumpMessages != null && preJumpMessages.Count > 0;
    }

    // ═══════════════════════════════════════
    // DIALOGUE NODE
    // ═══════════════════════════════════════

    [Serializable]
    public class DialogueNode
    {
        public string nodeName;
        public List<MessageData> messages;
        public List<ChoiceData> choices;
        public List<PausePoint> pausePoints;
        public JumpTarget jump;                 // replaces nextNode string
        public int choicePauseIndex = -1;       // message index where >> choice begins, -1 if no choice block
        public bool choicesResolved = false;    // set to true after a choice is selected — prevents re-showing choices
        public bool isExplicitEnd = false;      // set by >> END — marks intentional conversation stop

        /// <summary>
        /// Initializes an empty dialogue node with blank lists and no jump target.
        /// </summary>
        public DialogueNode()
        {
            messages    = new List<MessageData>();
            choices     = new List<ChoiceData>();
            pausePoints = new List<PausePoint>();
            jump        = null;
        }

        public DialogueNode(string name) : this()
        {
            nodeName = name;
        }

        public bool ShouldPauseAfter(int messageIndex)
        {
            foreach (var p in pausePoints)
                if (p.stopIndex == messageIndex) return true;
            return false;
        }

        /// <summary>
        /// Returns the PausePoint at the given message index, or null if none exists.
        /// </summary>
        public PausePoint GetPauseAt(int messageIndex)
        {
            foreach (var p in pausePoints)
                if (p.stopIndex == messageIndex) return p;
            return null;
        }
    }

    // ═══════════════════════════════════════
    // PAUSE POINT
    // ═══════════════════════════════════════

    /// <summary>
    /// Represents a pause point in a dialogue node.
    /// Stops message flow at <c>stopIndex</c> and optionally pairs with a player message.
    ///
    /// Two cases:
    ///   playerMessageIndex = -1  — pure pacing pause (... in .bub), nothing sent on tap
    ///   playerMessageIndex >= 0  — player-turn pause (Player: "text" in .bub), message sent on tap
    /// </summary>
    [Serializable]
    public class PausePoint
    {
        public int stopIndex;
        public int playerMessageIndex;

        /// <summary>
        /// Returns true if this pause point has a paired player message to emit on continue.
        /// </summary>
        public bool HasPlayerMessage => playerMessageIndex >= 0;

        /// <summary>
        /// Initializes a pause point at the given stop index with an optional paired player message index.
        /// </summary>
        public PausePoint(int stop, int playerMsg = -1)
        {
            stopIndex          = stop;
            playerMessageIndex = playerMsg;
        }
    }

    // ═══════════════════════════════════════
    // RESUME TARGET
    // ═══════════════════════════════════════

    /// <summary>
    /// UI state when exiting a conversation.
    /// Used to determine where to resume when the player returns to the conversation.
    /// </summary>
    /// <param name="None">No resume target; default state.</param>
    /// <param name="Pause">Resume at the most recent pause point.</param>
    /// <param name="Choices">Resume at the most recent choice point.</param>
    /// <param name="End">Resume at the end of the conversation (no more messages).</param>
    /// <param name="Interrupted">Resume at the exact message where the conversation was interrupted.</param>
    public enum ResumeTarget
    {
        None,
        Pause,
        Choices,
        End,
        Interrupted
    }

    // ═══════════════════════════════════════
    // CONVERSATION STATE
    // ═══════════════════════════════════════

    [Serializable]
    public class ConversationState
    {
        public const int CURRENT_VERSION = 3;   // bumped — JumpTarget replaces nextNode/targetNode strings

        public int version = CURRENT_VERSION;
        public string conversationId;
        public string characterName;
        public string currentChapterId;
        public string currentNodeName;
        public int currentMessageIndex;
        public bool isInPauseState;
        public ResumeTarget resumeTarget;
        public List<string> readMessageIds;
        public List<MessageData> messageHistory;
        public List<string> unlockedCGs;

        /// <summary>
        /// Initializes a blank conversation state at chapter 0 with empty history lists.
        /// </summary>
        public ConversationState()
        {
            version             = CURRENT_VERSION;
            conversationId      = "";
            characterName       = "";
            currentChapterId    = "";
            currentNodeName     = "";
            currentMessageIndex = 0;
            isInPauseState      = false;
            resumeTarget        = ResumeTarget.None;
            readMessageIds      = new List<string>();
            messageHistory      = new List<MessageData>();
            unlockedCGs         = new List<string>();
        }

        /// <summary>
        /// Initializes a blank conversation state with the given conversation ID.
        /// </summary>
        public ConversationState(string convId) : this()
        {
            conversationId = convId;
        }
    }
}