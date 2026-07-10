using DivinityModManager.Controls;
using DivinityModManager.Converters;
using DivinityModManager.Models;
using DivinityModManager.Util;
using DivinityModManager.Util.ScreenReader;
using DivinityModManager.ViewModels;

using GongSolutions.Wpf.DragDrop.Utilities;

using ReactiveMarbles.ObservableEvents;

using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace DivinityModManager.Views;

public interface IModViewLayout
{
	void UpdateViewSelection(IEnumerable<ISelectable> dataList, ListView listView = null);
	void SelectMods(IEnumerable<DivinityModData> mods);
	void DeselectAll();
	void FixActiveModsScrollbar();
	void RefreshDataView(ListView target);
	ModListView ActiveModsView { get; }
	ModListView InactiveModsView { get; }
	ModListView ForceLoadedModsView { get; }
}

public class HorizontalModLayoutBase : ReactiveUserControl<MainWindowViewModel> { }

/// <summary>
/// Interaction logic for HorizonalModLayout.xaml
/// </summary>
public partial class HorizontalModLayout : HorizontalModLayoutBase, IModViewLayout
{
	private const double DefaultModDetailsRowHeight = 295;
	private const double MinimumExpandedModDetailsRowHeight = 295;
	private const double CollapsedModDetailsRowHeight = 58;
	private const double ModDetailsSplitterHeight = 6;

	private object _focusedList = null;
	private double _lastExpandedModDetailsRowHeight = DefaultModDetailsRowHeight;
	private readonly Dictionary<GridViewColumn, double> _visibleModListColumnWidths = new();
	private static readonly string[] OptionalModListColumns =
	[
		"Version",
		"Author",
		"Last Updated",
		"Last Modified",
		"Source"
	];

	public ModListView ActiveModsView => ActiveModsListView;
	public ModListView InactiveModsView => InactiveModsListView;
	public ModListView ForceLoadedModsView => ForceLoadedModsListView;

	private bool ListHasFocus(ListView listView)
	{
		if (_focusedList == listView || listView.IsFocused || listView.IsKeyboardFocused)
		{
			return true;
		}
		if (listView.SelectedItem is ListViewItem item && (item.IsFocused || item.IsKeyboardFocused))
		{
			return true;
		}
		return false;
	}

	private bool FocusSelectedItem(ListView lv)
	{
		try
		{
			var listBoxItem = (ListBoxItem)lv.ItemContainerGenerator.ContainerFromItem(lv.SelectedItem);
			if (listBoxItem == null)
			{
				var firstItem = lv.Items.GetItemAt(0);
				if (firstItem != null)
				{
					listBoxItem = (ListBoxItem)lv.ItemContainerGenerator.ContainerFromItem(firstItem);
				}
			}
			if (listBoxItem != null)
			{
				listBoxItem.Focus();
				Keyboard.Focus(listBoxItem);
				return true;
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"{ex}");
		}
		return false;
	}

	private void FocusList(ListView listView)
	{
		if (!FocusSelectedItem(listView))
		{
			listView.Focus();
		}
	}

