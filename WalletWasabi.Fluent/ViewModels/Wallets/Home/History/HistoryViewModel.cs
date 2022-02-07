using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using DynamicData;
using DynamicData.Binding;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Blockchain.Analysis.Clustering;
using WalletWasabi.Blockchain.Transactions;
using WalletWasabi.Fluent.Extensions;
using WalletWasabi.Fluent.ViewModels.Wallets.Home.History.HistoryItems;
using WalletWasabi.Fluent.Views.Wallets.Home.History.Columns;
using WalletWasabi.Logging;

namespace WalletWasabi.Fluent.ViewModels.Wallets.Home.History;

public partial class HistoryViewModel : ActivatableViewModel
{
	private readonly SourceList<HistoryItemViewModelBase> _transactionSourceList;
	private readonly WalletViewModel _walletViewModel;
	private readonly IObservable<Unit> _updateTrigger;
	private readonly ObservableCollectionExtended<HistoryItemViewModelBase> _transactions;
	private readonly ObservableCollectionExtended<HistoryItemViewModelBase> _unfilteredTransactions;
	private readonly object _transactionListLock = new();

	[AutoNotify] private HistoryItemViewModelBase? _selectedItem;
	[AutoNotify(SetterModifier = AccessModifier.Private)] private bool _isTransactionHistoryEmpty;
	[AutoNotify(SetterModifier = AccessModifier.Private)] private bool _isInitialized;

