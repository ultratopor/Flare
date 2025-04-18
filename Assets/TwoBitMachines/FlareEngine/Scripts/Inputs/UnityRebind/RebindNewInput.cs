using System;
using System.Collections.Generic;
using UnityEngine.Events;

////TODO: localization support

////TODO: deal with composites that have parts bound in different control schemes
///https://www.youtube.com/watch?v=qXbjyzBlduY,How to Rebind Your Controls in Unity (With Icons!) | Input System

namespace UnityEngine.InputSystem.Samples.RebindUI
{
        /// <summary>
        /// A reusable component with a self-contained UI for rebinding a single action.
        /// </summary>
        public class RebindNewInput : MonoBehaviour
        {
                /// <summary>
                /// Reference to the action that is to be rebound.
                /// </summary>
                public InputActionReference actionReference
                {
                        get => m_Action;
                        set
                        {
                                m_Action = value;
                                UpdateButtonLabel ( );
                                UpdateBindingDisplay ( );
                        }
                }

                /// <summary>
                /// ID (in string form) of the binding that is to be rebound on the action.
                /// </summary>
                /// <seealso cref="InputBinding.id"/>
                public string bindingId
                {
                        get => m_BindingId;
                        set
                        {
                                m_BindingId = value;
                                UpdateBindingDisplay ( );
                        }
                }

                public InputBinding.DisplayStringOptions displayStringOptions
                {
                        get => m_DisplayStringOptions;
                        set
                        {
                                m_DisplayStringOptions = value;
                                UpdateBindingDisplay ( );
                        }
                }

                /// <summary>
                /// Text component that receives the display string of the binding. Can be <c>null</c> in which
                /// case the component entirely relies on <see cref="updateBindingUIEvent"/>.
                /// </summary>
                public TMPro.TextMeshProUGUI bindingLabel
                {
                        get => m_BindingLabel;
                        set
                        {
                                m_BindingLabel = value;
                                UpdateBindingDisplay ( );
                        }
                }

                /// <summary>
                /// Optional text component that receives a text prompt when waiting for a control to be actuated.
                /// </summary>
                /// <seealso cref="startRebindEvent"/>
                /// <seealso cref="rebindOverlay"/>
                public TMPro.TextMeshProUGUI rebindPrompt
                {
                        get => m_RebindText;
                        set => m_RebindText = value;
                }

                /// <summary>
                /// Optional UI that is activated when an interactive rebind is started and deactivated when the rebind
                /// is finished. This is normally used to display an overlay over the current UI while the system is
                /// waiting for a control to be actuated.
                /// </summary>
                /// <remarks>
                /// If neither <see cref="rebindPrompt"/> nor <c>rebindOverlay</c> is set, the component will temporarily
                /// replaced the <see cref="bindingLabel"/> (if not <c>null</c>) with <c>"Waiting..."</c>.
                /// </remarks>
                /// <seealso cref="startRebindEvent"/>
                /// <seealso cref="rebindPrompt"/>
                public GameObject rebindOverlay
                {
                        get => m_RebindOverlay;
                        set => m_RebindOverlay = value;
                }

                /// <summary>
                /// Event that is triggered every time the UI updates to reflect the current binding.
                /// This can be used to tie custom visualizations to bindings.
                /// </summary>
                public UpdateBindingUIEvent updateBindingUIEvent
                {
                        get
                        {
                                if (m_UpdateBindingUIEvent == null)
                                        m_UpdateBindingUIEvent = new UpdateBindingUIEvent ( );
                                return m_UpdateBindingUIEvent;
                        }
                }

                /// <summary>
                /// Event that is triggered when an interactive rebind is started on the action.
                /// </summary>
                public InteractiveRebindEvent startRebindEvent
                {
                        get
                        {
                                if (m_RebindStartEvent == null)
                                        m_RebindStartEvent = new InteractiveRebindEvent ( );
                                return m_RebindStartEvent;
                        }
                }

                /// <summary>
                /// Event that is triggered when an interactive rebind has been completed or canceled.
                /// </summary>
                public InteractiveRebindEvent stopRebindEvent
                {
                        get
                        {
                                if (m_RebindStopEvent == null)
                                        m_RebindStopEvent = new InteractiveRebindEvent ( );
                                return m_RebindStopEvent;
                        }
                }

