﻿//------------------------------------------------------------------------------
// <copyright file="ToolWindow1Control.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace SplayCode
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for ToolWindow1Control.
    /// </summary>
    public partial class SplayCodeToolWindowControl : UserControl
    {

        private Point firstPoint = new Point();
        private List<ChromeControl> items = new List<ChromeControl>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindow1Control"/> class.
        /// </summary>
        public SplayCodeToolWindowControl()
        {
            this.InitializeComponent();
        }

        public void AddItem(ChromeControl item)
        {
            item.SetParent(this);
            items.Add(item);
            cavRoot.Children.Add(item);
            Thickness t = new Thickness();
            t.Left = 200 * items.Count;
            t.Top = 0;
            item.Margin = t;
            
            InitMouseCapture(item);
        }

        public void RemoveItem(ChromeControl item)
        {
            items.Remove(item);
            cavRoot.Children.Remove(item);
        }

        public void InitMouseCapture(ChromeControl element)
        {
            element.MouseLeftButtonDown += (ss, ee) =>
            {
                firstPoint = ee.GetPosition(this);
                element.CaptureMouse();
                Debug.WriteLine("Mouse clicked");
            };

            element.MouseMove += (ss, ee) =>
            {
                Debug.WriteLine("Mouse moved");
                if (ee.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    Point temp = ee.GetPosition(this);
                    Point res = new Point(firstPoint.X - temp.X, firstPoint.Y - temp.Y);

                    if (temp.X > 0)
                    {
                        Thickness t = element.Margin;
                        t.Left = t.Left - res.X;
                        element.Margin = t;
                    }

                    if (temp.Y > 0)
                    {
                        Thickness t = element.Margin;
                        t.Top = t.Top - res.Y;
                        element.Margin = t;
                    }
                    firstPoint = temp;
                }
            };
            element.MouseUp += (ss, ee) => { element.ReleaseMouseCapture(); };
        }

    }
}