	private void SetupListView(ListView listView)
	{
		listView.InputBindings.Add(new KeyBinding(ApplicationCommands.SelectAll, new KeyGesture(Key.A, ModifierKeys.Control)));
		listView.CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, (_sender, _e) =>
		{
			listView.SelectAll();
		}));

		listView.InputBindings.Add(new KeyBinding(ReactiveCommand.Create(() =>
		{
			listView.SelectedItems.Clear();

		}), new KeyGesture(Key.D, ModifierKeys.Control)));

		listView.ItemContainerStyle = this.FindResource("ListViewItemMouseEvents") as Style;
		listView.GotFocus += (object sender, RoutedEventArgs e) =>
		{
			_focusedList = sender;
		};
		listView.LostFocus += (object sender, RoutedEventArgs e) =>
		{
			if (_focusedList == sender)
			{
				_focusedList = null;
			}
		};
	}

	public void FixActiveModsScrollbar()
	{
		if (ActiveModsListView.FindVisualChildren<ScrollViewer>().FirstOrDefault() is ScrollViewer sv)
		{
			sv.ScrollToHorizontalOffset(0d);
		}
	}

	public void UpdateViewSelection(IEnumerable<ISelectable> dataList, ListView listView = null)
	{
		if (dataList != null)
		{
			if (listView == null)
			{
				if (dataList == ViewModel.ActiveMods)
				{
					listView = ActiveModsListView;
				}
				else if (dataList == ViewModel.InactiveMods)
				{
					listView = InactiveModsListView;
				}
				else if (dataList == ViewModel.ForceLoadedMods)
				{
					listView = ForceLoadedModsListView;
				}
			}

			if (listView != null && dataList.Count() > 0)
			{
				IInputElement focusedItem = FocusManager.GetFocusedElement(listView);
				foreach (var mod in dataList)
				{
					var listItem = (ListViewItem)listView.ItemContainerGenerator.ContainerFromItem(mod);
					if (listItem != null)
					{
						if (mod.Visibility == Visibility.Visible)
						{
							listItem.IsSelected = mod.IsSelected;
							if (listView.IsFocused && focusedItem == null && mod.IsSelected)
							{
								focusedItem = listItem;
								FocusManager.SetFocusedElement(listView, focusedItem);
							}
						}
						else
						{
							listItem.IsSelected = false;
						}
					}
				}
			}
		}
	}

	public void DeselectAll()
	{
		this.ActiveModsListView.ClearSelectedItems();
		this.InactiveModsListView.ClearSelectedItems();
		this.ForceLoadedModsListView.ClearSelectedItems();
	}

	public void SelectMods(IEnumerable<DivinityModData> mods)
	{
		if (mods != null)
		{
			foreach (var mod in mods)
			{
				ModListView listView = null;
				if (mod.IsForceLoaded && !mod.IsForceLoadedMergedMod && !mod.ForceAllowInLoadOrder)
				{
					listView = ForceLoadedModsListView;
				}
				else if (mod.IsActive)
				{
					listView = ActiveModsListView;
				}
				else
				{
					listView = InactiveModsListView;
				}
				if (listView.ItemContainerGenerator.ContainerFromItem(mod) is ListViewItem listItem)
				{
					listItem.IsSelected = mod.Visibility == Visibility.Visible;
				}
			}
		}
	}

	private void UpdateIsSelected(SelectionChangedEventArgs e, IEnumerable<DivinityModData> list)
	{
		if (e != null && list != null)
		{
			var targetUUIDs = list.Select(x => x.UUID).ToHashSet();

			if (e.RemovedItems != null && e.RemovedItems.Count > 0)
			{
				foreach (var removedItem in e.RemovedItems.Cast<DivinityModData>())
				{
					if (targetUUIDs.Contains(removedItem.UUID))
					{
						removedItem.IsSelected = false;
					}
				}
			}

			if (e.AddedItems != null && e.AddedItems.Count > 0)
			{
				foreach (var addedItem in e.AddedItems.Cast<DivinityModData>())
				{
					addedItem.IsSelected = true;
				}
			}
		}
	}

	private DivinityModData GetSelectedModForDetails()
	{
		return ActiveModsListView.SelectedItems.OfType<DivinityModData>().LastOrDefault()
			?? InactiveModsListView.SelectedItems.OfType<DivinityModData>().LastOrDefault()
			?? ForceLoadedModsListView.SelectedItems.OfType<DivinityModData>().LastOrDefault();
	}

	private void UpdateModDetailsSelection(SelectionChangedEventArgs e)
	{
		var selectedMod = e?.AddedItems?.OfType<DivinityModData>().LastOrDefault()
			?? GetSelectedModForDetails();

		if (selectedMod == null)
		{
			RememberExpandedModDetailsHeight();
		}

		ModDetailsContent.Content = selectedMod;
		ModDetailsPanel.Visibility = selectedMod != null ? Visibility.Visible : Visibility.Collapsed;
		UpdateModDetailsLayout(selectedMod != null);
	}

	private void RememberExpandedModDetailsHeight()
	{
		if (ModDetailsToggleButton.IsChecked == true && ModDetailsRow.ActualHeight >= MinimumExpandedModDetailsRowHeight)
		{
			_lastExpandedModDetailsRowHeight = ModDetailsRow.ActualHeight;
		}
	}

	private void UpdateModDetailsLayout(bool hasSelectedMod)
	{
		if (!hasSelectedMod)
		{
			ModDetailsGridSplitter.Visibility = Visibility.Collapsed;
			ModDetailsSplitterRow.Height = new GridLength(0);
			ModDetailsRow.MinHeight = 0;
			ModDetailsRow.Height = new GridLength(0);
			return;
		}

		if (ModDetailsToggleButton.IsChecked == false)
		{
			ModDetailsGridSplitter.Visibility = Visibility.Collapsed;
			ModDetailsSplitterRow.Height = new GridLength(0);
			ModDetailsRow.MinHeight = CollapsedModDetailsRowHeight;
			ModDetailsRow.Height = new GridLength(CollapsedModDetailsRowHeight);
			return;
		}

		ModDetailsGridSplitter.Visibility = Visibility.Visible;
		ModDetailsSplitterRow.Height = new GridLength(ModDetailsSplitterHeight);
		ModDetailsRow.MinHeight = MinimumExpandedModDetailsRowHeight;
		ModDetailsRow.Height = new GridLength(Math.Max(MinimumExpandedModDetailsRowHeight, _lastExpandedModDetailsRowHeight));
	}

	private void ModDetailsToggleButton_Checked(object sender, RoutedEventArgs e)
	{
		UpdateModDetailsLayout(ModDetailsPanel.Visibility == Visibility.Visible);
	}

	private void ModDetailsToggleButton_Unchecked(object sender, RoutedEventArgs e)
	{
		RememberExpandedModDetailsHeight();
		UpdateModDetailsLayout(ModDetailsPanel.Visibility == Visibility.Visible);
	}

	private void ModDetailsGridSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
	{
		Dispatcher.BeginInvoke(new Action(RememberExpandedModDetailsHeight));
	}

	private IDisposable updatingActiveViewSelection;
	private IDisposable updatingInactiveViewSelection;
	private IDisposable updatingForcedViewSelection;

	private void ActiveModListView_ItemContainerStatusChanged(EventArgs e)
	{
		if (ActiveModsListView.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
		{
			if (updatingActiveViewSelection == null)
			{
				updatingActiveViewSelection = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(25), () =>
				{
					UpdateViewSelection(ViewModel.ActiveMods, ActiveModsListView);
					updatingActiveViewSelection.Dispose();
					updatingActiveViewSelection = null;
				});
			}
		}
	}

	private void InactiveModListView_ItemContainerStatusChanged(EventArgs e)
	{
		if (InactiveModsListView.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
		{
			if (updatingInactiveViewSelection == null)
			{
				updatingInactiveViewSelection = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(25), () =>
				{
					UpdateViewSelection(ViewModel.InactiveMods, InactiveModsListView);
					updatingInactiveViewSelection.Dispose();
					updatingInactiveViewSelection = null;
				});
			}

		}
	}

	private void ForceLoadedModsListView_ItemContainerStatusChanged(EventArgs e)
	{
		if (ForceLoadedModsListView.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
		{
			if (updatingForcedViewSelection == null)
			{
				updatingForcedViewSelection = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(25), () =>
				{
					UpdateViewSelection(ViewModel.ForceLoadedMods, ForceLoadedModsListView);
					updatingForcedViewSelection.Dispose();
					updatingForcedViewSelection = null;
				});
			}

		}
	}

	private IDisposable _updateScroll;

	private void MoveSelectedMods()
	{
		if (ListHasFocus(ActiveModsListView))
		{
			var selectedMods = ViewModel.ActiveMods.Where(x => x.IsSelected).ToList();

			if (selectedMods.Count <= 0) return;

			var selectedMod = selectedMods.FirstOrDefault();
			var nextSelectedIndex = ViewModel.ActiveMods.IndexOf(selectedMod);

			var scrollTargetIndex = InactiveModsListView.SelectedIndex;
			var dropInfo = new ManualDropInfo(selectedMods, InactiveModsListView.SelectedIndex, InactiveModsListView, ViewModel.InactiveMods, ViewModel.ActiveMods);
			InactiveModsListView.UnselectAll();
			ViewModel.DropHandler.Drop(dropInfo);
			string countSuffix = selectedMods.Count > 1 ? "mods" : "mod";
			string text = $"Moved {selectedMods.Count} {countSuffix} to the inactive mods list.";
			if (Services.ScreenReader.IsScreenReaderActive()) Services.ScreenReader.Speak(text);
			ViewModel.ShowAlert(text, AlertType.Info, 10);
			ViewModel.CanMoveSelectedMods = false;

			if (ViewModel.Settings.ShiftListFocusOnSwap)
			{
				InactiveModsListView.Focus();
			}

			_updateScroll?.Dispose();

			_updateScroll = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), _ =>
			{
				//InactiveModsListView.UpdateLayout();
				if (scrollTargetIndex <= 0)
				{
					ScrollToTop(InactiveModsListView);
				}
				else if (scrollTargetIndex >= InactiveModsListView.Items.Count)
				{
					ScrollToBottom(InactiveModsListView);
				}
				else
				{
					ScrollToMod(InactiveModsListView, selectedMod);
					//FocusMod(InactiveModsListView, selectedMod);
				}

				if (nextSelectedIndex >= ViewModel.ActiveMods.Count)
				{
					nextSelectedIndex = ViewModel.ActiveMods.Count - 1;
				}

				ActiveModsListView.SelectedIndex = nextSelectedIndex;
				//FocusMod(ActiveModsListView, ActiveModsListView.SelectedItem);
			});
		}
		else if (ListHasFocus(InactiveModsListView))
		{
			var selectedMods = ViewModel.InactiveMods.Where(x => x.IsSelected).ToList();

			if (selectedMods.Count <= 0) return;

			var selectedMod = selectedMods.FirstOrDefault();
			var nextSelectedIndex = ViewModel.InactiveMods.IndexOf(selectedMod);

			var scrollTargetIndex = ActiveModsListView.SelectedIndex;
			var dropInfo = new ManualDropInfo(selectedMods, ActiveModsListView.SelectedIndex, ActiveModsListView, ViewModel.ActiveMods, ViewModel.InactiveMods);
			ActiveModsListView.UnselectAll();
			ViewModel.DropHandler.Drop(dropInfo);

			string countSuffix = selectedMods.Count > 1 ? "mods" : "mod";
			string text = $"Moved {selectedMods.Count} {countSuffix} to the active mods list.";
			if (Services.ScreenReader.IsScreenReaderActive()) Services.ScreenReader.Speak(text);
			ViewModel.ShowAlert(text, AlertType.Info, 10);
			ViewModel.CanMoveSelectedMods = false;

			if (ViewModel.Settings.ShiftListFocusOnSwap)
			{
				ActiveModsListView.Focus();
			}

			_updateScroll?.Dispose();

			_updateScroll = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), _ =>
			{
				//ActiveModsListView.UpdateLayout();
				if (scrollTargetIndex <= 0)
				{
					ScrollToTop(ActiveModsListView);
				}
				else if (scrollTargetIndex >= ActiveModsListView.Items.Count)
				{
					ScrollToBottom(ActiveModsListView);
				}
				else
				{
					ScrollToMod(ActiveModsListView, selectedMod);
					//FocusMod(ActiveModsListView, selectedMod);
				}

				if (nextSelectedIndex >= ViewModel.InactiveMods.Count)
				{
					nextSelectedIndex = ViewModel.InactiveMods.Count - 1;
				}

				InactiveModsListView.SelectedIndex = nextSelectedIndex;
				//FocusMod(InactiveModsListView, InactiveModsListView.SelectedItem);
			});
		}
	}

	public void FocusInitialActiveSelected()
	{
		if (ViewModel.ActiveSelected <= 0)
		{
			ActiveModsListView.SelectedIndex = 0;
		}
		try
		{
			ListViewItem item = (ListViewItem)ActiveModsListView.ItemContainerGenerator.ContainerFromItem(ActiveModsListView.SelectedItem);
			if (item != null)
			{
				Keyboard.Focus(item);
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error focusing selected item:{ex}");
		}
	}

	public bool FocusMod(ModListView modListView, object mod)
	{
		if (modListView.ItemContainerGenerator.ContainerFromItem(mod) is ListViewItem item)
		{
			FocusManager.SetFocusedElement(modListView, item);
			//item.BringIntoView();
			return true;
		}
		return false;
	}

	public void ScrollToMod(ModListView modListView, DivinityModData mod)
	{
		var index = modListView.Items.IndexOf(mod);
		if (index > -1)
		{
			modListView.UpdateLayout();
			modListView.ScrollIntoView(modListView.Items[index]);
		}
	}

	public void ScrollToTop(ModListView modListView)
	{
		if (modListView.GetVisualDescendent<ScrollViewer>() is ScrollViewer scrollViewer)
		{
			scrollViewer.ScrollToTop();
		}
	}

	public void ScrollToBottom(ModListView modListView)
	{
		if (modListView.GetVisualDescendent<ScrollViewer>() is ScrollViewer scrollViewer)
		{
			scrollViewer.ScrollToBottom();
		}
	}

	public HorizontalModLayout()
	{
		InitializeComponent();
		CaptureModListColumnWidths();

		ModDetailsToggleButton.Checked += ModDetailsToggleButton_Checked;
		ModDetailsToggleButton.Unchecked += ModDetailsToggleButton_Unchecked;
		ModDetailsGridSplitter.DragCompleted += ModDetailsGridSplitter_DragCompleted;

		SetupListView(ActiveModsListView);
		SetupListView(InactiveModsListView);

		bool setInitialFocus = true;

		this.WhenActivated(d =>
		{
			if (ViewModel != null)
			{
				d(this.Events().KeyUp.Select(e => e.Key != Key.System ? e.Key : e.SystemKey).Subscribe(ViewModel.OnKeyUp));
				d(this.Events().KeyDown.Select(e => e.Key != Key.System ? e.Key : e.SystemKey).Subscribe(key =>
				{
					ViewModel.OnKeyDown(key);
					HorizontalModLayout_KeyDown(key);
				}));
				d(this.Events().LostFocus.Subscribe((e) => ViewModel.CanMoveSelectedMods = true));
				d(this.Events().Loaded.ObserveOn(RxApp.MainThreadScheduler).Subscribe((e) =>
				{
					if (setInitialFocus)
					{
						this.ActiveModsListView.Focus();
						setInitialFocus = false;
					}
				}));

				d(this.ActiveModsListView.ItemContainerGenerator.Events().StatusChanged.ObserveOn(RxApp.MainThreadScheduler).Subscribe(ActiveModListView_ItemContainerStatusChanged));
				d(this.InactiveModsListView.ItemContainerGenerator.Events().StatusChanged.ObserveOn(RxApp.MainThreadScheduler).Subscribe(InactiveModListView_ItemContainerStatusChanged));
				d(this.ForceLoadedModsListView.ItemContainerGenerator.Events().StatusChanged.ObserveOn(RxApp.MainThreadScheduler).Subscribe(ForceLoadedModsListView_ItemContainerStatusChanged));

				d(Observable.FromEventPattern<SelectionChangedEventArgs>(ActiveModsListView, "SelectionChanged")
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe((e) =>
				{
					UpdateIsSelected(e.EventArgs, ViewModel.ActiveMods);
					UpdateModDetailsSelection(e.EventArgs);
				}));

				d(Observable.FromEventPattern<SelectionChangedEventArgs>(InactiveModsListView, "SelectionChanged")
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe((e) =>
				{
					UpdateIsSelected(e.EventArgs, ViewModel.InactiveMods);
					UpdateModDetailsSelection(e.EventArgs);
				}));

				d(Observable.FromEventPattern<SelectionChangedEventArgs>(ForceLoadedModsListView, "SelectionChanged")
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe((e) =>
				{
					UpdateIsSelected(e.EventArgs, ViewModel.ForceLoadedMods);
					UpdateModDetailsSelection(e.EventArgs);
				}));

				d(this.ViewModel.WhenAnyValue(x => x.OrderJustLoaded).ObserveOn(RxApp.MainThreadScheduler).Subscribe((b) =>
				{
					if (b)
					{
						this.AutoSizeNameColumn_ActiveMods();
						this.AutoSizeNameColumn_InactiveMods();
					}
				}));

				ViewModel.Layout = this;
				ApplyModListColumnVisibility();

				d(this.OneWayBind(ViewModel, vm => vm.ActiveMods, v => v.ActiveModsListView.ItemsSource));
				d(this.OneWayBind(ViewModel, vm => vm.InactiveMods, v => v.InactiveModsListView.ItemsSource));
				d(this.OneWayBind(ViewModel, vm => vm.ForceLoadedMods, v => v.ForceLoadedModsListView.ItemsSource));

				d(this.OneWayBind(ViewModel, vm => vm.HasForceLoadedMods, v => v.ForceLoadedModsListView.Visibility, BoolToVisibilityConverter.FromBool));
				d(this.OneWayBind(ViewModel, vm => vm.HasForceLoadedMods, v => v.ActiveModListViewGridSplitter.Visibility, BoolToVisibilityConverter.FromBool));

				d(this.Bind(ViewModel, vm => vm.ActiveModFilterText, v => v.ActiveModsFilterTextBox.Text));
				d(this.Bind(ViewModel, vm => vm.InactiveModFilterText, v => v.InactiveModsFilterTextBox.Text));

				d(this.OneWayBind(ViewModel, vm => vm.ActiveModsFilterResultText, v => v.ActiveModsFilterResultText.Text));
				d(this.OneWayBind(ViewModel, vm => vm.InactiveModsFilterResultText, v => v.InactiveModsFilterResultText.Text));
				d(this.OneWayBind(ViewModel, vm => vm.TotalActiveModsHidden, v => v.ActiveModsFilterResultText.Visibility, IntToVisibilityConverter.FromInt));
				d(this.OneWayBind(ViewModel, vm => vm.TotalInactiveModsHidden, v => v.InactiveModsFilterResultText.Visibility, IntToVisibilityConverter.FromInt));

				d(this.OneWayBind(ViewModel, vm => vm.ActiveSelectedText, v => v.ActiveSelectedText.Text));
				d(this.OneWayBind(ViewModel, vm => vm.ActiveSelected, v => v.ActiveSelectedText.Visibility, IntToVisibilityConverter.FromInt));
				d(this.OneWayBind(ViewModel, vm => vm.InactiveSelectedText, v => v.InactiveSelectedText.Text));
				d(this.OneWayBind(ViewModel, vm => vm.InactiveSelected, v => v.InactiveSelectedText.Visibility, IntToVisibilityConverter.FromInt));

				var gridLengthConverter = new GridLengthConverter();
				var zeroHeight = (GridLength)gridLengthConverter.ConvertFrom(0);
				var forceModsHeight = (GridLength)gridLengthConverter.ConvertFrom("2*");

				d(ViewModel.WhenAnyValue(x => x.HasForceLoadedMods).ObserveOn(RxApp.MainThreadScheduler).Subscribe((b) =>
				{
					foreach (var row in this.ActiveModListGrid.RowDefinitions.Where(x => x.Name != "ActiveModsListRow"))
					{
						if (b)
						{
							if (row.Name == "ActiveModsListGridRow")
							{
								row.Height = GridLength.Auto;
							}
							else if (row.Name == "ActiveModsListForcedModsRow")
							{
								row.Height = forceModsHeight;
							}
						}
						else
						{
							row.Height = zeroHeight;
						}
					}
				}));

				ViewModel.Keys.MoveFocusLeft.AddAction(() =>
				{
					DivinityApp.IsKeyboardNavigating = true;
					this.ActiveModsListView.Focus();

					if (ViewModel != null)
					{
						if (ViewModel.ActiveSelected <= 0)
						{
							ActiveModsListView.SelectedIndex = 0;
						}
					}

					//InactiveModsListView.UnselectAll();
					FocusList(ActiveModsListView);
				});

				ViewModel.Keys.MoveFocusRight.AddAction(() =>
				{
					DivinityApp.IsKeyboardNavigating = true;
					InactiveModsListView.Focus();
					if (ViewModel != null)
					{
						if (ViewModel.ActiveSelected <= 0)
						{
							InactiveModsListView.SelectedIndex = 0;
						}
					}
					//ActiveModsListView.UnselectAll();
					FocusList(InactiveModsListView);
				});


				ViewModel.Keys.SwapListFocus.AddAction(() =>
				{
					if (ListHasFocus(InactiveModsListView))
					{
						DivinityApp.IsKeyboardNavigating = true;
						FocusList(ActiveModsListView);
					}
					else if (ListHasFocus(ActiveModsListView))
					{
						DivinityApp.IsKeyboardNavigating = true;
						FocusList(InactiveModsListView);
					}
				});

				//InactiveModsListView.InputBindings.Add(new InputBinding(ViewModel.MoveLeftCommand, new KeyGesture(Key.Left)));
				ViewModel.Keys.ToggleFilterFocus.AddAction(() =>
				{
					if (ListHasFocus(ActiveModsListView))
					{
						if (!this.ActiveModsFilterTextBox.IsFocused)
						{
							this.ActiveModsFilterTextBox.Focus();
						}
						else
						{
							FocusSelectedItem(ActiveModsListView);
						}
					}
					else
					{
						if (!this.InactiveModsFilterTextBox.IsFocused)
						{
							this.InactiveModsFilterTextBox.Focus();
						}
						else
						{
							FocusSelectedItem(InactiveModsListView);
						}
					}
				});

				//ActiveModsListView.InputBindings.Add(new InputBinding(ViewModel.MoveRightCommand, new KeyGesture(Key.Right)));

				d(ViewModel.WhenAnyValue(x => x.ActiveSelected).Subscribe((c) =>
				{
					if (c > 1 && DivinityApp.IsScreenReaderActive())
					{
						var peer = UIElementAutomationPeer.FromElement(this.ActiveSelectedText);
						if (peer == null)
						{
							peer = UIElementAutomationPeer.CreatePeerForElement(this.ActiveSelectedText);
						}
						peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
					}
				}));

				d(ViewModel.WhenAnyValue(x => x.InactiveSelected).Subscribe((c) =>
				{
					if (c > 1 && DivinityApp.IsScreenReaderActive())
					{
						var peer = UIElementAutomationPeer.FromElement(this.InactiveSelectedText);
						if (peer == null)
						{
							peer = UIElementAutomationPeer.CreatePeerForElement(this.InactiveSelectedText);
						}
						peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
					}
				}));
			}
			//BindingHelper.CreateCommandBinding(ViewModel.View.EditFocusActiveListMenuItem, "MoveLeftCommand", ViewModel);
		});
	}

	private void HorizontalModLayout_KeyDown(Key key)
	{
		var keyIsDown = key == ViewModel.Keys.Confirm.Key && (ViewModel.Keys.Confirm.Modifiers == ModifierKeys.None || Keyboard.Modifiers.HasFlag(ViewModel.Keys.Confirm.Modifiers));
		if (!keyIsDown && (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
		{
			if (key == Key.Right && ActiveModsListView.IsKeyboardFocusWithin)
			{
				keyIsDown = true;
			}
			else if (key == Key.Left && InactiveModsListView.IsKeyboardFocusWithin)
			{
				keyIsDown = true;
			}
		}
		if (ViewModel.CanMoveSelectedMods && keyIsDown)
		{
			DivinityApp.IsKeyboardNavigating = true;
			if (ViewModel.ActiveSelected > 0 || ViewModel.InactiveSelected > 0)
			{
				MoveSelectedMods();
			}
		}
	}

	private IEnumerable<GridView> GetModListGridViews()
	{
		if (ActiveModsListView.View is GridView activeView)
		{
			yield return activeView;
		}
		if (ForceLoadedModsListView.View is GridView forceLoadedView)
		{
			yield return forceLoadedView;
		}
		if (InactiveModsListView.View is GridView inactiveView)
		{
			yield return inactiveView;
		}
	}

	private static string GetColumnName(GridViewColumn column)
	{
		return column.Header switch
		{
			TextBlock textBlock => textBlock.Text,
			string header => header,
			_ => String.Empty
		};
	}

	private void CaptureModListColumnWidths()
	{
		foreach (var gridView in GetModListGridViews())
		{
			foreach (var column in gridView.Columns)
			{
				if (!_visibleModListColumnWidths.ContainsKey(column))
				{
					_visibleModListColumnWidths[column] = column.Width;
				}
			}
		}
	}

	private bool IsModListColumnVisible(string columnName)
	{
		if (ViewModel?.Settings == null)
		{
			return true;
		}

		return columnName switch
		{
			"Version" => ViewModel.Settings.ShowModListVersionColumn,
			"Author" => ViewModel.Settings.ShowModListAuthorColumn,
			"Last Updated" => ViewModel.Settings.ShowModListLastUpdatedColumn,
			"Last Modified" => ViewModel.Settings.ShowModListLastModifiedColumn,
			"Source" => ViewModel.Settings.ShowModListSourceColumn,
			_ => true
		};
	}

	private void SetModListColumnSetting(string columnName, bool isVisible)
	{
		if (ViewModel?.Settings == null)
		{
			return;
		}

		switch (columnName)
		{
			case "Version":
				ViewModel.Settings.ShowModListVersionColumn = isVisible;
				break;
			case "Author":
				ViewModel.Settings.ShowModListAuthorColumn = isVisible;
				break;
			case "Last Updated":
				ViewModel.Settings.ShowModListLastUpdatedColumn = isVisible;
				break;
			case "Last Modified":
				ViewModel.Settings.ShowModListLastModifiedColumn = isVisible;
				break;
			case "Source":
				ViewModel.Settings.ShowModListSourceColumn = isVisible;
				break;
		}
	}

	private static double GetDefaultColumnWidth(string columnName)
	{
		return columnName switch
		{
			"Version" => 80,
			"Author" => 100,
			"Last Updated" => 100,
			"Last Modified" => 100,
			"Source" => 90,
			_ => 100
		};
	}

	private void SetGridViewColumnVisibility(GridView gridView, string columnName, bool isVisible)
	{
		var column = gridView.Columns.FirstOrDefault(candidate => GetColumnName(candidate) == columnName);
		if (column == null)
		{
			return;
		}

		if (isVisible)
		{
			if (column.Width == 0)
			{
				column.Width = _visibleModListColumnWidths.TryGetValue(column, out var storedWidth)
					? storedWidth
					: GetDefaultColumnWidth(columnName);
			}
		}
		else
		{
			if (column.Width != 0)
			{
				_visibleModListColumnWidths[column] = column.Width;
				column.Width = 0;
			}
		}
	}

	private void ApplyModListColumnVisibility()
	{
		CaptureModListColumnWidths();
		foreach (var gridView in GetModListGridViews())
		{
			foreach (var columnName in OptionalModListColumns)
			{
				SetGridViewColumnVisibility(gridView, columnName, IsModListColumnVisible(columnName));
			}
		}
	}

	private static MenuItem CreateFixedColumnMenuItem(string header)
	{
		return new MenuItem
		{
			Header = header,
			IsCheckable = true,
			IsChecked = true,
			IsEnabled = false
		};
	}

	private void ListViewColumnHeader_RightClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is not ModListView listView || e.OriginalSource is not UIElement clickedElement)
		{
			return;
		}

		// The routed event is attached to the ListView, so row and empty-space clicks
		// also reach this handler. Only open the chooser for a real column header.
		var clickedHeader = clickedElement as GridViewColumnHeader
			?? clickedElement.FindVisualParent<GridViewColumnHeader>();
		if (clickedHeader == null)
		{
			return;
		}

		var menu = new ContextMenu
		{
			Placement = PlacementMode.MousePoint,
			PlacementTarget = listView
		};
		menu.Items.Add(new MenuItem
		{
			Header = "Visible Columns",
			FontWeight = FontWeights.SemiBold,
			IsEnabled = false
		});
		menu.Items.Add(new Separator());

		if (ReferenceEquals(listView, ActiveModsListView))
		{
			menu.Items.Add(CreateFixedColumnMenuItem("#  (load order — always shown)"));
		}
		menu.Items.Add(CreateFixedColumnMenuItem("Name  (always shown)"));

		foreach (var columnName in OptionalModListColumns)
		{
			var item = new MenuItem
			{
				Header = columnName,
				IsCheckable = true,
				IsChecked = IsModListColumnVisible(columnName)
			};
			item.Click += (_, _) =>
			{
				SetModListColumnSetting(columnName, item.IsChecked);
				ApplyModListColumnVisibility();
				ViewModel.QueueSave();
			};
			menu.Items.Add(item);
		}

		menu.Items.Add(new Separator());
		var resetItem = new MenuItem { Header = "Reset Columns" };
		resetItem.Click += (_, _) =>
		{
			foreach (var columnName in OptionalModListColumns)
			{
				SetModListColumnSetting(columnName, true);
			}
			ApplyModListColumnVisibility();
			ViewModel.QueueSave();
		};
		menu.Items.Add(resetItem);

		menu.IsOpen = true;
		e.Handled = true;
	}

	GridViewColumnHeader _lastHeaderClicked = null;
	ListSortDirection _lastDirection = ListSortDirection.Ascending;

	public GridViewColumnHeader LastSortHeader => _lastHeaderClicked;
	public ListSortDirection LastSortDirection => _lastDirection;

	private void ListView_Click(object sender, RoutedEventArgs e)
	{
		GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
		ListSortDirection direction;

		if (headerClicked != null)
		{
			if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
			{
				if (headerClicked != _lastHeaderClicked)
				{
					direction = ListSortDirection.Ascending;
				}
				else
				{
					if (_lastDirection == ListSortDirection.Ascending)
					{
						direction = ListSortDirection.Descending;
					}
					else
					{
						direction = ListSortDirection.Ascending;
					}
				}

				string header = "";

				if (headerClicked.Column.Header is TextBlock textBlock)
				{
					header = textBlock.Text;
				}
				else if (headerClicked.Column.Header is string gridHeader)
				{
					header = gridHeader;
				}

				Sort(header, direction, sender);

				_lastHeaderClicked = headerClicked;
				_lastDirection = direction;
			}
		}
	}

	public void Sort(string sortBy, ListSortDirection direction, object sender)
	{
		if (sortBy == "Version") sortBy = "Version.Version";
		if (sortBy == "#") sortBy = "Index";
		if (sortBy == "Name") sortBy = "DisplayName";
		if (sortBy == "Modes") sortBy = "Targets";
		if (sortBy == "Last Updated") sortBy = "DisplayLastUpdated";
		if (sortBy == "Last Modified") sortBy = "LastModified";
		if (sortBy == "Source") sortBy = "DisplaySource";

		try
		{
			ListView lv = sender as ListView;
			ICollectionView dataView =
			  CollectionViewSource.GetDefaultView(lv.ItemsSource);

			dataView.SortDescriptions.Clear();
			SortDescription sd = new SortDescription(sortBy, direction);
			dataView.SortDescriptions.Add(sd);
			dataView.Refresh();
		}
		catch (Exception ex)
		{
			DivinityApp.Log("Error sorting mods:");
			DivinityApp.Log(ex.ToString());
		}
	}

	public void RefreshDataView(ListView target)
	{
		var dataView = CollectionViewSource.GetDefaultView(target.ItemsSource);
		if (dataView != null)
		{
			dataView.Refresh();
		}
	}

	private int _FontSizeMeasurePadding = 48;

	public void AutoSizeNameColumn_ActiveMods()
	{
		if (ViewModel == null || ActiveModsListView.UserResizedColumns) return;
		var count = Math.Max(ViewModel.ActiveMods.Count, ViewModel.ForceLoadedMods.Count);
		if (count > 0 && ActiveModsListView.View is GridView gridView && gridView.Columns.Count >= 2)
		{
			RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), () =>
			{
				count = Math.Max(ViewModel.ActiveMods.Count, ViewModel.ForceLoadedMods.Count);
				if (count > 0)
				{
					//var longestName = ViewModel.ActiveMods.OrderByDescending(m => m.Name.Length).FirstOrDefault()?.Name ?? "";
					//var longestOverrideName = ViewModel.ForceLoadedMods.OrderByDescending(m => m.Name.Length).FirstOrDefault()?.Name ?? "";
					var longestName = "";
					var iconPadding = 0;

					foreach(var mod in ViewModel.Mods)
					{
						if(mod.IsActive || mod.IsForceLoaded)
						{
							if(!String.IsNullOrEmpty(mod.Name) && mod.Name.Length > longestName.Length)
							{
								longestName = mod.Name;
							}
							var modIcons = 0;
							if(mod.OsirisStatusVisibility == Visibility.Visible)
							{
								modIcons++;
							}
							if(mod.ExtenderStatusVisibility == Visibility.Visible)
							{
								modIcons++;
							}
							if(mod.ToolkitIconVisibility == Visibility.Visible)
							{
								modIcons++;
							}
							if(mod.HasInvalidUUIDVisibility == Visibility.Visible)
							{
								modIcons++;
							}
							if(mod.MissingDependencyIconVisibility == Visibility.Visible)
							{
								modIcons++;
							}
							if(modIcons > iconPadding)
							{
								iconPadding = modIcons;
							}
						}
					}

					if (iconPadding > 0) iconPadding *= 16;

					if (!String.IsNullOrEmpty(longestName))
					{
						//DivinityApp.LogMessage($"Autosizing active mods grid for name {longestName}");
						var targetWidth = ElementHelper.MeasureText(ActiveModsListView, longestName,
							ActiveModsListView.FontFamily,
							ActiveModsListView.FontStyle,
							ActiveModsListView.FontWeight,
							ActiveModsListView.FontStretch,
							ActiveModsListView.FontSize).Width + _FontSizeMeasurePadding + iconPadding;
						if (Math.Abs(gridView.Columns[1].Width - targetWidth) >= 30)
						{
							ActiveModsListView.Resizing = true;
							gridView.Columns[1].Width = targetWidth;
						}
					}
				}
			});
		}
	}

	public void AutoSizeNameColumn_InactiveMods()
	{
		if (ViewModel == null || InactiveModsListView.UserResizedColumns) return;
		if (ViewModel.InactiveMods.Count > 0 && InactiveModsListView.View is GridView gridView && gridView.Columns.Count >= 2)
		{
			var longestName = "";
			var iconPadding = 0;

			foreach (var mod in ViewModel.InactiveMods)
			{
				if (!String.IsNullOrEmpty(mod.Name) && mod.Name.Length > longestName.Length)
				{
					longestName = mod.Name;
				}
				var modIcons = 0;
				if (mod.OsirisStatusVisibility == Visibility.Visible)
				{
					modIcons++;
				}
				if (mod.ExtenderStatusVisibility == Visibility.Visible)
				{
					modIcons++;
				}
				if (mod.ToolkitIconVisibility == Visibility.Visible)
				{
					modIcons++;
				}
				if (mod.HasInvalidUUIDVisibility == Visibility.Visible)
				{
					modIcons++;
				}
				if (mod.MissingDependencyIconVisibility == Visibility.Visible)
				{
					modIcons++;
				}
				if (modIcons > iconPadding)
				{
					iconPadding = modIcons;
				}
			}

			if (iconPadding > 0) iconPadding *= 16;

			if (!String.IsNullOrEmpty(longestName))
			{
				InactiveModsListView.Resizing = true;
				//DivinityApp.LogMessage($"Autosizing inactive mods grid for name {longestName}");
				gridView.Columns[0].Width = ElementHelper.MeasureText(InactiveModsListView, longestName,
					InactiveModsListView.FontFamily,
					InactiveModsListView.FontStyle,
					InactiveModsListView.FontWeight,
					InactiveModsListView.FontStretch,
					InactiveModsListView.FontSize).Width + _FontSizeMeasurePadding + iconPadding;
			}
		}
	}

	private void ListViewItem_ModifySelection(object sender, MouseButtonEventArgs e)
	{
		//Fix for when virtualization is enabled, and selected entries outside the view don't get deselected
		if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
		{
			if (sender is ListViewItem listViewitem)
			{
				if (listViewitem.DataContext is DivinityModData modData)
				{
					if (modData.IsActive)
					{
						foreach (var x in ViewModel.ActiveMods)
						{
							if (x != modData && x.IsSelected) x.IsSelected = false;
						}
					}
					else
					{
						foreach (var x in ViewModel.InactiveMods)
						{
							if (x != modData && x.IsSelected) x.IsSelected = false;
						}
					}
				}
			}
		}
	}
}
