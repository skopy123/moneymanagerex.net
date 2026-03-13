using System.ComponentModel;

namespace mmex.net.winform.Controls;

/// <summary>A TextBox that accepts only numeric decimal input and formats it as currency.</summary>
public class CurrencyTextBox : TextBox
{
    private decimal _value;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal Value
    {
        get => _value;
        set
        {
            _value = value;
            Text = value.ToString("N2");
        }
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        // Allow digits, decimal separator, minus, backspace
        if (!char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ','
            && e.KeyChar != '-' && e.KeyChar != '\b')
        {
            e.Handled = true;
        }
        base.OnKeyPress(e);
    }

    protected override void OnLeave(EventArgs e)
    {
        if (decimal.TryParse(Text.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var v))
        {
            Value = v;
        }
        else
        {
            Text = _value.ToString("N2");
        }
        base.OnLeave(e);
    }
}