                /// <summary>
                /// When an interactive rebind is in progress, this is the rebind operation controller.
                /// Otherwise, it is <c>null</c>.
                /// </summary>
                public InputActionRebindingExtensions.RebindingOperation ongoingRebind => m_RebindOperation;

                /// <summary>
                /// Return the action and binding index for the binding that is targeted by the component
                /// according to
                /// </summary>
                /// <param name="action"></param>
                /// <param name="bindingIndex"></param>
                /// <returns></returns>
                public bool ResolveActionAndBinding (out InputAction action, out int bindingIndex)
                {
                        bindingIndex = -1;

                        action = m_Action?.action;
                        if (action == null)
                                return false;

                        if (string.IsNullOrEmpty (m_BindingId))
                                return false;

                        // Look up binding index.
                        var bindingId = new Guid (m_BindingId);
                        bindingIndex = action.bindings.IndexOf (x => x.id == bindingId);
                        if (bindingIndex == -1)
                        {
                                Debug.LogError ($"Cannot find binding with ID '{bindingId}' on '{action}'", this);
                                return false;
                        }

                        return true;
                }

                /// <summary>
                /// Trigger a refresh of the currently displayed binding.
                /// </summary>
                public void UpdateBindingDisplay ( )
                {
                        var displayString = string.Empty;
                        var deviceLayoutName = default (string);
                        var controlPath = default (string);

                        // Get display string from action.
                        var action = m_Action?.action;
                        if (action != null)
                        {
                                var bindingIndex = action.bindings.IndexOf (x => x.id.ToString ( ) == m_BindingId);
                                if (bindingIndex != -1)
                                        displayString = action.GetBindingDisplayString (bindingIndex, out deviceLayoutName, out controlPath, displayStringOptions);
                        }

                        // Set on label (if any).
                        if (m_BindingLabel != null)
                                m_BindingLabel.text = displayString;

                        // Give listeners a chance to configure UI in response.
                        m_UpdateBindingUIEvent?.Invoke (bindingLabel?.gameObject, controlPath);
                }

                /// <summary>
                /// Remove currently applied binding overrides.
                /// </summary>
                public void ResetToDefault ( )
                {
                        if (!ResolveActionAndBinding (out var action, out var bindingIndex))
                                return;

                        ResetBinding (action, bindingIndex);
                        if (action.bindings[bindingIndex].isComposite)
                        {
                                // It's a composite. Remove overrides from part bindings.
                                for (var i = bindingIndex + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; ++i)
                                        action.RemoveBindingOverride (i);
                        }
                        else
                        {
                                action.RemoveBindingOverride (bindingIndex);
                        }

                        UpdateBindingDisplay ( );
                }

                private void ResetBinding (InputAction action, int bindingIndex)
                {
                        InputBinding newBinding = action.bindings[bindingIndex];
                        string oldOverridePath = newBinding.overridePath;

                        action.RemoveBindingOverride (bindingIndex);
                        int currentIndex = -1;

                        foreach (InputAction otherAction in action.actionMap.actions)
                        {
                                currentIndex++;
                                InputBinding currentBinding = action.actionMap.bindings[currentIndex];

                                if (otherAction == action)
                                {
                                        if (newBinding.isPartOfComposite)
                                        {
                                                if (currentBinding.overridePath == newBinding.path)
                                                {
                                                        otherAction.ApplyBindingOverride (currentIndex, oldOverridePath);
                                                }
                                        }

                                        else
                                        {
                                                continue;
                                        }
                                }

                                for (int i = 0; i < otherAction.bindings.Count; i++)
                                {
                                        InputBinding binding = otherAction.bindings[i];
                                        if (binding.overridePath == newBinding.path)
                                        {
                                                otherAction.ApplyBindingOverride (i, oldOverridePath);
                                        }
                                }
                        }
                }
                /// <summary>
                /// Initiate an interactive rebind that lets the player actuate a control to choose a new binding
                /// for the action.
                /// </summary>
                public void StartInteractiveRebind ( )
                {
                        if (!ResolveActionAndBinding (out var action, out var bindingIndex))
                                return;

                        // If the binding is a composite, we need to rebind each part in turn.
                        if (action.bindings[bindingIndex].isComposite)
                        {
                                var firstPartIndex = bindingIndex + 1;
                                if (firstPartIndex < action.bindings.Count && action.bindings[firstPartIndex].isPartOfComposite)
                                        PerformInteractiveRebind (action, firstPartIndex, allCompositeParts : true);
                        }
                        else
                        {
                                PerformInteractiveRebind (action, bindingIndex);
                        }
                }

