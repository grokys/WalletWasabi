using Avalonia.Controls.Models.TreeDataGrid;

namespace WalletWasabi.Fluent.ViewModels.Wallets;

internal class PrivacyTextCell : ICell
{
	public PrivacyTextCell(string? value) => Value = value;
	public bool CanEdit => false;
	public string? Value { get; }
	object? ICell.Value => Value;
}
