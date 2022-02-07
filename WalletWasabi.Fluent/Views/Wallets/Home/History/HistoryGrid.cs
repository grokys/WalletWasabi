using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using WalletWasabi.Fluent.ViewModels.Wallets;
using WalletWasabi.Fluent.Views.Wallets.Home.History.Columns;

namespace WalletWasabi.Fluent.Views.Wallets.Home.History;

internal class HistoryGrid : TreeDataGrid, IStyleable
{
	Type IStyleable.StyleKey => typeof(TreeDataGrid);

	protected override IElementFactory CreateElementFactory()
	{
		return new HistoryElementFactory();
	}

	private class HistoryElementFactory : TreeDataGridElementFactory
	{
		protected override IControl CreateElement(object? data)
		{
			return data is PrivacyTextCell ?
				new TreeDataGridPrivacyTextCell() :
				base.CreateElement(data);
		}

		protected override string GetDataRecycleKey(object data)
		{
			return data is PrivacyTextCell ?
				typeof(TreeDataGridPrivacyTextCell).FullName! :
				base.GetDataRecycleKey(data);
		}
	}
}