                private void PerformInteractiveRebind (InputAction action, int bindingIndex, bool allCompositeParts = false)
                {
                        m_RebindOperation?.Cancel ( ); // Will null out m_RebindOperation.

                        void CleanUp ( )
                        {
                                m_RebindOperation?.Dispose ( );
                                m_RebindOperation = null;
                        }

                        // disable the action before use to prevent errors
                        actionTemp = action;
                        action.Disable ( );

                        // Configure the rebind.
                        m_RebindOperation = action.PerformInteractiveRebinding (bindingIndex)
                                .WithCancelingThrough ("<Keyboard>/escape") // must update to at least 1.4.1 input system version
                                .OnCancel (
                                        operation =>
                                        {
                                                action.Enable ( );
                                                m_RebindStopEvent?.Invoke (this, operation);
                                                UpdateBindingDisplay ( );
                                                CleanUp ( );
                                        })
                                .OnComplete (
                                        operation =>
                                        {
                                                action.Enable ( );
                                                m_RebindStopEvent?.Invoke (this, operation);

                                                if (CheckDuplicateBindings (action, bindingIndex, allCompositeParts))
                                                {
                                                        action.RemoveBindingOverride (bindingIndex);
                                                        CleanUp ( );
                                                        PerformInteractiveRebind (action, bindingIndex, allCompositeParts);
                                                        return;
                                                }

                                                UpdateBindingDisplay ( );
                                                CleanUp ( );

                                                // If there's more composite parts we should bind, initiate a rebind
                                                // for the next part.
                                                if (allCompositeParts)
                                                {
                                                        var nextBindingIndex = bindingIndex + 1;
                                                        if (nextBindingIndex < action.bindings.Count && action.bindings[nextBindingIndex].isPartOfComposite)
                                                                PerformInteractiveRebind (action, nextBindingIndex, true);
                                                }
                                        });

                        // If it's a part binding, show the name of the part in the UI.
                        var partName = default (string);
                        if (action.bindings[bindingIndex].isPartOfComposite)
                        {
                                partName = $"Binding '{action.bindings[bindingIndex].name}'. ";
                        }

                        m_RebindStartEvent?.Invoke (this, m_RebindOperation);
                        m_RebindOperation.Start ( );
                }

                public void CancelKeyRebind ( )
                {
                        m_RebindOperation?.Cancel ( ); // Will null out m_RebindOperation.
                        m_RebindOperation?.Dispose ( );
                        m_RebindOperation = null;

                        actionTemp?.Enable ( );
                        m_RebindStopEvent?.Invoke (this, m_RebindOperation);
                        UpdateBindingDisplay ( );
                }

                private bool CheckDuplicateBindings (InputAction action, int bindingIndex, bool allCompositeParts = false)
                {
                        InputBinding newBinding = action.bindings[bindingIndex];
                        int currentIndex = -1;

                        foreach (InputBinding binding in action.actionMap.bindings)
                        {
                                currentIndex++;

                                if (binding.action == newBinding.action)
                                {
                                        if (binding.isPartOfComposite && currentIndex != bindingIndex)
                                        {
                                                if (binding.effectivePath == newBinding.effectivePath)
                                                {
                                                        Debug.Log ("Duplicate binding found in composite: " + newBinding.effectivePath);
                                                        return true;
                                                }
                                        }

                                        else
                                        {
                                                continue;
                                        }
                                }

                                if (binding.effectivePath == newBinding.effectivePath)
                                {
                                        Debug.Log ("Duplicate binding found: " + newBinding.effectivePath);
                                        return true;
                                }

                        }

                        if (allCompositeParts)
                        {
                                for (int i = 1; i < bindingIndex; i++)
                                {
                                        if (action.bindings[i].effectivePath == newBinding.overridePath)
                                        {
                                                //Debug.Log("Duplicate binding found: " + newBinding.effectivePath);
                                                return true;
                                        }
                                }
                        }

                        return false;
                }
                protected void OnEnable ( )
                {
                        if (s_RebindNewInputs == null) s_RebindNewInputs = new List<RebindNewInput> ( );
                        s_RebindNewInputs.Add (this);
                        if (s_RebindNewInputs.Count == 1) InputSystem.onActionChange += OnActionChange;
                }

