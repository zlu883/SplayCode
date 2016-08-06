﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SplayCode.Data
{
    /// <summary>
    /// This is a singleton class that manages the placement and manipulation of 
    /// blocks in the virtual space.
    /// </summary>
    class BlockManager
    {
        private List<BlockControl> blockList;
        public List<BlockControl> BlockList
        {
            get { return blockList; }
        }

        private List<BlockControl> selectedBlocks;
        private BlockControl activeBlock;
        public BlockControl ActiveBlock
        {
            get { return activeBlock; }
        }

        private static readonly int MINIMUM_Z_INDEX = 0;
        private int topmostZIndex;
        public int TopmostZIndex
        {
            get { return topmostZIndex; }
        }

        private static readonly int MINIMUM_BLOCK_ID = 0;
        private int currentBlockId;
        public int CurrentBlockId
        {
            get { return currentBlockId; }
        }

        private static BlockManager instance;
        public static BlockManager Instance
        {
            get
            {
                if (instance==null)
                {
                    instance = new BlockManager();
                }
                return instance;
            }
        }

        private BlockManager()
        {
            blockList = new List<BlockControl>();
            selectedBlocks = new List<BlockControl>();
            activeBlock = null;
            topmostZIndex = MINIMUM_Z_INDEX;
            currentBlockId = MINIMUM_BLOCK_ID;
        }

        /// <summary>
        /// Add a block using default properties.
        /// </summary>
        public void AddBlock(string label, string documentPath)
        {
            Point blockPosition = VirtualSpaceControl.Instance.GetNextBlockPosition(null);
            double xPos = blockPosition.X;
            double yPos = blockPosition.Y;
            double width = BlockControl.DEFAULT_BLOCK_WIDTH;
            double height = BlockControl.DEFAULT_BLOCK_HEIGHT;
            int zIndex = topmostZIndex + 1;
            int blockId = currentBlockId + 1;
            AddBlock(label, documentPath, xPos, yPos, width, height, zIndex, blockId, true);
        }

        /// <summary>
        /// Add a block at the preferred position.
        /// </summary>
        public void AddBlock(string label, string documentPath, double preferredXPos, double preferredYPos)
        {
            Point preferredPosition = new Point(preferredXPos, preferredYPos);
            Point blockPosition = VirtualSpaceControl.Instance.GetNextBlockPosition(preferredPosition);
            double xPos = blockPosition.X;
            double yPos = blockPosition.Y;
            double width = BlockControl.DEFAULT_BLOCK_WIDTH;
            double height = BlockControl.DEFAULT_BLOCK_HEIGHT;
            int zIndex = topmostZIndex + 1;
            int blockId = currentBlockId + 1;
            AddBlock(label, documentPath, xPos, yPos, width, height, zIndex, blockId, true);
        }

        /// <summary>
        /// Add a block with given parameters.
        /// </summary>
        public void AddBlock(string label, string documentPath, double xPos, 
            double yPos, double height, double width, int zIndex, int blockId, bool setActive)
        {
            UndoManager.Instance.SaveState();

            BlockControl newBlock = new BlockControl(label, documentPath, blockId);
            newBlock.Width = width;
            newBlock.Height = height;
            newBlock.Margin = new Thickness(xPos, yPos, 0, 0);
            Panel.SetZIndex(newBlock, zIndex);
            if (zIndex > topmostZIndex)
            {
                topmostZIndex = zIndex;
            }
            if (blockId > currentBlockId)
            {
                currentBlockId = blockId;
            }

            blockList.Add(newBlock);
            if (setActive)
            {
                SetActiveBlock(newBlock);
            }

            VirtualSpaceControl.Instance.InsertBlock(newBlock);
            //VirtualSpaceControl.Instance.ExpandWidth(newBlock.Margin.Left + newBlock.Width);
            //VirtualSpaceControl.Instance.ExpandHeight(newBlock.Margin.Top + newBlock.Height);
            VirtualSpaceControl.Instance.ExpandToSize(newBlock.Margin.Left + newBlock.Width,
                newBlock.Margin.Top + newBlock.Height);
        }

        /* Replace the current state of blocks with the given state of blocks. */ 
        public void LoadBlockStates(List<BlockState> newBlockStates)
        {
            List<BlockControl> blocksToAdd = new List<BlockControl>();
            List<BlockControl> blocksToRemove = new List<BlockControl>();
            foreach (BlockControl block in blockList)
            {
                bool hasBlockState = false;
                foreach(BlockState blockState in newBlockStates)
                {
                    // find the corresponding block in the replacement state and refresh
                    // block properties from its data
                    if (blockState.BlockId == block.BlockId)
                    {
                        block.Height = blockState.BlockHeight;
                        block.Width = blockState.BlockWidth;
                        Thickness t = block.Margin;
                        t.Left = blockState.BlockXPos;
                        t.Top = blockState.BlockYPos;
                        block.Margin = t;
                        Panel.SetZIndex(block, blockState.BlockZIndex);
                        hasBlockState = true;
                        break;
                    }
                }
                // if a block exists in the block list but not in the replacement state, remove it
                if (!hasBlockState)
                {
                    blocksToRemove.Add(block);
                }
            }

            foreach(BlockState blockState in newBlockStates)
            {
                bool hasBlock = false;
                foreach(BlockControl block in blockList)
                {
                    if (block.BlockId == blockState.BlockId)
                    {
                        hasBlock = true;
                    }
                }
                if (!hasBlock)
                {
                    AddBlock(blockState.Label, blockState.DocumentPath, blockState.BlockXPos,
                        blockState.BlockYPos, blockState.BlockHeight, blockState.BlockWidth,
                        blockState.BlockZIndex, blockState.BlockId, false);
                }
            }
            SetTopmostBlockAsActive();
        }

        /* Set the given block as active. The active block gets highlight and is brought
        to the top. */
        public void SetActiveBlock(BlockControl block)
        {
            if (Panel.GetZIndex(block) < topmostZIndex)
            {
                topmostZIndex++; // [BUG] possible but extremely unlikely to reach int limit
                Panel.SetZIndex(block, topmostZIndex);
            }
            if (activeBlock != null)
            {
                activeBlock.SetHighlight(false);
            }
            activeBlock = block;
            activeBlock.SetHighlight(true);
        }

        /* Remove the given block. If it is the active block, set the next
        topmost block as active. */
        public void RemoveBlock(BlockControl block)
        {
            VirtualSpaceControl.Instance.DeleteBlock(block);
            blockList.Remove(block);
            if (block.Equals(activeBlock))
            {
                SetTopmostBlockAsActive();                
            }
        }

        /* Remove all blocks and resets fields. */
        public void RemoveAllBlocks()
        {
            foreach(BlockControl block in blockList)
            {
                VirtualSpaceControl.Instance.DeleteBlock(block);
            }
            blockList.Clear();
            selectedBlocks.Clear();
            activeBlock = null;
            topmostZIndex = MINIMUM_Z_INDEX;
        }

        /* Find the block that has the highest z-index and set it as active.
        If there are no blocks, nothing happens. */
        private void SetTopmostBlockAsActive()
        {
            int currentZIndex = MINIMUM_Z_INDEX;
            BlockControl topmostBlock = null;
            foreach(BlockControl block in blockList)
            {
                if (Panel.GetZIndex(block) > currentZIndex)
                {
                    currentZIndex = Panel.GetZIndex(block);
                    topmostBlock = block;
                }
            }
            if (topmostBlock != null)
            {
                topmostZIndex = Panel.GetZIndex(topmostBlock);
                SetActiveBlock(topmostBlock);
            }
        }

        /* Shift all blocks by the given x and y delta values. */
        public void ShiftAllBlocks(double xDelta, double yDelta)
        {
            foreach (BlockControl block in blockList)
            {
                block.Reposition(xDelta, yDelta);
            }
        }

        /* Test whether a block containing the given document already exists. */
        public bool BlockAlreadyExists(string documentPath)
        {
            foreach (BlockControl block in blockList)
            {
                if (block.DocumentPath.Equals(documentPath))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
