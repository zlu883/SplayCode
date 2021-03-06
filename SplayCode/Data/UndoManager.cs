﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplayCode.Data
{
    /// <summary>
    /// Singleton class that manages the undo functionality.
    /// </summary>
    class UndoManager
    {
        private static readonly int UNDO_LIMIT = 30;

        private List<UndoState> stateStack;
        public List<UndoState> StateStack
        {
            get { return stateStack; }
        }

        private static UndoManager instance;
        public static UndoManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new UndoManager();
                }
                return instance;
            }
        }

        private UndoManager()
        {
            stateStack = new List<UndoState>();
        }

        /// <summary>
        /// Captures the current state of SplayCode. Call this before
        /// proceeding on an action that should be undoable.
        /// </summary>
        public void SaveState()
        {
            UndoState newState = new UndoState(BlockManager.Instance.BlockList);
            if (stateStack.Count >= UNDO_LIMIT)
            {
                stateStack.RemoveAt(0);
            }
            stateStack.Add(newState);
        }

        /// <summary>
        /// Return SplayCode to the last captured state.
        /// </summary>
        public void Undo()
        {
            if (stateStack.Count != 0)
            {
                UndoState oldState = stateStack[stateStack.Count - 1];
                stateStack.RemoveAt(stateStack.Count - 1);
                BlockManager.Instance.LoadBlockStates(oldState.BlockStates);
            }
        }

        /// <summary>
        /// Clears the undo stack.
        /// </summary>
        public void Reset()
        {
            stateStack.Clear();
        }

    }
}