                protected void OnDisable ( )
                {
                        m_RebindOperation?.Dispose ( );
                        m_RebindOperation = null;

                        s_RebindNewInputs.Remove (this);
                        if (s_RebindNewInputs.Count == 0)
                        {
                                s_RebindNewInputs = null;
                                InputSystem.onActionChange -= OnActionChange;
                        }
                }

                // When the action system re-resolves bindings, we want to update our UI in response. While this will
                // also trigger from changes we made ourselves, it ensures that we react to changes made elsewhere. If
                // the user changes keyboard layout, for example, we will get a BoundControlsChanged notification and
                // will update our UI to reflect the current keyboard layout.
                private static void OnActionChange (object obj, InputActionChange change)
                {
                        if (change != InputActionChange.BoundControlsChanged)
                                return;

                        var action = obj as InputAction;
                        var actionMap = action?.actionMap ?? obj as InputActionMap;
                        var actionAsset = actionMap?.asset ?? obj as InputActionAsset;

                        for (var i = 0; i < s_RebindNewInputs.Count; ++i)
                        {
                                var component = s_RebindNewInputs[i];
                                var referencedAction = component.actionReference?.action;
                                if (referencedAction == null)
                                        continue;

                                if (referencedAction == action ||
                                        referencedAction.actionMap == actionMap ||
                                        referencedAction.actionMap?.asset == actionAsset)
                                        component.UpdateBindingDisplay ( );
                        }
                }

                private InputAction actionTemp;

                [Tooltip ("Reference to action that is to be rebound from the UI.")]
                [SerializeField]
                private InputActionReference m_Action;

                [SerializeField]
                private string m_BindingId;

                [SerializeField]
                private InputBinding.DisplayStringOptions m_DisplayStringOptions;

                [Tooltip ("The button label. Optional. Set to None to have the " +
                        "rebind UI not show a label for the action.")]
                [SerializeField]
                private TMPro.TextMeshProUGUI m_ButtonLabel;

                [Tooltip ("The binding label.")]
                [SerializeField]
                private TMPro.TextMeshProUGUI m_BindingLabel;

                [Tooltip ("Override button label.")]
                [SerializeField]
                private bool overrideButtonLabel = true;

                [Tooltip ("Optional UI that will be shown while a rebind is in progress.")]
                [SerializeField]
                private GameObject m_RebindOverlay;

                [Tooltip ("Optional text label that will be updated with prompt for user input.")]
                [SerializeField]
                private TMPro.TextMeshProUGUI m_RebindText;

                [Tooltip ("Event that is triggered when the way the binding is display should be updated. This allows displaying " +
                        "bindings in custom ways, e.g. using images instead of text.")]
                [SerializeField]
                private UpdateBindingUIEvent m_UpdateBindingUIEvent;

                [Tooltip ("Event that is triggered when an interactive rebind is being initiated. This can be used, for example, " +
                        "to implement custom UI behavior while a rebind is in progress. It can also be used to further " +
                        "customize the rebind.")]
                [SerializeField]
                private InteractiveRebindEvent m_RebindStartEvent;

                [Tooltip ("Event that is triggered when an interactive rebind is complete or has been aborted.")]
                [SerializeField]
                private InteractiveRebindEvent m_RebindStopEvent;

                private InputActionRebindingExtensions.RebindingOperation m_RebindOperation;

                private static List<RebindNewInput> s_RebindNewInputs;

                // We want the label for the action name to update in edit mode, too, so
                // we kick that off from here.
                #if UNITY_EDITOR
                protected void OnValidate ( )
                {
                        UpdateButtonLabel ( );
                        UpdateBindingDisplay ( );
                }

                #endif

                private void UpdateButtonLabel ( )
                {
                        if (m_ButtonLabel != null)
                        {
                                var action = m_Action?.action;
                                if (action != null && overrideButtonLabel)
                                {
                                        m_ButtonLabel.text = action.name;
                                }
                        }
                }

                [Serializable]
                public class UpdateBindingUIEvent : UnityEvent<GameObject, string> { }

                [Serializable]
                public class InteractiveRebindEvent : UnityEvent<RebindNewInput, InputActionRebindingExtensions.RebindingOperation> { }
        }
}