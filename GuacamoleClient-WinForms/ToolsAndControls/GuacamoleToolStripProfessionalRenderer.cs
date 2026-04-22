using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections;

namespace GuacamoleClient.WinForms
{
    public class GuacamoleToolStripProfessionalRenderer : ToolStripProfessionalRenderer
    {
        public GuacamoleToolStripProfessionalRenderer()
        {
        }

        //internal GuacamoleToolStripProfessionalRenderer(bool isDefault) : base(isDefault)
        //{
        //}

        public GuacamoleToolStripProfessionalRenderer(ProfessionalColorTable professionalColorTable) : base(professionalColorTable)
        {
        }
    }

#if false
    /// <summary>
    ///  Summary description for ProfessionalToolStripRenderer.
    /// </summary>
    public class GuacamoleToolStripProfessionalRendererFullReimplementationAsOfficialDotNet : ToolStripRenderer
    {
        private const int GRIP_PADDING = 4;
        private int gripPadding = GRIP_PADDING;

        private const int ICON_WELL_GRADIENT_WIDTH = 12;
        private int iconWellGradientWidth = ICON_WELL_GRADIENT_WIDTH;

        private static readonly Size onePix = new(1, 1);

        private bool isScalingInitialized;
        private const int OVERFLOW_BUTTON_WIDTH = 12;
        private const int OVERFLOW_ARROW_WIDTH = 9;
        private const int OVERFLOW_ARROW_HEIGHT = 5;
        private const int OVERFLOW_ARROW_OFFSETY = 8;
        private int overflowButtonWidth = OVERFLOW_BUTTON_WIDTH;
        private int overflowArrowWidth = OVERFLOW_ARROW_WIDTH;
        private int overflowArrowHeight = OVERFLOW_ARROW_HEIGHT;
        private int overflowArrowOffsetY = OVERFLOW_ARROW_OFFSETY;

        private const int DROP_DOWN_MENU_ITEM_PAINT_PADDING_SIZE = 1;
        private Padding scaledDropDownMenuItemPaintPadding = new(DROP_DOWN_MENU_ITEM_PAINT_PADDING_SIZE + 1, 0, DROP_DOWN_MENU_ITEM_PAINT_PADDING_SIZE, 0);
        private readonly ProfessionalColorTable? professionalColorTable;
        private bool roundedEdges = true;
        private ToolStripRenderer? toolStripHighContrastRenderer;
        private ToolStripRenderer? toolStripLowResolutionRenderer;

        public GuacamoleToolStripProfessionalRendererFullReimplementationAsOfficialDotNet()
        {
        }

        //internal GuacamoleToolStripProfessionalRendererFullReimplementationAsOfficialDotNet(bool isDefault) : base(isDefault)
        //{
        //}

        public GuacamoleToolStripProfessionalRendererFullReimplementationAsOfficialDotNet(ProfessionalColorTable professionalColorTable)
        {
            this.professionalColorTable = professionalColorTable;
        }

        public ProfessionalColorTable ColorTable
        {
            get
            {
                if (professionalColorTable is null)
                {
                    return ProfessionalColors.ColorTable;
                }

                return professionalColorTable;
            }
        }

        internal override ToolStripRenderer? RendererOverride
        {
            get
            {
                if (DisplayInformation.HighContrast)
                {
                    return HighContrastRenderer;
                }

                if (DisplayInformation.LowResolution)
                {
                    return LowResolutionRenderer;
                }

                return null;
            }
        }

        internal ToolStripRenderer HighContrastRenderer
        {
            get
            {
                toolStripHighContrastRenderer ??= new ToolStripHighContrastRenderer(/*renderLikeSystem*/false);

                return toolStripHighContrastRenderer;
            }
        }

        internal ToolStripRenderer LowResolutionRenderer
        {
            get
            {
                toolStripLowResolutionRenderer ??= new ToolStripProfessionalLowResolutionRenderer();

                return toolStripLowResolutionRenderer;
            }
        }

        public bool RoundedEdges
        {
            get
            {
                return roundedEdges;
            }
            set
            {
                roundedEdges = value;
            }
        }

        private bool UseSystemColors
        {
            get { return (ColorTable.UseSystemColors || !ToolStripManager.VisualStylesEnabled); }
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            if (RendererOverride is not null)
            {
                base.OnRenderToolStripBackground(e);
                return;
            }

            ToolStrip toolStrip = e.ToolStrip;

            if (!ShouldPaintBackground(toolStrip))
            {
                return;
            }

            if (toolStrip is ToolStripDropDown)
            {
                RenderToolStripDropDownBackground(e);
            }
            else if (toolStrip is MenuStrip)
            {
                RenderMenuStripBackground(e);
            }
            else if (toolStrip is StatusStrip)
            {
                RenderStatusStripBackground(e);
            }
            else
            {
                RenderToolStripBackgroundInternal(e);
            }
        }

        protected override void OnRenderOverflowButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.ToolStrip is not null)
            {
                ScaleObjectSizesIfNeeded(e.ToolStrip.DeviceDpi);
            }

            if (RendererOverride is not null)
            {
                base.OnRenderOverflowButtonBackground(e);
                return;
            }

            ToolStripItem item = e.Item;
            Graphics g = e.Graphics;

            // fill in the background colors
            bool rightToLeft = item.RightToLeft == RightToLeft.Yes;
            RenderOverflowBackground(e, rightToLeft);

            bool horizontal = e.ToolStrip is not null && e.ToolStrip.Orientation == Orientation.Horizontal;

            Rectangle overflowArrowRect;
            if (rightToLeft)
            {
                overflowArrowRect = new Rectangle(0, item.Height - overflowArrowOffsetY, overflowArrowWidth, overflowArrowHeight);
            }
            else
            {
                overflowArrowRect = new Rectangle(item.Width - overflowButtonWidth, item.Height - overflowArrowOffsetY, overflowArrowWidth, overflowArrowHeight);
            }

            ArrowDirection direction = horizontal ? ArrowDirection.Down : ArrowDirection.Right;

            // in RTL the white highlight goes BEFORE the black triangle.
            int rightToLeftShift = (rightToLeft && horizontal) ? -1 : 1;

            // draw highlight
            overflowArrowRect.Offset(1 * rightToLeftShift, 1);
            RenderArrowInternal(g, overflowArrowRect, direction, SystemBrushes.ButtonHighlight);

            // draw black triangle
            overflowArrowRect.Offset(-1 * rightToLeftShift, -1);
            Point middle = RenderArrowInternal(g, overflowArrowRect, direction, SystemBrushes.ControlText);

