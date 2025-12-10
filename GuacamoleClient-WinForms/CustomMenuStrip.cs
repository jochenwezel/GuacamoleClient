using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    internal class CustomMenuStrip : MenuStrip
    {
        //public CustomMenuStrip(Color newBackgroundColor, Color newBackgroundHoverColor, Color newTextColor) : base()
        //{
        //    // Standardfarben setzen
        //    SetMenuStripColorsRecursive(this, newBackgroundColor, newBackgroundHoverColor, newTextColor);
        //}

        //protected override void OnPaintBackground(PaintEventArgs e)
        //{
        //    using (var b = new SolidBrush(Color.Brown))
        //    {
        //        e.Graphics.FillRectangle(b, this.ClientRectangle);
        //    }
        //}
        //
        //protected override void OnPaint(PaintEventArgs e)
        //{
        //    return;
        //    base.OnPaint(e);
        //}
        //
        //[EditorBrowsable(EditorBrowsableState.Advanced)]
        //protected override void OnBackColorChanged(EventArgs e)
        //{
        //    return;
        //}

        private sealed class CustomMenuStripColorTable : ProfessionalColorTable
        {
            public CustomMenuStripColorTable(Color newBackgroundColor, Color newBackgroundHoverColor, Color newTextColor)
            {
                this.newBackgroundColor = newBackgroundColor;
                this.newBackgroundHoverColor = newBackgroundHoverColor;
                this.newTextColor = newTextColor;
            }

            private Color newBackgroundColor;
            private Color newBackgroundHoverColor;
            private Color newTextColor;

            public override Color MenuItemSelected => newBackgroundHoverColor;
            public override Color ButtonPressedHighlight => newBackgroundHoverColor;
            public override Color ButtonCheckedHighlight => newBackgroundHoverColor;
            public override Color CheckPressedBackground => newBackgroundHoverColor;
            public override Color CheckSelectedBackground => newBackgroundHoverColor;
            public override Color CheckBackground => newBackgroundColor;
            public override Color MenuItemBorder => newBackgroundColor;
            public override Color MenuStripGradientBegin => newBackgroundColor;
            public override Color MenuStripGradientEnd => newBackgroundColor;
        }

        private sealed class ColoredMenuItemRenderer : ToolStripProfessionalRenderer
        {
            public ColoredMenuItemRenderer(Color newBackgroundColor, Color newBackgroundHoverColor, Color newTextColor, Color newSeparatorLineForeColor) : base(new CustomMenuStripColorTable(newBackgroundColor, newBackgroundHoverColor, newTextColor))
            {
                this._newBackgroundColor = newBackgroundColor;
                this._newBackgroundHoverColor = newBackgroundHoverColor;
                this._newTextColor = newTextColor;
                this._newSeparatorLineForeColor = newSeparatorLineForeColor;
            }

            private readonly Color _newBackgroundColor;
            private readonly Color _newBackgroundHoverColor;
            private readonly Color _newTextColor;
            private readonly Color _newSeparatorLineForeColor;


            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                // Hintergrund füllen
                e.Graphics.FillRectangle(new SolidBrush(_newBackgroundColor), 0, 0, e.Item.Width, e.Item.Height); // e.Item.Bounds

                // Separations-Linie zeichnen
                using (var pen = new Pen(_newSeparatorLineForeColor, 1))
                {
                    int y = e.Item.Bounds.Height / 2;
                    e.Graphics.DrawLine(pen, 2, y, e.Item.Bounds.Width - 2, y);
                }
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                base.OnRenderToolStripBackground(new ToolStripRenderEventArgs(e.Graphics, e.ToolStrip, e.AffectedBounds, _newBackgroundColor));
            }

            protected override void OnRenderToolStripContentPanelBackground(ToolStripContentPanelRenderEventArgs e)
            {
                base.OnRenderToolStripContentPanelBackground(e);
            }

            protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
            {
                base.OnRenderButtonBackground(e);
            }

            protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
            {
                base.OnRenderDropDownButtonBackground(e);
            }

            protected override void OnRenderToolStripPanelBackground(ToolStripPanelRenderEventArgs e)
            {
                base.OnRenderToolStripPanelBackground(e);
            }

            protected override void OnRenderItemBackground(ToolStripItemRenderEventArgs e)
            {
                base.OnRenderItemBackground(e);
            }

            override protected void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                base.OnRenderMenuItemBackground(e);
                //if (e.Item.Selected)
                //{
                //    using (SolidBrush brush = new SolidBrush(_newBackgroundHoverColor))
                //    {
                //        e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
                //    }
                //}
                //else
                //{
                //    using (SolidBrush brush = new SolidBrush(_newBackgroundColor))
                //    {
                //        e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
                //    }
                //}
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = _newTextColor;
                base.OnRenderItemText(e);
            }
        }

        private sealed class CustomMenuStripColorTable2 : ProfessionalColorTable
        {
            private readonly Color _back;
            private readonly Color _hover;

            public CustomMenuStripColorTable2(Color back, Color hover)
            {
                _back = back;
                _hover = hover;
            }

            public override Color MenuStripGradientBegin => _back;
            public override Color MenuStripGradientEnd => _back;

            public override Color ToolStripDropDownBackground => _back;

            public override Color MenuItemSelected => _hover;
            public override Color MenuItemSelectedGradientBegin => _hover;
            public override Color MenuItemSelectedGradientEnd => _hover;

            public override Color MenuItemPressedGradientBegin => _hover;
            public override Color MenuItemPressedGradientEnd => _hover;

            public override Color ImageMarginGradientBegin => _back;
            public override Color ImageMarginGradientMiddle => _back;
            public override Color ImageMarginGradientEnd => _back;
        }

        private sealed class ColoredMenuItemRenderer2 : ToolStripProfessionalRenderer
        {
            private readonly Color _backColor;
            private readonly Color _hoverColor;
            private readonly Color _textColor;

            public ColoredMenuItemRenderer2(Color backColor, Color hoverColor, Color textColor)
                : base(new CustomMenuStripColorTable2(backColor, hoverColor)) // ColorTable brauchen wir hier kaum noch
            {
                _backColor = backColor;
                _hoverColor = hoverColor;
                _textColor = textColor;
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                // Komplettes MenuStrip einfärben
                using (var b = new SolidBrush(_backColor))
                {
                    e.Graphics.FillRectangle(b, e.AffectedBounds);
                }
                // KEIN base.OnRenderToolStripBackground(e);
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);
                var mi = e.Item as ToolStripMenuItem;

                bool isHot = e.Item.Selected;
                bool isDroppedDown = mi != null && mi.DropDown.Visible;

                Color fill = (isHot || isDroppedDown) ? _hoverColor : _backColor;

                using (var b = new SolidBrush(fill))
                {
                    e.Graphics.FillRectangle(b, rect);
                }
                // KEIN base.OnRenderMenuItemBackground(e);
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = _textColor;
                base.OnRenderItemText(e);
            }

            private static int VerticalCenter(Rectangle r)
            {
                return r.Top + r.Height / 2;
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                // Optional: Separator-Farbe anpassen
                using (var p = new Pen(_textColor))
                {
                    int y = VerticalCenter(e.Item.ContentRectangle);
                    e.Graphics.DrawLine(p, e.Item.ContentRectangle.Left, y,
                                           e.Item.ContentRectangle.Right, y);
                }
                // kein base
            }
        }

        private sealed class FlatMenuRenderer : ToolStripRenderer
        {
            private readonly Color _backColor;
            private readonly Color _hoverColor;
            private readonly Color _textColor;

            public FlatMenuRenderer(Color backColor, Color hoverColor, Color textColor)
            {
                _backColor = backColor;
                _hoverColor = hoverColor;
                _textColor = textColor;
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using (var b = new SolidBrush(_backColor))
                {
                    e.Graphics.FillRectangle(b, e.AffectedBounds);
                }
                // KEIN base-Aufruf
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);

                bool isHot = e.Item.Selected;
                bool isPressed = e.Item.Pressed;

                Color fill = (isHot || isPressed) ? _hoverColor : _backColor;

                using (var b = new SolidBrush(fill))
                {
                    e.Graphics.FillRectangle(b, rect);
                }
                // KEIN base-Aufruf
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = _textColor;
                base.OnRenderItemText(e);
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                // Separator an Textfarbe anpassen
                using (var p = new Pen(_textColor))
                {
                    var r = e.Item.ContentRectangle;
                    int y = r.Top + r.Height / 2;
                    e.Graphics.DrawLine(p, r.Left, y, r.Right, y);
                }
                // kein base
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                // Keine 3D-Ränder o.ä. zeichnen
                // NICHT base aufrufen -> borderlos / flach
            }
        }

        public void SetMenuStripColorsRecursive(Color newStripColor, Color newBackgroundColor, Color newBackgroundHoverColor, Color newTextColor, Color newSeparatorLineForeColor)
        {
            SetMenuStripColorsRecursive(this, newStripColor, newBackgroundColor, newBackgroundHoverColor, newTextColor, newSeparatorLineForeColor);
        }

        private static void SetMenuStripColorsRecursive(MenuStrip item, Color newStripColor, Color newBackgroundColor, Color newBackgroundHoverColor, Color newTextColor, Color newSeparatorLineForeColor)
        {
            //item.Renderer = new ColoredMenuItemRenderer2(newBackgroundColor, newBackgroundHoverColor, newTextColor);
            item.BackColor = newStripColor;
            //item.BackColor = Color.DarkGreen;
            item.ForeColor = newTextColor;
            //item.Renderer = new FlatMenuRenderer(Color.DarkGreen, Color.DarkRed, newTextColor);
            item.Renderer = new ToolStripProfessionalRenderer(new CustomMenuStripColorTable(newBackgroundColor, newBackgroundHoverColor, newTextColor));
            item.Renderer = new ColoredMenuItemRenderer(newBackgroundColor, newBackgroundHoverColor, newTextColor, newSeparatorLineForeColor);

            //item.Renderer = new ColoredSeparatorRenderer(SystemColors.ButtonShadow, Color.Red);

            foreach (ToolStripItem child in item.Items)
            {
                if (child is ToolStripMenuItem m2)
                    SetMenuStripBackgroundColorRecursive(m2, newBackgroundColor, newBackgroundHoverColor,newTextColor);
                else
                {
                    child.BackColor = newBackgroundColor;
                    child.ForeColor = newTextColor;
                }
            }
        }

        private static void SetMenuStripBackgroundColorRecursive(ToolStripMenuItem item, Color newBackgroundColor, Color newBackgroundHoverColor, Color newTextColor)
        {
            item.BackColor = newBackgroundColor;
            item.ForeColor = newTextColor;
            foreach (ToolStripItem child in item.DropDownItems)
                if (child is ToolStripMenuItem m2)
                    SetMenuStripBackgroundColorRecursive(m2, newBackgroundColor, newBackgroundHoverColor,newTextColor);
                else
                {
                    child.BackColor = newBackgroundColor;
                    child.ForeColor = newTextColor;
                }
        }
    }
}
