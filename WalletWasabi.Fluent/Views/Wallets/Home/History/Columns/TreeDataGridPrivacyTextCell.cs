using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using WalletWasabi.Fluent.ViewModels.Wallets;

namespace WalletWasabi.Fluent.Views.Wallets.Home.History.Columns;

internal class TreeDataGridPrivacyTextCell : TreeDataGridCell
{
	private string? _text;
	private FormattedText? _formattedText;

	public TreeDataGridPrivacyTextCell()
	{
	}

	public override void Realize(IElementFactory factory, ICell model, int columnIndex, int rowIndex)
	{
		var text = ((PrivacyTextCell)model).Value;

		if (text != _text)
		{
			_text = text;
			_formattedText = null;
		}

		base.Realize(factory, model, columnIndex, rowIndex);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		if (string.IsNullOrWhiteSpace(_text))
		{
			return default;
		}

		if (availableSize != _formattedText?.Constraint)
		{
			_formattedText = new FormattedText(
				_text,
				new Typeface(FontFamily, FontStyle, FontWeight),
				FontSize,
				TextAlignment.Left,
				TextWrapping.NoWrap,
				availableSize);
		}

		return _formattedText.Bounds.Size;
	}

	public override void Render(DrawingContext context)
	{
		if (_formattedText is not null)
		{
			context.DrawText(Foreground, default, _formattedText);
		}
	}
}