            // draw lines
            if (horizontal)
            {
                rightToLeftShift = rightToLeft ? -2 : 0;
                // width of the both lines is 1 pixel and lines are drawn next to each other, this the highlight line is 1 pixel below the black line
                g.DrawLine(SystemPens.ControlText,
                    middle.X - Offset2X,
                    overflowArrowRect.Y - Offset2Y,
                    middle.X + Offset2X,
                    overflowArrowRect.Y - Offset2Y);
                g.DrawLine(SystemPens.ButtonHighlight,
                    middle.X - Offset2X + 1 + rightToLeftShift,
                    overflowArrowRect.Y - Offset2Y + 1,
                    middle.X + Offset2X + 1 + rightToLeftShift,
                    overflowArrowRect.Y - Offset2Y + 1);
            }
            else
            {
                g.DrawLine(SystemPens.ControlText,
                    overflowArrowRect.X,
                    middle.Y - Offset2Y,
                    overflowArrowRect.X,
                    middle.Y + Offset2Y);
                g.DrawLine(SystemPens.ButtonHighlight,
                    overflowArrowRect.X + 1,
                    middle.Y - Offset2Y + 1,
                    overflowArrowRect.X + 1,
                    middle.Y + Offset2Y + 1);
            }
        }

        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (RendererOverride is not null)
            {
                base.OnRenderDropDownButtonBackground(e);
                return;
            }

            if (e.Item is ToolStripDropDownItem item && item.Pressed && item.HasDropDownItems)
            {
                Rectangle bounds = new Rectangle(Point.Empty, item.Size);

                RenderPressedGradient(e.Graphics, bounds);
            }
            else
            {
                RenderItemInternal(e, /*useHotBorder =*/true);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            if (RendererOverride is not null)
            {
                base.OnRenderSeparator(e);
                return;
            }

            RenderSeparatorInternal(e.Graphics, e.Item, new Rectangle(Point.Empty, e.Item.Size), e.Vertical);
        }

        protected override void OnRenderSplitButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (RendererOverride is not null)
            {
                base.OnRenderSplitButtonBackground(e);
                return;
            }

            Graphics g = e.Graphics;

            if (!(e.Item is ToolStripSplitButton item))
            {
                return;
            }

            Rectangle bounds = new Rectangle(Point.Empty, item.Size);
            if (item.BackgroundImage is not null)
            {
                Rectangle fillRect = item.Selected ? item.ContentRectangle : bounds;
                ControlPaint.DrawBackgroundImage(g, item.BackgroundImage, item.BackColor, item.BackgroundImageLayout, bounds, fillRect);
            }

            bool buttonPressedOrSelected = (item.Pressed || item.ButtonPressed || item.Selected || item.ButtonSelected);

            if (buttonPressedOrSelected)
            {
                RenderItemInternal(e, useHotBorder: true);
            }

            if (item.ButtonPressed)
            {
                Rectangle buttonBounds = item.ButtonBounds;
                // We subtract 1 from each side except the right.
                // This is because we've already drawn the border, and we don't
                // want to draw over it.  We don't do the right edge, because we
                // drew the border around the whole control, not the button.
                Padding deflatePadding = item.RightToLeft == RightToLeft.Yes ? new Padding(0, 1, 1, 1) : new Padding(1, 1, 0, 1);
                buttonBounds = LayoutUtils.DeflateRect(buttonBounds, deflatePadding);
                RenderPressedButtonFill(g, buttonBounds);
            }
            else if (item.Pressed)
            {
                RenderPressedGradient(e.Graphics, bounds);
            }

            Rectangle dropDownRect = item.DropDownButtonBounds;

            if (buttonPressedOrSelected && !item.Pressed)
            {
                using var brush = ColorTable.ButtonSelectedBorder.GetCachedSolidBrushScope();
                g.FillRectangle(brush, item.SplitterBounds);
            }

            DrawArrow(new ToolStripArrowRenderEventArgs(g, item, dropDownRect, SystemColors.ControlText, ArrowDirection.Down));
        }

        protected override void OnRenderToolStripStatusLabelBackground(ToolStripItemRenderEventArgs e)
        {
            if (RendererOverride is not null)
            {
                base.OnRenderToolStripStatusLabelBackground(e);
                return;
            }

            RenderLabelInternal(e);
            ToolStripStatusLabel? item = e.Item as ToolStripStatusLabel;
            if (item is not null)
            {
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, item.Width, item.Height), item.BorderStyle, (Border3DSide)item.BorderSides);
            }
        }

        protected override void OnRenderLabelBackground(ToolStripItemRenderEventArgs e)
        {
            if (RendererOverride is not null)
            {
                base.OnRenderLabelBackground(e);
                return;
            }

            RenderLabelInternal(e);
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (RendererOverride is not null)
            {
                base.OnRenderButtonBackground(e);
                return;
            }

            ToolStripButton? item = e.Item as ToolStripButton;
            Graphics g = e.Graphics;
            Rectangle bounds = new Rectangle(Point.Empty, item?.Size ?? Size.Empty);

            if (item is not null && item.CheckState == CheckState.Unchecked)
            {
                RenderItemInternal(e, useHotBorder: true);
            }
            else
            {
                Rectangle fillRect = item is not null && item.Selected ? item.ContentRectangle : bounds;

                if (item?.BackgroundImage is not null)
                {
                    ControlPaint.DrawBackgroundImage(g, item.BackgroundImage, item.BackColor, item.BackgroundImageLayout, bounds, fillRect);
                }

                if (UseSystemColors)
                {
                    if (item is not null && item.Selected)
                    {
                        RenderPressedButtonFill(g, bounds);
                    }
                    else
                    {
                        RenderCheckedButtonFill(g, bounds);
                    }

                    using var pen = ColorTable.ButtonSelectedBorder.GetCachedPenScope();
                    g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                }
                else
                {
                    if (item is not null && item.Selected)
                    {
                        RenderPressedButtonFill(g, bounds);
                    }
                    else
                    {
                        RenderCheckedButtonFill(g, bounds);
                    }

                    using var pen = ColorTable.ButtonSelectedBorder.GetCachedPenScope();
                    g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                }
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (RendererOverride is not null)
            {
                base.OnRenderToolStripBorder(e);
                return;
            }

            ToolStrip toolStrip = e.ToolStrip;
            Graphics g = e.Graphics;

            if (toolStrip is ToolStripDropDown)
            {
                RenderToolStripDropDownBorder(e);
            }
            else if (toolStrip is MenuStrip)
            {
            }
            else if (toolStrip is StatusStrip)
            {
                RenderStatusStripBorder(e);
            }
            else
            {
                Rectangle bounds = new Rectangle(Point.Empty, toolStrip.Size);

                // draw the shadow lines on the bottom and right
                using (var pen = ColorTable.ToolStripBorder.GetCachedPenScope())
                {
                    if (toolStrip.Orientation == Orientation.Horizontal)
                    {
                        // horizontal line at bottom
                        g.DrawLine(pen, bounds.Left, bounds.Height - 1, bounds.Right, bounds.Height - 1);
                        if (RoundedEdges)
                        {
                            // one pix corner rounding (right bottom)
                            g.DrawLine(pen, bounds.Width - 2, bounds.Height - 2, bounds.Width - 1, bounds.Height - 3);
                        }
                    }
                    else
                    {
                        // draw vertical line on the right
                        g.DrawLine(pen, bounds.Width - 1, 0, bounds.Width - 1, bounds.Height - 1);
                        if (RoundedEdges)
                        {
                            // one pix corner rounding (right bottom)
                            g.DrawLine(pen, bounds.Width - 2, bounds.Height - 2, bounds.Width - 1, bounds.Height - 3);
                        }
                    }
                }

                if (RoundedEdges)
                {
                    // OverflowButton rendering
                    if (toolStrip.OverflowButton.Visible)
                    {
                        RenderOverflowButtonEffectsOverBorder(e);
                    }
                    else
                    {
                        // Draw 1PX edging to round off the toolStrip
                        Rectangle edging;
                        if (toolStrip.Orientation == Orientation.Horizontal)
                        {
                            edging = new Rectangle(bounds.Width - 1, 3, 1, bounds.Height - 3);
                        }
                        else
                        {
                            edging = new Rectangle(3, bounds.Height - 1, bounds.Width - 3, bounds.Height - 1);
                        }

                        ScaleObjectSizesIfNeeded(toolStrip.DeviceDpi);
                        FillWithDoubleGradient(ColorTable.OverflowButtonGradientBegin, ColorTable.OverflowButtonGradientMiddle, ColorTable.OverflowButtonGradientEnd, e.Graphics, edging, iconWellGradientWidth, iconWellGradientWidth, LinearGradientMode.Vertical, /*flipHorizontal=*/false);
                        RenderToolStripCurve(e);
                    }
                }
            }
        }

        protected override void OnRenderGrip(ToolStripGripRenderEventArgs e)
        {
            if (RendererOverride is not null)
            {
                base.OnRenderGrip(e);
                return;
            }

            ScaleObjectSizesIfNeeded(e.ToolStrip.DeviceDpi);

            Graphics g = e.Graphics;
            Rectangle bounds = e.GripBounds;
            ToolStrip toolStrip = e.ToolStrip;

            bool rightToLeft = (e.ToolStrip.RightToLeft == RightToLeft.Yes);

            int height = (toolStrip.Orientation == Orientation.Horizontal) ? bounds.Height : bounds.Width;
            int width = (toolStrip.Orientation == Orientation.Horizontal) ? bounds.Width : bounds.Height;

            int numRectangles = (height - (gripPadding * 2)) / 4;

            if (numRectangles > 0)
            {
                // a MenuStrip starts its grip lower and has fewer grip rectangles.
                int yOffset = (toolStrip is MenuStrip) ? 2 : 0;

                Rectangle[] shadowRects = new Rectangle[numRectangles];
                int startY = gripPadding + 1 + yOffset;
                int startX = width / 2;

                for (int i = 0; i < numRectangles; i++)
                {
                    shadowRects[i] = (toolStrip.Orientation == Orientation.Horizontal) ?
                                        new Rectangle(startX, startY, 2, 2) :
                                        new Rectangle(startY, startX, 2, 2);

                    startY += 4;
                }

                // in RTL the GripLight rects should paint to the left of the GripDark rects.
                int xOffset = rightToLeft ? 1 : -1;

                if (rightToLeft)
                {
                    // scoot over the rects in RTL so they fit within the bounds.
                    for (int i = 0; i < numRectangles; i++)
                    {
                        shadowRects[i].Offset(-xOffset, 0);
                    }
                }

                using var gripLightBrush = ColorTable.GripLight.GetCachedSolidBrushScope();
                g.FillRectangles(gripLightBrush, shadowRects);

                for (int i = 0; i < numRectangles; i++)
                {
                    shadowRects[i].Offset(xOffset, -1);
                }

                using var gripDarkBrush = ColorTable.GripDark.GetCachedSolidBrushScope();
                g.FillRectangles(gripDarkBrush, shadowRects);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (RendererOverride is not null)
            {
                base.OnRenderMenuItemBackground(e);
                return;
            }

            ToolStripItem item = e.Item;
            Graphics g = e.Graphics;
            Rectangle bounds = new Rectangle(Point.Empty, item.Size);

            if ((bounds.Width == 0) || (bounds.Height == 0))
            {
                return;  // can't new up a linear gradient brush with no dimension.
            }

            if (item != null && item.GetType().FullName == "System.Windows.Forms.MdiControlStrip.SystemMenuItem")
            {
                return; // no highlights are painted behind a system menu item
            }

            if (item.IsOnDropDown)
            {
                ScaleObjectSizesIfNeeded(item.DeviceDpi);

                bounds = LayoutUtils.DeflateRect(bounds, scaledDropDownMenuItemPaintPadding);

                if (item.Selected)
                {
                    Color borderColor = ColorTable.MenuItemBorder;
                    if (item.Enabled)
                    {
                        if (UseSystemColors)
                        {
                            borderColor = SystemColors.Highlight;
                            RenderSelectedButtonFill(g, bounds);
                        }
                        else
                        {
                            using Brush b = new LinearGradientBrush(
                                bounds,
                                ColorTable.MenuItemSelectedGradientBegin,
                                ColorTable.MenuItemSelectedGradientEnd,
                                LinearGradientMode.Vertical);

                            g.FillRectangle(b, bounds);
                        }
                    }

                    // Draw selection border - always drawn regardless of Enabled.
                    using var pen = borderColor.GetCachedPenScope();
                    g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                }
                else
                {
                    Rectangle fillRect = bounds;

                    if (item.BackgroundImage is not null)
                    {
                        ControlPaint.DrawBackgroundImage(
                            g,
                            item.BackgroundImage,
                            item.BackColor,
                            item.BackgroundImageLayout,
                            bounds,
                            fillRect);
                    }
                    else if (item.Owner is not null && item.BackColor != item.Owner.BackColor)
                    {
                        using var brush = item.BackColor.GetCachedSolidBrushScope();
                        g.FillRectangle(brush, fillRect);
                    }
                }
            }
            else
            {
                if (item.Pressed)
                {
                    // Toplevel toolstrip rendering
                    RenderPressedGradient(g, bounds);
                }
                else if (item.Selected)
                {
                    //Hot, Pressed behavior
                    // Fill with orange
                    Color borderColor = ColorTable.MenuItemBorder;

                    if (item.Enabled)
                    {
                        if (UseSystemColors)
                        {
                            borderColor = SystemColors.Highlight;
                            RenderSelectedButtonFill(g, bounds);
                        }
                        else
                        {
                            using Brush b = new LinearGradientBrush(
                                bounds,
                                ColorTable.MenuItemSelectedGradientBegin,
                                ColorTable.MenuItemSelectedGradientEnd,
                                LinearGradientMode.Vertical);

                            g.FillRectangle(b, bounds);
                        }
                    }

                    // Draw selection border - always drawn regardless of Enabled.
                    using var pen = borderColor.GetCachedPenScope();
                    g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                }
                else
                {
                    Rectangle fillRect = bounds;

                    if (item.BackgroundImage is not null)
                    {
                        ControlPaint.DrawBackgroundImage(g, item.BackgroundImage, item.BackColor, item.BackgroundImageLayout, bounds, fillRect);
                    }
                    else if (item.Owner is not null && item.BackColor != item.Owner.BackColor)
                    {
                        using var brush = item.BackColor.GetCachedSolidBrushScope();
                        g.FillRectangle(brush, fillRect);
                    }
                }
            }
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            if (RendererOverride is not null)
            {
                base.OnRenderArrow(e);
                return;
            }

            ToolStripItem? item = e.Item;

            if (item is ToolStripDropDownItem)
            {
                e.DefaultArrowColor = item.Enabled ? SystemColors.ControlText : SystemColors.ControlDark;
            }

            base.OnRenderArrow(e);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            if (RendererOverride is not null)
            {
                base.OnRenderImageMargin(e);
                return;
            }

            ScaleObjectSizesIfNeeded(e.ToolStrip.DeviceDpi);

            Rectangle bounds = e.AffectedBounds;
            bounds.Y += 2;
            bounds.Height -= 4; /*shrink to accomodate 1PX line*/
            RightToLeft rightToLeft = e.ToolStrip.RightToLeft;
            Color begin = (rightToLeft == RightToLeft.No) ? ColorTable.ImageMarginGradientBegin : ColorTable.ImageMarginGradientEnd;
            Color end = (rightToLeft == RightToLeft.No) ? ColorTable.ImageMarginGradientEnd : ColorTable.ImageMarginGradientBegin;

            FillWithDoubleGradient(begin, ColorTable.ImageMarginGradientMiddle, end, e.Graphics, bounds, iconWellGradientWidth, iconWellGradientWidth, LinearGradientMode.Horizontal, /*flipHorizontal=*/(e.ToolStrip.RightToLeft == RightToLeft.Yes));
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (RendererOverride is not null)
            {
                base.OnRenderItemText(e);
                return;
            }

            if (e.Item is ToolStripMenuItem && (e.Item.Selected || e.Item.Pressed))
            {
                e.DefaultTextColor = e.Item.ForeColor;
            }

            base.OnRenderItemText(e);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            if (RendererOverride is not null)
            {
                base.OnRenderItemCheck(e);
                return;
            }

            RenderCheckBackground(e);
            base.OnRenderItemCheck(e);
        }

        protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
        {
            if (RendererOverride is not null)
            {
                base.OnRenderItemImage(e);
                return;
            }

            Rectangle imageRect = e.ImageRectangle;
            Image? image = e.Image;

            if (e.Item is ToolStripMenuItem)
            {
                ToolStripMenuItem? item = e.Item as ToolStripMenuItem;
                if (item is not null && item.CheckState != CheckState.Unchecked)
                {
                    if (item.ParentInternal is ToolStripDropDownMenu dropDownMenu && !dropDownMenu.ShowCheckMargin && dropDownMenu.ShowImageMargin)
                    {
                        RenderCheckBackground(e);
                    }
                }
            }

            if (imageRect != Rectangle.Empty && image is not null)
            {
                if (!e.Item.Enabled)
                {
                    base.OnRenderItemImage(e);
                    return;
                }

                // Since office images don't scoot one px we have to override all painting but enabled = false;
                if (e.Item.ImageScaling == ToolStripItemImageScaling.None)
                {
                    e.Graphics.DrawImage(image, imageRect, new Rectangle(Point.Empty, imageRect.Size), GraphicsUnit.Pixel);
                }
                else
                {
                    e.Graphics.DrawImage(image, imageRect);
                }
            }
        }

        protected override void OnRenderToolStripPanelBackground(ToolStripPanelRenderEventArgs e)
        {
            if (RendererOverride is not null)
            {
                base.OnRenderToolStripPanelBackground(e);
                return;
            }

            ToolStripPanel toolStripPanel = e.ToolStripPanel;

            if (!ShouldPaintBackground(toolStripPanel))
            {
                return;
            }

            // don't paint background effects
            e.Handled = true;

            RenderBackgroundGradient(e.Graphics, toolStripPanel, ColorTable.ToolStripPanelGradientBegin, ColorTable.ToolStripPanelGradientEnd);
        }

        protected override void OnRenderToolStripContentPanelBackground(ToolStripContentPanelRenderEventArgs e)
        {
            if (RendererOverride is not null)
            {
                base.OnRenderToolStripContentPanelBackground(e);
                return;
            }

            ToolStripContentPanel toolStripContentPanel = e.ToolStripContentPanel;

            if (!ShouldPaintBackground(toolStripContentPanel))
            {
                return;
            }

            if (SystemInformation.InLockedTerminalSession())
            {
                return;
            }

            // don't paint background effects
            e.Handled = true;

            e.Graphics.Clear(ColorTable.ToolStripContentPanelGradientEnd);

            //        RenderBackgroundGradient(e.Graphics, toolStripContentPanel, ColorTable.ToolStripContentPanelGradientBegin, ColorTable.ToolStripContentPanelGradientEnd);
        }

        #region PrivatePaintHelpers

        // consider make public
        internal override Region? GetTransparentRegion(ToolStrip toolStrip)
        {
            if (toolStrip is ToolStripDropDown || toolStrip is MenuStrip || toolStrip is StatusStrip)
            {
                return null;
            }

            if (!RoundedEdges)
            {
                return null;
            }

            Rectangle bounds = new Rectangle(Point.Empty, toolStrip.Size);

            // Render curve
            // eat away at the corners by drawing the parent background
            if (toolStrip.ParentInternal is not null)
            {
                // Paint pieces of the parent here to give toolStrip rounded effect
                Point topLeft = Point.Empty;
                Point topRight = new Point(bounds.Width - 1, 0);
                Point bottomLeft = new Point(0, bounds.Height - 1);
                Point bottomRight = new Point(bounds.Width - 1, bounds.Height - 1);

                // Pixels to eat away with the parent background
                // Grip side
                Rectangle topLeftParentHorizontalPixels = new Rectangle(topLeft, onePix);
                Rectangle bottomLeftParentHorizontalPixels = new Rectangle(bottomLeft, new Size(2, 1));
                Rectangle bottomLeftParentVerticalPixels = new Rectangle(bottomLeft.X, bottomLeft.Y - 1, 1, 2);

                // OverflowSide
                Rectangle bottomRightHorizontalPixels = new Rectangle(bottomRight.X - 1, bottomRight.Y, 2, 1);
                Rectangle bottomRightVerticalPixels = new Rectangle(bottomRight.X, bottomRight.Y - 1, 1, 2);

                // TopSide
                Rectangle topRightHorizontalPixels, topRightVerticalPixels;

                if (toolStrip.OverflowButton.Visible)
                {
                    topRightHorizontalPixels = new Rectangle(topRight.X - 1, topRight.Y, 1, 1);
                    topRightVerticalPixels = new Rectangle(topRight.X, topRight.Y, 1, 2);
                }
                else
                {
                    topRightHorizontalPixels = new Rectangle(topRight.X - 2, topRight.Y, 2, 1);
                    topRightVerticalPixels = new Rectangle(topRight.X, topRight.Y, 1, 3);
                }

                Region parentRegionToPaint = new Region(topLeftParentHorizontalPixels);
                parentRegionToPaint.Union(topLeftParentHorizontalPixels);
                parentRegionToPaint.Union(bottomLeftParentHorizontalPixels);
                parentRegionToPaint.Union(bottomLeftParentVerticalPixels);
                parentRegionToPaint.Union(bottomRightHorizontalPixels);
                parentRegionToPaint.Union(bottomRightVerticalPixels);
                parentRegionToPaint.Union(topRightHorizontalPixels);
                parentRegionToPaint.Union(topRightVerticalPixels);

                return parentRegionToPaint;
            }

            return null;
        }

        /// <summary>
        /// We want to make sure the overflow button looks like it's the last thing on the toolbar.
        /// This touches up the few pixels that get clobbered by painting the border.
        /// </summary>
        private void RenderOverflowButtonEffectsOverBorder(ToolStripRenderEventArgs e)
        {
            ToolStrip toolStrip = e.ToolStrip;
            ToolStripItem item = toolStrip.OverflowButton;
            if (!item.Visible)
            {
                return;
            }

            Graphics g = e.Graphics;

            Color overflowBottomLeftShadow, overflowTopShadow;

            if (item.Pressed)
            {
                overflowBottomLeftShadow = ColorTable.ButtonPressedGradientBegin;
                overflowTopShadow = overflowBottomLeftShadow;
            }
            else if (item.Selected)
            {
                overflowBottomLeftShadow = ColorTable.ButtonSelectedGradientMiddle;
                overflowTopShadow = overflowBottomLeftShadow;
            }
            else
            {
                overflowBottomLeftShadow = ColorTable.ToolStripBorder;
                overflowTopShadow = ColorTable.ToolStripGradientMiddle;
            }

            // Extend the gradient color over the border.
            using (var brush = overflowBottomLeftShadow.GetCachedSolidBrushScope())
            {
                g.FillRectangle(brush, toolStrip.Width - 1, toolStrip.Height - 2, 1, 1);
                g.FillRectangle(brush, toolStrip.Width - 2, toolStrip.Height - 1, 1, 1);
            }

            using (var brush = overflowTopShadow.GetCachedSolidBrushScope())
            {
                g.FillRectangle(brush, toolStrip.Width - 2, 0, 1, 1);
                g.FillRectangle(brush, toolStrip.Width - 1, 1, 1, 1);
            }
        }

        /// <summary>
        ///  This function paints with three colors, beginning, middle, and end.
        ///  it paints:
        ///  (1)the entire bounds in the middle color
        ///  (2)gradient from beginning to middle of width firstGradientWidth
        ///  (3)gradient from middle to end of width secondGradientWidth
        ///
        ///  if there isn't enough room to do (2) and (3) it merges into a single gradient from beginning to end.
        /// </summary>
        private static void FillWithDoubleGradient(Color beginColor, Color middleColor, Color endColor, Graphics g, Rectangle bounds, int firstGradientWidth, int secondGradientWidth, LinearGradientMode mode, bool flipHorizontal)
        {
            if ((bounds.Width == 0) || (bounds.Height == 0))
            {
                return;  // can't new up a linear gradient brush with no dimension.
            }

            Rectangle endGradient = bounds;
            Rectangle beginGradient = bounds;
            bool useDoubleGradient;

            if (mode == LinearGradientMode.Horizontal)
            {
                if (flipHorizontal)
                {
                    Color temp = endColor;
                    endColor = beginColor;
                    beginColor = temp;
                }

                beginGradient.Width = firstGradientWidth;
                endGradient.Width = secondGradientWidth + 1;
                endGradient.X = bounds.Right - endGradient.Width;
                useDoubleGradient = (bounds.Width > (firstGradientWidth + secondGradientWidth));
            }
            else
            {
                beginGradient.Height = firstGradientWidth;
                endGradient.Height = secondGradientWidth + 1;
                endGradient.Y = bounds.Bottom - endGradient.Height;
                useDoubleGradient = (bounds.Height > (firstGradientWidth + secondGradientWidth));
            }

            if (useDoubleGradient)
            {
                // Fill with middleColor
                using (var brush = middleColor.GetCachedSolidBrushScope())
                {
                    g.FillRectangle(brush, bounds);
                }

                // draw first gradient
                using (Brush b = new LinearGradientBrush(beginGradient, beginColor, middleColor, mode))
                {
                    g.FillRectangle(b, beginGradient);
                }

                // draw second gradient
                using (LinearGradientBrush b = new LinearGradientBrush(endGradient, middleColor, endColor, mode))
                {
                    if (mode == LinearGradientMode.Horizontal)
                    {
                        endGradient.X += 1;
                        endGradient.Width -= 1;
                    }
                    else
                    {
                        endGradient.Y += 1;
                        endGradient.Height -= 1;
                    }

                    g.FillRectangle(b, endGradient);
                }
            }
            else
            {
                // not big enough for a swath in the middle.  lets just do a single gradient.
                using Brush b = new LinearGradientBrush(bounds, beginColor, endColor, mode);
                g.FillRectangle(b, bounds);
            }
        }

        private void RenderStatusStripBorder(ToolStripRenderEventArgs e)
        {
            using Pen p = new Pen(ColorTable.StatusStripBorder);
            e.Graphics.DrawLine(p, 0, 0, e.ToolStrip.Width, 0);
        }

        private void RenderStatusStripBackground(ToolStripRenderEventArgs e)
        {
            StatusStrip? statusStrip = e.ToolStrip as StatusStrip;
            if (statusStrip is not null)
            {
                RenderBackgroundGradient(
                    e.Graphics,
                    statusStrip,
                    ColorTable.StatusStripGradientBegin,
                    ColorTable.StatusStripGradientEnd,
                    statusStrip.Orientation);
            }
        }

        private void RenderCheckBackground(ToolStripItemImageRenderEventArgs e)
        {
            Rectangle bounds = DpiHelper.IsScalingRequired
                ? new Rectangle(
                    e.ImageRectangle.Left - 2,
                    (e.Item.Height - e.ImageRectangle.Height) / 2 - 1,
                    e.ImageRectangle.Width + 4, e.ImageRectangle.Height + 2)
                : new Rectangle(e.ImageRectangle.Left - 2, 1, e.ImageRectangle.Width + 4, e.Item.Height - 2);

            Graphics g = e.Graphics;

            if (!UseSystemColors)
            {
                Color fill = (e.Item.Selected) ? ColorTable.CheckSelectedBackground : ColorTable.CheckBackground;
                fill = (e.Item.Pressed) ? ColorTable.CheckPressedBackground : fill;
                using var brush = fill.GetCachedSolidBrushScope();
                g.FillRectangle(brush, bounds);

                using var pen = ColorTable.ButtonSelectedBorder.GetCachedPenScope();
                g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            }
            else
            {
                if (e.Item.Pressed)
                {
                    RenderPressedButtonFill(g, bounds);
                }
                else
                {
                    RenderSelectedButtonFill(g, bounds);
                }

                g.DrawRectangle(SystemPens.Highlight, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            }
        }

        private void RenderPressedGradient(Graphics g, Rectangle bounds)
        {
            if ((bounds.Width == 0) || (bounds.Height == 0))
            {
                return;  // can't new up a linear gradient brush with no dimension.
            }

            // Paints a horizontal gradient similar to the image margin.
            using Brush b = new LinearGradientBrush(
                bounds,
                ColorTable.MenuItemPressedGradientBegin,
                ColorTable.MenuItemPressedGradientEnd,
                LinearGradientMode.Vertical);
            g.FillRectangle(b, bounds);

            // draw a box around the gradient
            using var pen = ColorTable.MenuBorder.GetCachedPenScope();
            g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        }

        private void RenderMenuStripBackground(ToolStripRenderEventArgs e)
        {
            RenderBackgroundGradient(e.Graphics, e.ToolStrip, ColorTable.MenuStripGradientBegin, ColorTable.MenuStripGradientEnd, e.ToolStrip.Orientation);
        }

        private static void RenderLabelInternal(ToolStripItemRenderEventArgs e)
        {
            Graphics g = e.Graphics;
            ToolStripItem item = e.Item;
            Rectangle bounds = new Rectangle(Point.Empty, item.Size);

            Rectangle fillRect = item.Selected ? item.ContentRectangle : bounds;

            if (item.BackgroundImage is not null)
            {
                ControlPaint.DrawBackgroundImage(g, item.BackgroundImage, item.BackColor, item.BackgroundImageLayout, bounds, fillRect);
            }
        }

        private static void RenderBackgroundGradient(Graphics g, Control control, Color beginColor, Color endColor)
        {
            RenderBackgroundGradient(g, control, beginColor, endColor, Orientation.Horizontal);
        }

        // renders the overall gradient
        private static void RenderBackgroundGradient(Graphics g, Control control, Color beginColor, Color endColor, Orientation orientation)
        {
            if (control.RightToLeft == RightToLeft.Yes)
            {
                Color temp = beginColor;
                beginColor = endColor;
                endColor = temp;
            }

            if (orientation != Orientation.Horizontal)
            {
                using var brush = beginColor.GetCachedSolidBrushScope();
                g.FillRectangle(brush, new Rectangle(Point.Empty, control.Size));
                return;
            }

            Control? parent = control.ParentInternal;
            if (parent is not null)
            {
                Rectangle gradientBounds = new Rectangle(Point.Empty, parent.Size);
                if (!LayoutUtils.IsZeroWidthOrHeight(gradientBounds))
                {
                    using LinearGradientBrush b = new LinearGradientBrush(
                        gradientBounds,
                        beginColor,
                        endColor,
                        LinearGradientMode.Horizontal);
                    b.TranslateTransform(parent.Width - control.Location.X, parent.Height - control.Location.Y);
                    g.FillRectangle(b, new Rectangle(Point.Empty, control.Size));
                }
            }
            else
            {
                Rectangle gradientBounds = new Rectangle(Point.Empty, control.Size);
                if (!LayoutUtils.IsZeroWidthOrHeight(gradientBounds))
                {
                    // Don't have a parent that we know about - paint the gradient as if there isn't another container.
                    using LinearGradientBrush b = new LinearGradientBrush(
                        gradientBounds,
                        beginColor,
                        endColor,
                        LinearGradientMode.Horizontal);
                    g.FillRectangle(b, gradientBounds);
                }
            }
        }

        private void RenderToolStripBackgroundInternal(ToolStripRenderEventArgs e)
        {
            ScaleObjectSizesIfNeeded(e.ToolStrip.DeviceDpi);

            ToolStrip toolStrip = e.ToolStrip;
            Rectangle bounds = new Rectangle(Point.Empty, e.ToolStrip.Size);

            // fill up the background
            LinearGradientMode mode = (toolStrip.Orientation == Orientation.Horizontal) ? LinearGradientMode.Vertical : LinearGradientMode.Horizontal;
            FillWithDoubleGradient(ColorTable.ToolStripGradientBegin, ColorTable.ToolStripGradientMiddle, ColorTable.ToolStripGradientEnd, e.Graphics, bounds, iconWellGradientWidth, iconWellGradientWidth, mode, /*flipHorizontal=*/false);
        }

        private void RenderToolStripDropDownBackground(ToolStripRenderEventArgs e)
        {
            Rectangle bounds = new Rectangle(Point.Empty, e.ToolStrip.Size);

            using var brush = ColorTable.ToolStripDropDownBackground.GetCachedSolidBrushScope();
            e.Graphics.FillRectangle(brush, bounds);
        }

        private void RenderToolStripDropDownBorder(ToolStripRenderEventArgs e)
        {
            Graphics g = e.Graphics;

            if (e.ToolStrip is ToolStripDropDown toolStripDropDown)
            {
                Rectangle bounds = new Rectangle(Point.Empty, toolStripDropDown.Size);

                using (var pen = ColorTable.MenuBorder.GetCachedPenScope())
                {
                    g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                }

                if (!(toolStripDropDown is ToolStripOverflow))
                {
                    // make the neck connected.
                    using var brush = ColorTable.ToolStripDropDownBackground.GetCachedSolidBrushScope();
                    g.FillRectangle(brush, e.ConnectedArea);
                }
            }
        }

        private void RenderOverflowBackground(ToolStripItemRenderEventArgs e, bool rightToLeft)
        {
            ScaleObjectSizesIfNeeded(e.Item.DeviceDpi);

            Graphics g = e.Graphics;
            ToolStripOverflowButton? item = e.Item as ToolStripOverflowButton;
            Rectangle overflowBoundsFill = new Rectangle(Point.Empty, e.Item.Size);
            Rectangle bounds = overflowBoundsFill;

            bool drawCurve = RoundedEdges && (item?.GetCurrentParent() is not MenuStrip);
            bool horizontal = e.ToolStrip is not null && e.ToolStrip.Orientation == Orientation.Horizontal;
            // undone RTL

            if (horizontal)
            {
                overflowBoundsFill.X += overflowBoundsFill.Width - overflowButtonWidth + 1;
                overflowBoundsFill.Width = overflowButtonWidth;
                if (rightToLeft)
                {
                    overflowBoundsFill = LayoutUtils.RTLTranslate(overflowBoundsFill, bounds);
                }
            }
            else
            {
                overflowBoundsFill.Y = overflowBoundsFill.Height - overflowButtonWidth + 1;
                overflowBoundsFill.Height = overflowButtonWidth;
            }

            Color overflowButtonGradientBegin, overflowButtonGradientMiddle, overflowButtonGradientEnd, overflowBottomLeftShadow, overflowTopShadow;

            if (item is not null && item.Pressed)
            {
                overflowButtonGradientBegin = ColorTable.ButtonPressedGradientBegin;
                overflowButtonGradientMiddle = ColorTable.ButtonPressedGradientMiddle;
                overflowButtonGradientEnd = ColorTable.ButtonPressedGradientEnd;
                overflowBottomLeftShadow = ColorTable.ButtonPressedGradientBegin;
                overflowTopShadow = overflowBottomLeftShadow;
            }
            else if (item is not null && item.Selected)
            {
                overflowButtonGradientBegin = ColorTable.ButtonSelectedGradientBegin;
                overflowButtonGradientMiddle = ColorTable.ButtonSelectedGradientMiddle;
                overflowButtonGradientEnd = ColorTable.ButtonSelectedGradientEnd;
                overflowBottomLeftShadow = ColorTable.ButtonSelectedGradientMiddle;
                overflowTopShadow = overflowBottomLeftShadow;
            }
            else
            {
                overflowButtonGradientBegin = ColorTable.OverflowButtonGradientBegin;
                overflowButtonGradientMiddle = ColorTable.OverflowButtonGradientMiddle;
                overflowButtonGradientEnd = ColorTable.OverflowButtonGradientEnd;
                overflowBottomLeftShadow = ColorTable.ToolStripBorder;
                overflowTopShadow = horizontal ? ColorTable.ToolStripGradientMiddle : ColorTable.ToolStripGradientEnd;
            }

            if (drawCurve)
            {
                // draw shadow pixel on bottom left +1, +1
                using var pen = overflowBottomLeftShadow.GetCachedPenScope();
                Point start = new Point(overflowBoundsFill.Left - 1, overflowBoundsFill.Height - 2);
                Point end = new Point(overflowBoundsFill.Left, overflowBoundsFill.Height - 2);
                if (rightToLeft)
                {
                    start.X = overflowBoundsFill.Right + 1;
                    end.X = overflowBoundsFill.Right;
                }

                g.DrawLine(pen, start, end);
            }

            LinearGradientMode mode = horizontal ? LinearGradientMode.Vertical : LinearGradientMode.Horizontal;

            // fill main body
            FillWithDoubleGradient(overflowButtonGradientBegin, overflowButtonGradientMiddle, overflowButtonGradientEnd, g, overflowBoundsFill, iconWellGradientWidth, iconWellGradientWidth, mode, false);

            if (!drawCurve)
            {
                return;
            }

            // Render shadow pixels (ToolStrip only)

            // top left and top right shadow pixels
            using (var brush = overflowTopShadow.GetCachedSolidBrushScope())
            {
                if (horizontal)
                {
                    Point top1 = new Point(overflowBoundsFill.X - 2, 0);
                    Point top2 = new Point(overflowBoundsFill.X - 1, 1);

                    if (rightToLeft)
                    {
                        top1.X = overflowBoundsFill.Right + 1;
                        top2.X = overflowBoundsFill.Right;
                    }

                    g.FillRectangle(brush, top1.X, top1.Y, 1, 1);
                    g.FillRectangle(brush, top2.X, top2.Y, 1, 1);
                }
                else
                {
                    g.FillRectangle(brush, overflowBoundsFill.Width - 3, overflowBoundsFill.Top - 1, 1, 1);
                    g.FillRectangle(brush, overflowBoundsFill.Width - 2, overflowBoundsFill.Top - 2, 1, 1);
                }
            }

            using (var brush = overflowButtonGradientBegin.GetCachedSolidBrushScope())
            {
                if (horizontal)
                {
                    Rectangle fillRect = new Rectangle(overflowBoundsFill.X - 1, 0, 1, 1);
                    if (rightToLeft)
                    {
                        fillRect.X = overflowBoundsFill.Right;
                    }

                    g.FillRectangle(brush, fillRect);
                }
                else
                {
                    g.FillRectangle(brush, overflowBoundsFill.X, overflowBoundsFill.Top - 1, 1, 1);
                }
            }
        }

        private void RenderToolStripCurve(ToolStripRenderEventArgs e)
        {
            Rectangle bounds = new Rectangle(Point.Empty, e.ToolStrip.Size);
            ToolStrip toolStrip = e.ToolStrip;
            Rectangle displayRect = toolStrip.DisplayRectangle;

            Graphics g = e.Graphics;

            Point topLeft = Point.Empty;
            Point topRight = new Point(bounds.Width - 1, 0);
            Point bottomLeft = new Point(0, bounds.Height - 1);

            // Add in shadow pixels - the detail that makes them look round

            // Draw in rounded shadow pixels on the top left & right (consider: if this is slow use precanned corners)
            using (var brush = ColorTable.ToolStripGradientMiddle.GetCachedSolidBrushScope())
            {
                // there are two shadow rects (one pixel wide) on the top
                Rectangle topLeftShadowRect = new Rectangle(topLeft, onePix);
                topLeftShadowRect.X += 1;

                // second shadow rect
                Rectangle topLeftShadowRect2 = new Rectangle(topLeft, onePix);
                topLeftShadowRect2.Y += 1;

                // on the right there are two more shadow rects
                Rectangle topRightShadowRect = new Rectangle(topRight, onePix);
                topRightShadowRect.X -= 2; // was 2?

                // second top right shadow pix
                Rectangle topRightShadowRect2 = topRightShadowRect;
                topRightShadowRect2.Y += 1;
                topRightShadowRect2.X += 1;

                Rectangle[] paintRects = new Rectangle[] { topLeftShadowRect, topLeftShadowRect2, topRightShadowRect, topRightShadowRect2 };

                // prevent the painting of anything that would obscure an item.
                for (int i = 0; i < paintRects.Length; i++)
                {
                    if (displayRect.IntersectsWith(paintRects[i]))
                    {
                        paintRects[i] = Rectangle.Empty;
                    }
                }

                g.FillRectangles(brush, paintRects);
            }

            // Draw in rounded shadow pixels on the bottom left
            using (var brush = ColorTable.ToolStripGradientEnd.GetCachedSolidBrushScope())
            {
                // this gradient is the one just before the dark shadow line starts on pixel #3.
                Point gradientCopyPixel = bottomLeft;
                gradientCopyPixel.Offset(1, -1);
                if (!displayRect.Contains(gradientCopyPixel))
                {
                    g.FillRectangle(brush, new Rectangle(gradientCopyPixel, onePix));
                }

                // set the one dark pixel in the bottom left hand corner
                Rectangle otherBottom = new Rectangle(bottomLeft.X, bottomLeft.Y - 2, 1, 1);
                if (!displayRect.IntersectsWith(otherBottom))
                {
                    g.FillRectangle(brush, otherBottom);
                }
            }
        }

        private void RenderSelectedButtonFill(Graphics g, Rectangle bounds)
        {
            if ((bounds.Width == 0) || (bounds.Height == 0))
            {
                return;  // can't new up a linear gradient brush with no dimension.
            }

            if (!UseSystemColors)
            {
                using Brush b = new LinearGradientBrush(
                    bounds,
                    ColorTable.ButtonSelectedGradientBegin,
                    ColorTable.ButtonSelectedGradientEnd,
                    LinearGradientMode.Vertical);

                g.FillRectangle(b, bounds);
            }
            else
            {
                using var brush = ColorTable.ButtonSelectedHighlight.GetCachedSolidBrushScope();
                g.FillRectangle(brush, bounds);
            }
        }

        private void RenderCheckedButtonFill(Graphics g, Rectangle bounds)
        {
            if ((bounds.Width == 0) || (bounds.Height == 0))
            {
                return;  // can't new up a linear gradient brush with no dimension.
            }

            if (!UseSystemColors)
            {
                using Brush b = new LinearGradientBrush(
                    bounds,
                    ColorTable.ButtonCheckedGradientBegin,
                    ColorTable.ButtonCheckedGradientEnd,
                    LinearGradientMode.Vertical);

                g.FillRectangle(b, bounds);
            }
            else
            {
                using var brush = ColorTable.ButtonCheckedHighlight.GetCachedSolidBrushScope();
                g.FillRectangle(brush, bounds);
            }
        }

        private void RenderSeparatorInternal(Graphics g, ToolStripItem item, Rectangle bounds, bool vertical)
        {
            bool isSeparator = item is ToolStripSeparator;
            bool isHorizontalSeparatorNotOnDropDownMenu = false;

            if (isSeparator)
            {
                if (vertical)
                {
                    if (!item.IsOnDropDown)
                    {
                        // center so that it matches office
                        bounds.Y += 3;
                        bounds.Height = Math.Max(0, bounds.Height - 6);
                    }
                }
                else
                {
                    // offset after the image margin
                    if (item.GetCurrentParent() is ToolStripDropDownMenu dropDownMenu)
                    {
                        if (dropDownMenu.RightToLeft == RightToLeft.No)
                        {
                            // scoot over by the padding (that will line you up with the text - but go two PX before so that it visually looks
                            // like the line meets up with the text).
                            bounds.X += dropDownMenu.Padding.Left - 2;
                            bounds.Width = dropDownMenu.Width - bounds.X;
                        }
                        else
                        {
                            // scoot over by the padding (that will line you up with the text - but go two PX before so that it visually looks
                            // like the line meets up with the text).
                            bounds.X += 2;
                            bounds.Width = dropDownMenu.Width - bounds.X - dropDownMenu.Padding.Right;
                        }
                    }
                    else
                    {
                        isHorizontalSeparatorNotOnDropDownMenu = true;
                    }
                }
            }

            using var foreColorPen = ColorTable.SeparatorDark.GetCachedPenScope();
            using var highlightColorPen = ColorTable.SeparatorLight.GetCachedPenScope();

            if (vertical)
            {
                if (bounds.Height >= 4)
                {
                    bounds.Inflate(0, -2);     // scoot down 2PX and start drawing
                }

                bool rightToLeft = (item.RightToLeft == RightToLeft.Yes);
                Pen leftPen = (rightToLeft) ? highlightColorPen : foreColorPen;
                Pen rightPen = (rightToLeft) ? foreColorPen : highlightColorPen;

                // Draw dark line
                int startX = bounds.Width / 2;

                g.DrawLine(leftPen, startX, bounds.Top, startX, bounds.Bottom - 1);

                // Draw highlight one pixel to the right
                startX++;
                g.DrawLine(rightPen, startX, bounds.Top + 1, startX, bounds.Bottom);
            }
            else
            {
                // Horizontal separator- draw dark line

                if (isHorizontalSeparatorNotOnDropDownMenu && bounds.Width >= 4)
                {
                    bounds.Inflate(-2, 0);     // scoot down 2PX and start drawing
                }

                int startY = bounds.Height / 2;

                g.DrawLine(foreColorPen, bounds.Left, startY, bounds.Right - 1, startY);

                if (!isSeparator || isHorizontalSeparatorNotOnDropDownMenu)
                {
                    // Draw highlight one pixel to the right
                    startY++;
                    g.DrawLine(highlightColorPen, bounds.Left + 1, startY, bounds.Right - 1, startY);
                }
            }
        }

        private void RenderPressedButtonFill(Graphics g, Rectangle bounds)
        {
            if ((bounds.Width == 0) || (bounds.Height == 0))
            {
                return;  // can't new up a linear gradient brush with no dimension.
            }

            if (!UseSystemColors)
            {
                using Brush b = new LinearGradientBrush(
                    bounds,
                    ColorTable.ButtonPressedGradientBegin,
                    ColorTable.ButtonPressedGradientEnd,
                    LinearGradientMode.Vertical);
                g.FillRectangle(b, bounds);
            }
            else
            {
                using var brush = ColorTable.ButtonPressedHighlight.GetCachedSolidBrushScope();
                g.FillRectangle(brush, bounds);
            }
        }

        private void RenderItemInternal(ToolStripItemRenderEventArgs e, bool useHotBorder)
        {
            Graphics g = e.Graphics;
            ToolStripItem item = e.Item;
            Rectangle bounds = new Rectangle(Point.Empty, item.Size);
            bool drawHotBorder = false;

            Rectangle fillRect = (item.Selected) ? item.ContentRectangle : bounds;

            if (item.BackgroundImage is not null)
            {
                ControlPaint.DrawBackgroundImage(g, item.BackgroundImage, item.BackColor, item.BackgroundImageLayout, bounds, fillRect);
            }

            if (item.Pressed)
            {
                RenderPressedButtonFill(g, bounds);
                drawHotBorder = useHotBorder;
            }
            else if (item.Selected)
            {
                RenderSelectedButtonFill(g, bounds);
                drawHotBorder = useHotBorder;
            }
            else if (item.Owner is not null && item.BackColor != item.Owner.BackColor)
            {
                using var brush = item.BackColor.GetCachedSolidBrushScope();
                g.FillRectangle(brush, bounds);
            }

            if (drawHotBorder)
            {
                using var pen = ColorTable.ButtonSelectedBorder.GetCachedPenScope();
                g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            }
        }

        private void ScaleObjectSizesIfNeeded(int currentDeviceDpi)
        {
            if (DpiHelper.IsPerMonitorV2Awareness)
            {
                if (previousDeviceDpi != currentDeviceDpi)
                {
                    ScaleArrowOffsetsIfNeeded(currentDeviceDpi);
                    overflowButtonWidth = DpiHelper.LogicalToDeviceUnits(OVERFLOW_BUTTON_WIDTH, currentDeviceDpi);
                    overflowArrowWidth = DpiHelper.LogicalToDeviceUnits(OVERFLOW_ARROW_WIDTH, currentDeviceDpi);
                    overflowArrowHeight = DpiHelper.LogicalToDeviceUnits(OVERFLOW_ARROW_HEIGHT, currentDeviceDpi);
                    overflowArrowOffsetY = DpiHelper.LogicalToDeviceUnits(OVERFLOW_ARROW_OFFSETY, currentDeviceDpi);

                    gripPadding = DpiHelper.LogicalToDeviceUnits(GRIP_PADDING, currentDeviceDpi);
                    iconWellGradientWidth = DpiHelper.LogicalToDeviceUnits(ICON_WELL_GRADIENT_WIDTH, currentDeviceDpi);
                    int scaledSize = DpiHelper.LogicalToDeviceUnits(DROP_DOWN_MENU_ITEM_PAINT_PADDING_SIZE, currentDeviceDpi);
                    scaledDropDownMenuItemPaintPadding = new Padding(scaledSize + 1, 0, scaledSize, 0);
                    previousDeviceDpi = currentDeviceDpi;
                    isScalingInitialized = true;
                    return;
                }
            }

            if (isScalingInitialized)
            {
                return;
            }

            if (DpiHelper.IsScalingRequired)
            {
                ScaleArrowOffsetsIfNeeded();
                overflowButtonWidth = DpiHelper.LogicalToDeviceUnitsX(OVERFLOW_BUTTON_WIDTH);
                overflowArrowWidth = DpiHelper.LogicalToDeviceUnitsX(OVERFLOW_ARROW_WIDTH);
                overflowArrowHeight = DpiHelper.LogicalToDeviceUnitsY(OVERFLOW_ARROW_HEIGHT);
                overflowArrowOffsetY = DpiHelper.LogicalToDeviceUnitsY(OVERFLOW_ARROW_OFFSETY);

                gripPadding = DpiHelper.LogicalToDeviceUnitsY(GRIP_PADDING);
                iconWellGradientWidth = DpiHelper.LogicalToDeviceUnitsX(ICON_WELL_GRADIENT_WIDTH);
                int scaledSize = DpiHelper.LogicalToDeviceUnitsX(DROP_DOWN_MENU_ITEM_PAINT_PADDING_SIZE);
                scaledDropDownMenuItemPaintPadding = new Padding(scaledSize + 1, 0, scaledSize, 0);
            }

            isScalingInitialized = true;
        }

        // This draws differently sized arrows than the base one...
        // used only for drawing the overflow button madness.
        private static Point RenderArrowInternal(Graphics g, Rectangle dropDownRect, ArrowDirection direction, Brush brush)
        {
            Point middle = new Point(dropDownRect.Left + dropDownRect.Width / 2, dropDownRect.Top + dropDownRect.Height / 2);

            // if the width is odd - favor pushing it over one pixel right.
            middle.X += (dropDownRect.Width % 2);

            Point[] arrow;

            switch (direction)
            {
                case ArrowDirection.Up:
                    arrow = new Point[]
                    {
                    new Point(middle.X - Offset2X, middle.Y + 1),
                    new Point(middle.X + Offset2X + 1, middle.Y + 1),
                    new Point(middle.X, middle.Y - Offset2Y)
                    };
                    break;

                case ArrowDirection.Left:
                    arrow = new Point[]
                    {
                    new Point(middle.X + Offset2X, middle.Y - Offset2Y - 1),
                    new Point(middle.X + Offset2X, middle.Y + Offset2Y + 1),
                    new Point(middle.X - 1, middle.Y)
                    };
                    break;

                case ArrowDirection.Right:
                    arrow = new Point[]
                    {
                    new Point(middle.X - Offset2X, middle.Y - Offset2Y - 1),
                    new Point(middle.X - Offset2X, middle.Y + Offset2Y + 1),
                    new Point(middle.X + 1, middle.Y)
                    };
                    break;

                case ArrowDirection.Down:
                default:
                    arrow = new Point[]
                    {
                    new Point(middle.X - Offset2X, middle.Y - 1),
                    new Point(middle.X + Offset2X + 1, middle.Y - 1),
                    new Point(middle.X, middle.Y + Offset2Y)
                    };
                    break;
            }

            g.FillPolygon(brush, arrow);

            return middle;
        }

        #endregion PrivatePaintHelpers
    }

    // Utilities used by layout code.  If you use these outside of the layout
    // namespace, you should probably move them to WindowsFormsUtils.
    internal partial class LayoutUtils
    {
        public static readonly Size s_maxSize = new(int.MaxValue, int.MaxValue);
        public static readonly Size s_invalidSize = new(int.MinValue, int.MinValue);

        public static readonly Rectangle s_maxRectangle = new(0, 0, int.MaxValue, int.MaxValue);

        public const ContentAlignment AnyTop = ContentAlignment.TopLeft | ContentAlignment.TopCenter | ContentAlignment.TopRight;
        public const ContentAlignment AnyBottom = ContentAlignment.BottomLeft | ContentAlignment.BottomCenter | ContentAlignment.BottomRight;
        public const ContentAlignment AnyLeft = ContentAlignment.TopLeft | ContentAlignment.MiddleLeft | ContentAlignment.BottomLeft;
        public const ContentAlignment AnyRight = ContentAlignment.TopRight | ContentAlignment.MiddleRight | ContentAlignment.BottomRight;
        public const ContentAlignment AnyCenter = ContentAlignment.TopCenter | ContentAlignment.MiddleCenter | ContentAlignment.BottomCenter;
        public const ContentAlignment AnyMiddle = ContentAlignment.MiddleLeft | ContentAlignment.MiddleCenter | ContentAlignment.MiddleRight;

        public const AnchorStyles HorizontalAnchorStyles = AnchorStyles.Left | AnchorStyles.Right;
        public const AnchorStyles VerticalAnchorStyles = AnchorStyles.Top | AnchorStyles.Bottom;

        private static readonly AnchorStyles[] s_dockingToAnchor = new AnchorStyles[]
        {
        /* None   */ AnchorStyles.Top | AnchorStyles.Left,
        /* Top    */ AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        /* Bottom */ AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        /* Left   */ AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom,
        /* Right  */ AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
        /* Fill   */ AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
        };

        // A good, short test string for measuring control height.
        public const string TestString = "j^";

        // Returns the size of the largest string in the given collection. Non-string objects are converted
        // with ToString(). Uses OldMeasureString, not GDI+. Does not support multiline.
        public static Size OldGetLargestStringSizeInCollection(Font? font, ICollection? objects)
        {
            Size largestSize = Size.Empty;
            if (objects is not null)
            {
                foreach (object obj in objects)
                {
                    Size textSize = TextRenderer.MeasureText(obj.ToString(), font, new Size(short.MaxValue, short.MaxValue), TextFormatFlags.SingleLine);
                    largestSize.Width = Math.Max(largestSize.Width, textSize.Width);
                    largestSize.Height = Math.Max(largestSize.Height, textSize.Height);
                }
            }

            return largestSize;
        }

        /*
         *  We can cut ContentAlignment from a max index of 1024 (12b) down to 11 (4b) through
         *  bit twiddling.  The int result of this function maps to the ContentAlignment as indicated
         *  by the table below:
         *
         *          Left      Center    Right
         *  Top     0000 0x0  0001 0x1  0010 0x2
         *  Middle  0100 0x4  0101 0x5  0110 0x6
         *  Bottom  1000 0x8  1001 0x9  1010 0xA
         *
         *  (The high 2 bits determine T/M/B.  The low 2 bits determine L/C/R.)
         */

        public static int ContentAlignmentToIndex(ContentAlignment alignment)
        {
            /*
             *  Here is what content alignment looks like coming in:
             *
             *          Left    Center  Right
             *  Top     0x001   0x002   0x004
             *  Middle  0x010   0x020   0x040
             *  Bottom  0x100   0x200   0x400
             *
             *  (L/C/R determined bit 1,2,4.  T/M/B determined by 4 bit shift.)
             */

            int topBits = xContentAlignmentToIndex(((int)alignment) & 0x0F);
            int middleBits = xContentAlignmentToIndex(((int)alignment >> 4) & 0x0F);
            int bottomBits = xContentAlignmentToIndex(((int)alignment >> 8) & 0x0F);

            Debug.Assert((topBits != 0 && (middleBits == 0 && bottomBits == 0))
                || (middleBits != 0 && (topBits == 0 && bottomBits == 0))
                || (bottomBits != 0 && (topBits == 0 && middleBits == 0)),
                "One (and only one) of topBits, middleBits, or bottomBits should be non-zero.");

            int result = (middleBits != 0 ? 0x04 : 0) | (bottomBits != 0 ? 0x08 : 0) | topBits | middleBits | bottomBits;

            // zero isn't used, so we can subtract 1 and start with index 0.
            result--;

            Debug.Assert(result >= 0x00 && result <= 0x0A, "ContentAlignmentToIndex result out of range.");
            Debug.Assert(result != 0x00 || alignment == ContentAlignment.TopLeft, "Error detected in ContentAlignmentToIndex.");
            Debug.Assert(result != 0x01 || alignment == ContentAlignment.TopCenter, "Error detected in ContentAlignmentToIndex.");
            Debug.Assert(result != 0x02 || alignment == ContentAlignment.TopRight, "Error detected in ContentAlignmentToIndex.");
            Debug.Assert(result != 0x03, "Error detected in ContentAlignmentToIndex.");
            Debug.Assert(result != 0x04 || alignment == ContentAlignment.MiddleLeft, "Error detected in ContentAlignmentToIndex.");
            Debug.Assert(result != 0x05 || alignment == ContentAlignment.MiddleCenter, "Error detected in ContentAlignmentToIndex.");
            Debug.Assert(result != 0x06 || alignment == ContentAlignment.MiddleRight, "Error detected in ContentAlignmentToIndex.");
            Debug.Assert(result != 0x07, "Error detected in ContentAlignmentToIndex.");
            Debug.Assert(result != 0x08 || alignment == ContentAlignment.BottomLeft, "Error detected in ContentAlignmentToIndex.");
            Debug.Assert(result != 0x09 || alignment == ContentAlignment.BottomCenter, "Error detected in ContentAlignmentToIndex.");
            Debug.Assert(result != 0x0A || alignment == ContentAlignment.BottomRight, "Error detected in ContentAlignmentToIndex.");

            return result;
        }

        // Converts 0x00, 0x01, 0x02, 0x04 (3b flag) to 0, 1, 2, 3 (2b index)
        private static byte xContentAlignmentToIndex(int threeBitFlag)
        {
            Debug.Assert(threeBitFlag >= 0x00 && threeBitFlag <= 0x04 && threeBitFlag != 0x03, "threeBitFlag out of range.");
            byte result = threeBitFlag == 0x04 ? (byte)3 : (byte)threeBitFlag;
            Debug.Assert((result & 0x03) == result, "Result out of range.");
            return result;
        }

        public static Size ConvertZeroToUnbounded(Size size)
        {
            if (size.Width == 0)
            {
                size.Width = int.MaxValue;
            }

            if (size.Height == 0)
            {
                size.Height = int.MaxValue;
            }

            return size;
        }

        // Clamps negative values in Padding struct to zero.
        public static Padding ClampNegativePaddingToZero(Padding padding)
        {
            // Careful: Setting the LRTB properties causes Padding.All to be -1 even if LRTB all agree.
            if (padding.All < 0)
            {
                padding.Left = Math.Max(0, padding.Left);
                padding.Top = Math.Max(0, padding.Top);
                padding.Right = Math.Max(0, padding.Right);
                padding.Bottom = Math.Max(0, padding.Bottom);
            }

            return padding;
        }

        /*
         *  Maps an anchor to its opposite.  Does not support combinations.  None returns none.
         *
         *  Top     = 0x01
         *  Bottom  = 0x02
         *  Left    = 0x04
         *  Right   = 0x08
         */

        // Returns the positive opposite of the given anchor (e.g., L -> R, LT -> RB, LTR -> LBR, etc.).  None return none.
        private static AnchorStyles GetOppositeAnchor(AnchorStyles anchor)
        {
            AnchorStyles result = AnchorStyles.None;
            if (anchor == AnchorStyles.None)
            {
                return result;
            }

            // iterate through T,B,L,R
            // bitwise or      B,T,R,L as appropriate
            for (int i = 1; i <= (int)AnchorStyles.Right; i <<= 1)
            {
                switch (anchor & (AnchorStyles)i)
                {
                    case AnchorStyles.None:
                        break;
                    case AnchorStyles.Left:
                        result |= AnchorStyles.Right;
                        break;
                    case AnchorStyles.Top:
                        result |= AnchorStyles.Bottom;
                        break;
                    case AnchorStyles.Right:
                        result |= AnchorStyles.Left;
                        break;
                    case AnchorStyles.Bottom:
                        result |= AnchorStyles.Top;
                        break;
                    default:
                        break;
                }
            }

            return result;
        }

        public static TextImageRelation GetOppositeTextImageRelation(TextImageRelation relation)
        {
            return (TextImageRelation)GetOppositeAnchor((AnchorStyles)relation);
        }

        public static Size UnionSizes(Size a, Size b)
        {
            return new Size(
                Math.Max(a.Width, b.Width),
                Math.Max(a.Height, b.Height));
        }

        public static Size IntersectSizes(Size a, Size b)
        {
            return new Size(
                Math.Min(a.Width, b.Width),
                Math.Min(a.Height, b.Height));
        }

        public static bool IsIntersectHorizontally(Rectangle rect1, Rectangle rect2)
        {
            if (!rect1.IntersectsWith(rect2))
            {
                return false;
            }

            if (rect1.X <= rect2.X && rect1.X + rect1.Width >= rect2.X + rect2.Width)
            {
                //rect 1 contains rect 2 horizontally
                return true;
            }

            if (rect2.X <= rect1.X && rect2.X + rect2.Width >= rect1.X + rect1.Width)
            {
                //rect 2 contains rect 1 horizontally
                return true;
            }

            return false;
        }

        public static bool IsIntersectVertically(Rectangle rect1, Rectangle rect2)
        {
            if (!rect1.IntersectsWith(rect2))
            {
                return false;
            }

            if (rect1.Y <= rect2.Y && rect1.Y + rect1.Width >= rect2.Y + rect2.Width)
            {
                //rect 1 contains rect 2 vertically
                return true;
            }

            if (rect2.Y <= rect1.Y && rect2.Y + rect2.Width >= rect1.Y + rect1.Width)
            {
                //rect 2 contains rect 1 vertically
                return true;
            }

            return false;
        }

        //returns anchorStyles, transforms from DockStyle if necessary
        internal static AnchorStyles GetUnifiedAnchor(IArrangedElement element)
        {
            DockStyle dockStyle = DefaultLayout.GetDock(element);
            if (dockStyle != DockStyle.None)
            {
                return s_dockingToAnchor[(int)dockStyle];
            }

            return DefaultLayout.GetAnchor(element);
        }

        public static Rectangle AlignAndStretch(Size fitThis, Rectangle withinThis, AnchorStyles anchorStyles)
        {
            return Align(Stretch(fitThis, withinThis.Size, anchorStyles), withinThis, anchorStyles);
        }

        public static Rectangle Align(Size alignThis, Rectangle withinThis, AnchorStyles anchorStyles)
        {
            return VAlign(alignThis, HAlign(alignThis, withinThis, anchorStyles), anchorStyles);
        }

        public static Rectangle Align(Size alignThis, Rectangle withinThis, ContentAlignment align)
        {
            return VAlign(alignThis, HAlign(alignThis, withinThis, align), align);
        }

        public static Rectangle HAlign(Size alignThis, Rectangle withinThis, AnchorStyles anchorStyles)
        {
            if ((anchorStyles & AnchorStyles.Right) != 0)
            {
                withinThis.X += withinThis.Width - alignThis.Width;
            }
            else if (anchorStyles == AnchorStyles.None || (anchorStyles & HorizontalAnchorStyles) == 0)
            {
                withinThis.X += (withinThis.Width - alignThis.Width) / 2;
            }

            withinThis.Width = alignThis.Width;

            return withinThis;
        }

        private static Rectangle HAlign(Size alignThis, Rectangle withinThis, ContentAlignment align)
        {
            if ((align & AnyRight) != 0)
            {
                withinThis.X += withinThis.Width - alignThis.Width;
            }
            else if ((align & AnyCenter) != 0)
            {
                withinThis.X += (withinThis.Width - alignThis.Width) / 2;
            }

            withinThis.Width = alignThis.Width;

            return withinThis;
        }

        public static Rectangle VAlign(Size alignThis, Rectangle withinThis, AnchorStyles anchorStyles)
        {
            if ((anchorStyles & AnchorStyles.Bottom) != 0)
            {
                withinThis.Y += withinThis.Height - alignThis.Height;
            }
            else if (anchorStyles == AnchorStyles.None || (anchorStyles & VerticalAnchorStyles) == 0)
            {
                withinThis.Y += (withinThis.Height - alignThis.Height) / 2;
            }

            withinThis.Height = alignThis.Height;

            return withinThis;
        }

        public static Rectangle VAlign(Size alignThis, Rectangle withinThis, ContentAlignment align)
        {
            if ((align & AnyBottom) != 0)
            {
                withinThis.Y += withinThis.Height - alignThis.Height;
            }
            else if ((align & AnyMiddle) != 0)
            {
                withinThis.Y += (withinThis.Height - alignThis.Height) / 2;
            }

            withinThis.Height = alignThis.Height;

            return withinThis;
        }

        public static Size Stretch(Size stretchThis, Size withinThis, AnchorStyles anchorStyles)
        {
            Size stretchedSize = new Size(
                (anchorStyles & HorizontalAnchorStyles) == HorizontalAnchorStyles ? withinThis.Width : stretchThis.Width,
                (anchorStyles & VerticalAnchorStyles) == VerticalAnchorStyles ? withinThis.Height : stretchThis.Height);
            if (stretchedSize.Width > withinThis.Width)
            {
                stretchedSize.Width = withinThis.Width;
            }

            if (stretchedSize.Height > withinThis.Height)
            {
                stretchedSize.Height = withinThis.Height;
            }

            return stretchedSize;
        }

        public static Rectangle InflateRect(Rectangle rect, Padding padding)
        {
            rect.X -= padding.Left;
            rect.Y -= padding.Top;
            rect.Width += padding.Horizontal;
            rect.Height += padding.Vertical;
            return rect;
        }

        public static Rectangle DeflateRect(Rectangle rect, Padding padding)
        {
            rect.X += padding.Left;
            rect.Y += padding.Top;
            rect.Width -= padding.Horizontal;
            rect.Height -= padding.Vertical;
            return rect;
        }

        public static Size AddAlignedRegion(Size textSize, Size imageSize, TextImageRelation relation)
        {
            return AddAlignedRegionCore(textSize, imageSize, IsVerticalRelation(relation));
        }

        public static Size AddAlignedRegionCore(Size currentSize, Size contentSize, bool vertical)
        {
            if (vertical)
            {
                currentSize.Width = Math.Max(currentSize.Width, contentSize.Width);
                currentSize.Height += contentSize.Height;
            }
            else
            {
                currentSize.Width += contentSize.Width;
                currentSize.Height = Math.Max(currentSize.Height, contentSize.Height);
            }

            return currentSize;
        }

        public static Padding FlipPadding(Padding padding)
        {
            // If Padding.All != -1, then TLRB are all the same and there is no work to be done.
            if (padding.All != -1)
            {
                return padding;
            }

            // Padding is a stuct (passed by value, no need to make a copy)
            int temp;

            temp = padding.Top;
            padding.Top = padding.Left;
            padding.Left = temp;

            temp = padding.Bottom;
            padding.Bottom = padding.Right;
            padding.Right = temp;

            return padding;
        }

        public static Point FlipPoint(Point point)
        {
            // Point is a struct (passed by value, no need to make a copy)
            int temp = point.X;
            point.X = point.Y;
            point.Y = temp;
            return point;
        }

        public static Rectangle FlipRectangle(Rectangle rect)
        {
            // Rectangle is a stuct (passed by value, no need to make a copy)
            rect.Location = FlipPoint(rect.Location);
            rect.Size = FlipSize(rect.Size);
            return rect;
        }

        public static Rectangle FlipRectangleIf(bool condition, Rectangle rect)
        {
            return condition ? FlipRectangle(rect) : rect;
        }

        public static Size FlipSize(Size size)
        {
            // Size is a struct (passed by value, no need to make a copy)
            int temp = size.Width;
            size.Width = size.Height;
            size.Height = temp;
            return size;
        }

        public static Size FlipSizeIf(bool condition, Size size)
        {
            return condition ? FlipSize(size) : size;
        }

        public static bool IsHorizontalAlignment(ContentAlignment align)
        {
            return !IsVerticalAlignment(align);
        }

        // True if text & image should be lined up horizontally.  False if vertical or overlay.
        public static bool IsHorizontalRelation(TextImageRelation relation)
        {
            return (relation & (TextImageRelation.TextBeforeImage | TextImageRelation.ImageBeforeText)) != 0;
        }

        public static bool IsVerticalAlignment(ContentAlignment align)
        {
            Debug.Assert(align != ContentAlignment.MiddleCenter, "Result is ambiguous with an alignment of MiddleCenter.");
            return (align & (ContentAlignment.TopCenter | ContentAlignment.BottomCenter)) != 0;
        }

        // True if text & image should be lined up vertically.  False if horizontal or overlay.
        public static bool IsVerticalRelation(TextImageRelation relation)
        {
            return (relation & (TextImageRelation.TextAboveImage | TextImageRelation.ImageAboveText)) != 0;
        }

        public static bool IsZeroWidthOrHeight(Rectangle rectangle)
        {
            return (rectangle.Width == 0 || rectangle.Height == 0);
        }

        public static bool IsZeroWidthOrHeight(Size size)
        {
            return (size.Width == 0 || size.Height == 0);
        }

        public static bool AreWidthAndHeightLarger(Size size1, Size size2)
        {
            return ((size1.Width >= size2.Width) && (size1.Height >= size2.Height));
        }

        public static void SplitRegion(Rectangle bounds, Size specifiedContent, AnchorStyles region1Align, out Rectangle region1, out Rectangle region2)
        {
            region1 = region2 = bounds;
            switch (region1Align)
            {
                case AnchorStyles.Left:
                    region1.Width = specifiedContent.Width;
                    region2.X += specifiedContent.Width;
                    region2.Width -= specifiedContent.Width;
                    break;
                case AnchorStyles.Right:
                    region1.X += bounds.Width - specifiedContent.Width;
                    region1.Width = specifiedContent.Width;
                    region2.Width -= specifiedContent.Width;
                    break;
                case AnchorStyles.Top:
                    region1.Height = specifiedContent.Height;
                    region2.Y += specifiedContent.Height;
                    region2.Height -= specifiedContent.Height;
                    break;
                case AnchorStyles.Bottom:
                    region1.Y += bounds.Height - specifiedContent.Height;
                    region1.Height = specifiedContent.Height;
                    region2.Height -= specifiedContent.Height;
                    break;
                default:
                    Debug.Fail("Unsupported value for region1Align.");
                    break;
            }

            Debug.Assert(Rectangle.Union(region1, region2) == bounds,
                "Regions do not add up to bounds.");
        }

        // Expands adjacent regions to bounds.  region1Align indicates which way the adjacency occurs.
        public static void ExpandRegionsToFillBounds(Rectangle bounds, AnchorStyles region1Align, ref Rectangle region1, ref Rectangle region2)
        {
            switch (region1Align)
            {
                case AnchorStyles.Left:
                    Debug.Assert(region1.Right == region2.Left, "Adjacency error.");
                    region1 = SubstituteSpecifiedBounds(bounds, region1, AnchorStyles.Right);
                    region2 = SubstituteSpecifiedBounds(bounds, region2, AnchorStyles.Left);
                    break;
                case AnchorStyles.Right:
                    Debug.Assert(region2.Right == region1.Left, "Adjacency error.");
                    region1 = SubstituteSpecifiedBounds(bounds, region1, AnchorStyles.Left);
                    region2 = SubstituteSpecifiedBounds(bounds, region2, AnchorStyles.Right);
                    break;
                case AnchorStyles.Top:
                    Debug.Assert(region1.Bottom == region2.Top, "Adjacency error.");
                    region1 = SubstituteSpecifiedBounds(bounds, region1, AnchorStyles.Bottom);
                    region2 = SubstituteSpecifiedBounds(bounds, region2, AnchorStyles.Top);
                    break;
                case AnchorStyles.Bottom:
                    Debug.Assert(region2.Bottom == region1.Top, "Adjacency error.");
                    region1 = SubstituteSpecifiedBounds(bounds, region1, AnchorStyles.Top);
                    region2 = SubstituteSpecifiedBounds(bounds, region2, AnchorStyles.Bottom);
                    break;
                default:
                    Debug.Fail("Unsupported value for region1Align.");
                    break;
            }

            Debug.Assert(Rectangle.Union(region1, region2) == bounds, "region1 and region2 do not add up to bounds.");
        }

        public static Size SubAlignedRegion(Size currentSize, Size contentSize, TextImageRelation relation)
        {
            return SubAlignedRegionCore(currentSize, contentSize, IsVerticalRelation(relation));
        }

        public static Size SubAlignedRegionCore(Size currentSize, Size contentSize, bool vertical)
        {
            if (vertical)
            {
                currentSize.Height -= contentSize.Height;
            }
            else
            {
                currentSize.Width -= contentSize.Width;
            }

            return currentSize;
        }

        private static Rectangle SubstituteSpecifiedBounds(Rectangle originalBounds, Rectangle substitutionBounds, AnchorStyles specified)
        {
            int left = (specified & AnchorStyles.Left) != 0 ? substitutionBounds.Left : originalBounds.Left;
            int top = (specified & AnchorStyles.Top) != 0 ? substitutionBounds.Top : originalBounds.Top;
            int right = (specified & AnchorStyles.Right) != 0 ? substitutionBounds.Right : originalBounds.Right;
            int bottom = (specified & AnchorStyles.Bottom) != 0 ? substitutionBounds.Bottom : originalBounds.Bottom;
            return Rectangle.FromLTRB(left, top, right, bottom);
        }

        // given a rectangle, flip to the other side of (withinBounds)
        //
        // Never call this if you derive from ScrollableControl
        public static Rectangle RTLTranslate(Rectangle bounds, Rectangle withinBounds)
        {
            bounds.X = withinBounds.Width - bounds.Right;
            return bounds;
        }
    }
#endif 
}