	public HistoryViewModel(WalletViewModel walletViewModel, IObservable<Unit> updateTrigger)
	{
		_walletViewModel = walletViewModel;
		_updateTrigger = updateTrigger;
		_transactionSourceList = new SourceList<HistoryItemViewModelBase>();
		_transactions = new ObservableCollectionExtended<HistoryItemViewModelBase>();
		_unfilteredTransactions = new ObservableCollectionExtended<HistoryItemViewModelBase>();

		this.WhenAnyValue(x => x.UnfilteredTransactions.Count)
			.Subscribe(x => IsTransactionHistoryEmpty = x <= 0);

		_transactionSourceList
			.Connect()
			.ObserveOn(RxApp.MainThreadScheduler)
			.Sort(SortExpressionComparer<HistoryItemViewModelBase>.Descending(x => x.OrderIndex))
			.Bind(_unfilteredTransactions)
			.Bind(_transactions)
			.Subscribe();


			// [Column]			[View]						[Header]		[Width]		[MinWidth]		[MaxWidth]	[CanUserSort]
			// Indicators		IndicatorsColumnView		-				Auto		80				-			false
			// Date				DateColumnView				Date / Time		Auto		150				-			true
			// Labels			LabelsColumnView			Labels			*			75				-			false
			// Incoming			IncomingColumnView			Incoming (₿)	Auto		120				150			true
			// Outgoing			OutgoingColumnView			Outgoing (₿)	Auto		120				150			true
			// Balance			BalanceColumnView			Balance (₿)		Auto		120				150			true

			IControl IndicatorsColumnTemplate(HistoryItemViewModelBase node, INameScope ns) => new IndicatorsColumnView();
			IControl LabelsColumnTemplate(HistoryItemViewModelBase node, INameScope ns) => new LabelsColumnView();
			IControl IncomingColumnTemplate(HistoryItemViewModelBase node, INameScope ns) => new IncomingColumnView();
			IControl OutgoingColumnTemplate(HistoryItemViewModelBase node, INameScope ns) => new OutgoingColumnView();
			IControl BalanceColumnTemplate(HistoryItemViewModelBase node, INameScope ns) => new BalanceColumnView();

			Source = new FlatTreeDataGridSource<HistoryItemViewModelBase>(_transactions)
            {
                Columns =
                {
	                // Indicators
                    new TemplateColumn<HistoryItemViewModelBase>(
                        null,
                        new FuncDataTemplate<HistoryItemViewModelBase>(IndicatorsColumnTemplate, true),
                        options: new ColumnOptions<HistoryItemViewModelBase>
                        {
                            CanUserResizeColumn = false,
                            CanUserSortColumn = false,
                            MinWidth = new GridLength(80, GridUnitType.Pixel)
                        },
                        width: new GridLength(0, GridUnitType.Auto)),
                    // Date
                    new PrivacyTextColumn(
	                    "Date / Time",
						x => x.DateString,
	                    options: new ColumnOptions<HistoryItemViewModelBase>
	                    {
		                    CanUserResizeColumn = false,
		                    CanUserSortColumn = true,
		                    CompareAscending = HistoryItemViewModelBase.SortAscending(x => x.Date),
		                    CompareDescending = HistoryItemViewModelBase.SortDescending(x => x.Date),
		                    MinWidth = new GridLength(150, GridUnitType.Pixel)
	                    },
	                    width: new GridLength(0, GridUnitType.Auto)),
                    // Labels
                    new TemplateColumn<HistoryItemViewModelBase>(
	                    "Labels",
	                    new FuncDataTemplate<HistoryItemViewModelBase>(LabelsColumnTemplate, true),
	                    options: new ColumnOptions<HistoryItemViewModelBase>
	                    {
		                    CanUserResizeColumn = false,
		                    CanUserSortColumn = false,
		                    MinWidth = new GridLength(100, GridUnitType.Pixel)
	                    },
	                    width: new GridLength(1, GridUnitType.Star)),
                    // Incoming
                    new PrivacyTextColumn(
	                    "Incoming (₿)",
	                    x => x.IncomingAmount?.ToString(),
	                    options: new ColumnOptions<HistoryItemViewModelBase>
	                    {
		                    CanUserResizeColumn = false,
		                    CanUserSortColumn = true,
		                    CompareAscending = HistoryItemViewModelBase.SortAscending(x => x.IncomingAmount),
		                    CompareDescending = HistoryItemViewModelBase.SortDescending(x => x.IncomingAmount),
		                    MinWidth = new GridLength(120, GridUnitType.Pixel),
		                    MaxWidth = new GridLength(150, GridUnitType.Pixel)
	                    },
	                    width: new GridLength(0, GridUnitType.Auto)),
                    // Outgoing
                    new PrivacyTextColumn(
	                    "Outgoing (₿)",
	                    x => x.OutgoingAmount?.ToString(),
	                    options: new ColumnOptions<HistoryItemViewModelBase>
	                    {
		                    CanUserResizeColumn = false,
		                    CanUserSortColumn = true,
		                    CompareAscending = HistoryItemViewModelBase.SortAscending(x => x.OutgoingAmount),
		                    CompareDescending = HistoryItemViewModelBase.SortDescending(x => x.OutgoingAmount),
		                    MinWidth = new GridLength(120, GridUnitType.Pixel),
		                    MaxWidth = new GridLength(150, GridUnitType.Pixel)
	                    },
	                    width: new GridLength(0, GridUnitType.Auto)),
                    // Balance
                    new PrivacyTextColumn(
	                    "Balance (₿)",
	                    x => x.Balance?.ToString(),
	                    options: new ColumnOptions<HistoryItemViewModelBase>
	                    {
		                    CanUserResizeColumn = false,
		                    CanUserSortColumn = true,
		                    CompareAscending = HistoryItemViewModelBase.SortAscending(x => x.Balance),
		                    CompareDescending = HistoryItemViewModelBase.SortDescending(x => x.Balance),
		                    MinWidth = new GridLength(120, GridUnitType.Pixel),
		                    MaxWidth = new GridLength(150, GridUnitType.Pixel)
	                    },
	                    width: new GridLength(0, GridUnitType.Auto)),
                }
            };

			Source.RowSelection!.SingleSelect = true;

			Source.RowSelection
				.WhenAnyValue(x => x.SelectedItem)
				.Subscribe(x => SelectedItem = x);
	}

	public ObservableCollection<HistoryItemViewModelBase> UnfilteredTransactions => _unfilteredTransactions;

	public ObservableCollection<HistoryItemViewModelBase> Transactions => _transactions;

	public FlatTreeDataGridSource<HistoryItemViewModelBase> Source { get; }

