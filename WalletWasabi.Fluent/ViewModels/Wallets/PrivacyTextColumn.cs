using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using WalletWasabi.Fluent.ViewModels.Wallets.Home.History.HistoryItems;

namespace WalletWasabi.Fluent.ViewModels.Wallets;

internal class PrivacyTextColumn : ColumnBase<HistoryItemViewModelBase>
{
	private readonly Func<HistoryItemViewModelBase, string?> _getter;

	public PrivacyTextColumn(
		object? header,
		Func<HistoryItemViewModelBase, string?> getter,
		GridLength? width,
		ColumnOptions<HistoryItemViewModelBase>? options)
			: base(header, width, options)
	{
		_getter = getter;
	}

	public override ICell CreateCell(IRow<HistoryItemViewModelBase> row)
	{
		return new PrivacyTextCell(_getter(row.Model));
	}

	public override Comparison<HistoryItemViewModelBase?>? GetComparison(ListSortDirection direction)
	{
		// TODO
		return null;
	}
}
