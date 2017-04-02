using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

[DesignerCategory("Code")]
internal class Container : Panel
{
    public Container()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using (SolidBrush brush = new SolidBrush(Color.Transparent))
        {
            e.Graphics.FillRectangle(brush, ClientRectangle);
            e.Graphics.DrawRectangle(Pens.LightGray, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        }
    }
}

internal class OptionBox : RadioButton
{
    public string OptionName { get; set; }
}