	public void SelectTransaction(uint256 txid)
	{
		var txnItem = Transactions.FirstOrDefault(item =>
		{
			if (item is CoinJoinsHistoryItemViewModel cjGroup)
			{
				return cjGroup.CoinJoinTransactions.Any(x => x.TransactionId == txid);
			}

			return item.Id == txid;
		});

		if (txnItem is { })
		{
			SelectedItem = txnItem;
			SelectedItem.IsFlashing = true;

			var index = _transactions.IndexOf(SelectedItem);
			Source.RowSelection!.SelectedIndex = new IndexPath(index);
		}
	}

	protected override void OnActivated(CompositeDisposable disposables)
	{
		base.OnActivated(disposables);

		_updateTrigger
			.Subscribe(async _ => await UpdateAsync())
			.DisposeWith(disposables);
	}

	private async Task UpdateAsync()
	{
		try
		{
			var historyBuilder = new TransactionHistoryBuilder(_walletViewModel.Wallet);
			var rawHistoryList = await Task.Run(historyBuilder.BuildHistorySummary);
			var newHistoryList = GenerateHistoryList(rawHistoryList).ToArray();

			lock (_transactionListLock)
			{
				// NOTE: Original wasabi code
				/*
				var copyList = Transactions.ToList();

				foreach (var oldItem in copyList)
				{
					if (newHistoryList.All(x => x.Id != oldItem.Id))
					{
						_transactionSourceList.Remove(oldItem);
					}
				}

				foreach (var newItem in newHistoryList)
				{
					if (_transactions.FirstOrDefault(x => x.Id == newItem.Id) is { } item)
					{
						item.Update(newItem);
					}
					else
					{
						_transactionSourceList.Add(newItem);
					}
				}
				//*/

				// NOTE: Repro for TreeDataGrid scrolling issues
				/*
	            var rand = new Random(100);

	            for (var i = 0; i < 10_000; i++)
	            {
		            var ts = new TransactionSummary()
		            {
			            DateTime = DateTimeOffset.Now,
			            Label = new SmartLabel($"Label{i++}", $"Label{i++}"),
			            Amount = new Money(rand.NextDouble() < 0.5 ? rand.Next() : -rand.Next()),
		            };
	                var item = new TransactionHistoryItemViewModel(i, ts, _walletViewModel, Money.Zero, _updateTrigger);

	                _transactionSourceList.Add(item);
	            }
				//*/

				// NOTE: Repro for TreeDataGrid scrolling issues
				//*
				_transactionSourceList.Edit(x =>
				{
					var rand = new Random(100);

					for (var i = 0; i < 10_000; i++)
					{
						var ts = new TransactionSummary()
						{
							DateTime = DateTimeOffset.Now,
							Label = new SmartLabel($"Label{i++}", $"Label{i++}"),
							Amount = new Money(rand.NextDouble() < 0.5 ? rand.Next() : -rand.Next()),
						};
						var item = new TransactionHistoryItemViewModel(i, ts, _walletViewModel, Money.Zero, _updateTrigger);

						x.Add(item);
					}
				});
				//*/

				if (!IsInitialized)
				{
					IsInitialized = true;
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogError(ex);
		}
	}

	private IEnumerable<HistoryItemViewModelBase> GenerateHistoryList(List<TransactionSummary> txRecordList)
	{
		Money balance = Money.Zero;
		CoinJoinsHistoryItemViewModel? coinJoinGroup = default;

		for (var i = 0; i < txRecordList.Count; i++)
		{
			var item = txRecordList[i];

			balance += item.Amount;

			if (!item.IsLikelyCoinJoinOutput)
			{
				yield return new TransactionHistoryItemViewModel(i, item, _walletViewModel, balance, _updateTrigger);
			}

			if (item.IsLikelyCoinJoinOutput)
			{
				if (coinJoinGroup is null)
				{
					coinJoinGroup = new CoinJoinsHistoryItemViewModel(i, item);
				}
				else
				{
					coinJoinGroup.Add(item);
				}
			}

			if (coinJoinGroup is { } cjg &&
				(i + 1 < txRecordList.Count && !txRecordList[i + 1].IsLikelyCoinJoinOutput || // The next item is not CJ so add the group.
				 i == txRecordList.Count - 1)) // There is no following item in the list so add the group.
			{
				cjg.SetBalance(balance);
				yield return cjg;
				coinJoinGroup = null;
			}
		}
	}
}
