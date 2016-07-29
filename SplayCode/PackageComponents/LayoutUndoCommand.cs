﻿//------------------------------------------------------------------------------
// <copyright file="LayoutUndoCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SplayCode.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace SplayCode
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class LayoutUndoCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 7;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("9da3a146-946a-4fc8-a5a4-029f780074b9");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutUndoCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private LayoutUndoCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static LayoutUndoCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new LayoutUndoCommand(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            VirtualSpaceControl virtualSpace = VirtualSpaceControl.Instance;
            Stack<ActionDone> globalStack = virtualSpace.GlobalStack;
            
            ActionDone action = null;
            
            // undo can only be called when the GlobalStack is not empty            
            if (globalStack.Count != 0)
            {
                action = globalStack.Pop();
                // if the top action of the stack was pushed by an editor being closed, reopen it where it was
                if (action.EditorClosed)
                {
                    virtualSpace.AddBlock(virtualSpace.GetFileName(action.MovedBlock.GetEditor().getFilePath()), action.MovedBlock.GetEditor().getFilePath(),
                        action.BlockPositionX, action.BlockPositionY, action.BlockSizeY, action.BlockSizeX, action.ZIndex, action.BlockId);
                    globalStack.Pop();
                }
                // if the top action of the stack was pushed due to addition of a new editor, close it
                else if (action.EditorAdded)
                {
                    List<BlockControl> bcList = virtualSpace.FetchAllBlocks();
                    foreach (BlockControl bc in bcList)
                    {
                        if (bc.BlockId == action.BlockId)
                        {
                            virtualSpace.RemoveBlock(bc);
                            break;
                        }
                    }
                }
                // if the top action of the stack was pushed due to repositioning or resizing of an editor, position is back to where it was
                else
                {
                    action.MovedBlock.Width = action.BlockSizeX;
                    action.MovedBlock.Height = action.BlockSizeY;
                    Thickness t = action.MovedBlock.Margin;
                    t.Left = action.BlockPositionX;
                    t.Top = action.BlockPositionY;
                    action.MovedBlock.Margin = t;
                    Panel.SetZIndex(action.MovedBlock, action.ZIndex);
                }
            }
        }
    }
}